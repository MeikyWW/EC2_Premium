using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using Sirenix.OdinInspector;

public class HeroHealth : MonoBehaviour, IStatusEffect, IDamageable
{
    Renderer[] allRenderers;
    public ObscuredFloat hp;
    [HideInInspector] public bool superArmor, inited, die;
    public float healthCleanseTreshold;
    HeroStatus status;
    HeroControl control;
    HeroProps props;
    List<Material> allMats;
    HeroHUD heroHud;
    ParticleSystem flashEvasionFx;
    GameManager gm;
    InstanceBattleHandler battleHandler;

    #region SUBSCRIBTIONS 
    private void OnEnable()
    {
        PartyManager.OnHeroSwitch += UpdateHpBar;
        GameManager.OnPartyChanged += UpdateHpBar;

        TrialManager.OnTrialStart += RemoveAllBuff;
    }

    private void OnDisable()
    {
        PartyManager.OnHeroSwitch -= UpdateHpBar;
        GameManager.OnPartyChanged -= UpdateHpBar;

        TrialManager.OnTrialStart -= RemoveAllBuff;
    }
    #endregion

    private void Awake()
    {
        superArmorQueue = new Queue();
        superArmorTemporary = false;
    }
    private void Update()
    {
        UpdateFlashEvasion();
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F11)) Die();
        //if (Input.GetKeyDown(KeyCode.Minus)) TakeDamage(5f);
