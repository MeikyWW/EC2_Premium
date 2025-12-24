using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using MEC;
using System;
using UnityEngine.Events;
using System.Linq;
using Sirenix.OdinInspector;

public class EnemyHealth : MonoBehaviour, IStatusEffect
{
    public static event Action<GameObject, float, float> OnArmorValueChanged;
    public event Action OnHelmetBroken;

    [FoldoutGroup("Custom Die")]
    public bool customDie; //panggil sekali aja 
    [FoldoutGroup("Custom Die")]
    public bool myHealthIsMyShield;

    [FoldoutGroup("Helmet & Armor")]
    [SuffixLabel("%", Overlay = true)]
    public float armorWeakness = 0; //extra damage to armor
    [FoldoutGroup("Helmet & Armor")]
    [SerializeField]
    int shieldCount;
    [FoldoutGroup("Helmet & Armor")]
    public bool playEtcOnBlock;
    [FoldoutGroup("Helmet & Armor")]
    public int playEtcIndex;
    [FoldoutGroup("Helmet & Armor")]
    public GameObject helmetToSpawn, disabledObj;
    [FoldoutGroup("Helmet & Armor")]
    public Transform spawnPos;
    [FoldoutGroup("Helmet & Armor")]
    public float force = 3f;
    [FoldoutGroup("Helmet & Armor")]
    private bool isHelmetOn;
    [FoldoutGroup("Helmet & Armor")]
    public bool enableHelmetReturn;
    [FoldoutGroup("Helmet & Armor")]
    public int returnShield;

    public int ShieldCount
    {
        get => shieldCount;
        set
        {
            shieldCount = value;
            if (shieldCount == 1)
                OnHelmetBroken?.Invoke();
        }
    }

    [FoldoutGroup("Extra")]
    public List<StatusEffects> statusEffectToRemove;
    [FoldoutGroup("Extra")]
    public float extraDmgIncrease;

    [HideInInspector]
    public HpBarFollow hpBar;

    GameManager manager;
    Animator anim;
    EnemyStatus status;
    EnemyAI ai;
    public bool useArmorAsHealth = false;
    [ReadOnly] public float armor, maxArmor;
    public float Armor
    {
        get
        {
            return armor;
        }
        set
        {
            armor = value;
            if (useArmorAsHealth)
                OnArmorValueChanged(gameObject, armor, maxArmor);
        }
    }
    [ShowInInspector, BoxGroup("Main"), PropertyOrder(-1)]
    float hp, maxHp;
    bool disappearing = false;
    bool superArmor, stunned;
    [HideInInspector] public bool dead;
    [HideInInspector]
    public float time;
    float count;
    CharacterController _controller;
    StatusEffectManager SE;
    public float MaxHP => maxHp;

    //BREAK STATE
    public bool temporaryUnbreakableShield;
    public bool ragingAffectShield;
    public bool shieldBreak; //true if armor is reduced to 0 and enters break state.
    public float breakTime;

    SkinnedMeshRenderer[] allRenderers;
    Material[] allMats;

    public Material[] Mats
    {
        get => allMats;
    }
    Material flashMat;
    bool inited;

    public float armorDebuff;
    public float curse_armorDebuff;

    public float FinalArmorDebuff
    {
        get
        {
            float temp = 0f;
            temp += armorDebuff;
            temp += curse_armorDebuff;

            return temp;
        }
    }

    private void OnEnable()
    {
        MenuOptions.OnChangeCameraSetting += CameraSettingChanged;
    }

    private void OnDisable()
    {
        MenuOptions.OnChangeCameraSetting -= CameraSettingChanged;

        if (!inited) return;
        if (!status.hideOverheadHpBar)
        {
            if (hpBar) hpBar.DestroyHpBar();
        }
    }

