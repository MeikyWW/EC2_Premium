//ALL ENUMS GOES HERE
using Sirenix.OdinInspector;

//==== GENERAL USE ====//
public enum ShurikenEnd
{
    destroy,
    deactivate,
    ezPooling
}
public enum SupportedLanguages
{
    english,
    indonesia,
    portuguese,
    chinese_simplified,
    spanish,
    french,
    japanese
}
public enum DPadDirection
{
    up,
    down,
    left,
    right
}
[System.Serializable]
public struct ManaRegenInfo
{
    [LabelText("MP (initial hit)")]
    public float manaIncreaseOnInitialHit;
    [LabelText("MP (per extra enemy hit)")]
    public float manaIncreasePerExtraEnemy;
    [LabelText("MP (limit)")]
    public float maximumManaIncrease;
}

//==== CORE ====//
public enum GameState
{
    PLAYING, PAUSE
}

//==== ENEMY ====//
public enum EnemyMovementType
{
    chaseAndAttack,
    roamAndAttack,
    stayInPlace,
    custom
}
public enum EnemyThreatBehavior
{
    unaggressive, //sementara di off, dikarenakan game design
    aggressive
}

//==== INVENTORY ====//
public enum ItemType
{
    Equipment,
    Material,
    Consumable,
    KeyItem,
    Rune,
    Pet
}
public enum ItemIcon
{
    sword,
    gun,
    helmet,
    armor,
    glove,
    shoes,
    potion,
    sack,
    leaf,
    crystal,
    ring,
    cloth,
    horn,
    ore,
    key,
    essence,
    soul,
    cos_weapon, cos_hair, cos_mask, cos_suit, cos_wings,
    rune_weapon, rune_armor, rune_ring, rune_any, rune_shard,
    soul_void, soul_wind, soul_glacial, soul_inferno,
    claw,
    stronghorn,
    mask,
    soul_abstract,
    core,
    exppotion,
    selector,
    coupon_acc, coupon_soul, coupon_rune, coupon_mats,
    key_silverkey,
    fish_blue,
    fish_brown,
    fish_darkbrown,
    catalyst,
    mysticmine_pass,
    key_quizticket,
    grimoire_potion,
    bow,
    sword_shield,
    santa_selector, christmaswish, moonshard,
    spear,
    seashell,
    profile,
    bag,
    floaties,
    surfboard,
    underwear,
    airtank,
    summer_selector,
    hat,
    moonshardblue,
    scythe,
    summertix,
    spooky_treats,
    soul_abyssal
}
public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary

}

public enum SetEffectModifier
{
    None,
    Lifesteal,
    RefreshSkill,
    Vengeance,
    ValorDamageAmplifier,
    Croven,
    School,
    Maid,
    Demonic,
    OG_Claris,
    Casual,
    Santa,
    MardukVigor,
    Eclipse,
    Summer,
    Midnight,
    Entropy,
    WindVeil,
    FrostNova,
    PhoenixTrigger
}

public enum SocketType
{
    None,

    Attack,
    Defense,
    MaxMP,
    MaxHP,

    Recharge,
    Tranquility,
    Toughness,
    Wind,

    Burst,
    Bravery,
    Protection,
    Drain,
    Resistance,

    //skill modifier
    Transmutation,
    Ruin,
    Omni,
    Magna,
}

namespace EC2
{
    public enum Stats
    {
        //Empty
        None,

        //Base
        Attack,
        Defense,

        //Offensive
        AttackSpeed,
        Crit,
        CritDamage,

        //Defensive
        HealthPercentage,
        Recovery,
        ElementalResistance,
        PhysicalResistance,
        Evasion,

        //Utility
        Speciality,
        ManaReduce,
        CooldownReduce,

        //Hidden
        MaxHP,
        MaxMP,

        //NEW
        BasicAtkDamage,
        SkillAtkDamage,
        Accuracy,
        ManaGain,
        SPRegen,
        ConsumablePlus,

        //NEW SPECIAL
        AttackPercentage,
        DefensePercentage
    }

    /* old
    public enum Stats
    {
        //Base
        Attack,
        Defense,

        //Offensive
        AttackSpeed,
        Crit,
        CritDamage,
        Pierce,
        Break,

        //Defensive
        MaxHP,
        Recovery,
        DebuffResist,
        Toughness,
        Evasion,

        //Utility
        ManaGain,
        SpecialGauge,
        ManaReduce,
        CooldownReduce,
        ConsumablePlus,
        DropRatePlus,

        //Empty
        None
    }

*/
}

public enum EquipSlot
{
    MainWeapon,
    Headgear,
    Armor,
    Gloves,
    Shoes,
    Accessory
}
public enum EquipSet
{
    None,
    Valor,
    Haste,
    Spirit,
    Vengeance,
    Demonic,
    MardukVigor,
    BloodEclipse,
    MidnightAvenger,
    AbyssVoid,
    AbyssGale,
    AbyssGlacial,
    AbyssInferno
}
public enum EquipCategory
{
    A,
    B,
    None
}

public enum NpcService
{
    None,
    Merchant,
    Blacksmith,
    CustomService,
    Alchemist,
    RequestManager,
    RTrader,
    Runesmith,
    Teleporter,
    MysticMineGiver,
    OnlineManager,
    PetMaster
}