#endif
    }

    void InitHealth()
    {
        if (inited) return;

        canBeFrozen = true;
        gm = GameManager.instance;
        status = GetComponent<HeroStatus>();
        control = GetComponent<HeroControl>();
        props = GetComponent<HeroProps>();

        if (gm.HeroesInCharge.Contains(status))
        {
            heroHud = gm.heroHud;
            if (status.statusBar)
            {
                status.statusBar.SetHealthText(string.Format("{0:0,0} / {1:0,0}", hp, status.FinalMaxHP));
                status.statusBar.UpdateHpBar(hp / status.FinalMaxHP);
                status.statusBar.UpdateShieldBar(0);
                //Debug.Log(" Update HP Bar ", gameObject);
            }
        }

        allRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        flashEvasionFx = transform.Find("VFX/FlashEvasion").GetComponent<ParticleSystem>();

        allMats = new List<Material>();
        foreach (Renderer r in allRenderers)
            foreach (Material m in r.materials)
                allMats.Add(m);

        battleHandler = InstanceBattleHandler.instance;

        inited = true;
    }
    public void RefreshHealthBar()
    {
        InitHealth();
        if (die) return;

        if (hp <= 0) hp = status.FinalMaxHP;
        if (hp > status.FinalMaxHP) hp = status.FinalMaxHP;
        UpdateHpBar();
    }

    public void SetSkinColor(Color color)
    {
        foreach (var item in allMats)
        {
            if (item.HasProperty("_BaseTint"))
                item.SetColor("_BaseTint", color);

            if (item.HasProperty("_BaseTint1"))
                item.SetColor("_BaseTint1", color);
        }
    }

    public void RestoreHp(float amount, bool notify)
    {
        RestoreHp(amount, notify, true);
    }
    public void RestoreHp(float amount, bool notify, bool useVFX)
    {
        if (die) return;
        if (amount + hp >= status.FinalMaxHP)
            amount = status.FinalMaxHP - hp;

        hp += amount;
        if (hp > status.FinalMaxHP)
            hp = status.FinalMaxHP;
        hp = Mathf.RoundToInt(hp);

        if (notify)
        {
            if (useVFX) GameManager.instance.vfxManager.PotionHP(status.potionFxPos, 0);
            if (amount <= 0) return;
            HudManager.instance.PopHealHp(transform, 1, amount);
        }

        UpdateHpBar();
    }
    public void RestoreHpPercent(float _percent, bool notify)
    {
        RestoreHpPercent(_percent, notify, true);
    }

    public void RestoreHpPercent(float _percent, bool notify, bool useVFX)
    {
        float amount = _percent / 100 * status.FinalMaxHP;
        if (hp + amount >= status.FinalMaxHP) amount = status.FinalMaxHP - hp;

        RestoreHp(amount, notify, useVFX);
    }

    public float GetRestoreHpPercent(float _percent)
    {
        float amount = _percent / 100 * status.FinalMaxHP;
        if (hp + amount >= status.FinalMaxHP) amount = status.FinalMaxHP - hp;

        return amount;
    }
    bool disablePlayLowHealth;
    CoroutineHandle handlerPlayLowHealth;
    void UpdateHpBar()
    {
        InitHealth();
        if (!GetComponent<HeroStatus>().IsMine()) return;
        int currentHpVal = Mathf.RoundToInt(hp);
        if (currentHpVal < 0)
        {
            if (status.statusBar)
            {
                status.statusBar.SetHealthText(string.Format("0 / {0:0,0}", status.FinalMaxHP));
                status.statusBar.UpdateHpBar(hp / status.FinalMaxHP);
                UpdateShieldBar();
            }
            return;
        }

        if (gm.IsGameOver()) return;

        if (!die)
        {
            if (!disablePlayLowHealth)
            {
                if (GetHpPercent() <= 0.45f)
                {
                    if (props.PlayLowHealth())
                    {
                        Timing.KillCoroutines(handlerPlayLowHealth);
                        disablePlayLowHealth = true;
                        handlerPlayLowHealth = Timing.RunCoroutine(DoAfterSeconds(60f, () => { disablePlayLowHealth = false; }).CancelWith(gameObject));
                    }
                }
            }
        }

        TriggerCleanse();

        if (status.socketEffect.bravery.count > 0)
        {
            TriggerSomething(treshold: status.socketEffect.bravery.GetValue(1),
                () => status.heroStatusEffect.SetNotification("icon_rune_" + SocketType.Bravery.ToString().ToLower(), ""),
                () => status.heroStatusEffect.RemoveNotification("icon_rune_" + SocketType.Bravery.ToString().ToLower())
            );
        }

        string currentHp = hp < 10 ? currentHpVal.ToString() : (string.Format("{0:0,0}", currentHpVal));

        if (gm.HeroesInCharge.Contains(status))
        {
            if (status.statusBar)
            {
                status.statusBar.SetHealthText(string.Format("{0} / {1:0,0}", currentHp, status.FinalMaxHP));
                status.statusBar.UpdateHpBar(hp / status.FinalMaxHP);
            }
        }
    }
    public void UpdateShieldBar()
    {
        bool shieldAvailable = maxShield > 0 && shieldAmount > 0 && shieldAmount <= maxShield;
        if (shieldAvailable)
        {
            status.statusBar.UpdateShieldBar(shieldAmount / maxShield);
        }
        else
        {
            status.statusBar.UpdateShieldBar(0);
        }
    }
    IEnumerator<float> DoAfterSeconds(float second, System.Action OnAfter)
    {
        yield return Timing.WaitForSeconds(second);
        OnAfter?.Invoke();
    }
    public float GetHpPercent()
    {
        return hp / status.FinalMaxHP;
    }
    //Take Direct Hit from Enemy
    [HideInInspector] public bool ignoreFlinch, dieAnimationPlayed, megaArmor;
    [HideInInspector] public float damageRedPct;
    [HideInInspector] public float fs_damageRedPct;
    [HideInInspector] public bool buff_abyssal_windveil;

    public void SetHpTo(Transform source, float value)
    {
        //forcefully set hp to value
        float totalDmg = hp - value;

        TakeDamage(source, new DamageRequest()
        {
            damage = totalDmg,
            pureDamage = true,
            unmissable = true,
            unresistable = true,
            dontBreakIce = true,
            dontRespondOnHit = true
        }, out DamageResult result);
    }

    public void TakeDamage(Transform source, DamageRequest request, out DamageResult result)
    {
        result = new DamageResult();
        result.isMiss = false;
        if (gm.STATE == GameState.PAUSE) return;

        //Debug.Log("[0] pure dmg taken : " + damage);

        //Damage-Nullifiers
        if (flashEvasionFrame)
        {
            flashEvasionFrame = false;
            control.OnPerfectEvasionSuccess();
            FlashEvasionSucceed(source);
            result.isMiss = true;
            return;
        }

        if (buff_abyssal_windveil)
        {
            return;
        }

        if (!request.unmissable)
        {
            EnemyAI sourceEnemy = source.GetComponent<EnemyAI>();
            if (sourceEnemy != null)
            {
                if (sourceEnemy.isDebuffBlind)
                {
                    if (AutoEvasionSuccess(true) && !GetFrozenState())
                    {
                        result.isMiss = true;
                        if (status.heroReference.hero == Hero.Chase)
                            GetComponent<Hero_Chase>().TryCounterEvasion();
                        else if (status.heroReference.hero == Hero.Louisa)
                            GetComponent<Hero_Louisa>().TryCounterEvasion();
                        return;
                    }
                }
                else
                {
                    if (AutoEvasionSuccess() && !GetFrozenState())
                    {
                        result.isMiss = true;
                        if (status.heroReference.hero == Hero.Chase)
                            GetComponent<Hero_Chase>().TryCounterEvasion();
                        else if (status.heroReference.hero == Hero.Louisa)
                            GetComponent<Hero_Louisa>().TryCounterEvasion();
                        return;
                    }
                }
            }
        }

        //Toughness / SA / Evasion proc
        if (status.socketEffect.toughness.count > 0)
        {
            status.ActivateToughnessSocketFx();
        }

        ignoreFlinch = false;
        ignoreFlinch = superArmor || PhysicalResistanceProc(source) || result.isMiss || megaArmor || control.IsUsingSkill() || shieldAmount > 0;
        float extraDmgReduction = ignoreFlinch ? SuperArmorDamageReduction : 0;

        //Calculate all damage reduction
        float finalDmg;

        //Damage reduced by Defense Stat
        float dr = EC2Utils.GetDefenseDamageReduction(status.FinalDefense, GetSourceLevel(source));
        //Debug.Log("[1] damage reduction : " + dr + "%");

        //pure damage bypass def
        if (request.pureDamage)
        {
            extraDmgReduction = 0;
            dr = 0;
        }

        finalDmg = request.damage - (dr / 100 * request.damage);

        //Debug.Log("[2] final dmg (after DR) : " + finalDmg);
        if (GetFrozenState())
            finalDmg = finalDmg + (finalDmg * (1f - status.FinalElementalResistance / 100f));

        //Damage reduced by Super Armor
        finalDmg -= extraDmgReduction / 100 * finalDmg;
        //Debug.Log("[3] final dmg (after SuperArmor) : " + finalDmg);

        /*
        //Increase damage income by using Polarize socket effect
        finalDmg += (status.RuneAtkDamageAmplifier / 100 * finalDmg);
        */

        //Final rounding
        finalDmg *= (1 - damageRedPct / 100f);
        finalDmg *= (1 - fs_damageRedPct / 100f);
        finalDmg = Mathf.Round(finalDmg);
        if (finalDmg < 1) finalDmg = 1;

        float dmgToDisplay = finalDmg;

        //Calculate Shield
        if (shieldAmount > 0)
        {
            shieldAmount -= finalDmg;
            finalDmg = 0;

            status.statusBar.UpdateShieldBar(shieldAmount / maxShield);
        }

        if (!megaArmor)
        {
            hp -= finalDmg;
            TriggerFireShield();
            control.DamageTaken(finalDmg, source);
            HudManager.instance.PopDamagePlayer(transform, 1, dmgToDisplay);
            status.TriggerVengeance();
            if (ignoreFlinch)
            {
                control.StaggerResisted();
            }
            if (hp > 0)
            {
                if (GetFrozenState() && !request.dontBreakIce) EndFreeze();
                if (!ignoreFlinch && !request.dontRespondOnHit) control.Hit(source, request.knockback, request.statusEffect != StatusEffects.freeze);

                control.TakeDirectDamage();
            }
            else
            {
                bool DEAD = true;
                bool hasRevivalEgg = RevivalEggAvailable();

                if (status.heroReference.hero == Hero.Elze) //ELZE UNDYING RESOLVE
                {
                    Hero_Elze elze = GetComponent<Hero_Elze>();
                    if (elze.undyingResolve_TimerReady && elze.MasteryUnlocked(5))
                    {
                        if (elze.winterRoses.RoseCount > 0)
                        {
                            status.heroHealth.hp = 1;
                            RestoreHpPercent(20, true);
                            elze.UseUndyingResolveImmortality();
                            DEAD = false;
                        }
                    }
                }

                if (status.socketEffect.protection.count > 0 && DEAD)
                {
                    //Rune : Protection
                    var procProtect = status.ActivateProtectionSocketFx();
                    if (procProtect)
                    {
                        DEAD = false;
                    }
                }

                if (hasRevivalEgg && DEAD)
                {
                    //Revival Egg
                    status.UseRevivalEgg();
                    gm.inventory.CharacterInventory().RemoveItem("consum_sunny", 1);

                    status.heroHealth.hp = 1;
                    RestoreHpPercent(30, true);
                    DEAD = false;
                }

                if (DEAD)
                {
                    result.isDead = true;
                    Die();

                    // Debug.Log("dieded");
                }
                else
                {
                    control.DisableAntiDeadblow();
                    if (!ignoreFlinch && !request.dontRespondOnHit)
                        control.Hit(source, request.knockback, request.statusEffect != StatusEffects.freeze);

                    // Debug.Log("survived");
                }
            }
        }

        //check instance battle handler
        if (battleHandler)
        {
            battleHandler.HeroTakeDamage(finalDmg, GetHpPercent());
        }

        UpdateHpBar();
    }


    //Shield Mechanism
    public float shieldAmount = 0f;
    public float shieldDuration = 0f;
    private float maxShield;
    public void ActivateShield(float amount, float duration)
    {
        shieldAmount = amount;
        shieldDuration = duration;
        maxShield = amount;

        status.statusBar.UpdateShieldBar(1);

        Timing.RunCoroutine(ShieldCoroutine().CancelWith(gameObject));
    }
    private IEnumerator<float> ShieldCoroutine()
    {
        int timer = 0;
        while (timer < shieldDuration)
        {
            status.heroStatusEffect.SetNotification("al_guardianwill", (shieldDuration - timer).ToString());
            timer += 1;
            yield return Timing.WaitForSeconds(1);
        }

        shieldAmount = 0f;
        status.statusBar.UpdateShieldBar(0);

        status.heroStatusEffect.RemoveNotification("al_guardianwill");
    }

    public void TriggerCleanse()
    {
        if (GetHpPercent() * 100f < healthCleanseTreshold)
            control.CleanseDebuff();
    }
    public void TriggerFireShield()
    {
        var se = GetStatusEffect(StatusEffects.fireShield);
        if (se != null)
        {
            try
            {
                var instanced = se.GetComponent<InstancedObject>();
                if (instanced)
                {
                    instanced.TakeInstance(1, () =>
                    {
                        //Try remove effect
                        if (statusEffects.ContainsKey(StatusEffects.fireShield))
                        {
                            statusEffects[StatusEffects.fireShield].EndStatusEffect();
                        }
                    });
                }
            }
            catch
            {
            }
        }
    }

    public void TriggerSomething(float treshold, System.Action OnTreshold, System.Action OutsideTreshold)
    {
        if (GetHpPercent() * 100f < treshold)
            OnTreshold?.Invoke();
        else
            OutsideTreshold?.Invoke();
    }

    public static event System.Action OnForceSwitch;
    public System.Action OnDie;
    public static event System.Action OnGeneralDie;

    [Button]
    public void Die()
    {
        if (die) return;
        OnGeneralDie?.Invoke();
        OnDie?.Invoke();
        die = true;
        RemoveAllStatusEffect();

        hp = 0;
        UpdateHpBar();
        if (control.isStunned) control.StunEnd();
        if (control.isStrongPulled) control.EndStrongPull();
        GetComponent<Animator>().SetBool("stunned", false);
        GetComponent<Animator>().SetFloat("speed", 0);
        ResetAllTriggers();

        GetComponent<CharacterController>().enabled = false;

        props.PlayDie();

        if (gm.AllowParty)
        {
            if (gameObject == gm.ActiveHero.gameObject)
                OnForceSwitch?.Invoke();
            else
            {
                if (status.statusBar)
                    status.statusBar.SetPictureDie();
            }
        }
        else
        {
            gm.SetGameOver();

            //check instance battle handler
            if (InstanceBattleHandler.instance)
                InstanceBattleHandler.instance.HeroDead();
        }

        handler = Timing.RunCoroutine(PlayDieAnimation().CancelWith(gameObject), Segment.LateUpdate);
    }

    private void ResetAllTriggers()
    {
        var anim = GetComponent<Animator>();
        if (anim)
        {
            foreach (var param in anim.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    anim.ResetTrigger(param.name);
                }
            }
        }
    }

    public CoroutineHandle handler;
    IEnumerator<float> PlayDieAnimation()
    {
        dieAnimationPlayed = false;
        GetComponent<Animator>().ResetTrigger("die");

        while (!dieAnimationPlayed)
        {
            yield return Timing.WaitForOneFrame;
            GetComponent<Animator>().SetTrigger("die");
        }
    }
    public void DieAnimationPlayed()
    {
        if (handler.IsValid)
        {
            Timing.KillCoroutines(handler);
        }
        dieAnimationPlayed = true;
        GetComponent<Animator>().ResetTrigger("die");

        control.Die();
    }
    public void SilentDie()
    {
        control.DisableMove();

        RemoveAllStatusEffect();
        GetComponent<Animator>().SetTrigger("die");
        GetComponent<CharacterController>().enabled = false;

        die = true;
    }


    //==== SPECIAL PROPERTIES ====//
    public float SuperArmorDamageReduction
    {
        get
        {
            return 25 + claris_ironwill;
        }
    }
    [HideInInspector] public float claris_ironwill; //Claris Mastery : Iron Will

    //Auto-Evasion : Chance to Dodge an attack completely using the "Evasion" attribute
    [HideInInspector] public bool disableEvasion;
    public bool AutoEvasionSuccess()
    {
        if (disableEvasion) return false;

        bool evadeSuccess = Random.Range(0, 100) < status.FinalEvasion;
        if (evadeSuccess)
        {
            HudManager.instance.PopMiss(transform);
        }

        return evadeSuccess;
    }
    public bool AutoEvasionSuccess(bool isEnemyBlind)
    {
        if (disableEvasion) return false;

        bool evadeSuccess = Random.Range(0, 100) < status.FinalEvasion + 15f;
        if (evadeSuccess)
        {
            HudManager.instance.PopMiss(transform);
        }

        return evadeSuccess;
    }

    //Flash-Evasion : Dodge an Attack just-before a hit lands, grants massive boost for a brief time
    bool flashEvasionFrame, flashEvasionActive;
    public bool zeravCounter;
    float flashEvasionTimer;
    public void StartFlashEvasionFrame()
    {
        flashEvasionFrame = true;
    }
    public void EndFlashEvasionFrame()
    {
        flashEvasionFrame = false;
    }
    void FlashEvasionSucceed(Transform attacker) //Perfect Evasion
    {
        if (flashEvasionActive)
        {
            FlashEvasionEnd();
        }

        props.PlayPerfectEvasion();
        flashEvasionActive = true;
        if (flashEvasionFx) flashEvasionFx.Play();
        GameManager.instance.vfxManager.FlashLong();
        gm.TimeBreak();

        control.StartIframe();
        flashEvasionTimer = 5;

        //FLASH EVASION BUFF (5 sec)
        //Reset Evasion Cooldown, Doubles Mana Generation, Super Armor
        control.RefreshEvasion();
        status.TemporaryManaIncrease(100);
        StartSuperArmor();

        if (zeravCounter)
        {
            Transform counterTarget = null;
            if (attacker != null)
                if (attacker.CompareTag("enemy")) counterTarget = attacker;
            GetComponent<Hero_Zerav>().CounterBlinkStrike(counterTarget);
        }
    }
    void FlashEvasionEnd()
    {
        flashEvasionActive = false;
        status.TemporaryManaIncrease(0);
        EndSuperArmor();

        //SetSuperArmorPriority(0);
        //else EndSuperArmor(3);
    }
    void UpdateFlashEvasion()
    {
        if (!flashEvasionActive) return;

        flashEvasionTimer -= Time.deltaTime;
        if (flashEvasionTimer <= 0) FlashEvasionEnd();
    }
    public bool IsFlashEvasionActive()
    {
        return flashEvasionActive;
    }

    //Super Armor
    //int superArmorPriority;
    Queue superArmorQueue;
    bool superArmorTemporary;

    public void StartSuperArmorTemporary()
    {
        if (!EC2Utils.IsMine()) return;
        superArmor = true;
        superArmorTemporary = true;
        status.heroStatusEffect.SetNotification("super_armor", "");
    }
    public void ResetSuperArmorTemporary()
    {
        if (!EC2Utils.IsMine()) return;
        superArmorTemporary = false;
        CheckSuperArmor();
    }

    public void StartSuperArmor()
    {
        if (!EC2Utils.IsMine()) return;
        //if (priority < superArmorPriority) return;

        //superArmorPriority = priority;
        superArmorQueue.Enqueue("");
        superArmor = true;

        status.heroStatusEffect.SetNotification("super_armor", "");
        //Debug.Log("ARMOR SUPA : " + superArmorQueue.Count);
        /*
        foreach (Material m in allMats)
        {
            m.SetFloat("_ASEOutlineWidth", 0.02f);
            m.SetColor("_ASEOutlineColor", Color.red);
        }*/
    }
    public void EndSuperArmor()
    {
        //if (priority < superArmorPriority) return;

        //superArmorPriority = 0;
        //Debug.Log("ARMOR DESUPA : " + superArmorQueue.Count);

        if (superArmorQueue.Count > 0) superArmorQueue.Dequeue();
        CheckSuperArmor();
        /*
        foreach (Material m in allMats)
        {
            m.SetFloat("_ASEOutlineWidth", 0.01f);
            m.SetColor("_ASEOutlineColor", Color.black);
        }*/
    }

    public void CheckSuperArmor()
    {
        if (!status) return;
        if (!status.IsMine()) return;
        if (superArmorQueue.Count == 0 && !superArmorTemporary)
        {
            superArmor = false;

            status.heroStatusEffect.RemoveNotification("super_armor");
        }
    }

    public void ResetSuperArmor()
    {
        superArmor = false;

        status.heroStatusEffect.RemoveNotification("super_armor");
        superArmorQueue.Clear();
        superArmorTemporary = false;
    }
    //public void SetSuperArmorPriority(int priority)
    //{
    //    superArmorPriority = priority;
    //}

    //Toughness : 1 level difference grants 0.5% flinch resistance
    //Toughness Point increase even more level difference

    bool PhysicalResistanceProc(Transform source)
    {
        //if (superArmor) return true;

        /*
        int sourceLevel = GetSourceLevel(source);
        int toughness = (status.Level + Mathf.RoundToInt(status.FinalPhysicalResistance)) - sourceLevel;
        float ignoreFlinchChance = toughness * 0.5f;*/

        bool ignoreFlinch = Random.Range(0, 100) < status.FinalPhysicalResistance;

        //toughness cue
        //if (ignoreFlinch) Timing.RunCoroutine(ToughnessFx().CancelWith(gameObject));

        return ignoreFlinch;
    }
    /*
    IEnumerator<float> ToughnessFx()
    {
        foreach (Material m in allMats)
        {
            m.SetFloat("_ASEOutlineWidth", 0.02f);
            m.SetColor("_ASEOutlineColor", Color.cyan);
        }

        yield return Timing.WaitForSeconds(0.1f);

        foreach (Material m in allMats)
        {
            m.SetFloat("_ASEOutlineWidth", 0.01f);
            m.SetColor("_ASEOutlineColor", Color.black);
        }
    }*/
    int GetSourceLevel(Transform source)
    {
        int result;

        try
        {
            result = source.GetComponent<EnemyStatus>().level;
        }
        catch
        {
            //hero probably got hit by environmental obstacles
            result = status.Level;
        }

        return result;
    }

    //Stun mechanism
    public void Stun(float duration)
    {
        if (die) return;
        if (gm.IsGameOver()) return;

        //nullify
        if (ignoreFlinch || control.IsSiege || control.IsUsingSkill() || megaArmor)
        {
            HudManager.instance.PopResist(transform);
            return;
        }

        control.Stun(duration);
    }

    public void Paralyze(float duration)
    {
        if (die) return;
        if (gm.IsGameOver()) return;

        //nullify
        if (Random.Range(0, 100) <= status.FinalElementalResistance || megaArmor || control.IsSiege || control.IsUsingSkill())
        {
            HudManager.instance.PopResist(transform);
            return;
        }

        control.Stun(duration);
    }

    //Knockdown buildup
    float knockdownThreshold = 100;
    float kdPoint, kdReducePerSec = 2;
    bool KnockdownThresholdReached(float val)
    {
        kdPoint += val;
        if (kdPoint >= knockdownThreshold)
        {
            kdPoint = 0;
            return true;
        }
        else
        {
            return false;
        }
    }
    void UpdateKD()
    {
        if (kdPoint > 0)
            kdPoint -= Time.deltaTime * kdReducePerSec;
    }


    public void CureStatusEffect(StatusEffects statusEffect, bool notify)
    {
        if (statusEffects.ContainsKey(statusEffect))
        {
            EC2StatusEffect se = statusEffects[statusEffect];
            se.EndStatusEffect();
        }
        if (notify) GameManager.instance.vfxManager.PotionCure(status.potionFxPos, 0);
    }
    public void CureStatusEffect(StatusEffects statusEffect)
    {
        CureStatusEffect(statusEffect, false);
    }
    public void RemoveAllStatusEffect()
    {
        RemoveAllStatusEffect(false);
    }
    public void RemoveAllStatusEffect(bool notify)
    {
        control.stunFx.SetActive(false);

        /*
        RemoveStatusEffect(StatusEffects.bleed);
        RemoveStatusEffect(StatusEffects.poison);
        RemoveStatusEffect(StatusEffects.paralyze);*/

        foreach (var key in statusEffects.Keys.ToList())
        {
            if (statusEffects.ContainsKey(key))
            {
                if (!StatusEffectManager.instance.IsBuff(key))
                    statusEffects[key].EndStatusEffect();
            }
        }

        if (notify) GameManager.instance.vfxManager.PotionCure(status.potionFxPos, 0);
    }
    public void RemoveAllBuff()
    {
        foreach (var key in statusEffects.Keys.ToList())
        {
            if (statusEffects.ContainsKey(key))
            {
                if (StatusEffectManager.instance.IsBuff(key))
                    statusEffects[key].EndStatusEffect();
            }
        }
    }


    [HideInInspector] public float elze_freeze_RES;
    //===== STATUS EFFECT INTERFACE =====//
    public Dictionary<StatusEffects, EC2StatusEffect> statusEffects = new Dictionary<StatusEffects, EC2StatusEffect>();
    public void SetStatusEffect(DamageRequest damageRequest)
    {
        StatusEffects effect = damageRequest.statusEffect;


        if (buff_abyssal_windveil) return;

        //Frozen cant be doubled
        if (!canBeFrozen && effect == StatusEffects.freeze) return;

        //Index 2 ~ 6 is positive status effect. Else, is negative.
        bool isBuff = StatusEffectManager.instance.IsBuff(effect);

        //Apply resistance if taking negative status effect
        if (!isBuff)
        {
            bool nullify;
            var random = Random.Range(0, 100);

            switch (effect)
            {
                case StatusEffects.bleed:
                case StatusEffects.exhaust:
                    //case StatusEffects.armorDown:
                    nullify = random <= status.FinalPhysicalResistance;
                    break;

                case StatusEffects.burn:
                case StatusEffects.poison:
                case StatusEffects.paralyze:
                    nullify = random <= status.FinalElementalResistance;
                    break;

                case StatusEffects.freeze:
                    nullify = random <= (status.FinalElementalResistance + elze_freeze_RES);
                    break;

                default:
                    nullify = false;
                    break;
            }

            if ((nullify || megaArmor) && !damageRequest.unresistable)
            {
                HudManager.instance.PopResist(transform);
                return;
            }
        }
        //apply effect
        switch (effect)
        {
            case StatusEffects.poison:
                ApplyPoison(0.8f);
                break;
            case StatusEffects.atkUp:
                IncreaseAtk(damageRequest.statusEffectDamage);
                break;
            case StatusEffects.eleResUp:
                IncreaseElementalResistance(damageRequest.statusEffectDamage);
                break;
            case StatusEffects.phyResUp:
                IncreasePhysicalResistance(damageRequest.statusEffectDamage);
                break;
            case StatusEffects.exhaust:
                ApplyExhaust(damageRequest.statusEffectDamage);
                break;
            case StatusEffects.freeze:
                if (canBeFrozen)
                    Freeze();
                break;
            case StatusEffects.ensnare:
                ApplyEnsnare(0.7f);
                break;
            case StatusEffects.silence:
                ApplySilence();
                break;
            case StatusEffects.blind:
                ApplyBlind(damageRequest.statusEffectDamage);
                break;
            case StatusEffects.slow:
                ApplySlow(0.80f);
                break;
            case StatusEffects.snowSlow:
                ApplySnowSlow(damageRequest.statusEffectDamage);
                break;
            case StatusEffects.fireShield:
                ApplyFireShield(damageRequest.statusEffectDamage);
                break;
            case StatusEffects.critUp:
                IncreaseCrit(damageRequest.statusEffectDamage);
                break;
            case StatusEffects.armorDown:
                ApplyArmorDown();
                break;
            default: break;
        }

        if (statusEffects.ContainsKey(effect))
        {
            //udah ada dalam list. update attribut aja
            statusEffects[effect].SetAttribute(transform, damageRequest);
        }
        else
        {
            //spawn objek status baru
            EC2StatusEffect effectObj = StatusEffectManager.instance.GetObject(effect, transform, true).GetComponent<EC2StatusEffect>();
            effectObj.SetAttribute(transform, damageRequest);
            statusEffects.Add(effect, effectObj);

            if (!isBuff) AddNegativeEffect();
        }

        if (statusEffects[effect].notify)
        {
            //set status notification with attributes[0] (lifetime)
            status.heroStatusEffect.SetNotification("status_" +
                effect.ToString().ToLower(), damageRequest.statusEffectDuration.ToString(), statusEffects[effect].stack);
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

    public void Tick(DamageRequest damageRequest, int stack)
    {
        StatusEffects effect = damageRequest.statusEffect;

        switch (effect)
        {
            case StatusEffects.bleed:
                TakeDebuffDamage(damageRequest);
                break;
            case StatusEffects.paralyze:
                ParaStun(damageRequest.statusEffectDamage, 1f);
                break;
            case StatusEffects.poison:
                DamageRequest poisonReq = new DamageRequest()
                {
                    statusEffect = StatusEffects.poison,
                    statusEffectDamage = 1.5f * stack
                };
                TakeDebuffDamagePercentage(poisonReq);
                break;
            case StatusEffects.burn:
                DamageRequest burnReq = new DamageRequest()
                {
                    statusEffect = StatusEffects.burn,
                    statusEffectDamage = 3 * stack
                };
                TakeDebuffDamagePercentage(burnReq);
                break;
            default: break;
        }
    }

    public void RemoveStatusEffect(StatusEffects effect)
    {
        if (statusEffects.ContainsKey(effect))
        {
            bool isBuff = StatusEffectManager.instance.IsBuff(effect);
            if (!isBuff) MinNegativeEffect();

            if (statusEffects[effect].notify)
            {
                //remove status notification
                status.heroStatusEffect.RemoveNotification("status_" + effect.ToString().ToLower());
            }

            statusEffects.Remove(effect);

            switch (effect)
            {
                case StatusEffects.poison:
                    RemovePoison();
                    break;
                case StatusEffects.atkUp:
                    IncreaseAtk(0);
                    break;
                case StatusEffects.eleResUp:
                    IncreaseElementalResistance(0);
                    break;
                case StatusEffects.phyResUp:
                    IncreasePhysicalResistance(0);
                    break;
                case StatusEffects.freeze:
                    EndFreeze();
                    break;
                case StatusEffects.exhaust:
                    ApplyExhaust(0);
                    break;
                case StatusEffects.ensnare:
                    RemoveEnsnare();
                    break;
                case StatusEffects.silence:
                    RemoveSilence();
                    break;
                case StatusEffects.blind:
                    ApplyBlind(0);
                    break;
                case StatusEffects.slow:
                    RemoveSlow();
                    break;
                case StatusEffects.snowSlow:
                    RemoveSnowSlow();
                    break;
                case StatusEffects.fireShield:
                    ApplyFireShield(0);
                    break;
                case StatusEffects.critUp:
                    IncreaseCrit(0);
                    break;
                case StatusEffects.armorDown:
                    RemoveArmorDown();
                    break;
                default: break;
            }
        }
    }

    public void TakeDebuffDamage(DamageRequest damageRequest)
    {
        if (gm.IsGameOver()) return;
        if (gm.STATE == GameState.PAUSE) return;



        StatusEffects effect = damageRequest.statusEffect;
        switch (effect)
        {
            case StatusEffects.bleed:
            case StatusEffects.exhaust:
                damageRequest.statusEffectDamage *= (1 - status.FinalPhysicalResistance / 100);
                break;

            case StatusEffects.burn:
            case StatusEffects.freeze:
            case StatusEffects.poison:
            case StatusEffects.paralyze:
                damageRequest.statusEffectDamage *= (1 - status.FinalElementalResistance / 100);
                break;
        }

        float finalDmg = Mathf.RoundToInt(damageRequest.statusEffectDamage);
        HudManager.instance.PopDebuffDamage(transform, 2, finalDmg, effect);

        hp -= finalDmg;
        UpdateHpBar();

        if (hp <= 0) CheckOnDie();
    }

    public void CheckOnDie()
    {
        if (status.socketEffect.protection.count > 0)
        {
            //Rune : Protection
            var procProtect = status.ActivateProtectionSocketFx();
            if (procProtect)
            {
                control.DisableAntiDeadblow();
            }
            else
                Die();
        }
        else Die();
    }
    public void TakeDebuffDamagePercentage(DamageRequest damageRequest)
    {
        if (gm.IsGameOver()) return;
        if (gm.STATE == GameState.PAUSE) return;

        StatusEffects effect = damageRequest.statusEffect;
        switch (effect)
        {
            case StatusEffects.bleed:
            case StatusEffects.exhaust:
                damageRequest.statusEffectDamage *= (1 - status.FinalPhysicalResistance / 100);
                break;

            default:
                damageRequest.statusEffectDamage *= (1 - status.FinalElementalResistance / 100);
                break;
        }

        float finalDmg = Mathf.FloorToInt(damageRequest.statusEffectDamage / 100 * status.FinalMaxHP);
        HudManager.instance.PopDebuffDamage(transform, 2, finalDmg, effect);

        hp -= finalDmg;
        UpdateHpBar();

        if (hp <= 0) CheckOnDie();
    }
    public void TakePureDamage(float dmg)
    {
        if (gm.IsGameOver()) return;
        if (gm.STATE == GameState.PAUSE) return;

        //float finalDmg = Mathf.RoundToInt(percent / 100 * status.FinalMaxHP);
        float finalDmg = dmg;
        HudManager.instance.PopDamagePlayer(transform, 2, finalDmg);

        hp -= finalDmg;
        UpdateHpBar();

        if (hp <= 0) CheckOnDie();
    }
    public void TakePercentageDamage(float pct)
    {
        if (gm.IsGameOver()) return;
        if (gm.STATE == GameState.PAUSE) return;

        //float finalDmg = Mathf.RoundToInt(percent / 100 * status.FinalMaxHP);
        float finalDmg = pct / 100 * status.FinalMaxHP;
        HudManager.instance.PopDamagePlayer(transform, 2, finalDmg);

        hp -= finalDmg;
        UpdateHpBar();

        if (hp <= 0) CheckOnDie();
    }
    public void ParaStun(float damage, float duration)
    {
        if (gm.IsGameOver()) return;
        damage = damage * (1 - status.FinalElementalResistance / 100);
        duration = duration * (1 - status.FinalElementalResistance / 100);
        TakeDamage(transform, new DamageRequest() { damage = damage }, out _);
        Paralyze(duration);
    }
    public void AlterSpeed(float percent)
    {
        control.SetMoveSpeed(percent);
        control.SetAttackSpeed(percent);
    }


    //=== POISONED ===//
    [HideInInspector] public bool isPoisoned;
    [HideInInspector] public float poisonSlowValue = 1f;
    public void ApplyPoison(float slowMod) // 0~1
    {
        isPoisoned = true;
        poisonSlowValue = slowMod;
        control.SetAttackSpeed(poisonSlowValue);
        control.ResetMoveSpeed();
    }

    public void RemovePoison()
    {
        isPoisoned = false;
        poisonSlowValue = 1;
        control.SetAttackSpeed(poisonSlowValue);
        control.ResetMoveSpeed();
    }

    //=== SILENTO ===//
    [HideInInspector] public bool isSilenced;
    public void ApplySilence() // 0~1
    {
        isSilenced = true;
    }

    public void RemoveSilence()
    {
        isSilenced = false;
    }
    //=== ENSNARE ===//
    [HideInInspector] public bool isEnsnared;
    [HideInInspector] public float ensnareValue = 0f;
    public void ApplyEnsnare(float slowMod) // 0~1
    {
        isEnsnared = true;
        ensnareValue = slowMod;
        control.ResetMoveSpeed();
    }

    public void RemoveEnsnare()
    {
        isEnsnared = false;
        ensnareValue = 1f;
        control.ResetMoveSpeed();
    }

    //==== SLOW ====//
    [HideInInspector] public bool isSlowed;
    [HideInInspector] public float slowedValue = 0f;
    public void ApplySlow(float slowMod) // 0~1
    {
        isSlowed = true;
        slowedValue = slowMod;
        control.SetAttackSpeed(slowedValue);
        control.ResetMoveSpeed();
    }

    public void RemoveSlow()
    {
        isSlowed = false;
        slowedValue = 1f;
        control.SetAttackSpeed(slowedValue);
        control.ResetMoveSpeed();
    }


    //==== SNOW SLOW ====//
    [HideInInspector] public bool isSnowSlowed;
    [HideInInspector] public float snowSlowedValue = 0f;
    public void ApplySnowSlow(float slowMod) // 0~1
    {
        isSnowSlowed = true;
        snowSlowedValue = slowMod;
        control.ResetMoveSpeed();
    }

    public void RemoveSnowSlow()
    {
        isSnowSlowed = false;
        snowSlowedValue = 1f;
        control.ResetMoveSpeed();
    }

    //==== Freeze ====//
    [HideInInspector] public bool isFrozen;
    [HideInInspector] public bool canBeFrozen;
    public bool Freeze()
    {
        if (megaArmor) return false;
        if (isFrozen) return false;

        status.animator.speed = 0f;
        isFrozen = true;
        canBeFrozen = false;
        control.DisableMove();

        StatusEffectManager.instance.Freeze(transform);
        return true;
    }

    public void EndFreeze()
    {
        if (!isFrozen) return;
        status.animator.speed = 1f;
        isFrozen = false;
        if (!control.isStunned) control.EnableMove();


        StatusEffectManager.instance.Unfreeze(transform);

        Timing.RunCoroutine(DelayFrozeable(0.3f).CancelWith(gameObject), EC2Constant.STATE_DEPENDENT);

        //Try remove effect
        if (statusEffects.ContainsKey(StatusEffects.freeze))
        {
            statusEffects[StatusEffects.freeze].EndStatusEffect();
        }
    }

    IEnumerator<float> DelayFrozeable(float delay)
    {
        yield return Timing.WaitForSeconds(delay);
        canBeFrozen = true;
    }

    public bool GetFrozenState() => isFrozen;

    public void StatusEffectTickNotify(StatusEffects effect, string text, int stack)
    {
        if (!statusEffects.ContainsKey(effect)) return;
        if (!statusEffects[effect].notify) return;
        //set status notification with attributes[0] (lifetime)
        status.heroStatusEffect.SetNotification("status_" + effect.ToString().ToLower(), text, stack);
    }
    public void IncreaseAtk(float percent)
    {
        status.consum_atk_boost = percent / 100 * status.damage;
    }

    public void IncreaseCrit(float val)
    {
        status.consum_crit_rate_up = val;
    }
    public void ApplyBlind(float percent)
    {
        status.blindDebuff = percent;
    }

    public void ApplyArmorDown()
    {
        status.armorDown_stack++;
    }
    public void RemoveArmorDown()
    {
        status.armorDown_stack = 0;
    }
    public void ApplyFireShield(float percent)
    {
        fs_damageRedPct = percent;
    }

    public void ApplyExhaust(float percent)
    {
        status.exhaustDebuffCDR = percent;
    }

    public void IncreasePhysicalResistance(float percent)
    {
        status.consum_phyres_boost = percent;
    }

    public void IncreaseElementalResistance(float percent)
    {
        status.consum_eleres_boost = percent;
    }

    int totalNegativeEffect = 0;
    void AddNegativeEffect()
    {
        totalNegativeEffect++;
        if (totalNegativeEffect > 0) control.InflictNegativeEffects();
    }
    void MinNegativeEffect()
    {
        totalNegativeEffect--;
        if (totalNegativeEffect <= 0) control.EndNegativeEffects();
    }

    public bool IsDead()
    {
        return die;
    }


    //Magical Egg
    private bool RevivalEggAvailable()
    {
        bool hasEgg = gm.inventory.CharacterInventory().GetQuantity("consum_sunny") > 0;

        if (hasEgg && !status.revivalEggOnCooldown)
        {
            Debug.Log("revival egg available");
            return true;
        }
        else
        {
            Debug.Log("revival egg not available");
            return false;
        }
    }
}

