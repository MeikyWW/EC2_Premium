using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using CodeStage.AntiCheat.ObscuredTypes;
using Sirenix.OdinInspector;
using System.Linq;

public enum CombatStatus
{
    OnCombat, OffCombat
}

public class HeroStatus : MonoBehaviour
{
    //======= PUBLIC FIELDS =========//
    [HideInInspector] public CombatStatus combatStat;
    public HeroReference heroReference;
    public Transform potionFxPos;
    [HideInEditorMode] public bool currentlyActive;

    [HideInInspector] public Animator animator;
    [HideInInspector] public FishingTools fishingTools;

    #region Properties
    public ObscuredInt Level
    {
        get
        {
            if (heroData == null) ReferenceInit();
            return heroData.currentLevel;
        }
    }
    public ObscuredInt Exp
    {
        get
        {
            if (heroData == null) ReferenceInit();
            return heroData.currentEXP;
        }
    }
    public ObscuredInt MaxExp { get; private set; }
    public ObscuredFloat HpPercent
    {
        get => heroHealth.GetHpPercent();
    }
    public ObscuredFloat CurrentHp
    {
        get => heroHealth.hp;
    }


    [HideInInspector] public float amy_crit_charge;
    [HideInInspector] public float consum_crit_rate_up;
    [HideInInspector] public float louisa_composure;
    public ObscuredFloat FinalCriticalRate
    {
        get
        {
            float totalCrit = critical;

            float total = heroData.GetRawValue(EC2.Stats.Crit, totalCrit * 10);

            total += amy_crit_charge;
            total += consum_crit_rate_up * 10;
            total += louisa_composure;

            totalCrit = heroData.ConvertedValue(EC2.Stats.Crit, total);

            return totalCrit;
        }
    }

    public ObscuredFloat FinalCriticalDamage
    {
        get
        {
            float totalCdmg = critDmg; //13.5

            if (socketEffect.bravery.count > 0)
            {
                if (HpPercent * 100f < socketEffect.bravery.GetValue(1))
                {
                    float total = heroData.GetRawValue(EC2.Stats.CritDamage, totalCdmg * 10); // 135

                    total += socketEffect.bravery.GetValue(0) * 10f; // 135 + 50 = 185

                    totalCdmg = heroData.ConvertedValue(EC2.Stats.CritDamage, total); // 18.5
                }
            }

            return totalCdmg;
        }
    }

    [HideInInspector] public bool isWindActive;
    [HideInInspector] public float attackSpeedAlter = 1f; // 0~1
    [HideInInspector] public float amy_field_aspd;
    [HideInInspector] public float swiftFire_aspd;
    [HideInInspector] public float gemini_aspd;
    [HideInInspector] public float louisa_countereva_aspd;
    [HideInInspector] public float santa_rush;
    [HideInInspector] public float elze_fervor_aspd, elze_fervor_batk, abyssal_windveil_aspd;
    public ObscuredFloat FinalAttackSpeed
    {
        get
        {
            float total = atkSpeed;
            float temp = heroData.GetRawValue(EC2.Stats.AttackSpeed, total * 10);

            if (isWindActive)
            {
                temp += socketEffect.wind.GetValue(0) * 10f;
            }

            temp += swiftFire_aspd * 10;

            temp += amy_field_aspd * 10;

            temp += gemini_aspd * 10;

            temp += louisa_countereva_aspd * 10;

            temp += santa_rush * 10;

            temp += elze_fervor_aspd * 10;

            temp += abyssal_windveil_aspd * 10;

            total = heroData.ConvertedValue(EC2.Stats.AttackSpeed, temp);
            return total;
        }
    }

    [HideInInspector] public List<Amy_PassionField> passionFields;
    [HideInInspector] public List<Elze_Rosefield> roseFields;
    [HideInInspector] public List<Louisa_MoraleBoost> moraleFields;
    [HideInInspector] public List<Alaster_ProtectionField> protectionFields;
    [HideInInspector] public List<Edna_FieryField> fieryFields;
    [HideInInspector] public float moveSpeedAlter = 1f;
    [HideInInspector] public float amy_field_mspd;
    [HideInInspector] public float louisa_nimble_mspd;
    public ObscuredFloat FinalMoveSpeed
    {
        get
        {
            float total = control.baseMovSpd;

            if (isWindActive)
            {
                total += (total * socketEffect.wind.GetValue(0) / 100f);
            }

            total += amy_field_mspd;

            total += louisa_nimble_mspd;

            if (heroHealth)
            {
                if (heroHealth.isPoisoned)
                    total *= heroHealth.poisonSlowValue;

                if (heroHealth.isEnsnared)
                    total *= heroHealth.ensnareValue;

                if (heroHealth.isSlowed)
                    total *= heroHealth.slowedValue;

                if (heroHealth.isSnowSlowed)
                    total *= heroHealth.snowSlowedValue;
            }


            total *= moveSpeedAlter;
            return total;
        }
    }

    public ObscuredFloat MpPercent
    {
        get => mana / FinalMaxMP;
    }
    public Dictionary<EquipSet, int> setCategories = new Dictionary<EquipSet, int>();

    public Dictionary<CostumeSet, int> costumeSets = new Dictionary<CostumeSet, int>();
    #endregion

    [FoldoutGroup("Properties")]
    public ObscuredFloat damage, atkSpeed, critical, critDmg, pierce, armorBreak,
        health, defense, recovery, elementalResistance, physicalResistance, evasion,
        manaReduction, cooldownReduction, speciality, dropRateMod,
        basicAtkDamage, skillAtkDamage, accuracy, spRegen, consumablePlus, manaGain;
    [FoldoutGroup("Properties")]
    public ObscuredFloat moveSpeed = 10;

    [HideInInspector] public float amy_wolf_fang, amy_fighter_fury, louisa_morale_batk, alaster_ebex_batk;
    [HideInInspector] public float extraBasicDmg;
    [HideInInspector] public float cot_batk_mod;
    public ObscuredFloat FinalBasicAtkDamage
    {
        get
        {
            float temp = basicAtkDamage;

            float total = heroData.GetRawValue(EC2.Stats.BasicAtkDamage, temp * 10); // 135

            if (vengeanceStack > 0)
            {
                total += vengeance_skill_bonus * 10f; // 135 + 50 = 185
            }

            total += amy_wolf_fang * 10f;
            total += amy_fighter_fury * 10f;
            total += louisa_morale_batk * 10;
            total += alaster_ebex_batk * 10;
            total += elze_fervor_batk * 10;
            total += extraBasicDmg * 10;
            total += cot_batk_mod * 10;

            temp = heroData.ConvertedValue(EC2.Stats.BasicAtkDamage, total); // 18.5
            return temp;
        }
    }

