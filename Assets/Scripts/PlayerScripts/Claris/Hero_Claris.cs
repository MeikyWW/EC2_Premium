/* Hero specific controllers, this handles all Skills Function
 * (Initialize, Activate, and Cooldown)
 * And Basic Attack
 * */
using EZ_Pooling;
using MEC;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Hero_Claris : HeroControl
{
    //Mastery References
    private bool AllowMastery
    {
        get
        {
            var isTrial = gm.IsTrialHero(Hero.Claris);
            return gm.AllowMasterySkill || isTrial;
        }
    }
    private bool isInited = false;
    private int indexProfile;

    #region SUBSCRIBTIONS 
    public override void OnEnable()
    {
        if (!EC2Utils.IsMine()) return;
        base.OnEnable();
        PartyManager.OnHeroSwitch += CheckRageGauge;
        GameManager.OnPartyChanged += CheckRageGauge;
        GameManager.OnDestinationChanged += CheckRageGauge;
    }

    public override void OnDisable()
    {
        if (!EC2Utils.IsMine()) return;
        base.OnDisable();
        PartyManager.OnHeroSwitch -= CheckRageGauge;
        GameManager.OnPartyChanged -= CheckRageGauge;
        GameManager.OnDestinationChanged -= CheckRageGauge;
    }
    #endregion

    private void Awake()
    {
        if (!EC2Utils.IsMine()) return;
        ragePoint = PlayerPrefs.GetFloat(EC2Utils.GetCurrentProfilePrefs("ClarisRage"));
    }
    private void Start()
    {
        Initialize();
        Timing.RunCoroutine(WaitOnStart());
    }
    IEnumerator<float> WaitOnStart()
    {
        while (!_status.heroData.saveDataLoaded)
        {
            yield return Timing.WaitForOneFrame;
        }
        isInited = true;
        if (EC2Utils.IsMine())
        {
            InitAllMasteries();

            CheckRageGauge();
            UpdateRageBar();
        }
    }

    private void Update()
    {
        if (!isInited) return;
        if (gm.STATE == GameState.PAUSE) return;
        if (gm.IsGameOver()) return;
        if (!EC2Utils.IsMine()) return;

        MovementUpdate();
        UpdateEvasionCooldown();
        UpdateSkillsCooldown();
        UpdateLifeDrainCooldown(); UpdateBurstCooldown();
        UpdateSetEffectCooldown();
        UpdateMaidEvasionCD();

        UpdateStunTimer();

        //Skills
        UpdateIronBody();
        UpdateManaBlade();

        UpdateRage();
        UpdateSkillTimer();

        //Mastery
        UpdateKnightsBravery(gm.detectedEnemies);
        UpdateFortitude();
        UpdateTenacity();
        UpdateTenacityBuffDuration();
        UpdateInitiateTimer();

        UpdateTotalEclipse();

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F7))
        {
            AddRagePoint(100);
            UpdateRageBar();
            RefreshAllSkillCooldown();
            _status.AddManaPercent(100);
        }
