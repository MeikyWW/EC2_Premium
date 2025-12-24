using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using CodeStage.AntiCheat.ObscuredTypes;

[CreateAssetMenu(fileName = "Item", menuName = "EC2/Item", order = 1)]
public class Item : ScriptableObject
{
    #region EDITOR DISPLAY
    [BoxGroup("General Info"), ShowInInspector, PropertyOrder(-1), DisplayAsString, HideLabel, GUIColor(0, 1, 0)]
    private string Editor_Item_Name
    {
        get { return "[" + I2.Loc.LocalizationManager.GetTranslation("item/" + id + "/name") + "]"; }
        set { }
    }
    [BoxGroup("General Info"), ShowInInspector, PropertyOrder(-1), DisplayAsString(false), HideLabel, ShowIf("@itemType != ItemType.Equipment")]
    private string Editor_Item_Desc
    {
        get
        {
            return Description();
            /*
            if (itemType == ItemType.Rune)
                return Description(); 
            else
                return I2.Loc.LocalizationManager.GetTranslation("item/" + id + "/desc");*/
        }
        set { }
    }
    public string ItemName()
    {
        return I2.Loc.LocalizationManager.GetTranslation("item/" + id + "/name");
    }
    public string Description()
    {
        if (!string.IsNullOrEmpty(overrideDescriptionID))
            return I2.Loc.LocalizationManager.GetTranslation("item/" + overrideDescriptionID + "/desc");
        else
            return I2.Loc.LocalizationManager.GetTranslation("item/" + id + "/desc");
    }
    public string ItemIcon
    {
        get
        {
            if (!string.IsNullOrEmpty(overrideIcon))
                return overrideIcon;
            else
                return itemIcon.ToString();
        }
    }
    public string IconId
    {
        get
        {
            if (!string.IsNullOrEmpty(overrideIcon))
                return $"icon_{overrideIcon}";
            else
                return id;
        }
    }
    public string ConsumableDescription()
    {
        string result = "\n\n[AFFF99]";
        //dibawah description dikasi
        //[Group : Throwing Item]
        //[Cooldown : 60s]

        string groupText = I2.Loc.LocalizationManager.GetTranslation("menu/inventory/consumable/desc_group");
        result += string.Format(groupText, consumable.FamilyName().ToString());

        string cdText = I2.Loc.LocalizationManager.GetTranslation("menu/inventory/consumable/desc_cooldown");
        result += "\n" + string.Format(cdText, consumable.cooldown.ToString());

        return result + "[-]";
    }

    public string GetRuneDescription(int level)
    {
        if (itemType == ItemType.Rune)
        {
            string result;
            try
            {
                var runeInfo = socket.GetSocketValue(level);
                var args = new List<object>();
                for (int i = 0; i < runeInfo.Count; i++)
                {
                    args.Add(runeInfo[i]);
                }

                var format = Description();// I2.Loc.LocalizationManager.GetTranslation("item/" + id + "/desc");
                result = string.Format(format, args.ToArray());
            }
            catch
            {
                result = Description();// I2.Loc.LocalizationManager.GetTranslation("item/" + id + "/desc");
            }

            return result;
        }
        else return "";
    }
    public string GetRuneDescriptionWithValueRange()
    {
        if (itemType == ItemType.Rune)
        {
            string result;
            try
            {
                List<float> runeInfoLv0 = socket.GetSocketValue(0);
                List<float> runeInfoLvMax = socket.GetSocketValue(99);

                List<string> args = new List<string>();

                for (int i = 0; i < runeInfoLv0.Count; i++)
                {
                    if (runeInfoLv0[i] == runeInfoLvMax[i])
                        args.Add(runeInfoLv0[i].ToString());
                    else args.Add(runeInfoLv0[i].ToString() + " ~ " + runeInfoLvMax[i].ToString());
                }

                var format = Description();// I2.Loc.LocalizationManager.GetTranslation("item/" + id + "/desc");
                result = string.Format(format, args.ToArray());
            }
            catch
            {
                result = Description();// I2.Loc.LocalizationManager.GetTranslation("item/" + id + "/desc");
            }

            return result;
        }
        else return "";
    }
    public override bool Equals(object obj)
    {
        if (obj is not Item other) return false;
        return id == other.id;
    }
    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
    #endregion