    [HideInInspector] public float vengeance_skill_bonus; // in percent 5 -> 5%
    [HideInInspector] public float extraSkillDmg; // in percent 5 -> 5%
    [HideInInspector] public float maidSkillDmg; // in percent 5 -> 5%
    [HideInInspector] public float fieryField_skillDmg; // in percent 5 -> 5%
    [HideInInspector] public float zerav_violentreap_satk; // in percent 5 -> 5%
    [HideInInspector] public float cot_satk_mod; // in percent 5 -> 5%
    public ObscuredFloat FinalSkillAtkDamage
    {
        get
        {
            float temp = skillAtkDamage;

            float total = heroData.GetRawValue(EC2.Stats.SkillAtkDamage, temp * 10); // 135

            if (vengeanceStack > 0)
                total += vengeance_skill_bonus * 10f; // 135 + 50 = 185

            if (extraSkillDmg > 0)
                total += extraSkillDmg * 10f;

            if (maidSkillDmg > 0)
                total += maidSkillDmg * 10f;

            if (fieryField_skillDmg > 0)
                total += fieryField_skillDmg * 10f;

            if (zerav_violentreap_satk > 0)
                total += zerav_violentreap_satk * 10f;

            total += cot_satk_mod * 10f;

            temp = heroData.ConvertedValue(EC2.Stats.SkillAtkDamage, total); // 18.5

            return temp;
        }
    }

    [HideInInspector] public float blindDebuff, amy_pippi_talon;
    public ObscuredFloat FinalAccuracy
    {
        get
        {
            float total = accuracy;
            float temp = heroData.GetRawValue(EC2.Stats.Accuracy, total * 10);

            temp -= blindDebuff * 10;
            temp += amy_pippi_talon * 10;

            total = heroData.ConvertedValue(EC2.Stats.Accuracy, temp);
            return total;
        }
    }

    public ObscuredFloat FinalSPRegen
    {
        get
        {
            return spRegen;
        }
    }

    public ObscuredFloat FinalConsumablePlus
    {
        get
        {
            return consumablePlus;
        }
    }

    [HideInInspector] public float edna_bonusManaGain;
    [HideInInspector] public float fieryField_bonusManaGain;
    [HideInInspector] public float elze_manasurge;
    public ObscuredFloat FinalManaGain
    {
        get
        {
            float total = heroData.GetRawValue(EC2.Stats.ManaGain, manaGain * 10)
                + edna_bonusManaGain * 10
                + fieryField_bonusManaGain * 10
                + elze_manasurge * 10
                + zerav_warcry * 10;

            float final = heroData.ConvertedValue(EC2.Stats.ManaGain, total);
            return final;
        }
    }

    [HideInInspector] public int armorDown_stack;
    [HideInInspector] public float tester_extraDefense;
    public ObscuredFloat FinalDefense
    {
        get
        {
            float total = defense;

            if (socketEffect.defense.count > 0)
            {
                total += (socketEffect.defense.GetValue(0) / 100f) * defense;
            }

            total += tester_extraDefense;

            total -= armorDown_stack * EC2Constant.REDUCTION_ARMOR_PER_STACK;

            return total;
        }
    }

    [HideInInspector] public float vit_burst_bonus;
    [HideInInspector] public float tester_extraHp;
    public ObscuredFloat FinalMaxHP
    {
        get
        {
            float total = health;

            if (socketEffect.maxHP.count > 0)
            {
                total += (socketEffect.maxHP.GetValue(0) / 100f) * health;
            }

            if (vit_burst_bonus > 0f)
            {
                total += total * (vit_burst_bonus / 100);
            }

            total += tester_extraHp;

            return total;
        }
    }

    [HideInInspector] public float edna_bonusMaxMP;
    [HideInInspector] public float edna_intMultiplierAsBonus;
    [HideInInspector] public float zerav_siphonBonus;
    public ObscuredFloat FinalMaxMP
    {
        get
        {
            float total = maxMana;

            if (socketEffect.maxMP.count > 0)
            {
                total += socketEffect.maxMP.GetValue(0);
            }

            total += edna_bonusMaxMP;
            total += zerav_siphonBonus;

            try
            {
                var INT = heroData.currentINT + heroData.currentLevel - 1;
                total += edna_intMultiplierAsBonus * INT;
            }
            catch
            {
            }

            return Mathf.Round(total);
        }
    }
    public ObscuredFloat Zerav_FinalMpBeforeSiphon
    {
        get
        {
            float total = maxMana;

            if (socketEffect.maxMP.count > 0)
            {
                total += socketEffect.maxMP.GetValue(0);
            }
            return Mathf.Round(total);
        }
    }
    #region SPECIAL PROPERTIES

    //Hero's modified damage. Includes basic damage
    //and added damage from possible buffs
    public float GetModifiedFinalDamage()
    {
        float finalDamage = damage
                + consum_atk_boost
                + claris_bravery_atkBoost
                + claris_rage_atkBoost
                + chase_counter_eva_atkBonus
                + amy_encouragement_atkBoost;

        if (socketEffect.attack.count > 0)
        {
            finalDamage += (socketEffect.attack.GetValue(0) / 100f) * damage;
        }

        if (cot_truelove_active)
            finalDamage *= cot_truelove_atkMod;

        return finalDamage;
    }
    [HideInInspector] public float consum_atk_boost;    //Attack Boost from consumables
    [HideInInspector] public float claris_bravery_atkBoost;    //Claris mastery : Knight's Bravery
    [HideInInspector] public float claris_rage_atkBoost; //Soul Resonance : 30% bonus dmg + speciality%
    [HideInInspector] public float chase_counter_eva_atkBonus; //Counter Evasion
    [HideInInspector] public float amy_encouragement_atkBoost;
    [HideInInspector] public float cot_truelove_atkMod;
    [HideInInspector] public bool cot_truelove_active;
    [HideInInspector] public float alaster_guardExpert, louisa_morale_pres;
    [HideInInspector] public float zerav_warcry;
    public ObscuredFloat FinalPhysicalResistance
    {
        get
        {
            float total = heroData.GetRawValue(EC2.Stats.PhysicalResistance, physicalResistance * 10)
                + consum_phyres_boost * 10
                + claris_bravery_resBoost * 10
                + claris_tenacity_resBoost * 10
                + alaster_guardExpert * 10
                + louisa_morale_pres * 10
                + zerav_warcry * 10;

            if (socketEffect.toughness.count > 0)
            {
                if (control.IsUsingItem())
                    total += socketEffect.toughness.GetValue(0) * 10f;
            }
            float final = heroData.ConvertedValue(EC2.Stats.PhysicalResistance, total);
            return final;
        }
    }
    [HideInInspector] public float consum_phyres_boost;      //Toughness Pills
    [HideInInspector] public float claris_bravery_resBoost;    //Claris mastery : Knight's Bravery
    [HideInInspector] public float claris_tenacity_resBoost;    //Claris mastery : Tenacity
    [HideInInspector] public float elze_frostbloom_res;     //Elze's Frostbloom Plague [Trans]

