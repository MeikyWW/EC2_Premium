using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Consumable
{
    public ConsumableUseType useType;
    public ConsumableFamily family;
    public float cooldown = 1;

    public List<ConsumableEffect> effects;
    [ShowIf("@this.useType == ConsumableUseType.Throwable")]
    public ThrowableData throwableData;

    public string FamilyName()
    {
        return I2.Loc.LocalizationManager.GetTranslation("menu/consumables/group/" + family.ToString());
    }


    // [Title("OLD SHIT")]
    // public string aaa;

    // [HideIf("@this.useType == ConsumableUseType.Throwable")]
    // public ConsumableType consumableType;

    // [Title("Explosion")]
    // [ShowIf("@this.useType == ConsumableUseType.Throwable")]
    // public GameObject throwedPrefab;

    // [ShowIf("@this.useType == ConsumableUseType.Throwable && throwedPrefab != null")]
    // public bool critable;

    // [LabelText("Potency/Dmg%")]
    // public int value;

    // [ShowIf("@this.consumableType == ConsumableType.HPRecoveryPercent")]
    // public int rawValue;

    // [HideIf("@this.useType == ConsumableUseType.Throwable")]
    // public float duration;
    // [ShowIf("@this.useType == ConsumableUseType.Throwable && throwedPrefab != null")]
    // public float stunDuration;

    // [ShowIf("@(this.useType == ConsumableUseType.Throwable && throwedPrefab != null) || " +
    //     "(useType == ConsumableUseType.Normal && consumableType == ConsumableType.SetStatusEffect)")]
    // public StatusEffects statusEffect = StatusEffects.none;
    // [ShowIf("@this.statusEffect != StatusEffects.none && ((useType == ConsumableUseType.Throwable && throwedPrefab != null) || " +
    //     "(useType == ConsumableUseType.Normal && consumableType == ConsumableType.SetStatusEffect))")]
    // public float statusEffectDuration;
    // [ShowIf("@this.statusEffect != StatusEffects.none && ((useType == ConsumableUseType.Throwable && throwedPrefab != null) || " +
    //     "(useType == ConsumableUseType.Normal && consumableType == ConsumableType.SetStatusEffect))")]
    // public float statusEffectPotency;
    // [ShowIf("@this.statusEffect != StatusEffects.none && ((useType == ConsumableUseType.Throwable && throwedPrefab != null) || " +
    //     "(useType == ConsumableUseType.Normal && consumableType == ConsumableType.SetStatusEffect))")]
    // public bool unresistable;

    // [Title("Spawned Field")]
    // [ShowIf("@this.useType == ConsumableUseType.Throwable")]
    // public GameObject spawnedField;

}

[System.Serializable]
public class ConsumableEffect
{
    public ConsumableType effect;
    public float value;
    public float duration;

    [ShowIf("@effect == ConsumableType.HPRecoveryCombined")]
    public float rawValue;

    [ShowIf("@effect == ConsumableType.SetStatusEffect")]
    public ConsumableStatusEffectData statusEffectData;
}

[System.Serializable]
public class ThrowableData
{
    public GameObject throwedPrefab;
    public GameObject spawnedField;
    public bool critable;
    public float potency;
    public float stunDuration;
    public ConsumableStatusEffectData statusEffectData;
}
[System.Serializable]
public class ConsumableStatusEffectData
{
    public StatusEffects statusEffect = StatusEffects.none;
    public float statusEffectDuration;
    public float statusEffectPotency;
    public bool unresistable;
}

public enum ConsumableUseType
{
    Normal,
    Throwable,
}

public enum ConsumableType
{
    HPRecovery,
    HPRecoveryPercent,
    ManaRecovery,
    ManaRecoveryPercent,
    CureAll,
    CurePoison,
    CureBurn,
    CureFreeze,
    CureBleed,
    CurePara,
    CureSilence,
    DmgBoost,
    CritBoost,
    DurBoost,
    SpeedBoost,
    RecBoost,
    Poison,
    EleResBoost,
    PhyResBoost,
    SetStatusEffect,
    HPRecoveryCombined
}

public enum ConsumableFamily
{
    HpPotion, //includes apples and any hp recovery
    MpPotion, //includes honey and any mp recovery
    Ailment,  //status removal items like bandages
    BuffItem, //status amping items like fishes and crit pots
    Throwing, //like void balls
    Food
}