    [LabelText("ID"), PropertyOrder(-2)]
    public string id;
    [PropertyOrder(-2)]
    public string overrideDescriptionID;
    [PropertyOrder(-2)]
    public string overrideIcon;

    [BoxGroup("General Info"), PropertySpace(SpaceBefore = 10, SpaceAfter = 0)]
    public int sellPrice;
    [BoxGroup("General Info"), PropertySpace(SpaceBefore = 0, SpaceAfter = 0)]
    public int sortingPriority;
    [BoxGroup("General Info"), PropertySpace(SpaceBefore = 0, SpaceAfter = 10)]
    public int catalystPoint;
    [BoxGroup("General Info"), PropertySpace(SpaceBefore = 0, SpaceAfter = 10)]
    public float foodPoint = 5f;

    [BoxGroup("Item Details")]//, ShowIf("@this.itemType != ItemType.Consumable")]
    public Material itemDropMaterial;
    [BoxGroup("Item Details")]//, ShowIf("@this.itemType != ItemType.Consumable")]
    public ItemIcon itemIcon;
    [BoxGroup("Item Details"), LabelText("Starting Rarity")]
    public Rarity baseRarity;
    [BoxGroup("Item Details"), PropertySpace(SpaceBefore = 0, SpaceAfter = 10), LabelText("Max Rarity")]
    public Rarity maxRarity;


    [OnValueChanged("SetItemType"), EnumToggleButtons, HideLabel]
    [BoxGroup("Item Details")]
    public ItemType itemType;


    //---------------------------//
    [ShowIfGroup("Item Details/select_eq"), BoxGroup("Item Details/select_eq/Equipment", showLabel: false), HideLabel]
    public Equipment equipment;

    [ShowIfGroup("Item Details/select_csm"), BoxGroup("Item Details/select_csm/Consumable", showLabel: false), HideLabel]
    public Consumable consumable;

    [ShowIfGroup("Item Details/select_rune"), BoxGroup("Item Details/select_rune/Rune", showLabel: false), HideLabel]
    public RuneInfo socket;

    [ShowIfGroup("Item Details/select_key"), BoxGroup("Item Details/select_key/Key", showLabel: false), HideLabel]
    public KeyInfo keyInfo;

    [ShowIfGroup("Item Details/select_pet"), BoxGroup("Item Details/select_pet/Pet", showLabel: false), HideLabel]
    public PetInfo petInfo;

    [ListDrawerSettings(ShowIndexLabels = false), PropertyOrder(5), PropertySpace(SpaceBefore = 20), LabelText("Crafting Material")]
    public CraftingRequirement[] craftingRequirement;
    [ListDrawerSettings(ShowIndexLabels = false), PropertyOrder(5), ShowIf("@maxRarity >= Rarity.Uncommon && baseRarity < Rarity.Uncommon"), LabelText("Upgrade Material (Uncommon)")]
    public CraftingRequirement[] upgradeMat_uncommon;
    [ListDrawerSettings(ShowIndexLabels = false), PropertyOrder(5), ShowIf("@maxRarity >= Rarity.Rare && baseRarity < Rarity.Rare"), LabelText("Upgrade Material (Rare)")]
    public CraftingRequirement[] upgradeMat_rare;
    [ListDrawerSettings(ShowIndexLabels = false), PropertyOrder(5), ShowIf("@maxRarity >= Rarity.Epic && baseRarity < Rarity.Epic"), LabelText("Upgrade Material (Epic)")]
    public CraftingRequirement[] upgradeMat_epic;
    [ListDrawerSettings(ShowIndexLabels = false), PropertyOrder(5), ShowIf("@maxRarity >= Rarity.Legendary && baseRarity < Rarity.Legendary"), LabelText("Upgrade Material (Epic)")]
    public CraftingRequirement[] upgradeMat_legend;

    //editor
    [OnInspectorGUI]
    private void SetItemType()
    {
        select_eq = itemType == ItemType.Equipment;
        select_mat = itemType == ItemType.Material;
        select_csm = itemType == ItemType.Consumable;
        select_key = itemType == ItemType.KeyItem;
        select_rune = itemType == ItemType.Rune;
        select_pet = itemType == ItemType.Pet;
    }
    private bool select_eq, select_mat, select_csm, select_key, select_rune, select_pet;
}