    public ObscuredFloat FinalElementalResistance
    {
        get
        {
            float total = heroData.GetRawValue(EC2.Stats.ElementalResistance, elementalResistance * 10)
                + consum_eleres_boost * 10
                + claris_tenacity_resBoost * 10
                + elze_frostbloom_res * 10;

            float final = heroData.ConvertedValue(EC2.Stats.ElementalResistance, total);
            return final;
        }
    }
    [HideInInspector] public float consum_eleres_boost;      //Toughness Pills

    public ObscuredFloat FinalCDReduction
    {
        get
        {
            float total = heroData.GetRawValue(EC2.Stats.CooldownReduce, cooldownReduction * 10)
                + extraCdr * 10
                - exhaustDebuffCDR * 10;
            float final = heroData.ConvertedValue(EC2.Stats.CooldownReduce, total);
            return final;
        }
    }
    [HideInInspector] public float extraCdr;
    [HideInInspector] public float exhaustDebuffCDR;
    public ObscuredFloat FinalMpCostReduction
    {
        get
        {
            float total = heroData.GetRawValue(EC2.Stats.ManaReduce, manaReduction * 10) + chase_overdrive_mpred * 10;
            float final = heroData.ConvertedValue(EC2.Stats.ManaReduce, total);
            return final;
        }
    }


    public ObscuredFloat AddedTempFinalMpCostReduction(float value)
    {
        float total = heroData.GetRawValue(EC2.Stats.ManaReduce, manaReduction * 10) + chase_overdrive_mpred * 10 + value * 10;
        float final = heroData.ConvertedValue(EC2.Stats.ManaReduce, total);
        return final;
    }

    [HideInInspector] public float chase_overdrive_mpred;   //Chase mastery : Efficiency



    [HideInInspector] public HeroSetEffect setEffect;
    public ObscuredFloat ValorDamageAmplifier
    {
        get
        {
            return CurrentHp < FinalMaxHP * 80 / 100 ? 0 : setEffect.valorDamageAmplifier;
        }
    }
    // ----------SOCKET------------ //
    [HideInInspector] public HeroSocketEffect socketEffect;
    public ObscuredFloat RuneAtkDamageAmplifier
    {
        get
        {
            if (socketEffect.attack.count > 0)
                return socketEffect.attack.values[0];
            else
                return 0f;
        }
    }

    // Costume Set
    [HideInInspector] public HeroCostumeSetEffect costumeEffect;
    #endregion

    [HideInInspector] public float counterEvasion;    //Chase mastery Counter Evasion
    public ObscuredFloat FinalEvasion
    {
        get
        {
            float total = heroData.GetRawValue(EC2.Stats.Evasion, evasion * 10) + counterEvasion * 10;
            float finalEva = heroData.ConvertedValue(EC2.Stats.Evasion, total);
            return finalEva;
        }
    }

    //================= REFERENCES =================//
    [HideInInspector] public HeroControl control;
    [HideInInspector] public HeroHealth heroHealth;
    [HideInInspector] public HeroProps heroProps;
    [HideInInspector] public CostumeChanger costumeChanger;
    GameManager gm;
    [HideInEditorMode] public HeroStatusBar statusBar;
    [HideInInspector] public HeroStatusEffect heroStatusEffect;
    [HideInInspector] public HeroSaveData heroData;

    //================= COMBAT STATUS =================//
    [Title("Basic Attacks")]
    public float[] basicAtkMV;
    public ManaRegenInfo[] basicAtkRegen;
    public float[] basicAtkBreak;

    [HideInInspector] public bool initialized;

    #region SUBSCRIBTIONS 
    private void OnEnable()
    {
        PartyManager.OnHeroSwitch += UpdateManaBar;
        PartyManager.OnHeroSwitch += UpdateExpBar;
        PartyManager.OnHeroSwitch += ReinitStatusbar;

        GameManager.OnPartyChanged += UpdateManaBar;
        GameManager.OnPartyChanged += ReinitStatusbar;
        GameManager.OnPartyChanged += UpdateExpBar;
    }

    private void OnDisable()
    {
        PartyManager.OnHeroSwitch -= UpdateManaBar;
        PartyManager.OnHeroSwitch -= UpdateExpBar;
        PartyManager.OnHeroSwitch -= ReinitStatusbar;

        GameManager.OnPartyChanged -= UpdateManaBar;
        GameManager.OnPartyChanged -= ReinitStatusbar;
        GameManager.OnPartyChanged -= UpdateExpBar;
    }
    #endregion

    public bool IsMine()
    {
        return EC2Utils.IsMine();
    }
    private void Awake()
    {
        passionFields = new List<Amy_PassionField>();
        protectionFields = new List<Alaster_ProtectionField>();
    }
    private void Start()
    {
        Initialize();
    }
    private void Update()
    {
        if (!initialized) return;

        UpdateRegeneration();
        UpdateSpecialityRecovery();
        UpdateSantaRush();
        //UpdateTropicalSquashCD();
        UpdateTropicalSquash();
        /*
        if (Input.GetKeyDown(KeyCode.F5))
        {
            AddManaPercent(100);
            control.RefreshAllSkillCooldown();
        }*/
    }
    public void Initialize()
    {
        if (initialized) return;
        //init references
        ReferenceInit();
        if (!IsMine())
        {
            initialized = true;
            return;
        }

        CalculateHeroAttributes();

        //set status bar
        //if(gm.userData.data.heroesInCharge.Contains(heroReference.hero))
        //{
        if (statusBar)
            statusBar.SetLevel(heroData.currentLevel);

        MaxExp = CalculateMaxExp();
        UpdateExpBar();
        heroHealth.RefreshHealthBar();
        mana = FinalMaxMP;
        UpdateManaBar();
        //}

        attackSpeedAlter = 1f; moveSpeedAlter = 1f;
        Timing.RunCoroutine(LateInit().CancelWith(gameObject));
        initialized = true;
    }
    int CalculateMaxExp()
    {
        return gm.dataSheet.GetMaxExp(heroData.currentLevel);
    }

    [HideInInspector] public bool isReferenceInited;
    private void ReferenceInit()
    {
        if (isReferenceInited) return;
        control = GetComponent<HeroControl>();
        costumeChanger = GetComponent<CostumeChanger>();
        heroHealth = GetComponent<HeroHealth>();
        heroProps = GetComponent<HeroProps>();
        animator = GetComponent<Animator>();
        fishingTools = GetComponent<FishingTools>();
        heroStatusEffect = GetComponent<HeroStatusEffect>();

        gm = GameManager.instance;

        //load hero attributes from savedata
        heroData = GetComponent<HeroSaveData>();

        isReferenceInited = true;
    }