    void Start()
    {
        Init();
    }
    void Init()
    {
        if (inited) return;

        manager = GameManager.instance;
        status = GetComponent<EnemyStatus>();
        ai = GetComponent<EnemyAI>();
        anim = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        SE = StatusEffectManager.instance;

        //get all renderer
        allRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>();

        //get all material
        allMats = new Material[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
            allMats[i] = allRenderers[i].material;

        //get flash material
        flashMat = manager.enemyMaterialOnHit;

        if (ShieldCount > 0) isHelmetOn = true;

        hpBreach = 1;

        inited = true;
    }

    private void Update()
    {
        UpdateBreakState();

        if (!manager.userData.settings.freelookCamera) return;
        if (status.hideOverheadHpBar) return;

        if (allRenderers != null && allRenderers.Length > 0)
        {
            if (allRenderers[0].isVisible)
            {
                if (Vector3.Distance(transform.position, manager.ActiveHero.transform.position) < manager.combatCam.maxEnemiesDistance)
                {
                    if (hpBar != null && !hpBar.gameObject.activeSelf) hpBar.gameObject.SetActive(true);
                }
                else
                {
                    if (hpBar != null && hpBar.gameObject.activeSelf) hpBar.gameObject.SetActive(false);
                }
            }

            else
            {
                if (hpBar != null && hpBar.gameObject.activeSelf) hpBar.gameObject.SetActive(false);
            }
        }

        frostbiteCd -= Time.deltaTime;
    }

    void CameraSettingChanged()
    {
        if (!manager.userData.settings.freelookCamera)
        {
            if (hpBar != null && !hpBar.gameObject.activeSelf) hpBar.gameObject.SetActive(true);
        }
    }
    void UpdateBreakState()
    {
        if (!shieldBreak) return;
        if (ai.GetFrozenState()) return;
        breakTime += Time.deltaTime;
        if (breakTime >= status.riseTime)
        {
            RefreshShield();
        }
    }

    CoroutineHandle refreshShieldHandler;
    public void RefreshShield()
    {
        breakTime = 0;

        if (ai.waitRiseAnimation)
            refreshShieldHandler = Timing.RunCoroutine(DelayShieldReset().CancelWith(gameObject), EC2Constant.STATE_DEPENDENT);
        else
        {
            shieldBreak = false;
            Armor = maxArmor;
            UpdateArmorBar();
        }

        ai.EndBreak();
    }

    public void ForceRefreshShield()
    {
        if (refreshShieldHandler != null)
            if (refreshShieldHandler.IsRunning)
                Timing.KillCoroutines(refreshShieldHandler);

        breakTime = 0;
        shieldBreak = false;
        Armor = maxArmor;
        UpdateArmorBar();
        ai.EndBreak();
    }

    IEnumerator<float> DelayShieldReset()
    {
        yield return Timing.WaitForSeconds(2);

        shieldBreak = false;
        Armor = maxArmor;
        UpdateArmorBar();
    }

    public IEnumerator<float> EnableAntiShieldDestroy(float duration)
    {
        temporaryUnbreakableShield = true;
        yield return Timing.WaitForSeconds(duration);
        temporaryUnbreakableShield = false;
    }

    public void ForceHealth(float health)
    {
        hp = health;
        UpdateHpBar();
    }
    public void SetHP(float health, float shield)
    {
        SetHP(health, health, shield);
    }
    public void SetHP(float maxHealth, float currentHealth, float shield)
    {
        Init();

        superArmor = status.armor > 0 || status.knockbackRes;

        if (status.armor > 0)
        {
            maxHealth *= 2.0f;
            currentHealth *= 2.0f;
        }
        hp = currentHealth;
        maxHp = maxHealth;
        Armor = maxArmor = shield;
        UpdateArmorBar();
    }
    public void Heal(float amount, bool isPercent)
    {
        float trueAmount = isPercent ? (amount / 100 * maxHp) : amount;
        hp += trueAmount;
        if (hp > maxHp) hp = maxHp;
        UpdateHpBar();
    }
    public void UpdateHpBar()
    {
        if (status.useBossHpBar) status.hpBarManager.UpdateBossHpBar(HpPercent());
        if (!status.hideOverheadHpBar) hpBar.UpdateHpBar(HpPercent());
    }
    public void UpdateArmorBar()
    {
        if (status.useBossHpBar) status.hpBarManager.UpdateBossArmorBar(ArmorPercent());
        if (!status.hideOverheadHpBar) hpBar.UpdateArmorBar(ArmorPercent());
    }
    public void MegaArmor(bool on)
    {
        megaArmor = on;
    }
    //Taking Damage

    [BoxGroup("Special"), PropertyOrder(-1)]
    public float cot_monolithProtection;
    [BoxGroup("Special"), PropertyOrder(-1)]
    public float cot_revival_threshold;
    bool evadeSuccess;
    bool megaArmor;

    public void TakeDamage(Transform source, DamageRequest dmgInput, out DamageResult result)
    {
        Init();

        result = new DamageResult();
        result.isDead = false;
        result.isMiss = false;

        if (manager.STATE == GameState.PAUSE) return;

        //mega armor state
        if (megaArmor) return;

        //Using Helmet (or any protection items)
        //Nullifies any damage taken
        if (ShieldCount > 1)
        {
            ShieldCount--;
            HudManager.instance.PopGuard(transform);
            if (playEtcOnBlock) status.PlayEtc(playEtcIndex);
            onHelmetHit?.Invoke();
            return;
        }

        if (helmetToSpawn && isHelmetOn) //if using helmet type AI
        {
            if (disabledObj) disabledObj.SetActive(false);

            if (spawnPos)
            {
                GameObject g = Instantiate(helmetToSpawn, spawnPos.position, Quaternion.identity);
                g.GetComponent<Rigidbody>().AddForce(Vector3.up * force, ForceMode.Impulse);
                g.GetComponent<Rigidbody>().AddTorque(Vector3.right * 500, ForceMode.Impulse);
            }

            //Destroy(g, 3);
            isHelmetOn = false;
        }

        DamageRequest request = new DamageRequest(dmgInput);

        //extra dmg % (0-1)
        float extraDmg = extraDmgIncrease * request.damage;

        float damage = request.damage + extraDmg;
        bool critical = request.isCritical;
        float knockback = request.knockback;
        float s_pierce = request.pierce;
        float s_break = request._break;
        float s_accuracy = request.accuracy;

        float finalArmorDebuff = request.ignoreDefDown ? 0 : FinalArmorDebuff;
        float armorDefense = status.armorDefense - finalArmorDebuff;

        if (!request.unmissable)
        {
            float finalEvasion = status.FinalEvasion - s_accuracy;

            if (finalEvasion > 0f)
            {
                evadeSuccess = UnityEngine.Random.Range(0, 100) < finalEvasion;
                if (evadeSuccess)
                {
                    HudManager.instance.PopMiss(transform);
                    if (critical)
                    {
                        critical = false;
                        request.isCritical = false;
                        // base damage 100 
                        var pctCdmg = (request.cdmg / 100f) + 1f; // 1.5f
                        damage = request.damage / pctCdmg; // 150 / 1.5f = 100 is source
                    }
                    damage *= 0.75f;
                }
            }
        }

        if (shieldBreak)
        {
            armorDefense -= 50;
            if (ragingAffectShield) armorDefense += Mathf.Abs(0.5f * armorDefense);
        }

        float dmgReduction = request.pureDamage ? 0 : armorDefense / 100 * damage;
        float finalDmg = Mathf.RoundToInt(damage - dmgReduction);

        //tribe buff
        if (finalDmgReduction_tribe > 0)
            finalDmg -= finalDmg * (finalDmgReduction_tribe / 100);

        //monolith protection
        if (cot_monolithProtection > 0 && shieldBreak == false)
            finalDmg -= finalDmg * (cot_monolithProtection / 100);

        if (finalDmg <= 0) finalDmg = 0;

        request.damage = finalDmg;


        //Debug.Log("source : " + request.damageSourceKey + request.hero);
        Hero sourceHero = request.hero;

        if (sourceHero == Hero.None)
            try { sourceHero = source.GetComponent<HeroStatus>().heroReference.hero; } catch { }

        if (!request.notDealingDamage)
        {
            PopupDamage(transform, request);//, finalDmg, critical);

            //register to dps meter
            status.gm.dpsMeter.AddDamage(new DamageRequest(request) { damage = finalDmg, hero = sourceHero });

            if (myHealthIsMyShield)
            {
                if (Armor > 0)
                {
                    DamageArmor(s_break, true, true);
                }
            }
            else
            {
                if (hp > 0)
                {
                    hp -= finalDmg;
                    Timing.RunCoroutine(HitFlash().CancelWith(gameObject));

                    if (hp <= 0)
                    {
                        Die();
                        result.isDead = true;
                    }
                    else
                    {
                        if (!superArmor)
                        {
                            //ignore if super armor
                            if (!ai.GetFrozenState())
                            {
                                status.PlayHit();
                                Knockback(source, knockback);
                                anim.SetTrigger("hit");
                            }
                        }

                        else
                        {
                            if (shieldBreak && status.disableKnockbackResOnBreak)
                            {
                                if (!ai.GetFrozenState())
                                {
                                    status.PlayHit();
                                    Knockback(source, knockback);
                                    anim.SetTrigger("hit");
                                }
                            }
                        }

                        if (!shieldBreak && !temporaryUnbreakableShield)
                        {
                            DamageArmor(s_break, true);
                        }

                        ai.Hit();
                        UpdateHpBar();
                        CheckHealthBreach();
                    }
                }

            }

            if (source.GetComponent<HeroHealth>() != null && source.GetComponent<HeroStatus>() != null)
            {
                var sourceHeroStatus = source.GetComponent<HeroStatus>();
                var sourceHeroHealth = source.GetComponent<HeroHealth>();
                var recalculatedLifesteal = sourceHeroStatus.setEffect.lifesteal / 100;
                if (finalDmg > sourceHeroStatus.GetModifiedFinalDamage())
                    recalculatedLifesteal = sourceHeroStatus.GetModifiedFinalDamage() / finalDmg * sourceHeroStatus.setEffect.lifesteal / 100;

                sourceHeroHealth.RestoreHp(finalDmg * recalculatedLifesteal, false);

            }
            status.TakeDamage(finalDmg, HpPercent());

            onHit?.Invoke(finalDmg);
            onHitRequest?.Invoke(request);

            if (cot_revival_threshold > 0)
                Timing.RunCoroutine(CheckRevival().CancelWith(gameObject), EC2Constant.STATE_DEPENDENT);
        }
    }
    private IEnumerator<float> CheckRevival()
    {
        yield return Timing.WaitForSeconds(0.1f);
        if (HpPercent() <= cot_revival_threshold / 100f && dead == false)
        {
            Heal(100, true);
        }
    }
    public void SetHpTo(float percent, float damageNumber = 0)
    {
        if (damageNumber > 0)
        {
            PopupDamage(transform, new DamageRequest() { damage = damageNumber });
        }

        hp = maxHp * percent / 100f;
        if (hp <= 0) hp = 1;

        UpdateHpBar();
    }
    public void DamageArmor(float dmg, bool percentage)
    {
        DamageArmor(dmg, percentage, false);
    }
    public void DamageArmor(float dmg, bool percentage, bool shieldAsHP)
    {
        if (status.armor <= 0) return;
        if (Armor <= 0) return;

        float armorDmg = percentage ? dmg / 100 * status.armor : dmg;
        float finalDmg = armorDmg + (armorWeakness / 100 * armorDmg);

        Armor -= finalDmg;

        UpdateArmorBar();
        if (Armor <= 0)
        {
            if (!shieldAsHP)
            {
                shieldBreak = true;
                ai.StartBreak(status.riseTime);
            }
            else
            {
                Die();
            }
        }
    }
    public float disappearDelay = 2f;
    public void Die()
    {
        ProceedDie(true);
    }
    public virtual void ProceedDie(bool animateDie)
    {
        //Set Die
        if (customDie)
        {
            onCustomDie?.Invoke();
            return;
        }

        if (dead) return;

        dead = true;
        status.Die();
        ai.Die();
        onDie?.Invoke();
        RemoveAllStatusEffects();
        if (!status.hideOverheadHpBar)
            if (hpBar) hpBar.DestroyHpBar();
        GetComponent<Collider>().enabled = false;
        gameObject.layer = 11;
        gameObject.tag = "Dead";
        if (animateDie) anim.SetTrigger("die");
        Invoke("Disappear", disappearDelay);
    }

    [FoldoutGroup("Events")]
    public float hpBreach = 1;
    public void CheckHealthBreach()
    {
        //75% breach
        if (HpPercent() <= 0.75 && hpBreach > 0.75f)
        {
            //print("75% breached");
            hpBreach = 0.75f;
            on75Health?.Invoke();
            return;
        }

        //66% breach
        if (HpPercent() <= 0.66 && hpBreach > 0.66f)
        {
            //print("66% breached");
            hpBreach = 0.66f;
            on66Health?.Invoke();
            return;
        }

        //50% breach
        if (HpPercent() <= 0.50 && hpBreach > 0.50f)
        {
            // print("50% breached");
            hpBreach = 0.50f;
            on50Health?.Invoke();
            return;
        }

        //40% breach
        if (HpPercent() <= 0.40 && hpBreach > 0.40f)
        {
            //print("40% breached");
            hpBreach = 0.40f;
            on40Health?.Invoke();
            return;
        }

        //33% breach
        if (HpPercent() <= 0.33 && hpBreach > 0.33f)
        {
            //print("50% breached");
            hpBreach = 0.33f;
            on33Health?.Invoke();
            return;
        }

        //25% breach
        if (HpPercent() <= 0.25 && hpBreach > 0.25f)
        {
            //print("25% breached");
            hpBreach = 0.25f;
            on25Health?.Invoke();
            return;
        }
    }

    void EnableCollider()
    {
        GetComponent<Collider>().enabled = true;
    }
    void Knockback(Transform source, float amount)
    {
        if (status.knockbackRes) return;
        if (amount == 0) return;
        transform.LookAt(new Vector3(source.position.x, transform.position.y, source.position.z));
        //if (WallBehind(amount)) return;

        _controller.Move(transform.forward * -amount);
    }

    bool WallBehind(float amt)
    {
        if (Physics.Raycast(transform.position, -transform.forward, amt * 2, 1 << 15 | 1 << 19))
            return true;
        else return false;
    }
    public float HpPercent()
    {
        return hp / maxHp;
    }
    public float ArmorPercent()
    {
        if (status.armorModifier > 0)
            return Armor / maxArmor;
        else
        {
            return 0;
        }
    }
    public bool IsSuperArmor()
    {
        return superArmor;
    }

    void Disappear()
    {
        try
        {
            GetComponent<OutlineSetter>().SetOutline(false);
        }
        catch { }

        DOTween.To(FadeMaterials, 1, 0, 2).OnComplete(() => Destroy(gameObject));
    }
    public void FadeMaterials(float alpha)
    {
        foreach (Material m in allMats)
            m.SetFloat("_Transparency", alpha);
    }

    public void RendererVisible(bool state)
    {
        for (int i = 0; i < allRenderers.Length; i++)
            allRenderers[i].enabled = state;
    }

    IEnumerator<float> HitFlash()
    {
        for (int i = 0; i < allRenderers.Length; i++)
            allRenderers[i].material = flashMat;

        yield return Timing.WaitForSeconds(0.1f);

        for (int i = 0; i < allRenderers.Length; i++)
            allRenderers[i].material = allMats[i];
    }
    public void SetInvincible(bool on)
    {
        gameObject.layer = on ? 11 : 10;
    }

    [SerializeField, FoldoutGroup("Extra")]
    bool ignoreStatusEffect;
    public void ResistAllStatusEffect(bool on)
    {
        ignoreStatusEffect = on;
    }


    //===== STATUS EFFECT INTERFACE =====//
    public Dictionary<StatusEffects, EC2StatusEffect> statusEffects = new Dictionary<StatusEffects, EC2StatusEffect>();
    public void SetStatusEffect(DamageRequest damageRequest)
    {
        //Debug.Log("status effect : " + damageRequest.statusEffect + " to " + transform.name + ")");
        if (ignoreStatusEffect)
        {
            return;
        }

        if (evadeSuccess)
        {
            evadeSuccess = false;
            return;
        }

        if (damageRequest.statusEffect == StatusEffects.chill)
        {
            //cant apply if alrd frozen
            if (statusEffects.ContainsKey(StatusEffects.freeze))
                return;
        }

        if (statusEffects.ContainsKey(damageRequest.statusEffect))
        {
            //udah ada dalam list. update attribut aja
            statusEffects[damageRequest.statusEffect].SetAttribute(transform, damageRequest);
        }
        else
        {
            //spawn objek status baru
            EC2StatusEffect effectObj = StatusEffectManager.instance.GetObject(damageRequest.statusEffect, transform, false).GetComponent<EC2StatusEffect>();
            effectObj.SetAttribute(transform, damageRequest);
            statusEffects.Add(damageRequest.statusEffect, effectObj);
        }

        //apply effect
        var stack = statusEffects[damageRequest.statusEffect].stack;
        switch (damageRequest.statusEffect)
        {
            case StatusEffects.poison:
                ai.ApplyPoison(0.25f);
                break;
            case StatusEffects.freeze:
                ai.Freeze();
                break;
            case StatusEffects.armorDown:
                ai.DebuffArmor(damageRequest.statusEffectDamage); // val lowing armor
                break;
            case StatusEffects.damageDown:
                ai.DebuffDamage(damageRequest.statusEffectDamage); // val lowing damag
                break;
            case StatusEffects.critResDown:
                ai.DebuffCritRes(damageRequest.statusEffectDamage); // val lowing damag
                break;
            case StatusEffects.cursed:
                ai.DebuffCurse(damageRequest.statusEffectAttributes[0] * stack, damageRequest.statusEffectAttributes[1] * stack); // val lowing damag
                break;
            case StatusEffects.tremor:
                ai.DebuffTremor(damageRequest.statusEffectAttributes[0], damageRequest.statusEffectAttributes[1]); // val lowing damag
                break;
            case StatusEffects.mysticFlame:
                ai.Ignite();
                break;
            case StatusEffects.focused:
                damageRequest.hero = Hero.Louisa;
                ai.OnLouisaFocus(damageRequest.statusEffectDamage);
                break;
            case StatusEffects.slow:
                ai.DebuffSlow(damageRequest.statusEffectDamage);
                break;
            case StatusEffects.chill:
                if (stack > 4) stack = 4;
                ai.DebuffChill(0.07f * stack);
                break;
            case StatusEffects.frostbiteFever:
                onHit += FrostbiteTrigger;
                break;
            case StatusEffects.zeravCurse:
                onHit += SoulCurseTrigger;
                break;
            default: break;
        }
        //Debug.Log("applied");

        CheckEntropyProc(damageRequest.hero);
    }
    private void CheckEntropyProc(Hero hero)
    {
        if (!manager.spawnedHeroes.ContainsKey(hero)) return;

        try
        {
            HeroStatus heroObject = manager.spawnedHeroes[hero].status;
            heroObject.control.TriggerEntropyDrain();
        }
        catch
        {
            Debug.Log("Entropy Gagal");
        }
    }

    public EC2StatusEffect GetStatusEffect(StatusEffects effect)
    {
        if (statusEffects.ContainsKey(effect))
        {
            return statusEffects[effect];
        }
        else
        {
            return null;
        }
    }

    public void RemoveStatusEffect(StatusEffects effect)
    {
        if (statusEffects.ContainsKey(effect))
        {
            statusEffects.Remove(effect);

            switch (effect)
            {
                case StatusEffects.poison:
                    ai.RemovePoison();
                    break;
                case StatusEffects.freeze:
                    ai.EndFreeze();
                    break;
                case StatusEffects.armorDown:
                    ai.EndDebuffArmor();
                    break;
                case StatusEffects.damageDown:
                    ai.EndDebuffDamage();
                    break;
                case StatusEffects.critResDown:
                    ai.EndDebuffCritRes();
                    break;
                case StatusEffects.cursed:
                    ai.EndDebuffCurse();
                    break;
                case StatusEffects.tremor:
                    ai.EndDebuffTremor();
                    break;
                case StatusEffects.slow:
                    ai.EndDebuffSlow();
                    break;
                case StatusEffects.mysticFlame:
                    ai.EndIgnite();
                    break;
                case StatusEffects.focused:
                    ai.EndLouisaFocus();
                    break;
                case StatusEffects.chill:
                    ai.EndDebuffChill();
                    break;
                case StatusEffects.frostbiteFever:
                    onHit -= FrostbiteTrigger;
                    break;
                case StatusEffects.zeravCurse:
                    onHit -= SoulCurseTrigger;
                    break;
                default: break;
            }
        }
    }
    public void RemoveStatusEffectImmediate(StatusEffects effect)
    {
        if (statusEffects.ContainsKey(effect))
        {
            EC2StatusEffect se = statusEffects[effect];
            se.EndStatusEffect();
        }
    }

    public static System.Action<float> OnEnemyBurnt;
    public void Tick(DamageRequest damageRequest, int stack)
    {
        StatusEffects effect = damageRequest.statusEffect;

        //Debug.Log("tick : " + damageRequest.damageSourceKey + damageRequest.hero);
        switch (effect)
        {
            case StatusEffects.bleed:
                TakeDebuffDamage(damageRequest);
                break;
            case StatusEffects.paralyze:
                ai.ParalyzeStun(1f);
                break;
            case StatusEffects.poison:
                TakeDebuffDamage(damageRequest);
                break;
            case StatusEffects.mysticFlame:
                TakeDebuffDamage(damageRequest, out float finalDamage);
                //Report to Edna
                OnEnemyBurnt?.Invoke(finalDamage);
                break;
            case StatusEffects.frostbiteFever:
                TakeDebuffDamage(damageRequest);
                break;
            case StatusEffects.zeravCurse:
                try
                {
                    damageRequest.source.GetComponent<Hero_Zerav>().TriggerSoulCurseDamage(this);
                }
                catch { }
                break;
            default: break;
        }
    }
    public void TakeDebuffDamage(DamageRequest damageRequest)
    {
        TakeDebuffDamage(damageRequest, out _);
    }
    public void TakeDebuffDamage(DamageRequest damageRequest, out float finalDamage)
    {
        finalDamage = 0;
        if (dead) return;
        if (myHealthIsMyShield) return;

        float finalDmg = Mathf.RoundToInt(damageRequest.statusEffectDamage);

        //tribe buff
        if (finalDmgReduction_tribe > 0)
            finalDmg -= finalDmg * (finalDmgReduction_tribe / 100);

        //monolith protection
        if (cot_monolithProtection > 0 && shieldBreak == false)
            finalDmg -= finalDmg * (cot_monolithProtection / 100);

        if (finalDmg <= 0) finalDmg = 0;

        HudManager.instance.PopDebuffDamage(transform, 2, finalDmg, damageRequest.statusEffect);


        //register to dps meter
        status.gm.dpsMeter.AddDamage(new DamageRequest(damageRequest) { damage = finalDmg });

        finalDamage = finalDmg;
        hp -= finalDmg;
        status.TakeDamage(finalDmg, HpPercent());

        onHitRequest?.Invoke(damageRequest);

        UpdateHpBar();
        CheckHealthBreach();
        if (hp <= 0)
        {
            Die();
        }
    }
    public void TakeDebuffDamagePercentage(DamageRequest damageRequest)
    {
        //if (dead) return;
        //if (myHealthIsMyShield) return;

        float finalDmg = Mathf.FloorToInt(damageRequest.statusEffectDamage / 100 * maxHp);
        damageRequest.damage = finalDmg;
        TakeDebuffDamage(damageRequest);

        /*
        //tribe buff
        if (finalDmgReduction_tribe > 0)
            finalDmg -= finalDmg * (finalDmgReduction_tribe / 100);

        HudManager.instance.PopDebuffDamage(transform, 2, finalDmg, effect);

        hp -= finalDmg;
        status.TakeDamage(finalDmg, HpPercent());

        UpdateHpBar();
        CheckHealthBreach();

        if (hp <= 0)
        {
            Die();
        }*/
    }

    public void RemoveAllStatusEffects()
    {
        foreach (var key in statusEffects.Keys.ToList())
            statusEffects[key].EndStatusEffect();

    }

    public IEnumerator<float> DelayRemoveSpecificStatusEffects(float delay)
    {
        yield return Timing.WaitForSeconds(delay);
        RemoveSpecificStatusEffects();
    }

    public void RemoveSpecificStatusEffects()
    {
        foreach (var key in statusEffectToRemove)
        {
            if (statusEffects.ContainsKey(key))
            {
#if UNITY_EDITOR
                Debug.Log("Removing Status Effects : " + key.ToString());
#endif
                statusEffects[key].EndStatusEffect();
            }
        }
    }

    public void PopupDamage(Transform target, DamageRequest drq)
    {
        if (drq.useCustomDmgPopUp)
        {
            float y_offset = 2 + drq.customDmgPopUp.offset;
            HudManager.instance.PopDamageEnemyCustom(transform, y_offset, drq);
        }
        else
        {
            HudManager.instance.PopDamageEnemy(transform, 2, drq.damage, drq.isCritical);
        }
    }

    //------------------------------------------

    public void AlterSpeed(float percent)
    {
        anim.speed = percent;
    }
    public void StatusEffectTickNotify(StatusEffects effect, string text, int stack)
    {

    }

    public bool HasStatusEffect(StatusEffects effect, out int stacks)
    {
        if (statusEffects.ContainsKey(effect))
        {
            stacks = statusEffects[effect].stack;
            return true;
        }
        else
        {
            stacks = 0;
            return false;
        }
    }
    public void IncreaseAtk(float percent)
    {
    }

    public void StartSuperArmor()
    {
        superArmor = true;
    }
    public void EndSuperArmor()
    {
        if (status.armor > 0 || status.knockbackRes) return;
        superArmor = false;
    }

    //Tribe Buff
    public float finalDmgReduction_tribe;

    public void Detonate_Bleed(DamageRequest req)
    {
        if (statusEffects.ContainsKey(StatusEffects.bleed))
        {
            EC2StatusEffect bleeding = statusEffects[StatusEffects.bleed];

            //use highest damage modifier
            if (bleeding.damage < req.damage) bleeding.damage = req.damage;

            float bleed_dmg = bleeding.GetFinalDamage() * (1 + (req.statusEffectDamage / 100));
            float remaining = bleeding.lifetime + 1;
            req.statusEffectDamage = bleed_dmg * remaining;

            //remove bleeding
            RemoveStatusEffectImmediate(StatusEffects.bleed);

            //detonate
            TakeDebuffDamage(req);
        }
    }

    float frostbiteCd;
    void FrostbiteTrigger(float dmg)
    {
        if (frostbiteCd > 0) return;
        frostbiteCd = 0.5f;

        EC2StatusEffect frostbite = GetStatusEffect(StatusEffects.frostbiteFever);
        if (frostbite)
        {
            frostbite.Tick(0);
        }
    }

    void SoulCurseTrigger(float dmg)
    {

    }

    //===== EVENT =====//
    [FoldoutGroup("Events")] public UnityEvent on75Health;
    [FoldoutGroup("Events")] public UnityEvent on66Health;
    [FoldoutGroup("Events")] public UnityEvent on50Health;
    [FoldoutGroup("Events")] public UnityEvent on40Health;
    [FoldoutGroup("Events")] public UnityEvent on33Health;
    [FoldoutGroup("Events")] public UnityEvent on25Health;
    [FoldoutGroup("Events")] public UnityEvent onDie;
    [FoldoutGroup("Events")] public UnityEvent onCustomDie;

    public System.Action<float> onHit;
    public System.Action onHelmetHit;
    public System.Action<DamageRequest> onHitRequest;


    //Helmet Return
    [FoldoutGroup("Helmet & Armor")]
    public GameObject helmetReturnVfx;
    public bool ReturnHelmet()
    {
        if (isHelmetOn) return false;
        if (!enableHelmetReturn) return false;

        ShieldCount = returnShield;
        isHelmetOn = true;
        if (disabledObj) disabledObj.SetActive(true);
        if (spawnPos) Instantiate(helmetReturnVfx, spawnPos.position, Quaternion.identity);

        return true;
    }
    public void ReturnHelmet(int helmetDefense)
    {
        ShieldCount = helmetDefense;
        isHelmetOn = true;
        if (disabledObj) disabledObj.SetActive(true);
        if (spawnPos) Instantiate(helmetReturnVfx, spawnPos.position, Quaternion.identity);
    }
}


[System.Serializable]
public class DamageRequest
{
    public float damage;
    public float knockback;
    public float pierce;
    public float _break;
    public float stunDuration;
    public float crate;
    public float cdmg;