[System.Serializable]
public struct CraftingRequirement
{
    [HorizontalGroup("mats"), HideLabel]
    public Item item;

    [HorizontalGroup("mats"), HideLabel]
    public int qty;

    [HorizontalGroup("mats"), HideLabel, ShowIf("@item != null && item.itemType == ItemType.Equipment")]
    public Rarity neededRarity;
}

[System.Serializable]
public class ItemInstance
{
    public string id;
    public ObscuredInt enhancementLevel;
    public ObscuredFloat currentEXP;
    public Rarity currentRarity;
    public EquipStats randomStat;
    public List<EquipStats> fixedStats = new List<EquipStats>();
    public List<EquipSocket> socketStats = new List<EquipSocket>();
    public ObscuredInt quantity;
    public int seed;

    public ItemInstance Copy()
    {
        ItemInstance other = (ItemInstance)this.MemberwiseClone();
        other.id = id;
        other.enhancementLevel = enhancementLevel;
        other.currentEXP = currentEXP;
        other.currentRarity = currentRarity;
        other.fixedStats = fixedStats;
        other.socketStats = socketStats;
        //other.setCategory = setCategory;
        other.quantity = quantity;
        other.seed = seed;
        return other;
    }
}

[System.Serializable]
public class KeyInfo
{
    public bool useQOL;

    [ShowIf("@this.useQOL")]
    public KeyQOLType qolType;

    [ShowIf("@this.qolType == KeyQOLType.Teleport && this.useQOL")]
    public string sceneTarget;

    [ShowIf("@this.qolType == KeyQOLType.Teleport && this.useQOL")]
    public int portalIndex;

    public string AreaName()
    {
        return I2.Loc.LocalizationManager.GetTranslation("map/" + sceneTarget);
    }
}

public enum KeyQOLType
{
    None,
    Teleport
}

[System.Serializable]
public class RuneInfo
{
    public SocketType type;

    [ShowIf("@this.type != SocketType.None")]
    public int maxUpgradeLevel;

    [ShowIf("@this.type != SocketType.None")]
    public bool unstackable;

    [ShowIf("@this.type != SocketType.None")]
    [ListDrawerSettings(ShowIndexLabels = false, Expanded = true)]
    public List<EquipSlot> socketableWith;

    [ShowIf("@this.type != SocketType.None")]
    public List<DescriptedFloats> valueDatas;

    public List<List<float>> GetSocketValues()
    {
        List<List<float>> tempList = new List<List<float>>();

        foreach (var item in valueDatas)
            tempList.Add(item.datas);

        return tempList;
    }

    public List<float> GetSocketValue(int level)
    {
        List<float> tempList = new List<float>();

        foreach (var cond in valueDatas)
        {
            if (level >= cond.datas.Count)
                tempList.Add(cond.datas[cond.datas.Count - 1]);
            else
                tempList.Add(cond.datas[level]);
        }

        return tempList;
    }
    public List<BoolFloatPair> GetRuneValues(int level)
    {
        List<BoolFloatPair> tempList = new List<BoolFloatPair>();

        foreach (var cond in valueDatas)
        {
            if (level >= cond.datas.Count)
                tempList.Add(new BoolFloatPair()
                {
                    basedOnHighestLevel = cond.basedOnHighestLevel,
                    value = cond.datas[cond.datas.Count - 1]
                });
            else
                tempList.Add(new BoolFloatPair()
                {
                    basedOnHighestLevel = cond.basedOnHighestLevel,
                    value = cond.datas[level]
                });
        }

        return tempList;
    }
}

[System.Serializable]
public class DescriptedFloats
{
    [TextArea, SuffixLabel("Description", overlay: true), HideLabel]
    public string description;
    public bool basedOnHighestLevel;
    public List<float> datas;
}


[System.Serializable]
public class BoolFloatPair
{
    public bool basedOnHighestLevel;
    public float value;
}

[System.Serializable]
public class PetInfo
{
    [LabelText("Summoned Prefab")]
    public GameObject summonedPetPrefab;
    public int stage;