    IEnumerator<float> LateInit()
    {
        yield return Timing.WaitForOneFrame;
        if (gameObject.activeSelf)
            control.RefreshAllSkillCooldown();
    }
    public void CalculateHeroAttributes()
    {
        if (!IsMine()) return;
        HeroAttributes allStats = heroData.CombinedStatus();
        HeroAttributes unlockedStatsCosu = heroData.GetCosuUnlockedStat();
        HeroAttributes myBuffs = gm.GetCalculatedDeveloperBuffs(out _, out _);
        HeroAttributes feastBuff = gm.GetFoodBuff();

        allStats.Merge(myBuffs);
        allStats.Merge(feastBuff);

        setEffect = heroData.GetEquipmentSetEffect(out HeroAttributes statsModifier);
        costumeEffect = heroData.GetCostumeSetEffect(out HeroAttributes costumeModifier);
        socketEffect = heroData.GetEquipmentSocketEffect();
        damage = allStats.Attack + statsModifier.Attack + gm.additionalDamage;
        //buff
        damage += damage * allStats.AttackPercentage / 1000f;

        atkSpeed = heroData.ConvertedValue(EC2.Stats.AttackSpeed, allStats.AttackSpeed + statsModifier.AttackSpeed + costumeModifier.AttackSpeed + unlockedStatsCosu.AttackSpeed);
        critical = heroData.ConvertedValue(EC2.Stats.Crit, allStats.Crit + statsModifier.Crit + costumeModifier.Crit + unlockedStatsCosu.Crit);
        critDmg = heroData.ConvertedValue(EC2.Stats.CritDamage, allStats.CritDamage + statsModifier.CritDamage + costumeModifier.CritDamage + unlockedStatsCosu.CritDamage);

        armorBreak = 0;
        pierce = 0; // heroData.ConvertedValue(Stats.Pierce, allStats.Pierce + statsModifier.Pierce);
        var healthPercentage = heroData.ConvertedValue(EC2.Stats.HealthPercentage, allStats.HealthPercentage + statsModifier.HealthPercentage + costumeModifier.HealthPercentage + unlockedStatsCosu.HealthPercentage);
        health = allStats.MaxHP;
        health += health * (healthPercentage / 100);
        defense = allStats.Defense + gm.additionaDefense + statsModifier.Defense + costumeModifier.Defense + unlockedStatsCosu.Defense;
        //buff
        defense += defense * allStats.DefensePercentage / 1000f;

        recovery = heroData.ConvertedValue(EC2.Stats.Recovery, allStats.Recovery + statsModifier.Recovery + costumeModifier.Recovery + unlockedStatsCosu.Recovery);
        elementalResistance = heroData.ConvertedValue(EC2.Stats.ElementalResistance, allStats.ElementalResistance + statsModifier.ElementalResistance + costumeModifier.ElementalResistance + unlockedStatsCosu.ElementalResistance) + gm.additionalResistance;
        physicalResistance = heroData.ConvertedValue(EC2.Stats.PhysicalResistance, allStats.PhysicalResistance + statsModifier.PhysicalResistance + costumeModifier.PhysicalResistance + unlockedStatsCosu.PhysicalResistance) + gm.additionalResistance;
        evasion = heroData.ConvertedValue(EC2.Stats.Evasion, allStats.Evasion + statsModifier.Evasion + costumeModifier.Evasion + unlockedStatsCosu.Evasion);

        maxMana = allStats.MaxMP + statsModifier.MaxMP + costumeModifier.MaxMP + unlockedStatsCosu.MaxMP;
        manaReduction = gm.additionalSkillPurpose + heroData.ConvertedValue(EC2.Stats.ManaReduce, allStats.ManaReduce + statsModifier.ManaReduce + costumeModifier.ManaReduce + unlockedStatsCosu.ManaReduce);
        cooldownReduction = gm.additionalSkillPurpose + heroData.ConvertedValue(EC2.Stats.CooldownReduce, allStats.CooldownReduce + statsModifier.CooldownReduce + costumeModifier.CooldownReduce + unlockedStatsCosu.CooldownReduce);
        speciality = heroData.ConvertedValue(EC2.Stats.Speciality, allStats.Speciality + statsModifier.Speciality + costumeModifier.Speciality + unlockedStatsCosu.Speciality);
        dropRateMod = 0; // heroData.ConvertedValue(Stats.DropRatePlus, allStats.DropRatePlus + statsModifier.DropRatePlus);

        basicAtkDamage = heroData.ConvertedValue(EC2.Stats.BasicAtkDamage, allStats.BasicAtkDamage + statsModifier.BasicAtkDamage + costumeModifier.BasicAtkDamage + unlockedStatsCosu.BasicAtkDamage);
        skillAtkDamage = heroData.ConvertedValue(EC2.Stats.SkillAtkDamage, allStats.SkillAtkDamage + statsModifier.SkillAtkDamage + costumeModifier.SkillAtkDamage + unlockedStatsCosu.SkillAtkDamage);
        accuracy = heroData.ConvertedValue(EC2.Stats.Accuracy, allStats.Accuracy + statsModifier.Accuracy + costumeModifier.Accuracy + unlockedStatsCosu.Accuracy);
        manaGain = heroData.ConvertedValue(EC2.Stats.ManaGain, allStats.ManaGain + statsModifier.ManaGain + costumeModifier.ManaGain + unlockedStatsCosu.ManaGain);
        spRegen = heroData.ConvertedValue(EC2.Stats.SPRegen, allStats.SPRegen + statsModifier.SPRegen + costumeModifier.SPRegen + unlockedStatsCosu.SPRegen);
        consumablePlus = heroData.ConvertedValue(EC2.Stats.ConsumablePlus, allStats.ConsumablePlus + statsModifier.ConsumablePlus + costumeModifier.ConsumablePlus + unlockedStatsCosu.ConsumablePlus);

        heroHealth.RefreshHealthBar();
        control.InitAttackSpeed();
        control.InitAttributes();

        //heroData.ApplyAuraWeapon();
        //setup semua temporary stat

        SetupRuneEffect();
        SetSPGaugeRecoveryThreshold();
    }

    public void SetupRuneEffect()
    {
        if (heroStatusEffect == null) return;

        heroStatusEffect.RemoveNotification("icon_rune_" + SocketType.Tranquility.ToString().ToLower());
        heroStatusEffect.RemoveNotification("icon_rune_" + SocketType.Protection.ToString().ToLower());
        heroStatusEffect.RemoveNotification("icon_rune_" + SocketType.Bravery.ToString().ToLower());
        heroStatusEffect.RemoveNotification("setEffect_" + EquipSet.Demonic.ToString().ToLower());

        ForceDisableProtection();
        ForceDisableToughness();

        if (maidSkillDmg > 0f)
        {
            if (costumeEffect.maidValue < 1f)
            {
                control.maidPerfectReady = false;
                heroStatusEffect.RemoveNotification("costume_" + CostumeSet.Maid.ToString().ToLower() + "_0");
                heroStatusEffect.RemoveNotification("costume_" + CostumeSet.Maid.ToString().ToLower() + "_1");
            }
        }
    }