    [HideIf("@this.unmissable")]
    public float accuracy;
    public Elemental elemental = Elemental.None;
    public StatusEffects statusEffect = StatusEffects.none;

    [HideIf("@this.statusEffect == StatusEffects.none")]
    public float statusEffectDuration;
    [HideIf("@this.statusEffect == StatusEffects.none")]
    public float statusEffectDamage;
    [HideIf("@this.statusEffect == StatusEffects.none")]
    public float[] statusEffectAttributes;

    [HideInInspector]
    public bool isCritical;
    public bool unmissable;
    public bool notDealingDamage;
    public bool dontRespondOnHit;
    public bool dontBreakIce;
    public bool unresistable;
    public bool ignoreDefDown;
    public bool pureDamage;
    public string damageSourceKey;
    public Hero hero;

    public bool useCustomDmgPopUp;
    public CustomDamagePopUp customDmgPopUp;

    public bool louisa_bullseye;
    public bool ignoreStatusICD;

    public StatusEffects extraDmgStatus_stat;
    public float extraDmgStatus_dmg;

    public Transform source;
    public AttackType attackType = AttackType.none;
    public DamageModifierType damageModifierType;

    public DamageRequest() { }
    public DamageRequest(DamageRequest request)
    {
        damage = request.damage;
        knockback = request.knockback;
        pierce = request.pierce;
        _break = request._break;
        stunDuration = request.stunDuration;

        crate = request.crate;
        cdmg = request.cdmg;
        accuracy = request.accuracy;
        elemental = request.elemental;
        statusEffect = request.statusEffect;
        statusEffectDuration = request.statusEffectDuration;
        statusEffectDamage = request.statusEffectDamage;
        statusEffectAttributes = request.statusEffectAttributes;

        isCritical = request.isCritical;
        dontBreakIce = request.dontBreakIce;
        unmissable = request.unmissable;
        notDealingDamage = request.notDealingDamage;
        dontRespondOnHit = request.dontRespondOnHit;
        unresistable = request.unresistable;
        ignoreDefDown = request.ignoreDefDown;
        pureDamage = request.pureDamage;

        damageSourceKey = request.damageSourceKey;
        hero = request.hero;
        source = request.source;

        useCustomDmgPopUp = request.useCustomDmgPopUp;
        customDmgPopUp = request.customDmgPopUp;

        louisa_bullseye = request.louisa_bullseye;
        ignoreStatusICD = request.ignoreStatusICD;

        extraDmgStatus_stat = request.extraDmgStatus_stat;
        extraDmgStatus_dmg = request.extraDmgStatus_dmg;

        attackType = request.attackType;
        damageModifierType = request.damageModifierType;

    }
}

[System.Serializable]
public class DamageResult
{
    public bool isDead;
    public bool isMiss;
    public bool isStunned;
}
[System.Serializable]
public class CustomDamagePopUp
{
    public float offset = 0;
    public int fontSize = 30;
    public Color color = Color.white;
    public CustomDamagePopUp()
    {

    }
    public CustomDamagePopUp(CustomDamagePopUp popup)
    {
        if (popup == null) return;

        offset = popup.offset;
        fontSize = popup.fontSize;
        color = popup.color;
    }
}