    [FoldoutGroup("Monologue"), InfoBox("By checking default monologue means it will use its own monologue (itemID/Gratitude/0) etc." +
        "\nIf it's unchecked it will use global default (Pet/Gratitude/0) etc")]
    [FoldoutGroup("Monologue")] public bool useDefaultNonsense = true;
    [FoldoutGroup("Monologue"), ShowIf(nameof(useDefaultNonsense))] public int nonsenseCount = 0;
    [FoldoutGroup("Monologue")] public bool useDefaultGratitude = true;
    [FoldoutGroup("Monologue"), ShowIf(nameof(useDefaultGratitude))] public int gratitudeCount = 0;
    [FoldoutGroup("Monologue")] public bool useDefaultHungry = true;
    [FoldoutGroup("Monologue"), ShowIf(nameof(useDefaultHungry))] public int hungryCount = 0;
    [FoldoutGroup("Monologue")] public bool useDefaultLove = true;
    [FoldoutGroup("Monologue"), ShowIf(nameof(useDefaultLove))] public int loveCount = 0;

    [FoldoutGroup("Properties"), MinValue(1)] public float baseDamage;
    [FoldoutGroup("Properties")] public bool hasAutoAttack;
    [FoldoutGroup("Properties")] public bool mountable;
    [FoldoutGroup("Properties"), ShowIf(nameof(mountable))] public GameObject mountPrefab;
    [FoldoutGroup("Properties")] public bool canCollectLoot;
    [FoldoutGroup("Properties"), LabelText("Percentage Up Each Level"), SuffixLabel("%", true)] public float pctUp = 4;
    [FoldoutGroup("Properties"), Title("Stat Given")] public List<EquipStats> statsGiven;

    [FoldoutGroup("Foods"), ListDrawerSettings(Expanded = true)] public List<Item> favoriteFoods;
    [FoldoutGroup("Foods"), ListDrawerSettings(Expanded = true)] public List<Item> disgustFoods;

    [FoldoutGroup("Ability")] public EC2HeroSkill skill;
    [FoldoutGroup("Ability")] public EC2HeroMastery passive;

    #region Editor Display
    //[BoxGroup("Evolve List Info"), ShowInInspector, DisplayAsString, HideLabel, GUIColor(0, 1, 0)]
    private string Editor_Evolve_to
    {
        get
        {
            string evTo = string.Empty;

            for (int i = 0; i < evolveableTo.Count; i++)
            {
                var evoInfo = evolveableTo[i];
                if (evoInfo.evolveTo == null) continue;
                if (i != 0) evTo += "\n";
                evTo += $"[{i}] {evoInfo.evolveTo.ItemName()} [{evoInfo.successRate}%]";
            }

            if (string.IsNullOrEmpty(evTo)) evTo = "This pet can't evolve to anything. [Or please assign it]";

            return evTo;
        }
        set { }
    }
    #endregion

    [FoldoutGroup("Evolve"), ListDrawerSettings(Expanded = true)] public int evolveRequiredItemCount;
    [FoldoutGroup("Evolve"), ListDrawerSettings(Expanded = true)] public List<Item> requiredItemToEvolve;
    [FoldoutGroup("Evolve"), ListDrawerSettings(Expanded = true)] public List<EvolveableTo> evolveableTo;

    public string GetRandomNonsense()
    {
        return string.Empty;
    }
    public string GetRandomGratitude()
    {
        return string.Empty;
    }
    public string GetRandomHungry()
    {
        return string.Empty;
    }
    public string GetRandomLove()
    {
        return string.Empty;
    }
}

[System.Serializable]
public class PetData
{
    public ItemInstance petInstance = new ItemInstance();
    public SkillAttackPattern skillAtkPattern = SkillAttackPattern.InPlace;
    public AttackTarget attackTarget = AttackTarget.Nearest;
}

[System.Serializable]
public enum SkillAttackPattern
{
    InPlace,
    ToNearestEnemy
}

[System.Serializable]
public enum AttackTarget
{
    Nearest,
    LowestHP,
}



[System.Serializable]
public class EvolveableTo
{
    [OnValueChanged("CheckEvolveTo")]
    public Item evolveTo;
    public float successRate;
    public List<SpecialFood> specialFoods;

    public void CheckEvolveTo()
    {
        if (evolveTo.itemType != ItemType.Pet)
            evolveTo = null;
    }
}

[System.Serializable]
public class SpecialFood
{
    public Item food;
    public float addedSuccessRate;
}