    //================= STATUS BAR =================//
    public void ReinitStatusbar()
    {
        ReferenceInit();
        if (!IsMine()) return;
        //if(gm.IsHeroInCharge(heroReference.hero))
        if (statusBar)
        {
            statusBar.Init(this);
            statusBar.SetLevel(heroData.currentLevel);
        }
    }

    //================= MANA SYSTEM =================//
    [HideInInspector] public float maxMana = 200, mana = 0;
    float tempRegenBoost = 0;

    public bool HasEnoughMana(float manaUsage)
    {
        if (control.IsFreeSkillMode()) return true;

        manaUsage *= 1 - (alaster_wok_mpred);

        if (mana - manaUsage < 0)
        {
            return false;
        }
        else
        {
            mana -= manaUsage;
            UpdateManaBar();
            return true;
        }
    }

    [HideInInspector]
    public float alaster_wok_mpred;
    public bool CheckMana(float manaUsage)
    {
        manaUsage *= 1 - (alaster_wok_mpred);

        if (control.IsFreeSkillMode()) manaUsage = 0;
        return mana > manaUsage;
    }

    public float GetMpPercent()
    {
        return mana / FinalMaxMP; // 1/100 -> 0.01
    }
    public float entropyExtraMp;
    public void AddManaValue(float val)
    {
        AddManaValue(val, false, false, true, true);
    }
    public void AddManaValue(float val, bool isPotion)
    {
        AddManaValue(val, isPotion, true, true, true);
    }
    public void AddManaValue(float val, bool isPotion, bool showgui, bool showVFX, bool affectedByMPGain)
    {
        val += entropyExtraMp;

        float manaIncrease = (FinalManaGain + tempRegenBoost) / 100 * val;
        float finalVal = affectedByMPGain ? val + manaIncrease : val;

        if (finalVal + mana >= FinalMaxMP)
            finalVal = FinalMaxMP - mana;

        if (!isPotion)
        {
            //mana regen by hitting enemies. Used to fill character's special gauge
            control.ManaGained(val);
        }
        else
        {
            //mana regen by potion. show mana regen gui
            if (showVFX) GameManager.instance.vfxManager.PotionMP(potionFxPos, 0);
        }

        if (showgui)
        {
            finalVal = Mathf.RoundToInt(finalVal);
            if (finalVal <= 0) return;
            HudManager.instance.PopHealMp(transform, 1, finalVal);
        }

        mana += finalVal;
        if (mana >= FinalMaxMP) mana = FinalMaxMP;
        UpdateManaBar();
    }
    public void AddManaPercent(float pct)
    {
        float amount = pct / 100 * FinalMaxMP;
        AddManaValue(amount);
    }
    public void TemporaryManaIncrease(float pct)
    {
        tempRegenBoost = pct;
    }

    public System.Action OnUpdatedMana;
    public void UpdateManaBar()
    {
        if (!IsMine()) return;

        //if(gm.IsHeroInCharge(heroReference.hero))
        //{
        //Debug.Log(transform.gameObject);
        if (statusBar)
        {
            statusBar.UpdateManaBar(mana / FinalMaxMP);
            statusBar.SetManaText(Mathf.FloorToInt(mana) + " / " + FinalMaxMP);

            heroHealth.UpdateShieldBar();
        }
        OnUpdatedMana?.Invoke();
        //}
    }

    float manaRegenTimer, hpRegenTimer, tranquilityTimer;
    void UpdateRegeneration()
    {
        if (gm.IsGameOver()) return;
        if (gm.STATE == GameState.PAUSE) return;
        if (heroHealth.die) return;

        //Mana regenerates by 1 per 1s tick
        manaRegenTimer += Time.deltaTime;
        if (manaRegenTimer > 1)
        {
            mana += 1 + ((FinalManaGain + tempRegenBoost) / 100);
            if (mana >= FinalMaxMP) mana = FinalMaxMP;
            UpdateManaBar();
            manaRegenTimer = 0;
        }

        //Health regenerates by recovery % per 3s tick
        hpRegenTimer += Time.deltaTime;
        if (hpRegenTimer > 3)
        {
            float hpAmt = ((recovery / 100) * FinalMaxHP) / 10;
            /*
            Debug.Log(string.Format("Recovery = (({0}/100) * {1})/10", recovery, health));
            Debug.Log("health restored : " + hpAmt);
            */
            heroHealth.RestoreHp(hpAmt, false);
            hpRegenTimer = 0;
        }

        //Tranquility
        if (socketEffect.tranquility.count > 0)
        {
            if (gm.ActiveHero != this)
            {
                heroStatusEffect.SetNotification("icon_rune_" + SocketType.Tranquility.ToString().ToLower(), "");
                tranquilityTimer += Time.deltaTime;
                if (tranquilityTimer > socketEffect.tranquility.GetValue(1))
                {
                    heroHealth.RestoreHpPercent(socketEffect.tranquility.GetValue(0), false);
                    tranquilityTimer = 0;
                }
            }
            else
            {
                heroStatusEffect.RemoveNotification("icon_rune_" + SocketType.Tranquility.ToString().ToLower());
            }
        }

        //Bravery
        if (socketEffect.bravery.count > 0)
        {
            if (HpPercent * 100f < socketEffect.bravery.GetValue(1))
            {
                heroStatusEffect.SetNotification("icon_rune_" + SocketType.Bravery.ToString().ToLower(), "");
            }
            else
            {
                heroStatusEffect.RemoveNotification("icon_rune_" + SocketType.Bravery.ToString().ToLower());
            }
        }
    }