#endif
    }

    public GameObject skill_special_flash;

    [Title("[Basic Attack]")]
    public SpawnFx[] basicAtkFx;
    public LayerMask layerMask;
    public HeroWeaponHitbox[] hitboxes;

    public override void BasicAtkPress()
    {
        if (gm.ActiveHero.heroHealth.GetFrozenState()) return;
        if (_status.combatStat == CombatStatus.OffCombat) return;

        _animator.SetBool("magna", ExActivated(5));

        base.BasicAtkPress();

        OmniHackActionPress();
    }
    public override void SpecialAtkPress()
    {
        if (_health.GetFrozenState()) return;
        if (_status.combatStat == CombatStatus.OffCombat) return;
        if (IsMovementRestricted()) return;

        //Activate Fury
        if (!allowSpecial) return;
        ActivateRageMode();
    }
    protected override void PlayAttackAnimation()
    {
        if (!actionFramePassed) return;
        if (idleState)
        {
            //Debug.Log("reset (idle state)");
            attackIndex = -1;
        }

        if (pressedBtn == ActionButton.BasicAtk) indexInc = 1;
        else indexInc = 0;

        base.PlayAttackAnimation();
    }
    void BasicAtkFx(int idx)
    {
        Transform fx = EZ_PoolManager.Spawn(basicAtkFx[idx].prefab,
            transform.position + transform.TransformDirection(basicAtkFx[idx].pos),
            transform.rotation * Quaternion.Euler(basicAtkFx[idx].rot));

        if (manaBladeActivated)
        {
            Timing.RunCoroutine(ManaBladeSpawn(fx.position, idx, 0), EC2Constant.STATE_DEPENDENT);

            //followup wave
            if (isRageActivated)
                Timing.RunCoroutine(ManaBladeSpawn(fx.position, idx, 0.15f), EC2Constant.STATE_DEPENDENT);
        }
    }
    private void CommenceBasicAtk(int hitboxIndex)
    {
        try
        {
            hitboxes[hitboxIndex].ResetTimestop();
            hitboxes[hitboxIndex].AttackWithCollider(out _);
        }
        catch
        {

        }
    }
    void ResetHitboxProperties()
    {
        for (int i = 0; i < hitboxes.Length; i++)
        {
            hitboxes[i].ResetTimestop();
        }
    }
    void DisableAllHitboxes()
    {
        for (int i = 0; i < hitboxes.Length; i++)
        {
            hitboxes[i].DisableCollider();
            //hitboxes[i].gameObject.SetActive(false);
        }
    }
    public override void EnterIdleState()
    {
        base.EnterIdleState();
        //DisableAllHitboxes();
    }

    //== CLARIS SKILLS ==//
    #region SKILLS
    protected override void InitAllSkills()
    {
        base.InitAllSkills();
        evasion.SetSkillAttributes(1);
        for (int i = 0; i < skills.Length; i++)
        {
            skills[i].SetSkillAttributes(_status.heroData.data.skillLevels[i]);
        }

        //Legendary Rune Effects for Initial MP Cost / Cooldown

        //Impact Crash - EX
        if (ExActivated(2))
        {
            skills[2].custom_extra_manacost = 70;
            leap_EX = true;
        }
        else
        {
            skills[2].custom_extra_manacost = 0;
            leap_EX = false;
        }

        //Tackle - EX
        if (ExActivated(4))
        {
            skills[4].custom_extra_manacost = -50;
            tackleEx = true;
        }
        else
        {
            skills[4].custom_extra_manacost = 0;
            tackleEx = false;
        }
    }
    protected override void SkillActivate(int skillIndex)
    {
        base.SkillActivate(skillIndex);

        DisableAllHitboxes();

        switch (skillIndex)
        {
            case 0: ActivateWarcry(); break;
            case 1: ActivateCrossSlash(); break;
            case 2: ActivateLeapStrike(); break;
            case 3: ActivateManaBlade(); break;
            case 4: ActivateTackle(); break;
            case 5: ActivateRoundForce(); break;
            case 6: ActivateJuggernautBuster(); break;
            case 7: ActivateHack(); break;
        }
    }
    public override void OnSwitchedOut()
    {
        base.OnSwitchedOut();
        EndRage(false, true);
    }
    public override void BeforeSwitch()
    {
        base.BeforeSwitch();
        EndRage(false, true);
    }
    public override void CommenceSwitch()
    {
        base.CommenceSwitch();

        if (_status.costumeEffect.ogClarisValue > 0)
        {
            AddRagePoint(_status.costumeEffect.ogClarisValue);
        }

        isUsingSkill = justSkill = true;
        DisableMove();
        ResetAttack();

        leap_EX = ExActivated(2);
        SetLeapAttack(2, true);
    }

    //IRON BODY
    float ironBodyTimer;
    bool ironBodyActive;
    void ActivateIronBody()
    {
        int skillIndex = 4;
        if (CanUseSkill(skillIndex, skills[skillIndex].ManaCost(_status.manaReduction), skills[skillIndex].GetFinalCooldown(_status.FinalCDReduction), true))
        {
            //Activate Super Armor for Attribute[0] Sec
            _props.PlayVFX(0);
            _health.StartSuperArmor();
            ironBodyTimer = skills[skillIndex].Attribute(3);
        }
    }
    void UpdateIronBody()
    {
        if (!ironBodyActive) return;
        if (ironBodyTimer > 0)
        {
            ironBodyTimer -= Time.deltaTime;
            if (ironBodyTimer <= 0)
            {
                _health.EndSuperArmor();
                ironBodyActive = false;
            }
        }
    }

    #region SKILL 0 - WARCRY
    [Title("[Skill : 0] Warcry")]
    public GameObject warcry;
    public float warcryRadius;
    void ActivateWarcry()
    {
        int skillIndex = 0;
        float manaCost = skills[skillIndex].ManaCost(_status.FinalMpCostReduction);
        float cooldown = skills[skillIndex].GetFinalCooldown(_status.FinalCDReduction);
        float affinityCost = skills[skillIndex].Attribute(2);

        bool succeed = false;
        if (CanUseRage(skillIndex, affinityCost, cooldown)) succeed = true;
        else if (CanUseSkill(skillIndex, manaCost, cooldown, false)) succeed = true;

        if (succeed)
        {
            AddSkillExp(skillIndex);

            StartIframe();
            //QuickRotate();
            SetTriggerAnimation("skill_warcry");
        }
    }
    void WarcryFx()
    {
        Instantiate(warcry, transform.position, transform.rotation);
    }
    void CommenceWarcry()
    {
        gm.TimeBreak();
        CombatCamera.instance.EarthQuake();
        _props.PlaySFX2(3);
        SetBuffDamage();
        float breakVal = skills[5].Attribute(3) * (1 + (_status.speciality / 100));
        ApplyCritical(GetFinalSkillDamage() * skills[0].Attribute(0) / 100, out float finalDmg, out bool isCritical);
        ApplyBuffedDamage(ref finalDmg);
        tackleHits = Physics.OverlapSphere(transform.position, warcryRadius, layerMask);
        bool isHitting = tackleHits.Length > 0;
        totalEnemyHit = tackleHits.Length;

        float intimidateDuration = skills[0].Attribute(1);

        foreach (Collider c in tackleHits)
        {
            if (c.CompareTag("enemy"))
            {
                DamageRequest req = new DamageRequest()
                {
                    damage = finalDmg,
                    isCritical = isCritical,
                    knockback = 1,
                    pierce = _status.pierce,
                    _break = breakVal,
                    accuracy = _status.FinalAccuracy,
                    cdmg = _status.FinalCriticalDamage,
                    damageSourceKey = skills[0].id,
                    hero = Hero.Claris,

                    statusEffect = StatusEffects.damageDown,
                    statusEffectDuration = intimidateDuration,
                    statusEffectDamage = isRageActivated ? 40 : 30
                };

                c.GetComponent<EnemyHealth>().TakeDamage(transform, req, out DamageResult result);

                if (!result.isMiss)
                {
                    if (isCritical)
                    {
                        GameManager.instance.vfxManager.Crit(transform.position, AttackType.blow);
                        ApplyDrain(req.damage);
                    }
                    if (!result.isDead)
                    {
                        //if enemy level is higher, Claris only has 30% chance to apply debuff
                        int enemyLv = c.GetComponent<EnemyStatus>().level;
                        float procRate = _status.Level < enemyLv ? 30 : 100;

                        if (Random.Range(0, 100) < procRate)
                        {
                            c.GetComponent<EnemyAI>().Stun(0.5f);
                            StatusEffectManager.instance.SetStatusEffect(c.transform, req);
                        }

                        //MAGNA : Reduce Defense
                        if (ExActivated(0))
                        {
                            DamageRequest defRed = new DamageRequest()
                            {
                                statusEffect = StatusEffects.armorDown,
                                statusEffectDuration = intimidateDuration,
                                statusEffectDamage = ExSkillValue(0) * ExSkillModifier(0)
                            };
                            StatusEffectManager.instance.SetStatusEffect(c.transform, defRed);
                        }
                    }
                }

            }
            if (c.CompareTag("Ore"))
            {
                c.GetComponent<GatheringPoint>().Hit();
            }
        }

        ManaRegen_CrossSlash();
        SkillUsed();
    }
    #endregion

    #region SKILL 1 - CROSS SLASH
    [Title("[Skill : 1] X - Saber")]
    public HeroWeaponHitbox xsaberHitbox;
    public AudioClip[] xsaberVoice0;
    public AudioClip[] xsaberVoice1;
    public GameObject[] fxCrossSlash;
    public GameObject[] fxCrossSlashAmped;
    public GameObject[] fxCrossSlashEX;
    public ManaRegenInfo slashManaRegen;
    int playedxsabervoice = 0;
    void ActivateCrossSlash()
    {
        int skillIndex = 1;
        float manaCost = skills[skillIndex].ManaCost(_status.manaReduction);
        float cooldown = skills[skillIndex].GetFinalCooldown(_status.FinalCDReduction);
        float affinityCost = skills[skillIndex].Attribute(4);

        bool succeed = false;
        if (CanUseRage(skillIndex, affinityCost, cooldown)) succeed = true;
        else if (CanUseSkill(skillIndex, manaCost, cooldown, false)) succeed = true;

        if (succeed)
        {
            AddSkillExp(skillIndex);

            //Trigger Cross Slash animation (Damage : Attribute[0] % * Attack)
            StartIframe();
            QuickRotate();

            float mvDamage = skills[skillIndex].Attribute(0);
            SetMotionValue(mvDamage, skills[1].id);

            //disableExtraBasicAttack = true;
            attackBreak = skills[skillIndex].Attribute(2) * (1 + (_status.speciality / 100));

            if (ExActivated(1))
                SetTriggerAnimation("skill_xsaber_ex");
            else SetTriggerAnimation("skill_CrossSlash");

            playedxsabervoice = Random.Range(0, xsaberVoice0.Length);
            _props.PlayVoice(xsaberVoice0[playedxsabervoice]);
        }
    }
    void PlayXsaberVoice()
    {
        if (playedxsabervoice < 2) return;
        _props.PlayVoice(xsaberVoice1);
    }
    void CrossSlash(int idx)
    {
        if (idx == 1 && ExActivated(1))
        {
            XSaberEX();
            return;
        }

        xsaberHitbox.ResetTimestop();
        xsaberHitbox.AttackWithCollider(out _);


        int skillIndex = 1;
        float finalDmg = GetFinalSkillDamage() * skills[skillIndex].Attribute(1) / 100;
        float offset = idx == 0 ? 1 : 2;

        GameObject[] selected = isRageActivated ? fxCrossSlashAmped : fxCrossSlash;
        if (ExActivated(1))
            selected = fxCrossSlashEX;

        GameObject wave = Instantiate(selected[idx],
            transform.position + Vector3.up * 2.3f + transform.forward * offset,
            transform.rotation);

        if (idx == 0) firstsaber = wave;

        SetBuffDamage();
        bool critProc;
        ApplyCritical(finalDmg, out finalDmg, out critProc);
        ApplyBuffedDamage(ref finalDmg);

        float breakVal = skills[skillIndex].Attribute(3) * (1 + (_status.speciality / 100));

        DamageRequest req = new DamageRequest()
        {
            damage = finalDmg,
            isCritical = critProc,
            pierce = _status.pierce,
            _break = breakVal,
            accuracy = _status.FinalAccuracy,
            cdmg = _status.FinalCriticalDamage,
            damageSourceKey = skills[skillIndex].id,
            hero = Hero.Claris
        };
        wave.GetComponent<Claris_CrossSlash>().SetDamage(transform, req);
        SkillUsed();
    }
    GameObject firstsaber;
    public ParticleSystem swordskillFlash;
    void XSaberExPrep()
    {
        swordskillFlash.Play();
    }
    void XSaberEX()
    {
        if (firstsaber) Destroy(firstsaber);
        CombatCamera.instance.EarthQuake(0.3f);

        int skillIndex = 1;
        float finalDmg = GetFinalSkillDamage() * skills[skillIndex].Attribute(1) / 100 * 12; //damage is 12x higher than original

        Vector3 pos = transform.position + Vector3.up * 2.3f + transform.forward * 1;
        GameObject wave = Instantiate(fxCrossSlashEX[1], pos, transform.rotation);

        //crit here
        SetBuffDamage();
        bool critProc;
        ApplyCritical(finalDmg, out finalDmg, out critProc);
        ApplyBuffedDamage(ref finalDmg);

        float breakVal = skills[skillIndex].Attribute(3) * (1 + (_status.speciality / 100));
        float bleedModifier = ExSkillValue(1) * ExSkillModifier(1) / 100;
        float bleedDamage = bleedModifier * GetFinalSkillDamage() * finalBleedPotency;

        DamageRequest req = new DamageRequest()
        {
            damage = finalDmg,
            isCritical = critProc,
            pierce = _status.pierce,
            _break = breakVal,
            accuracy = _status.FinalAccuracy,
            cdmg = _status.FinalCriticalDamage,
            damageSourceKey = skills[skillIndex].id,
            hero = Hero.Claris,

            statusEffect = StatusEffects.bleed,
            statusEffectDamage = bleedDamage,
            statusEffectDuration = 5,

            useCustomDmgPopUp = true,
            customDmgPopUp = new CustomDamagePopUp()
            {
                fontSize = 40
            }
        };
        wave.GetComponent<Claris_CrossSlash>().SetDamage(transform, req, true, 100);
        SkillUsed();
    }
    void ManaRegen_CrossSlash()
    {
        if (totalEnemyHit == 0) return;

        float result = slashManaRegen.manaIncreaseOnInitialHit +
            slashManaRegen.manaIncreasePerExtraEnemy * (totalEnemyHit - 1);
        if (result > slashManaRegen.maximumManaIncrease)
            result = slashManaRegen.maximumManaIncrease;

        _status.AddManaValue(result);
        totalEnemyHit = 0;
    }
    #endregion

    #region SKILL 2 - LEAP STRIKE
    [Title("[Skill : 2] Leap Strike")]
    public AudioClip[] leapStrikehVoice;
    public AudioClip[] leapExVoice;
    public GameObject objLeapStrike;
    public Transform leapFollow;
    public GameObject leapLimiterPrefab;
    public Transform[] leapEnemyCheckCaster;
    GameObject leapLimiterSpawned;
    Claris_LeapStrike leapStrikeObj;
    bool leap_EX;
    void ActivateLeapStrike()
    {
        int skillIndex = 2;

        if (ExActivated(2))
        {
            //extra 70mp cost
            skills[skillIndex].custom_extra_manacost = 70;
            leap_EX = true;
        }
        else
        {
            skills[skillIndex].custom_extra_manacost = 0;
            leap_EX = false;
        }

        float manaCost = skills[skillIndex].ManaCost(_status.manaReduction);
        float cooldown = skills[skillIndex].GetFinalCooldown(_status.FinalCDReduction);
        float affinityCost = skills[skillIndex].Attribute(3) + (leap_EX ? 20 : 0);

        bool succeed = false;
        if (CanUseRage(skillIndex, affinityCost, cooldown)) succeed = true;
        else if (CanUseSkill(skillIndex, manaCost, cooldown, false)) succeed = true;

        if (succeed)
        {
            AddSkillExp(skillIndex);
            SetLeapAttack(skillIndex, false);
        }
    }
    private void SetLeapAttack(int skillIndex, bool isSwitchIn)
    {
        if (!isSwitchIn)
        {
            if (leap_EX)
                _props.PlayVoice(leapExVoice);
            else
                _props.PlayVoice(leapStrikehVoice);
        }

        QuickRotate();

        if (leap_EX)
        {
            SetTriggerAnimation("skill_ic_ex");
            StartIframe();
            disableGravity = true;
        }
        else
        {
            SetTriggerAnimation("skill_Leap");
            _health.StartSuperArmor();

            leapStrikeObj = Instantiate(objLeapStrike, transform.position, transform.rotation).GetComponent<Claris_LeapStrike>();
            LeapStrike_FrontEnemyCheck();

            float finalDmg = GetFinalSkillDamage() * skills[skillIndex].Attribute(0) / 100;
            float finalBreak = skills[skillIndex].Attribute(2) * (1 + (_status.speciality / 100));

            SetBuffDamage();
            ApplyCritical(finalDmg, out finalDmg, out isCritical);
            ApplyBuffedDamage(ref finalDmg);

            DamageRequest req = new DamageRequest()
            {
                damage = finalDmg,
                isCritical = isCritical,
                knockback = 1,
                pierce = _status.pierce,
                _break = finalBreak,
                accuracy = _status.FinalAccuracy,
                cdmg = _status.FinalCriticalDamage,
                damageSourceKey = skills[2].id,
                hero = Hero.Claris
            };
            leapStrikeObj.SetDamage(leapFollow, skills[skillIndex].Attribute(1), req);
        }
    }
    void LeapStrike_Finisher()
    {
        try
        {
            leapStrikeObj.FinisherAttack(skills[2].Attribute(1), out totalEnemyHit);
            if (totalEnemyHit > 0) gm.TimeBreak(0.1f);
        }
        catch { }
        if (leapLimiterSpawned) Destroy(leapLimiterSpawned);
        ManaRegen_CrossSlash();
        ActivateInitiateBoost();
        SkillUsed();
    }
    void LeapStrike_FrontEnemyCheck()
    {
        for (int i = 0; i < leapEnemyCheckCaster.Length; i++)
        {
            if (Physics.Raycast(leapEnemyCheckCaster[i].transform.position, transform.forward, out RaycastHit hit, 15, 1 << 10))
            {
                if (hit.transform.CompareTag("enemy"))
                {
                    //spawn invisible wall at hitting point
                    //Debug.Log("hitting : " + hit.transform.name + " at " + hit.point);
                    leapLimiterSpawned = Instantiate(leapLimiterPrefab, hit.point, transform.rotation);
                    //Destroy(leapLimiterSpawned, 2);

                    break;
                }
            }
        }
    }
    void LeapStrike_EX()
    {
        disableGravity = false;

        float finalDmg = GetFinalSkillDamage() * skills[2].Attribute(0) / 100;
        float finalBreak = skills[2].Attribute(2) * (1 + (_status.speciality / 100));

        SetBuffDamage();
        ApplyCritical(finalDmg, out finalDmg, out isCritical);
        ApplyBuffedDamage(ref finalDmg);

        GameObject buster = Instantiate(juggernautBuster, juggerPos.position, transform.rotation);

        float ex_dmg = finalDmg * (ExSkillValue(2) * ExSkillModifier(2) / 100);// 0.1f * _status.GetExVal(skills[2].ex_rune[0]));
        float ex_brk = finalBreak;// * (1 + (_status.socketEffect.transmutation.GetValue(0) / 100));

        DamageRequest req = new DamageRequest()
        {
            damage = ex_dmg / 7f,
            isCritical = isCritical,
            _break = ex_brk / 7f,
            accuracy = _status.FinalAccuracy,
            cdmg = _status.FinalCriticalDamage,
            damageSourceKey = skills[2].id,
            hero = Hero.Claris
        };
        buster.GetComponent<Chase_StaticField>().CommenceField(transform, req);

        CombatCamera.instance.EarthQuake();
        ActivateInitiateBoost();
        SkillUsed();
    }
    #endregion

    #region SKILL 3 - MANA BLADE
    [Title("[Skill : 3] Crescent Blade")]
    public HeroWeaponHitbox crescentBladeHitbox;
    public AudioClip[] crescentVoice;
    public ParticleSystem manaBladeActivationFx;
    public Transform initialWave, manaBlade;
    public Vector3[] bAtkBladeRot;
    float manaWaveDmg, manaBladetimer;
    bool manaBladeActivated;
    void ActivateManaBlade()
    {
        int skillIndex = 3;
        float manaCost = skills[skillIndex].ManaCost(_status.manaReduction);
        float cooldown = skills[skillIndex].GetFinalCooldown(_status.FinalCDReduction);
        float affinityCost = skills[skillIndex].Attribute(5);

        bool succeed = false;
        if (CanUseRage(skillIndex, affinityCost, cooldown)) succeed = true;
        else if (CanUseSkill(skillIndex, manaCost, cooldown, false)) succeed = true;

        if (succeed)
        {
            AddSkillExp(skillIndex);
            StartIframe();
            QuickRotate();

            SetTriggerAnimation("skill_Manablade");

            _props.PlayVoice(crescentVoice);
        }
    }
    void ManaBladeMV()
    {
        float mvDamage = skills[3].Attribute(0);
        SetMotionValue(mvDamage, skills[3].id);

        attackBreak = skills[3].Attribute(3) * (1 + (_status.speciality / 100));
    }
    void ManaBlade_Strike()
    {
        crescentBladeHitbox.ResetTimestop();
        crescentBladeHitbox.AttackWithCollider(out _);

        SkillUsed();
        //_health.EndSuperArmor(1);
        manaWaveDmg = GetFinalBasicDamage() * skills[3].Attribute(1) / 100;

        _cam.MediumShake();
        gm.TimeBreak();

        manaBladeActivated = true;
        manaBladeActivationFx.Play();
        manaBladetimer = skills[3].Attribute(2);
    }
    IEnumerator<float> ManaBladeSpawn(Vector3 pos, int idx, float delay)
    {
        if (delay > 0)
            yield return Timing.WaitForSeconds(delay);
        else yield return Timing.WaitForOneFrame;

        Transform mblade = EZ_PoolManager.Spawn(manaBlade, pos,
            transform.rotation * Quaternion.Euler(bAtkBladeRot[idx]));

        SetBuffDamage();
        float fDmg; bool critProc;
        ApplyCritical(manaWaveDmg, out fDmg, out critProc);
        ApplyBuffedDamage(ref fDmg);

        float finalBreak = skills[3].Attribute(4) * (1 + (_status.speciality / 100));
        if (isRageActivated) finalBreak *= 0.5f;

        float extraDmg = 0;
        if (_status.HasExRune(SocketType.Omni))
        {
            float runeVal = ExSkillValue(3) * ExSkillModifier(3) / 100;
            extraDmg = runeVal * fDmg;
        }
        fDmg += extraDmg;

        DamageRequest req = new DamageRequest()
        {
            damage = fDmg,
            isCritical = critProc,
            pierce = _status.pierce,
            _break = finalBreak,
            accuracy = _status.FinalAccuracy,
            cdmg = _status.FinalCriticalDamage,
            damageSourceKey = skills[3].id,
            hero = Hero.Claris,

            useCustomDmgPopUp = true,
            customDmgPopUp = new CustomDamagePopUp()
            {
                color = new Color(0.5f, 1f, 1f),
                fontSize = 20,
                offset = 1
            }
        };
        mblade.GetComponent<HeroProjectile>().Shoot(transform, req);
        mblade.GetComponent<HeroProjectile>().onHittingEnemy.AddListener(HitboxHitLand);
    }
    void UpdateManaBlade()
    {
        if (omniBlade) return;

        if (manaBladetimer > 0)
        {
            manaBladetimer -= Time.deltaTime;
            if (manaBladetimer <= 0)
            {
                manaBladeActivated = false;
                manaBladeActivationFx.Stop();
                _status.heroStatusEffect.RemoveNotification(skills[3].id);
            }
            else
            {
                _status.heroStatusEffect.SetNotification(skills[3].id, Mathf.CeilToInt(manaBladetimer).ToString());
            }
        }
    }
    #endregion

    #region SKILL 4 - TACKLE
    [Title("[Skill : 4] Tackle")]
    public AudioClip[] tackleVoice;
    public Transform tacklePull;
    public ParticleSystem tackleHitFx;
    public float tackleRadius;
    GameObject realTacklePull;
    Collider[] tackleHits;
    bool tackleEx;
    float tackleExStartingHealth;
    void ActivateTackle()
    {
        int skillIndex = 4;

        if (ExActivated(4))
        {
            //no mp cost
            skills[skillIndex].custom_extra_manacost = -50;
            tackleEx = true;
        }
        else
        {
            skills[skillIndex].custom_extra_manacost = 0;
            tackleEx = false;
        }

        float manaCost = skills[skillIndex].ManaCost(_status.manaReduction);
        float cooldown = skills[skillIndex].GetFinalCooldown(_status.FinalCDReduction);
        float affinityCost = skills[skillIndex].Attribute(5);

        if (tackleEx)
        {
            manaCost = 0;
            affinityCost = 0;
        }

        bool succeed = false;
        if (CanUseRage(skillIndex, affinityCost, cooldown)) succeed = true;
        else if (CanUseSkill(skillIndex, manaCost, cooldown, false)) succeed = true;

        if (succeed)
        {
            AddSkillExp(skillIndex);
            QuickRotate();

            if (ExActivated(4))
            {
                SetTriggerAnimation("tackle_ex");
                _health.StartSuperArmor();

                //damage
                tackleExStartingHealth = _health.hp;
                //Debug.Log("start hp : " + tackleExStartingHealth);
                _health.TakePercentageDamage(5);

                _props.PlayVoice2(0);
            }
            else
            {
                SetTriggerAnimation("skill_Tackle");
                StartIframe();
            }

            if (!ironBodyActive) _health.StartSuperArmor();
            ironBodyActive = true;
            ironBodyTimer = skills[skillIndex].Attribute(3) + 0.9f;
        }
    }
    void PlayTackleVoices()
    {
        _props.PlayVoice(tackleVoice);
    }
    void CommenceTackleAttack()
    {
        //Damage : Attribute[0] % * Attack
        SetBuffDamage();

        float finalDmg = GetFinalSkillDamage() * skills[4].Attribute(0) / 100;
        ApplyBuffedDamage(ref finalDmg);

        //Stun Chance : Attribute[1]
        bool stunSucceed = Random.Range(0, 100) < skills[4].Attribute(1);
        //Stun Duration : Attribute [2]
        float stunDuration = skills[4].Attribute(2);

        realTacklePull = new GameObject("Tackle Magnet");
        realTacklePull.transform.parent = tacklePull;
        realTacklePull.transform.localPosition = Vector3.zero;

        tackleHits = Physics.OverlapSphere(transform.position + transform.forward, tackleRadius, layerMask);
        bool isHitting = tackleHits.Length > 0;
        totalEnemyHit = tackleHits.Length;
        ManaRegen_CrossSlash();

        float breakVal = skills[4].Attribute(4) * (1 + (_status.speciality / 100));

        //apply EX Rune skill
        //float currentPct = _health.GetHpPercent();
        float totalHpLoss = tackleExStartingHealth - _health.hp;
        //Debug.Log("current hp : " + _health.hp);
        //Debug.Log("total hp lost : " + totalHpLoss);

        float bonusDmgPercent = Mathf.Floor(totalHpLoss / 50);
        //Debug.Log("multiplier : " + bonusDmgPercent);

        float exDmgBonus = bonusDmgPercent * ExSkillValue(4) * ExSkillModifier(4);// _status.GetExVal(skills[4].ex_rune[0]) * skills[4].ex_modifier[0];

        //Debug.Log(string.Format("Dmg bonus : {0}%", exDmgBonus));
        //Debug.Log(string.Format("Base dmg : {0}", finalDmg));

        float ruinDmg = exDmgBonus / 100 * finalDmg;

        if (!ExActivated(4)) ruinDmg = 0;
        else _health.EndSuperArmor();

        finalDmg += ruinDmg;
        //Debug.Log(string.Format("Final dmg : {0}", finalDmg));

        ApplyCritical(finalDmg, out finalDmg, out isCritical);


        foreach (Collider c in tackleHits)
        {
            if (c.CompareTag("enemy"))
            {
                c.GetComponent<EnemyAI>().PullEffect(realTacklePull.transform, 1, 6);

                DamageRequest req = new DamageRequest()
                {
                    damage = finalDmg,
                    isCritical = isCritical,
                    pierce = _status.pierce,
                    _break = breakVal,
                    accuracy = _status.FinalAccuracy,
                    cdmg = _status.FinalCriticalDamage,
                    damageSourceKey = skills[4].id,
                    hero = Hero.Claris
                };

                c.GetComponent<EnemyHealth>().TakeDamage(transform, req, out DamageResult result);
                if (!result.isMiss)
                {
                    if (!result.isDead)
                    {
                        if (stunSucceed) c.GetComponent<EnemyAI>().Stun(stunDuration);
                    }
                    if (req.isCritical)
                    {
                        GameManager.instance.vfxManager.Crit(transform.position, AttackType.blow);
                        ApplyDrain(req.damage);
                    }
                }
            }

            if (c.CompareTag("Ore"))
                c.GetComponent<GatheringPoint>().Hit();
        }

        if (isHitting)
        {
            if (tackleHitFx) Instantiate(tackleHitFx, transform.position + transform.forward * 2 + Vector3.up, Quaternion.identity);
            gm.TimeBreak();
            CombatCamera.instance.LightShake();
        }

        ActivateInitiateBoost();
        SkillUsed();
        Timing.RunCoroutine(EndTacklePull().CancelWith(gameObject), EC2Constant.STATE_DEPENDENT);
    }

    IEnumerator<float> EndTacklePull()
    {
        yield return Timing.WaitForSeconds(0.2f);
        foreach (Collider c in tackleHits)
        {
            if (c.CompareTag("enemy"))
            {
                c.GetComponent<EnemyAI>().PullEffect(null, 0, 0);
            }
        }

        Destroy(realTacklePull.gameObject);
    }
    #endregion

    #region SKILL 5 - ROUND SLASH
    [Title("[Skill : 5] Round Slash")]
    public AudioClip[] roundVoice;
    public float roundForceRadius;
    public GameObject roundForceHitFx;
    void PlayRoundslashVoice()
    {
        _props.PlayVoice(roundVoice);
    }
    void ActivateRoundForce()
    {
        int skillIndex = 5;
        float manaCost = skills[skillIndex].ManaCost(_status.manaReduction);
        float cooldown = skills[skillIndex].GetFinalCooldown(_status.FinalCDReduction);
        float affinityCost = skills[skillIndex].Attribute(4);

        bool succeed = false;
        if (CanUseRage(skillIndex, affinityCost, cooldown)) succeed = true;
        else if (CanUseSkill(skillIndex, manaCost, cooldown, false)) succeed = true;

        if (succeed)
        {
            affectBleeding = false;

            AddSkillExp(skillIndex);
            StartIframe();
            QuickRotate();

            SetTriggerAnimation("skill_RoundForce");
        }
    }
    void CommenceRoundForceAttack(int batk)
    {
        CombatCamera.instance.MediumShake();

        tackleHits = Physics.OverlapSphere(transform.position, roundForceRadius, layerMask);
        bool isHitting = tackleHits.Length > 0;
        totalEnemyHit = tackleHits.Length;
        ManaRegen_CrossSlash();

        SetBuffDamage();

        float finalDmg;

        if (batk == 0)
        {
            finalDmg = GetFinalSkillDamage() * skills[5].Attribute(0) / 100;
        }
        else
        {
            finalDmg = GetFinalBasicDamage() * skills[5].Attribute(0) / 100;
            finalDmg *= ExSkillValue(5) * ExSkillModifier(5) / 100;
        }

        float breakVal = skills[5].Attribute(3) * (1 + (_status.speciality / 100));
        ApplyCritical(finalDmg, out finalDmg, out isCritical);
        ApplyBuffedDamage(ref finalDmg);

        DamageRequest req = new DamageRequest()
        {
            damage = finalDmg,
            isCritical = isCritical,
            knockback = 1,
            pierce = _status.pierce,
            _break = breakVal,
            accuracy = _status.FinalAccuracy,
            cdmg = _status.FinalCriticalDamage,
            damageSourceKey = skills[5].id,
            hero = Hero.Claris,
            damageModifierType = batk == 0 ? DamageModifierType.SkillAttack : DamageModifierType.BasicAttack
        };

        foreach (Collider c in tackleHits)
        {
            if (c.CompareTag("enemy"))
            {
                EnemyHealth enemy = c.GetComponent<EnemyHealth>();
                enemy.TakeDamage(transform, req, out DamageResult result);

                if (!result.isMiss)
                {
                    if (req.isCritical)
                    {
                        GameManager.instance.vfxManager.Crit(transform.position, AttackType.slash);
                        if (batk == 0) ApplyDrain(finalDmg);
                    }

                    //apply bleed
                    bool applyBleed = batk != 1 || (Random.Range(0, 100) < 25f);

                    if (applyBleed)
                    {
                        float bleedDuration = skills[5].Attribute(1);
                        float bleedDamage = skills[5].Attribute(2) / 100 * GetFinalSkillDamage() * finalBleedPotency;
                        DamageRequest bleedReq = new DamageRequest()
                        {
                            statusEffect = StatusEffects.bleed,
                            statusEffectDuration = bleedDuration,
                            statusEffectDamage = bleedDamage,
                            damageSourceKey = "other",
                            hero = Hero.Claris
                        };
                        StatusEffectManager.instance.SetStatusEffect(c.transform, bleedReq);
                    }
                }

                if (roundForceHitFx) Instantiate(roundForceHitFx, c.transform.position + Vector3.up, Quaternion.identity);
            }
            if (c.CompareTag("Ore"))
            {
                c.GetComponent<GatheringPoint>().Hit();
                if (roundForceHitFx) Instantiate(roundForceHitFx, c.transform.position + Vector3.up, Quaternion.identity);
            }
        }

        if (isHitting)
        {
            gm.TimeBreak();
        }
        if (batk == 0) SkillUsed();
    }
    #endregion

    #region SKILL 6 - JUGGERNAUT BUSTER
    [Title("[Skill : 6] Juggernaut Buster")]
    public AudioClip[] flurryVoice0;
    public AudioClip[] flurryVoice1;
    public ParticleSystem finalStrikeSuccessFx;
    public GameObject juggernautBuster;
    public Transform juggerPos;
    public HeroWeaponHitbox juggernautHitbox;
    private bool ampedBuster, juggernautPull;
    GameObject juggerPull;
    bool jugger_batk_activated;
    int playedanticipationvoice = 0;
    void ActivateJuggernautBuster()
    {
        int skillIndex = 6;
        float manaCost = skills[skillIndex].ManaCost(_status.manaReduction);
        float cooldown = skills[skillIndex].GetFinalCooldown(_status.FinalCDReduction);
        float affinityCost = skills[skillIndex].Attribute(5);

        //transmute rune reduce affinity usage by x%
        if (ExActivated(6))
        {
            float afreduction = ExSkillValue(6) / 100 * affinityCost;
            affinityCost -= afreduction;
        }

        bool succeed = false;
        if (CanUseRage(skillIndex, affinityCost, cooldown)) { succeed = true; ampedBuster = true; }
        else if (CanUseSkill(skillIndex, manaCost, cooldown, false)) succeed = true;

        if (succeed)
        {
            AddSkillExp(skillIndex);
            _health.StartSuperArmor();
            QuickRotate();
            SetTriggerAnimation("skill_juggernaut");

            float mvDamage = skills[skillIndex].Attribute(0);
            SetMotionValue(mvDamage, skills[6].id);

            float breakVal = skills[skillIndex].Attribute(2) * (1 + (_status.speciality / 100));
            attackBreak = breakVal;

            if (ExActivated(6))
            {
                //is now a basic attack
                jugger_batk_activated = true;
                juggernautHitbox.damageType = DamageModifierType.BasicAttack;

                if (ExActivated(7))
                {
                    //this attack also reduce cooldown
                    juggernautHitbox.OnEnemyHitFireOnce.AddListener(HitboxHitLand);
                }
                else
                {
                    juggernautHitbox.OnEnemyHitFireOnce.RemoveListener(HitboxHitLand);
                }
            }
            else
            {
                jugger_batk_activated = false;
                juggernautHitbox.damageType = DamageModifierType.SkillAttack;
            }

            //pull enemies slightly
            juggerPull = new GameObject("Flurry Magnet");
            juggerPull.transform.parent = tacklePull;
            juggerPull.transform.localPosition = Vector3.zero;
            Timing.RunCoroutine(JuggernautPulling().CancelWith(juggerPull));

            playedanticipationvoice = Random.Range(0, flurryVoice0.Length);
            _props.PlayVoice(flurryVoice0[playedanticipationvoice]);
        }
    }
    void PlayFlurryVoice()
    {
        if (playedanticipationvoice == 0) return;
        _props.PlayVoice(flurryVoice1);
    }
    IEnumerator<float> JuggernautPulling()
    {
        yield return Timing.WaitForSeconds(0.3f);

        juggernautPull = true;
        while (juggernautPull)
        {
            Collider[] victims = Physics.OverlapSphere(tacklePull.position, tackleRadius, layerMask);

            foreach (Collider c in victims)
            {
                if (c.CompareTag("enemy"))
                {
                    c.GetComponent<EnemyAI>().PullEffect(juggerPull.transform, 3, 6, 30);
                }
            }

            yield return Timing.WaitForSeconds(0.3f);
        }
    }
    void JuggernautSlash()
    {
        juggernautHitbox.ResetTimestop();
        juggernautHitbox.AttackWithCollider(out var result);

        totalEnemyHit = result.victims.Count;
        ManaRegen_CrossSlash();
    }
    void JuggernautFinalFx()
    {
        juggernautPull = false;
        Destroy(juggerPull.gameObject);

        if (ampedBuster)
            finalStrikeSuccessFx.Play();
    }
    void CommenceJuggernautBuster()
    {
        if (ampedBuster)
        {
            SetBuffDamage();
            ApplyCritical(GetFinalSkillDamage() + skills[6].Attribute(1) / 100, out float finalDmg, out bool critProc);
            ApplyBuffedDamage(ref finalDmg);

            GameObject buster = Instantiate(juggernautBuster, juggerPos.position, transform.rotation);
            DamageRequest req = new DamageRequest()
            {
                damage = finalDmg,
                isCritical = critProc,
                pierce = 1,
                accuracy = _status.FinalAccuracy,
                cdmg = _status.FinalCriticalDamage,
                damageSourceKey = skills[6].id,
                hero = Hero.Claris
            };

            buster.GetComponent<Chase_StaticField>().CommenceField(transform, req);

            CombatCamera.instance.EarthQuake();
        }

        ampedBuster = false;
        _health.EndSuperArmor();
        SkillUsed();
    }
    public SpawnFx[] juggerManabladeSpawns;
    void Juggernaut_Manablade(int idx)
    {
        if (!jugger_batk_activated) return;

        Vector3 bladepos = transform.position + transform.TransformDirection(juggerManabladeSpawns[idx].pos);

        if (manaBladeActivated)
        {
            Timing.RunCoroutine(ManaBladeSpawn(bladepos, idx, 0), EC2Constant.STATE_DEPENDENT);

            //followup wave
            if (isRageActivated)
                Timing.RunCoroutine(ManaBladeSpawn(bladepos, idx, 0.15f), EC2Constant.STATE_DEPENDENT);
        }
    }
    #endregion

    #region SKILL 7 - CROSS SLASH
    [Title("[Skill : 7] Cross Slash")]
    public AudioClip[] crossSlashVoice;
    public GameObject hackFx;
    public Transform hackPos;
    public HeroWeaponHitbox crossSlashColH, crossSlashColV;
    public GameObject hackHitFx;
    bool hackInputWindow, hackSuccess;
    float omniHackDmgMod;
    void ActivateHack()
    {
        int skillIndex = 7;
        float manaCost = skills[skillIndex].ManaCost(_status.manaReduction);
        float cooldown = skills[skillIndex].GetFinalCooldown(_status.FinalCDReduction);
        float affinityCost = skills[skillIndex].Attribute(5);

        bool succeed = false;
        if (CanUseRage(skillIndex, affinityCost, cooldown)) succeed = true;
        else if (CanUseSkill(skillIndex, manaCost, cooldown, false)) succeed = true;

        if (succeed)
        {
            affectBleeding = false;

            AddSkillExp(skillIndex);
            //StartIframe();
            SetMegaArmor(true);
            QuickRotate();

            //has omni rune
            SetTriggerAnimation("skill_Hack");
            _animator.ResetTrigger("skill_hack_ex");

            _props.PlayVoice(crossSlashVoice);
        }
    }
    private void OmniHackInputStart()
    {
        hackInputWindow = true;
        hackSuccess = false;
    }
    void OmniHackActionPress()
    {
        if (!ExActivated(7)) return;

        if (hackInputWindow)
        {
            hackSuccess = true;
            hackInputWindow = false;
        }
    }
    void OmniHackExecute()
    {
        hackInputWindow = false;

        if (hackSuccess)
        {
            isUsingSkill = true;
            DisableMove();

            QuickFreeRotate();

            swordskillFlash.Play();
            SetTriggerAnimation("skill_hack_ex");
        }
        else
        {
            SetMegaArmor(false);
        }
    }
    void CrossSlashHorizontalAttack()
    {
        CombatCamera.instance.LightShake();
        HackDamageEnemy(crossSlashColH);
    }
    void CrossSlashVerticalAttack()
    {
        CombatCamera.instance.MediumShake();
        HackDamageEnemy(crossSlashColV);
    }
    void CrossSlashFinalAttack()
    {
        CombatCamera.instance.HeavyShake();
        HackDamageEnemy(crossSlashColV, final: true);

        SetMegaArmor(false);
    }
    void HackDamageEnemy(HeroWeaponHitbox hitbox, bool final = false)
    {
        Collider[] victims = hitbox.GetVictimsInCollider();

        totalEnemyHit = victims.Length;
        if (totalEnemyHit > 0)
        {
            hitbox.PlayHitAudio();

            if (final)
                gm.TimeBreak(0.1f);
        }

        ManaRegen_CrossSlash();
        SetBuffDamage();

        float dmgmodifier = skills[7].Attribute(0) / 100;
        if (final) dmgmodifier = ExSkillValue(7) * ExSkillModifier(7) / 100;

        float breakVal = skills[7].Attribute(4) * (1 + (_status.speciality / 100));
        ApplyCritical(GetFinalSkillDamage() * dmgmodifier, out float finalDmg, out isCritical);
        ApplyBuffedDamage(ref finalDmg);

        DamageRequest req = new DamageRequest()
        {
            damage = finalDmg,
            isCritical = isCritical,
            knockback = hitbox.attackKnockback,
            pierce = _status.pierce,
            _break = breakVal,
            accuracy = _status.FinalAccuracy,
            cdmg = _status.FinalCriticalDamage,
            damageSourceKey = skills[7].id,
            hero = Hero.Claris
        };

        foreach (Collider c in victims)
        {
            if (c.CompareTag("enemy"))
            {
                EnemyHealth enemy = c.GetComponent<EnemyHealth>();
                enemy.TakeDamage(transform, req, out DamageResult result);

                if (!result.isMiss)
                {
                    if (req.isCritical)
                    {
                        GameManager.instance.vfxManager.Crit(transform.position, AttackType.slash);
                        ApplyDrain(finalDmg);
                    }

                    float bleedDuration = skills[7].Attribute(3);
                    //float skillDamage = _status.GetModifiedFinalDamage() + (_status.GetModifiedFinalDamage() * (_status.FinalSkillAtkDamage / 100f));
                    float bleedDamage = skills[7].Attribute(2) / 100 * GetFinalSkillDamage() * finalBleedPotency;

                    DamageRequest bleedReq = new DamageRequest()
                    {
                        statusEffect = StatusEffects.bleed,
                        statusEffectDuration = bleedDuration,
                        statusEffectDamage = bleedDamage,
                        damageSourceKey = "other",
                        hero = Hero.Claris
                    };

                    StatusEffectManager.instance.SetStatusEffect(c.transform, bleedReq);
                }

                if (hackHitFx) Instantiate(hackHitFx, c.transform.position + Vector3.up, Quaternion.identity);
            }
            if (c.CompareTag("Ore"))
            {
                c.GetComponent<GatheringPoint>().Hit();
                if (hackHitFx) Instantiate(hackHitFx, c.transform.position + Vector3.up, Quaternion.identity);
            }
        }
    }
    void CommenceHackAttack()
    {
        Instantiate(hackFx, hackPos.position, transform.rotation);
    }
    void ReduceCrossSlashCooldown()
    {
        if (ExActivated(7))
            ReduceSkillCooldownSecond(7, 0.3f);
    }

    protected override void SkillUsed()
    {
        base.SkillUsed();
        EndFinalSkill();
    }

    public override bool IsFreeSkillMode()
    {
        return isRageActivated;
    }
    public void EndFinalSkill()
    {
        if (isFinalSkill)
        {
            //disableExtraBasicAttack = false;
            isFinalSkill = false;
            extraCriticalOnce = 0;
            _status.extraSkillDmg = 0;
            _status.extraBasicDmg = 0;

            EndRage();
        }
    }
    #endregion
    #endregion

    [Title("[Special : Rage Gauge]")]
    public AudioClip[] rageActivationVoice;
    public ParticleSystem[] rageFx;
    public ParticleSystem rageStart, rageEnd;
    public float ragePoint, maxRage = 300;
    private float ragePointThreshold = 200, rageTimer;
    private bool omniBlade;
    private bool rageGaugeReady;
    public bool RageGaugeReady
    {
        get { return rageGaugeReady; }
        set
        {
            rageGaugeReady = value;
            OnRageStateChangeCall(_status.heroReference.hero, rageGaugeReady);
        }
    }

    void CheckRageGauge()
    {
        if (!isInited) return;
        UpdateRageBar();

        if (isRageActivated)
        {
            if (_status.statusBar)
                _status.statusBar.ClarisRageVfx(true);
            return;
        }

        if (ragePoint < ragePointThreshold)
        {
            if (_status.statusBar)
                _status.statusBar.ClarisRageVfx(false);
            return;
        }

        RageThresholdReached();
        CheckRageVFXReady();
    }
    void ActivateRageMode()
    {
        if (isRageActivated) return;
        if (ragePoint < ragePointThreshold) return;

        RageGaugeReady = false;
        SetRageVFX(true);
        if (rageStart) rageStart.Play();
        CombatCamera.instance.MediumShake();

        if (_status.statusBar)
            _status.statusBar.clarisRageFx.Play();

        _props.PlayStartRageSFX();
        isRageActivated = true;
        RefreshAllSkillCooldown();
        _health.StartSuperArmor();
        _status.extraCdr = relentlessCDR;
        //Debug.Log("CDR : " + _status.FinalCDReduction);
        //rage atk boost
        float baseAtkBoost = 0.3f * _status.damage;
        float specialityAmped = baseAtkBoost * (1 + _status.speciality / 100);
        _status.claris_rage_atkBoost = specialityAmped;
        //Debug.Log("base atk : " + _status.damage + " | resonance amp : " + specialityAmped + " | total : " + GetFinalSkillDamage());

        _props.PlayVoice(rageActivationVoice);

        if (MasteryUnlocked(5)) _status.heroStatusEffect.SetNotification(mastery[5].id, "");

        //Rune : OMNI
        if (_status.HasExRune(SocketType.Omni))
        {
            manaWaveDmg = GetFinalBasicDamage() * skills[3].Attribute(1) / 100;

            manaBladeActivated = true;
            manaBladetimer = 1;
            omniBlade = true;

            _status.heroStatusEffect.SetNotification(skills[3].id, "");
        }
    }
    void UpdateRage()
    {
        if (!isRageActivated)
            return;

        rageTimer += Time.deltaTime * 5;
        if (rageTimer >= 1)
        {
            rageTimer = 0;
            UseRagePoint(eclipseActive ? 0 : 1);
        }
    }
    public void EndRage()
    {
        EndRage(true, true);
    }
    public void EndRage(bool resetGauge, bool endSA)
    {
        if (!isRageActivated) return;
        if (resetGauge) ragePoint = 0;
        SetRageVFX(false);

        if (_status.statusBar)
            _status.statusBar.ClarisRageVfx(false);

        if (rageEnd) rageEnd.Play();

        isRageActivated = false;
        if (endSA) _health.EndSuperArmor();

        _status.extraCdr = 0;
        _status.claris_rage_atkBoost = 0;
        if (resetGauge) _props.PlayEndRageSFX();

        if (MasteryUnlocked(5)) _status.heroStatusEffect.RemoveNotification(mastery[5].id);

        omniBlade = false;
    }
    public void AddRagePoint(float amt)
    {
        if (isRageActivated) return;

        ragePoint += amt * (1 + (_status.speciality / 100));
        if (ragePoint > maxRage) ragePoint = maxRage;

        if (ragePoint >= ragePointThreshold)
        {
            RageThresholdReached();
        }
    }
    public void AddRagePointOverLimit(float amt)
    {
        if (isRageActivated) return;
        ragePoint = amt;
        ActivateRageMode();
    }
    bool UseRagePoint(float amt)
    {
        if (ragePoint <= 0) return false;

        bool isFinal = false;
        ragePoint -= amt;
        if (ragePoint <= 0)
        {
            if (amt > 1)
            {
                FinalRageSkill();
                ragePoint = 0;
                isFinal = true;
            }

            if (!isFinal)
            {
                EndRage();
            }
        }
        UpdateRageBar();

        return isFinal;
    }

    void CheckRageVFXReady()
    {
        bool isPause = false;
        if (GameManager.instance)
        {
            if (GameManager.instance.STATE == GameState.PAUSE)
                isPause = true;
        }

        if (RageGaugeReady && !isPause)
        {
            if (_status.statusBar)
                _status.statusBar.ClarisRageVfx(true);
        }
    }
    void RageThresholdReached()
    {
        if (!_status.currentlyActive) return;
        if (RageGaugeReady) return;
        RageGaugeReady = true;
        CombatCamera.instance.MediumShake();
        CheckRageVFXReady();
    }
    public override void DamageTaken(float amt, Transform source)
    {
        base.DamageTaken(amt, source);
        //for every 5% HP loss, Affinity is increased by 10
        float onePercent = _status.FinalMaxHP / 100 * 5;
        if (amt >= onePercent) AddRagePoint(10f);
        else AddRagePoint(2);
        UpdateRageBar();
    }
    public override void ManaGained(float amt)
    {
        AddRagePoint(amt / 2);
        UpdateRageBar();
    }
    void UpdateRageBar()
    {
        if (!EC2Utils.IsMine()) return;
        if (_status.statusBar)
        {
            _status.statusBar.SetGPText(Mathf.FloorToInt(ragePoint).ToString());
            _status.statusBar.UpdateGP(ragePoint / maxRage);
        }
    }
    void SetRageVFX(bool on)
    {
        for (int i = 0; i < rageFx.Length; i++)
        {
            if (on)
            {
                rageFx[i].gameObject.SetActive(true);
                rageFx[i].Play();
            }
            else
            {
                rageFx[i].Stop();
                rageFx[i].gameObject.SetActive(false);
            }
        }
    }

    bool CanUseRage(int skillIndex, float manaUsage, float cooldown)
    {
        if (!isRageActivated) return false;
        if (gm.STATE == GameState.PAUSE) return false;
        if (skills[skillIndex].disabledByEnsnare && _health.isEnsnared) return false;
        if (_health.GetFrozenState()) return false;
        if (_health.isSilenced) return false;
        if (isStrongPulled) return false;
        if (_status.combatStat == CombatStatus.OffCombat) return false;
        if (restrictSkill) return false;
        if (allSkillCooldown[skillIndex] > 0) return false;
        if (isUsingSkill) return false;

        isFinalSkill = UseRagePoint(manaUsage);

        //Debug.Log(skills[skillIndex].SkillName() + " as Final : " + isFinalSkill);

        isUsingSkill = justSkill = true;

        if (ApplyBurst())
        {
            allSkillCooldown[skillIndex] = 0.1f;
            skills[skillIndex].appliedCooldown = 0.1f;
        }
        else
        {
            allSkillCooldown[skillIndex] = cooldown;
            skills[skillIndex].appliedCooldown = cooldown;
        }
        ResetAttack();
        DisableMove();
        return true;
    }


    #region CLARIS MASTERIES
    public override void InitAllMasteries()
    {
        //init attributes by level
        for (int i = 0; i < mastery.Length; i++)
        {
            mastery[i].InitAttributes(MasteryLevel(i));
        }

        //init mastery data
        InitKnightsBravery();
        InitIronWill();
        InitSharpCut();
        InitInnerAffinity();
        InitInitiateStrike();
        InitRelentlessAssault();
        InitFortitude();
        InitTenacity();
    }
    protected override void BasicAttackCustomize()
    {
        if (isUsingSkill) return;

        if (MasteryUnlocked(2))
        {
            //Mastery - Sharp Cut effect
            //Basic attacks infict Bleeding at certain rate.
            //Duration  : 5 ticks.

            //apply bleed
            DamageRequest bleedReq = new DamageRequest()
            {
                statusEffect = StatusEffects.bleed,
                statusEffectDuration = 5,
                statusEffectDamage = (mastery[2].Attribute(2) / 100) * _status.GetModifiedFinalDamage(),
                damageSourceKey = "other",
                hero = Hero.Claris
            };

            //Debug.Log("apply bleed batk");
            ApplyBleeding(bleedChance, bleedReq);
        }
    }

    protected override void SetBuffDamage()
    {
        base.SetBuffDamage();
        //Sharp Cut
        if (MasteryUnlocked(2))
        {
            //Adds extra damage for basic attack, if enemy has bleed stacks
            int enemyBleedStack = 0;
            if (victim) victim.HasStatusEffect(StatusEffects.bleed, out enemyBleedStack);
            extraPctDamageOnce = (extraDamagePerStack / 100) * enemyBleedStack;
            //Debug.Log("Extra Sharp Cut : " + extraPctDamageOnce);
        }
    }
    [Title("[Mastery : 0] Knight's Bravery")]
    public float increaseToughness;
    public float increasedAttack;
    public int currentEnemiesDetected = -1;
    void InitKnightsBravery()
    {
        if (!MasteryUnlocked(0))
        {
            _status.claris_bravery_resBoost = 0;
            _status.claris_bravery_atkBoost = 0;
            //_status.heroStatusEffect.RemoveNotification(mastery[0].id);
            return;
        }
        increaseToughness = mastery[0].Attribute(0);
        increasedAttack = (mastery[0].Attribute(1) / 100f) * _status.damage;
    }
    void UpdateKnightsBravery(int enemies)
    {
        if (!AllowMastery) return;
        if (!MasteryUnlocked(0)) return;
        if (enemies == currentEnemiesDetected) return;

        //if (enemies > 5) enemies = 5;
        currentEnemiesDetected = enemies;

        if (enemies <= 2)
        {
            _status.claris_bravery_resBoost = 0;
            _status.claris_bravery_atkBoost = increasedAttack;
            //_status.heroStatusEffect.SetNotification(mastery[0].id, enemies.ToString());
        }
        else
        {
            _status.claris_bravery_resBoost = increaseToughness;
            _status.claris_bravery_atkBoost = 0;
            //_status.heroStatusEffect.SetNotification(mastery[0].id, enemies.ToString());
        }
    }

    [Title("[Mastery : 1] Iron Will")]
    public float increaseDamageReduction;
    void InitIronWill()
    {
        if (!MasteryUnlocked(1)) return;

        increaseDamageReduction = mastery[1].Attribute(0);
        _health.claris_ironwill = increaseDamageReduction;
    }

    [Title("[Mastery : 2] SharpCut")]
    public float bleedChance;
    public float extraDamagePerStack, finalBleedPotency;
    void InitSharpCut()
    {
        finalBleedPotency = 1;
        if (!MasteryUnlocked(2)) return;

        bleedChance = mastery[2].Attribute(0);
        extraDamagePerStack = mastery[2].Attribute(1);
        finalBleedPotency = 1 + (mastery[2].Attribute(2) / 100);
    }

    [Title("[Mastery : 3] Inner Affinity")]
    public float affinityThreshold;
    void InitInnerAffinity()
    {
        if (!MasteryUnlocked(3)) return;

        affinityThreshold = mastery[3].Attribute(0);
        ragePointThreshold = affinityThreshold;
    }
    void FinalRageSkill()
    {
        if (MasteryUnlocked(3))
        {
            extraCriticalOnce = 100;

            _status.extraSkillDmg = mastery[3].Attribute(1);
            _status.extraBasicDmg = mastery[3].Attribute(1);
        }
    }

    [Title("[Mastery : 4] Initiate")]
    //public int maxBoostedAttacks;
    public float initiateDmgIncrease;
    public float initiateBreakIncrease;
    void InitInitiateStrike()
    {
        if (!MasteryUnlocked(4)) return;

        initiateDmgIncrease = (mastery[4].Attribute(0) / 100f) * _status.damage;
        initiateBreakIncrease = mastery[4].Attribute(2);
        //maxBoostedAttacks = Mathf.RoundToInt(mastery[4].Attribute(1));
    }
    void ActivateInitiateBoost()
    {
        if (!AllowMastery) return;
        if (!MasteryUnlocked(4)) return;

        extraBreakFixed = initiateBreakIncrease;
        extraBasicAttackDamage = initiateDmgIncrease;
        initiate_counter = 7;

        //boostedBasicAttackCount = maxBoostedAttacks;
        _status.heroStatusEffect.SetNotification(mastery[4].id, initiate_counter.ToString());
    }
    private float initiate_timer, initiate_counter;
    private void UpdateInitiateTimer()
    {
        if (initiate_counter <= 0) return;

        initiate_timer += Time.deltaTime;
        if (initiate_timer >= 1)
        {
            initiate_timer = 0;

            initiate_counter--;
            if (initiate_counter <= 0)
            {
                extraBreakFixed = 0;
                extraBasicAttackDamage = 0;

                _status.heroStatusEffect.RemoveNotification(mastery[4].id);
            }
            else
            {
                _status.heroStatusEffect.SetNotification(mastery[4].id, initiate_counter.ToString());
            }
        }
    }
    protected override void UseBoostedBasicAttack()
    {
        /*
        if (!MasteryUnlocked(4)) return;

        boostedBasicAttackCount--;
        if (boostedBasicAttackCount <= 0)
        {
            extraBreakFixed = 0;
            extraBasicAttackDamage = 0;
            _status.heroStatusEffect.RemoveNotification(mastery[4].id);
        }
        else
        {
            _status.heroStatusEffect.SetNotification(mastery[4].id, boostedBasicAttackCount.ToString());
        }*/
    }

    //[Title("[Mastery : 5] Relentless Assault")]
    float relentlessCDR;
    void InitRelentlessAssault()
    {
        if (!MasteryUnlocked(5)) return;

        relentlessCDR = mastery[5].Attribute(0);
    }

    [Title("[Mastery : 6] Fortitude")]
    public float AffinityFill;
    public bool hasNegativeEffect;
    void InitFortitude()
    {
        if (!MasteryUnlocked(6))
        {
            _status.heroStatusEffect.RemoveNotification(mastery[6].id);
            return;
        }

        AffinityFill = mastery[6].Attribute(0);
    }
    void UpdateFortitude()
    {
        if (!AllowMastery) return;
        if (!MasteryUnlocked(6)) return;
        if (!hasNegativeEffect) return;
        if (ragePoint > maxRage) return;
        if (isRageActivated) return;

        ragePoint += Time.deltaTime * AffinityFill;
        UpdateRageBar();
    }
    public override void InflictNegativeEffects()
    {
        hasNegativeEffect = true;
        if (MasteryUnlocked(6)) _status.heroStatusEffect.SetNotification(mastery[6].id, "");
    }
    public override void EndNegativeEffects()
    {
        hasNegativeEffect = false;
        if (MasteryUnlocked(6)) _status.heroStatusEffect.RemoveNotification(mastery[6].id);
    }

    [Title("[Mastery : 7] Tenacity")]
    public float tenacityCooldown;
    public float tenacityBuffDuration;
    float tncTimer, tncBuffTimer;
    bool disableTenacityProc;
    void InitTenacity()
    {
        if (!MasteryUnlocked(7))
        {
            _health.healthCleanseTreshold = 0f;
            _status.claris_tenacity_resBoost = 0f;
            _status.heroStatusEffect.RemoveNotification(mastery[7].id + "-disabled");
            return;
        }

        _health.healthCleanseTreshold = mastery[7].Attribute(0);
        tenacityBuffDuration = mastery[7].Attribute(1);
        tenacityCooldown = mastery[7].Attribute(2);

        //_health.avoidDeathblow = true;
    }

    void UpdateTenacity()
    {
        if (!disableTenacityProc) return;
        if (!AllowMastery) return;
        if (!MasteryUnlocked(7)) return;
        //if (_health.avoidDeathblow) return;
        tncTimer -= Time.unscaledDeltaTime;

        if (tncTimer > 0f)
        {
            _status.heroStatusEffect.SetNotification(mastery[7].id + "-disabled", Mathf.CeilToInt(tncTimer).ToString());
        }
        else
        {
            disableTenacityProc = false;
            _status.heroStatusEffect.RemoveNotification(mastery[7].id + "-disabled");
        }
    }

    void UpdateTenacityBuffDuration()
    {
        if (!AllowMastery) return;
        if (!MasteryUnlocked(7)) return;
        //if (_health.avoidDeathblow) return;
        tncBuffTimer -= Time.unscaledDeltaTime;

        if (tncBuffTimer <= 0f)
        {
            _status.claris_tenacity_resBoost = 0f;
        }
    }
    #endregion


    //Blood Eclipse Set
    [Title("Blood Eclipse Set")]
    public ParticleSystem totalEclipseBuff;
    private float eclipseTimer;
    private float eclipseDuration = 20;
    private bool eclipseActive;
    public void UpdateTotalEclipse()
    {
        if (_status.costumeEffect.eclipseValue <= 0)
        {
            if (eclipseActive)
            {
                eclipseActive = false;
                eclipseDuration = 20;
                _status.heroStatusEffect.RemoveNotification("blood_eclipse");
                _status.heroStatusEffect.RemoveNotification("blood_eclipse_cd");
            }

            return;
        }
        //if (gm.detectedEnemies <= 0)
        //    return;


        if (eclipseActive)
        {
            eclipseTimer += Time.deltaTime;
            if (eclipseTimer > 1)
            {
                eclipseTimer = 0;

                //regen 25mp / 5ap per second
                AddRagePoint(10); UpdateRageBar();
                _status.AddManaValue(30, true, false, false, false);

                //reduce eclipse duration 1sec
                eclipseDuration -= 1;
                if (eclipseDuration <= 0)
                {
                    eclipseActive = false;
                    eclipseDuration = 20;

                    _status.heroStatusEffect.RemoveNotification("blood_eclipse");
                    totalEclipseBuff.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    _status.heroStatusEffect.SetNotification("blood_eclipse_cd", eclipseDuration.ToString("F0"));
                }
                else
                {
                    _status.heroStatusEffect.SetNotification("blood_eclipse", eclipseDuration.ToString("F0"));
                }
            }
        }
        else
        {
            eclipseTimer += Time.deltaTime;
            if (eclipseTimer > 1)
            {
                eclipseTimer = 0;

                //reduce eclipse duration 1sec
                eclipseDuration -= 1;
                if (eclipseDuration <= 0)
                {
                    eclipseActive = true;
                    eclipseDuration = 10;

                    _status.heroStatusEffect.RemoveNotification("blood_eclipse_cd");
                    totalEclipseBuff.Play();
                    _status.heroStatusEffect.SetNotification("blood_eclipse", eclipseDuration.ToString("F0"));
                }
                else
                {
                    _status.heroStatusEffect.SetNotification("blood_eclipse_cd", eclipseDuration.ToString("F0"));
                }
            }
        }
    }


    public override void TakeDirectDamage()
    {
        base.TakeDirectDamage();
    }

    public override void CleanseDebuff()
    {
        if (disableTenacityProc) return;
        if (!MasteryUnlocked(7)) return;

        _health.RemoveAllStatusEffect(true);
        disableTenacityProc = true;
        _status.claris_tenacity_resBoost = 50;

        tncTimer = tenacityCooldown;
        tncBuffTimer = tenacityBuffDuration;
    }

    public override void GoToSafeArea()
    {
        base.GoToSafeArea();

        if (isRageActivated)
        {
            isRageActivated = false;
            SetRageVFX(false);
            if (_status)
            {
                _status.extraCdr = 0;
                _status.claris_rage_atkBoost = 0;
                if (MasteryUnlocked(5)) _status.heroStatusEffect.RemoveNotification(mastery[5].id);
            }

            if (_health) _health.EndSuperArmor();
        }
        if (isInited) CheckRageGauge();
    }

    public override void Die()
    {
        base.Die();

        ampedBuster = false;
        if (leapLimiterSpawned) Destroy(leapLimiterSpawned);

        if (isRageActivated)
            EndRage(false, true);
    }

    private void OnDestroy()
    {
        SaveCurrentRagePoint();
    }

    private void SaveCurrentRagePoint()
    {
        PlayerPrefs.SetFloat(EC2Utils.GetCurrentProfilePrefs("ClarisRage"), ragePoint);
    }

    public void HitboxHitLand()
    {
        ReduceCrossSlashCooldown();
    }
}