    //Speciality Passive Recovery
    [HideInInspector] public float spGaugeRecoveryTimer, spGaugeRecoveryThreshold;
    void SetSPGaugeRecoveryThreshold()
    {
        switch (heroReference.hero)
        {
            case Hero.Claris: spGaugeRecoveryThreshold = 1.5f; break;
            case Hero.Chase: spGaugeRecoveryThreshold = 12.0f; break;
            case Hero.Amy: spGaugeRecoveryThreshold = 3f; break;
            case Hero.Alaster: spGaugeRecoveryThreshold = 1.5f; break;
            case Hero.Edna: spGaugeRecoveryThreshold = 1.5f; break;
            case Hero.Louisa: spGaugeRecoveryThreshold = 5.0f; break;
            case Hero.Elze: spGaugeRecoveryThreshold = 2.0f; break;
            case Hero.Zerav: spGaugeRecoveryThreshold = 2.0f; break;
            default: spGaugeRecoveryThreshold = 9999; break;
        }

        //apply speciality

        float thresholdReduction = FinalSPRegen / 100 * spGaugeRecoveryThreshold;
        spGaugeRecoveryThreshold -= thresholdReduction;
        //Debug.Log(heroReference.hero.ToString() + "(" + speciality + ") recovers SP Gauge every " + spGaugeRecoveryThreshold + "s");
    }
    void UpdateSpecialityRecovery()
    {
        if (gm.IsGameOver()) return;
        if (gm.STATE == GameState.PAUSE) return;
        if (heroHealth.die) return;
        if (gm.ActiveHero == this) return; //only works if character is benched

        spGaugeRecoveryTimer += Time.deltaTime;
        if (spGaugeRecoveryTimer >= spGaugeRecoveryThreshold)
        {
            spGaugeRecoveryTimer = 0;

            switch (heroReference.hero)
            {
                case Hero.Claris:
                    GetComponent<Hero_Claris>().AddRagePoint(1);
                    //Debug.Log("recovers 1 Resonance Point");
                    break;

                case Hero.Chase:
                    GetComponent<Hero_Chase>().IncreaseGauge(1);
                    //Debug.Log("recovers 1 Ammo");
                    break;

                case Hero.Amy:
                    GetComponent<Hero_Amy>().AddRagePoint(1);
                    //Debug.Log("Amy add rage point by 1");
                    break;

                case Hero.Alaster:
                    GetComponent<Hero_Alaster>().IncreaseSwordGauge(1);
                    //Debug.Log("Amy add rage point by 1");
                    break;

                case Hero.Edna:
                    GetComponent<Hero_Edna>().IncreaseHeatGauge(1);
                    //Debug.Log("Amy add rage point by 1");
                    break;

                case Hero.Louisa:
                    GetComponent<Hero_Louisa>().IncreaseTacticalGauge(1, true);
                    break;

                case Hero.Elze:
                    GetComponent<Hero_Elze>().IncreaseFrostcraftGauge(1);
                    break;

                case Hero.Zerav:
                    GetComponent<Hero_Zerav>().IncreaseAwakeningGauge(2);
                    break;

                default: break;
            }

        }

        //Claris' Resonance Point (recovers 1rp every 1s)


        //Chase's Ammo Gauge (recovers 1ammo every 12s)
    }

    public void DrainMana(float amount)
    {
        mana -= amount;
        if (mana < 0) mana = 0;

        UpdateManaBar();
    }

    //Protection Socket
    [HideInInspector] public bool protectionActive;
    bool disableProtector;
    CoroutineHandle protectionHandler;
    public bool ActivateProtectionSocketFx()
    {
        if (disableProtector) return false;
        if (protectionActive) return false;
        heroHealth.hp = 1f;
        heroHealth.RestoreHpPercent(socketEffect.protection.GetValue(0), true);

        heroHealth.RemoveAllStatusEffect(false);

        protectionActive = true;
        disableProtector = true;
        //Set Hero Layer Unhitable
        gameObject.layer = 15;

        protectionHandler = Timing.RunCoroutine(StatusEffectByDuration("icon_rune_" + SocketType.Protection.ToString().ToLower(),
            socketEffect.protection.GetValue(1), DisableProtectionSocketFx), EC2Constant.STATE_DEPENDENT);

        return true;
    }

    public void ForceDisableProtection()
    {
        if (!protectionActive) return;

        Timing.KillCoroutines(protectionHandler);
        heroStatusEffect.RemoveNotification("icon_rune_" + SocketType.Protection.ToString().ToLower());
        DisableProtectionSocketFx();
    }

    public void DisableProtectionSocketFx()
    {
        protectionActive = false;

        //Set Hero Hitable
        gameObject.layer = 9;

        //Set CD 
        Timing.RunCoroutine(StatusEffectByDuration("icon_rune_" + SocketType.Protection.ToString().ToLower() + "_disable",
            EC2Constant.MAX_PROTECTION_CD, () =>
            {
                disableProtector = false;
            }
            ), EC2Constant.STATE_DEPENDENT);
    }

    //Toughness//Protection Socket
    [HideInInspector] public bool toughnessActive;
    bool disableToughness;
    CoroutineHandle toughnessHandler;
    public bool ActivateToughnessSocketFx()
    {
        if (disableToughness) return false;
        if (toughnessActive) return false;

        toughnessActive = true;
        disableToughness = true;
        heroHealth.StartSuperArmor();

        toughnessHandler = Timing.RunCoroutine(StatusEffectByDuration("icon_rune_" + SocketType.Toughness.ToString().ToLower(),
            socketEffect.toughness.GetValue(0), DisableToughnessSocketFx), EC2Constant.STATE_DEPENDENT);

        return true;
    }

    public void ForceDisableToughness()
    {
        if (!toughnessActive) return;

        Timing.KillCoroutines(toughnessHandler);
        heroStatusEffect.RemoveNotification("icon_rune_" + SocketType.Toughness.ToString().ToLower());
        DisableToughnessSocketFx();
    }

    public void DisableToughnessSocketFx()
    {
        toughnessActive = false;
        heroHealth.EndSuperArmor();

        //Set CD 
        Timing.RunCoroutine(StatusEffectByDuration("icon_rune_" + SocketType.Toughness.ToString().ToLower() + "_disable",
            socketEffect.toughness.GetValue(1), () =>
            {
                disableToughness = false;
            }
            ), EC2Constant.STATE_DEPENDENT);
    }

    //Magical Egg
    CoroutineHandle revivalEggHandler;
    public bool revivalEggOnCooldown = false;
    public void UseRevivalEgg()
    {
        revivalEggOnCooldown = true;
        revivalEggHandler = Timing.RunCoroutine(StatusEffectByDuration("icon_sunnyegg", 300, RevivalEggCdDone), EC2Constant.STATE_DEPENDENT);
    }
    private void RevivalEggCdDone()
    {
        revivalEggOnCooldown = false;
        heroStatusEffect.RemoveNotification("icon_sunnyegg");
    }


    //Santa Costume
    //Santa Rush Effect
    private float santarush_duration;
    public void SantaRushStart()
    {
        santa_rush = costumeEffect.santaValue;
        santarush_duration = santa_rush > 20 ? 10 : 7;

        control.ResetAttackSpeed();
    }
    public void UpdateSantaRush()
    {
        if (santarush_duration <= 0) return;

        santarush_duration -= Time.deltaTime;
        heroStatusEffect.SetNotification("cosu_santa", (santarush_duration + 1).ToString("F0"));

        if (santarush_duration <= 0)
        {
            santa_rush = 0;

            heroStatusEffect.RemoveNotification("cosu_santa");
            control.ResetAttackSpeed();
        }
    }

    //Summer Costume
    //Tropical Squash Effect
    private float tropicalSquash_hpRegen, tropicalSquash_mpRegen;
    private float tropicalSquash_duration = 10;
    private float tropicalSquash_CD = 20;
    private bool tropicalSquashReady = true;
    private bool tropicalSquashActive = false;
    private float tropicalSquash_regenTimer;
    public void TropicalSquashStart(float hpRegenPotency, float mpRegenPotency)
    {
        if (costumeEffect.summerValue <= 0) return;
        if (tropicalSquashReady == false) return;

        tropicalSquash_hpRegen = hpRegenPotency * 0.5f / tropicalSquash_duration;
        tropicalSquash_mpRegen = mpRegenPotency * 0.5f / tropicalSquash_duration;

        tropicalSquash_regenTimer = 0;
        tropicalSquashReady = false;
        tropicalSquashActive = true;

        Timing.RunCoroutine(StatusEffectByDuration("setEffect_" + CostumeSet.Summer.ToString().ToLower(),
                                                    tropicalSquash_CD,
                                                    SetReadyTropicalSquash),
                                                    EC2Constant.STATE_DEPENDENT);
        Timing.RunCoroutine(RemoveTropicalSquashCo(), EC2Constant.STATE_DEPENDENT);
    }
    IEnumerator<float> RemoveTropicalSquashCo()
    {
        yield return Timing.WaitForSeconds(tropicalSquash_duration);
        tropicalSquashActive = false;
    }
    void SetReadyTropicalSquash()
    {
        tropicalSquashReady = true;
    }
    public void UpdateTropicalSquash()
    {
        if (gm.IsGameOver()) return;
        if (gm.STATE == GameState.PAUSE) return;
        if (heroHealth.die) return;
        if (tropicalSquashActive == false) return;

        //Regenerates per 1s
        tropicalSquash_regenTimer += Time.deltaTime;
        if (tropicalSquash_regenTimer > 1)
        {
            heroHealth.RestoreHp(tropicalSquash_hpRegen, true, false);

            AddManaValue(tropicalSquash_mpRegen, false, true, false, false);

            tropicalSquash_regenTimer = 0;
        }
    }


    //================= EXP & LEVEL =================//
    public static event System.Action<int> OnShareEXP;
    public void GainExp(int exp)
    {
        if (heroHealth.die) return;
        GainExp(exp, true);
    }
    public void GainExp(int exp, bool sharing)
    {
        if (heroData.currentEXP < 0) heroData.currentEXP = 0;
        int currentExp = heroData.currentEXP;

        if (sharing)
        {
            if (gm.AllowParty && gm.ActiveHero == this)
            {
                var sharedExp = Mathf.FloorToInt(exp * PartyManager.SHARED_EXP);
                OnShareEXP?.Invoke(sharedExp);
            }
        }

        if (heroData.currentLevel >= gm.heroMaxLevel)
            return;

        currentExp += exp;

        if (currentExp >= MaxExp)
        {
            LevelUp();
            if (heroData.currentLevel >= gm.heroMaxLevel)
            {
                currentExp = 0;
                //break;
            }

            currentExp -= MaxExp;
            if (currentExp < 0) currentExp = 0;

            MaxExp = CalculateMaxExp();
        }

        heroData.currentEXP = currentExp;
        UpdateExpBar();
    }
    public void UpdateExpBar()
    {
        ReferenceInit();
        if (!IsMine()) return;

        //if(gm.IsHeroInCharge(heroReference.hero))
        //{
        if (statusBar)
            statusBar.UpdateExpBar((float)heroData.currentEXP / (float)MaxExp);
        //}       
    }
    void LevelUp()
    {
        if (heroData.currentLevel >= gm.heroMaxLevel) return; //limit max level

        gm.NotifyLevelUp(heroReference.hero.ToString().ToLower(), heroReference.HeroName(), false, false);

        if (heroData.currentLevel == 1)
        {
            Timing.RunCoroutine(NotifyFirstLevelupGuide());
        }

        heroData.currentLevel++;
        gm.userData.LevelUpChar(heroReference.hero, heroData.currentLevel); //add statistic
        //MaxExp = CalculateMaxExp();
        if (statusBar) statusBar.SetLevel(heroData.currentLevel);

        //increase status and replenish hp/mp/cooldown
        CalculateHeroAttributes();
        if (!heroHealth.die)
        {
            heroHealth.RestoreHpPercent(100, false);
            AddManaPercent(100);
        }
        control.RefreshAllSkillCooldown();

        UnlockSkill();
    }
    public void UnlockSkill()
    {
        if (heroData.currentLevel >= 0) control.SkillUnlock(0);
        //Lv2 = skill 2
        if (heroData.currentLevel >= 2) control.SkillUnlock(1);

        //Lv5 - skill 3
        if (heroData.currentLevel >= 5) control.SkillUnlock(2);

        //Lv9 - skill 4
        if (heroData.currentLevel >= 9) control.SkillUnlock(3);

        //Lv12 - skill 5
        if (heroData.currentLevel >= 12) control.SkillUnlock(4);

        //Lv15 - skill 6
        if (heroData.currentLevel >= 15) control.SkillUnlock(5);

        //Lv15 - skill 6
        if (heroData.currentLevel >= 25) control.SkillUnlock(6);

        //Lv15 - skill 6
        if (heroData.currentLevel >= 30) control.SkillUnlock(7);
    }
    IEnumerator<float> NotifyFirstLevelupGuide()
    {
        yield return Timing.WaitForSeconds(1);

        gm.slideshow.SetActiveSlide(3);
        gm.interaction.StartInteraction();
    }

    public void HealCheckpoint()
    {
        heroHealth.RestoreHpPercent(100, false);
        heroHealth.RemoveAllStatusEffect();
        //mana = maxMana; UpdateManaBar();
        //control.RefreshAllSkillCooldown();
    }
    public void FullManaRestore()
    {
        mana = FinalMaxMP; UpdateManaBar();
        //mana = maxMana; UpdateManaBar();
        //control.RefreshAllSkillCooldown();
    }

    public void Revive()
    {
        if (heroHealth.handler != null)
            if (heroHealth.handler.IsRunning)
                heroHealth.DieAnimationPlayed();
        heroHealth.die = false;
        heroHealth.RestoreHpPercent(100, false);
        heroHealth.RemoveAllStatusEffect();
        mana = FinalMaxMP; UpdateManaBar();

        GetComponent<Animator>().ResetTrigger("die");
        GetComponent<CharacterController>().enabled = true;

        ReinitStatusbar();

        Timing.RunCoroutine(PlayReviveAnimation().CancelWith(gameObject), Segment.LateUpdate);
    }

    bool reviveAnimationPlayed;
    IEnumerator<float> PlayReviveAnimation()
    {
        reviveAnimationPlayed = false;
        GetComponent<Animator>().ResetTrigger("revive");

        while (!reviveAnimationPlayed)
        {
            yield return Timing.WaitForOneFrame;
            GetComponent<Animator>().SetTrigger("revive");
        }
    }

    public void ReviveAnimationPlayed()
    {
        //Debug.Log("revived");
        reviveAnimationPlayed = true;
        GetComponent<Animator>().ResetTrigger("revive");
    }

    public int GetEquippedSet(EquipSet set)
    {
        if (setCategories.ContainsKey(set))
        {
            return setCategories[set];
        }

        else return 0;
    }

    public void PlayVFXHealCheckpoint()
    {
        GameManager.instance.vfxManager.CheckPointHeal(transform, 2, 2);
    }

    //================= CONSUMABLE =================//
    float consum_dmg_duration;
    public void Consum_DamageBoost(float percent, float duration)
    {
        consum_atk_boost = percent / 100 * damage;
        consum_dmg_duration = duration;

        heroStatusEffect.SetNotification("consum_atkBoost", Mathf.FloorToInt(duration).ToString());
    }
    void End_DamageBoost()
    {
        //remove buff notif
        consum_atk_boost = 0;
    }
    IEnumerator<float> UpdateConsumDamageBoost()
    {
        while (consum_dmg_duration > 0)
        {
            consum_dmg_duration -= Time.deltaTime;
            if (consum_dmg_duration <= 0) End_DamageBoost();

            yield return Timing.WaitForSeconds(1);
        }
    }

    public IEnumerator<float> StatusEffectByDuration(string notification, float duration, System.Action OnEnded)
    {
        float tempDuration = 0f;

        while (tempDuration < duration)
        {
            heroStatusEffect.SetNotification(notification, Mathf.CeilToInt(duration - tempDuration).ToString());
            yield return Timing.DeltaTime;
            tempDuration += Timing.DeltaTime;
        }

        heroStatusEffect.RemoveNotification(notification);
        OnEnded?.Invoke();
    }

    public IEnumerator<float> StatusEffectByDurationAndStack(string notification, float duration, int stack,
        System.Action OnEnded)
    {
        float tempDuration = 0f;

        while (tempDuration < duration)
        {
            heroStatusEffect.SetNotification(notification, Mathf.CeilToInt(duration - tempDuration).ToString(),
                stack);
            yield return Timing.DeltaTime;
            tempDuration += Timing.DeltaTime;
        }

        heroStatusEffect.RemoveNotification(notification);
        OnEnded?.Invoke();
    }
    public void KillStatusEffectNotification(CoroutineHandle toKilled, string notification)
    {
        if (toKilled != null)
            Timing.KillCoroutines(toKilled);

        heroStatusEffect.RemoveNotification(notification);
    }

    //-- Gimmick
    int vengeanceStack;
    CoroutineHandle vengeanceNotif;
    public void TriggerVengeance()
    {
        if (setEffect.vengeance > 0f)
        {
            if (vengeanceStack < 5)
            {
                vengeanceStack++;
                vengeance_skill_bonus = vengeanceStack * setEffect.vengeance;
            }

            Timing.KillCoroutines(vengeanceNotif);

            vengeanceNotif = Timing.RunCoroutine(StatusEffectByDurationAndStack
                (
                    "setEffect_" + EquipSet.Vengeance.ToString().ToLower(), 12f, vengeanceStack, RemoveVengeance
                ),
                EC2Constant.STATE_DEPENDENT);
        }
    }

    public void RemoveVengeance()
    {
        vengeanceStack = 0;
        vengeance_skill_bonus = 0f;
    }

    public void AddPassionField(Amy_PassionField passionField)
    {
        if (passionFields.Contains(passionField)) return;
        passionFields.Add(passionField);
    }

    public void RemovePassionField(Amy_PassionField passionField)
    {
        passionFields.Remove(passionField);
    }

    public void AddRosefield(Elze_Rosefield rosefield)
    {
        if (roseFields.Contains(rosefield)) return;
        roseFields.Add(rosefield);
    }
    public void RemoveRosefield(Elze_Rosefield rosefield)
    {
        roseFields.Remove(rosefield);
    }

    public void AddMoraleField(Louisa_MoraleBoost moralefield)
    {
        if (moraleFields.Contains(moralefield)) return;
        moraleFields.Add(moralefield);
    }
    public void RemoveMoraleField(Louisa_MoraleBoost moralefield)
    {
        moraleFields.Remove(moralefield);
    }

    public void AddProtectionField(Alaster_ProtectionField protectionField)
    {
        if (protectionFields.Contains(protectionField)) return;
        protectionFields.Add(protectionField);
    }

    public void RemoveProtectionField(Alaster_ProtectionField protectionField)
    {
        protectionFields.Remove(protectionField);
    }

    public void AddFieryField(Edna_FieryField fieryField)
    {
        if (fieryFields.Contains(fieryField)) return;
        fieryFields.Add(fieryField);
    }
    public void RemoveFieryField(Edna_FieryField fieryField)
    {
        fieryFields.Remove(fieryField);
    }

    public bool CanSwitchedOut()
    {
        foreach (var key in heroHealth.statusEffects.Keys.ToList())
        {
            if (!StatusEffectManager.instance.IsBuff(key))
            {
                return false;
            }
        }

        if (control.IsUsingSkill()) return false;

        return true;
    }

    [HideInInspector] public bool onFieryField;
    public void ResetBonusFieryField()
    {
        onFieryField = false;
        fieryField_skillDmg = 0;
        fieryField_bonusManaGain = 0;
        heroStatusEffect.RemoveNotification("ed_burningfield");

        fieryFields = new List<Edna_FieryField>();
    }

    public void AddBonusFieryField(float mana, float skill)
    {
        onFieryField = true;
        fieryField_skillDmg = skill;
        fieryField_bonusManaGain = mana;
        heroStatusEffect.SetNotification("ed_burningfield", "");
    }


    public bool HasExRune(SocketType socket)
    {
        switch (socket)
        {
            case SocketType.Transmutation:
                return socketEffect.transmutation.count > 0;

            case SocketType.Ruin:
                return socketEffect.ruin.count > 0;

            case SocketType.Omni:
                return socketEffect.omni.count > 0;

            case SocketType.Magna:
                return socketEffect.magna.count > 0;

            default: return false;
        }
    }
    public float GetExVal(SocketType socket)
    {
        switch (socket)
        {
            case SocketType.Transmutation:
                return socketEffect.transmutation.GetValue(0);

            case SocketType.Ruin:
                return socketEffect.ruin.GetValue(0);

            case SocketType.Omni:
                return socketEffect.omni.GetValue(0);

            case SocketType.Magna:
                return socketEffect.magna.GetValue(0);

            default: return 0;
        }
    }





    //ALASTER PREMIUM ROD
    public bool IsUsingPremiumRod()
    {
        if (heroReference.hero != Hero.Alaster) return false;

        try
        {
            var savedata = GetComponent<HeroSaveData>().data;
            bool usingPremiumRodCostume = savedata.costumes[0].itemData.id == "summer_fishingrod";
            bool usingPremiumRodTransmog = savedata.cosuTransmog[0].itemData.id == "summer_fishingrod";

            if (usingPremiumRodCostume || usingPremiumRodTransmog)
                return true;
            else return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            return false;
        }
    }
}
