using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Language.Lua;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

//This script manages Item in the database
//Holds predetermined data (item id, name, price, attributes, etc)
//Player's inventory (and it's saved data) is managed in Container script

public enum InventoryType
{
    CharacterInventory, BankInventory
}

public class Inventory : MonoBehaviour
{
    public Item[] _equipments;
    public Item[] _materials;
    public Item[] _consumables;
    public Item[] _keyItems;
    public List<Item> _runes;
    public List<Item> _costumes;
    public Dictionary<string, Item> allItems = new Dictionary<string, Item>();

    public Dictionary<string, EC2TextAsset> setEffects = new Dictionary<string, EC2TextAsset>();
    public Dictionary<string, EC2TextAsset> socketEffects = new Dictionary<string, EC2TextAsset>();

    ItemInstance selectedItem;
    Container selectedItemContainer;
    public static Inventory instance;
    public Dictionary<InventoryType, Container> openedContainers = new Dictionary<InventoryType, Container>();

    AcquireNotif notif;
    GameManager gm;
    SFXManager sfx;

    private void Start()
    {
        if (instance == this) return;
        instance = this;

        //init inventory database
        foreach (var item in _equipments) if (!allItems.ContainsKey(item.id)) allItems.Add(item.id, item);
        foreach (var item in _costumes) if (!allItems.ContainsKey(item.id)) allItems.Add(item.id, item);
        foreach (var item in _materials) if (!allItems.ContainsKey(item.id)) allItems.Add(item.id, item);
        foreach (var item in _consumables) if (!allItems.ContainsKey(item.id)) allItems.Add(item.id, item);
        foreach (var item in _keyItems) if (!allItems.ContainsKey(item.id)) allItems.Add(item.id, item);
        foreach (var item in _runes) if (!allItems.ContainsKey(item.id)) allItems.Add(item.id, item);

        gm = GameManager.instance;
        sfx = gm.sfx;
    }

    public void InitMainInventory(Container cont)
    {
        openedContainers.Add(InventoryType.CharacterInventory, cont);
        selectedItemContainer = openedContainers[InventoryType.CharacterInventory];
    }
    public Container CharacterInventory()
    {
        return openedContainers[InventoryType.CharacterInventory];
    }
    public MainInventoryViewer InventoryGui()
    {
        return openedContainers[InventoryType.CharacterInventory].GetComponent<MainInventoryViewer>();
    }

    //==== GENERAL FUNCTION ====//
    public void AddInventory(InventoryType type, Container container)
    {
        if (openedContainers.ContainsKey(type)) return;
        openedContainers.Add(type, container);
    }

    public void SetSelectedContainer(InventoryType type) => selectedItemContainer = openedContainers[type];

    public Container GetContainer(InventoryType type) => openedContainers[type];
    public void ListMainInventory()
    {
        openedContainers[InventoryType.CharacterInventory].GetComponent<MainInventoryViewer>().OpenInventory();
    }
    public void SaveAllInventory()
    {/*
        foreach (Container c in openedContainers)
            c.SaveInventory();*/
    }

    public static System.Action<Item> OnGotItem;

    public bool AddItem(string itemId, int quantity, ItemInstance attribute)
    {
        return AddItem(itemId, quantity, attribute, false);
    }

    public bool AddItem(string itemId, int quantity, ItemInstance attribute, bool turnOffNotification)
    {
        return AddItem(itemId, quantity, attribute, turnOffNotification, false);
    }

    public bool AddItem(string itemId, int quantity, ItemInstance attribute, bool turnOffNotification, bool dropFromBag)
    {
        Item item = allItems[itemId];
        bool addSucceed = selectedItemContainer.AddItem(itemId, quantity, attribute);

        if (addSucceed) gm.userData.data.AddAcquiredItem(item);

        if (notif == null) notif = gm.GetComponent<AcquireNotif>();

        if (!turnOffNotification)
        {
            if (addSucceed)
            {
                Rarity rarity = attribute == null ? item.baseRarity : attribute.currentRarity;

                if (!dropFromBag) OnGotItem?.Invoke(item);

                notif.Notify(item, EC2Utils.GetRarityColor(rarity), item.ItemName(), quantity);
                sfx.GetItem();
            }
            else
            {
                notif.NotifyFull();
                sfx.ItemFull();
            }
        }

        return addSucceed;
    }

    public EquipStats SetSecondStat(Item item)
    {
        //second stat by stat stage
        EC2.Stats stat = GetRandomStat();
        return SetSecondStat(item, stat);
    }
    public EquipStats SetSecondStat(string itemId)
    {
        return SetSecondStat(allItems[itemId]);
    }
    public EquipStats SetSecondStat(string itemId, EC2.Stats stat)
    {
        if (stat == EC2.Stats.None) return SetSecondStat(allItems[itemId]);
        else return SetSecondStat(allItems[itemId], stat);
    }
    public EquipStats SetSecondStat(Item item, EC2.Stats stat)
    {
        float value = GetStatValueByStage(stat, item.equipment.levelRequirement, item.equipment.secondaryStatLevel);
        return new EquipStats(stat, value);
    }

    //Additional Fixed Status
    public List<EquipStats> SetFixedStat(string itemId, Rarity targetRarity, int enhancementLevel)
    {
        return SetFixedStat(allItems[itemId], targetRarity, enhancementLevel);
    }
    public List<EquipStats> SetFixedStat(Item item, Rarity targetRarity, int enhancementLevel)
    {
        List<EquipStats> result = new List<EquipStats>();

        if (EC2Utils.IsRarityAbove(targetRarity, Rarity.Uncommon))
            result.Add(GetRandomStat(item.equipment.levelRequirement, enhancementLevel));

        if (EC2Utils.IsRarityAbove(targetRarity, Rarity.Rare))
            result.Add(GetRandomStat(item.equipment.levelRequirement, enhancementLevel));

        if (EC2Utils.IsRarityAbove(targetRarity, Rarity.Epic))
            result.Add(GetRandomStat(item.equipment.levelRequirement, enhancementLevel));

        return result;
    }
    public List<EquipStats> AddFixedStat(Item item, List<EquipStats> currentStat, Rarity targetRarity, int enhancementLevel)
    {
        List<EquipStats> result = currentStat;

        if (EC2Utils.IsRarityAbove(targetRarity, Rarity.Uncommon) && result.Count < 1)
            result.Add(GetRandomStat(item.equipment.levelRequirement, enhancementLevel));

        if (EC2Utils.IsRarityAbove(targetRarity, Rarity.Rare) && result.Count < 2)
            result.Add(GetRandomStat(item.equipment.levelRequirement, enhancementLevel));

        if (EC2Utils.IsRarityAbove(targetRarity, Rarity.Epic) && result.Count < 3)
            result.Add(GetRandomStat(item.equipment.levelRequirement, enhancementLevel));

        return result;
    }
    public EquipStats GetRandomStat(int itemLevel, int enhancementLevel)
    {
        EC2.Stats stat = GetRandomStat();
        //Stats stat = (Stats)RandomStatIndex();
        float value = GetRandomStatValue(stat, itemLevel, enhancementLevel);
        return new EquipStats(stat, value);
    }

    EC2.Stats GetRandomStat()
    {
        EC2.Stats stat = EC2.Stats.None;
        var firstTierStats = new List<EC2.Stats>()
        {
            EC2.Stats.ManaReduce,
            EC2.Stats.CooldownReduce,
            EC2.Stats.SkillAtkDamage
        };

        var secondTierStats = new List<EC2.Stats>()
        {
            EC2.Stats.Crit,
            EC2.Stats.CritDamage,
            EC2.Stats.Evasion,
            EC2.Stats.AttackSpeed,
            EC2.Stats.BasicAtkDamage,
            EC2.Stats.Accuracy,
        };

        var thirdTierStats = new List<EC2.Stats>()
        {
            EC2.Stats.HealthPercentage,
            EC2.Stats.Recovery,
            EC2.Stats.ElementalResistance,
            EC2.Stats.PhysicalResistance,
            EC2.Stats.Speciality,
            EC2.Stats.ManaGain,
            EC2.Stats.ConsumablePlus,
            EC2.Stats.SPRegen,
        };

        float totalChance = EC2Constant.FIRST_TIER_STAT +
            EC2Constant.SECOND_TIER_STAT +
            EC2Constant.THIRD_TIER_STAT;

        var random = Random.Range(0, totalChance);

#if UNITY_EDITOR
        //Debug.Log(random);
#endif
        if (random <= EC2Constant.FIRST_TIER_STAT)
            stat = firstTierStats[Random.Range(0, firstTierStats.Count)];

        else if (random <= EC2Constant.FIRST_TIER_STAT + EC2Constant.SECOND_TIER_STAT)
            stat = secondTierStats[Random.Range(0, secondTierStats.Count)];

        else
            stat = thirdTierStats[Random.Range(0, thirdTierStats.Count)];

        return stat;
    }

    public int RandomStatIndex()
    {
        return Random.Range(3, 14);
    }
    int GetRandomStatValue(EC2.Stats stat, int itemLevel, int enhancementLevel)
    {
        int minStage = GetMinStage(enhancementLevel);
        int maxStage = GetMaxStage(enhancementLevel);

        int randomLevel = Random.Range(minStage, maxStage);

        return Mathf.CeilToInt(GetStatValueByStage(stat, itemLevel, randomLevel));
    }
    int GetMinStage(int enhancementLevel)
    {
        //appraisal stage is based on enhancement level
        //+0 ~ +6 = min 1
        //+7 ~ +9 = min 2
        //+10 ~ +12 = min 3
        //+13 ~ +15 = min 4
        int result;

        switch (enhancementLevel)
        {
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7: result = 2; break;
            case 8:
            case 9:
            case 10:
            case 11:
            case 12: result = 2; break;
            case 13:
            case 14:
            case 15: result = 3; break;
            default: result = 1; break;
        }

        return result;
    }
    int GetMaxStage(int enhancementLevel)
    {
        //appraisal stage is based on enhancement level
        //+0 ~ +3 = max 3
        //+4 ~ +6 = max 4
        //+7 ~ +9 = max 5
        //+10 ~ +12 = max 6
        //+13 ~ +15 = max 7
        int result;

        switch (enhancementLevel)
        {
            case 0:
            case 1:
            case 2:
            case 3: result = 3; break;
            case 4:
            case 5:
            case 6: result = 4; break;
            case 7:
            case 8:
            case 9: result = 5; break;
            case 10:
            case 11:
            case 12: result = 6; break;
            case 13:
            case 14:
            case 15: result = 7; break;
            default: result = 1; break;
        }

        return result + 1;
    }
    float GetStatValueByStage(EC2.Stats stat, int itemLevel, float stage)
    {
        SheetDataReferences dataSheet = gm.dataSheet;
        float min = dataSheet.GetMinSocketValue(stat);
        float max = dataSheet.GetMaxSocketValue(stat);

        //stat level : BAD(min) - NORMAL(30%) - GOOD(50%) - GREAT(70%) - PERFECT(max)
        float result = Mathf.CeilToInt(min + ((max - min) * ((float)stage / 5)));
        if (stat == EC2.Stats.MaxHP) result *= itemLevel * 10;

        return result;
    }

    EquipSocket SetEmptySocket()
    {
        return new EquipSocket("", 0, 0);
    }

    EquipStats SetEmptyStat()
    {
        return new EquipStats(EC2.Stats.None, 0);
    }

    //Socket Unlock
    public List<EquipSocket> UnlockSocket(string itemId, ItemInstance itemData)
    {
        return UnlockSocket(allItems[itemId], itemData);
    }
    public List<EquipSocket> UnlockSocket(Item item, ItemInstance itemData)
    {
        List<EquipSocket> result = new List<EquipSocket>();

        if (EC2Utils.IsRarityAbove(itemData.currentRarity, Rarity.Legendary))
        {
            result.Add(SetEmptySocket());
            result.Add(SetEmptySocket());
        }
        else if (EC2Utils.IsRarityAbove(itemData.currentRarity, Rarity.Rare))
        {
            result.Add(SetEmptySocket());
        }

        return result;
    }
    public List<EquipSocket> AddSocket(Item item, List<EquipSocket> currentSocket, Rarity targetRarity)
    {
        List<EquipSocket> result = currentSocket;

        if (EC2Utils.IsRarityAbove(targetRarity, Rarity.Rare) && result.Count < 1)
            result.Add(SetEmptySocket());

        if (EC2Utils.IsRarityAbove(targetRarity, Rarity.Epic) && result.Count < 2)
            result.Add(SetEmptySocket());

        return result;
    }

    //Set Effect
    public EquipSet GetRandomSetEffect(Item item)
    {
        if (EC2Utils.IsRarityAbove(item.baseRarity, Rarity.Legendary))
        {
            int random = Random.Range(1, System.Enum.GetNames(typeof(EquipSet)).Length);
            return (EquipSet)random;
        }
        else
        {
            return EquipSet.None;
        }
    }

    //misc
    public string GetRandomMaterial()
    {
        int max = _materials.Length;
        return _materials[Random.Range(0, max)].id;
    }
    public string GetRandomEquip()
    {
        int max = _equipments.Length;
        return _equipments[Random.Range(0, max)].id;
    }
    public string GetItemName(string key)
    {
        return allItems[key].ItemName();
    }
    public Rarity GetItemRarity(string key)
    {
        return allItems[key].baseRarity;
    }
    public ItemIcon GetItemIcon(string key)
    {
        return allItems[key].itemIcon;
    }
    public EquipSlot GetEquipSlot(string key)
    {
        return allItems[key].equipment.equipSlot;
    }

    public List<int> ListSelectedEquipmentType(EquipSlot slot, Hero hero)
    {
        List<int> list = new List<int>();
        for (int i = 0; i < CharacterInventory().equipments.Count; i++)
        {
            if (!string.IsNullOrEmpty(CharacterInventory().equipments[i].id))
            {
                if (slot == EquipSlot.MainWeapon)
                {
                    if (gm.inventory.allItems.ContainsKey(CharacterInventory().equipments[i].id))
                    {
                        var itemData = gm.inventory.allItems[CharacterInventory().equipments[i].id];
                        if (GetEquipSlot(CharacterInventory().equipments[i].id) == slot &&
                            itemData.equipment.characterRequirement == hero)
                        {
                            list.Add(i);
                        }
                    }
                }

                else
                {
                    if (GetEquipSlot(CharacterInventory().equipments[i].id) == slot)
                    {
                        list.Add(i);
                    }
                }
            }
        }
        return list;
    }

    public List<Item> ListCostumesByType(CostumeSlot slot, Hero hero)
    {
        return _costumes.FindAll(item =>
            item.equipment.costume.costumeType != CostumeSet.Default &&
            item.equipment.costume.costumePart == slot &&
            (item.equipment.costume.hero == hero || item.equipment.costume.hero == Hero.None));
    }
    public List<Item> ListCostumesByTypeIncludeDefault(CostumeSlot slot, Hero hero)
    {
        return _costumes.FindAll(item =>
            item.equipment.costume.costumePart == slot &&
            (item.equipment.costume.hero == hero || item.equipment.costume.hero == Hero.None));
    }
    public List<Item> ListCostumesDefault(CostumeSlot slot, Hero hero)
    {
        return _costumes.FindAll(item =>
            item.equipment.costume.costumeType == CostumeSet.Default &&
            item.equipment.costume.costumePart == slot &&
            item.equipment.costume.hero == hero);
    }
    public List<Item> ListUnlockedWeapons(CostumeSlot slot, Hero hero)
    {
        List<Item> eq = _equipments.ToList();

        return eq.FindAll(item =>
            item.equipment.equipmentType == EquipmentCostumeType.EquipmentCostume &&
            item.equipment.characterRequirement == hero &&
            item.equipment.costume.costumePart == slot &&
            item.equipment.costume.hero == hero &&
            gm.userData.data.WeaponAcquired(item));
    }

    public List<Item> GetAllRunes()
    {
        var tempList = new List<Item>();

        foreach (var item in _runes)
        {
            if (runes_forge_exclude.Contains(item.id)) continue;

            tempList.Add(item);
        }

        return tempList;
    }

    public Item[] GetAllMats()
    {
        return _materials;
    }

    public Item[] GetAllEqs()
    {
        return _equipments;
    }

    public Item[] GetAllCons()
    {
        return _consumables;
    }
    public Item[] GetAllKeys()
    {
        return _keyItems;
    }

    public List<Item> GetRunesByRarity(Rarity rarity)
    {
        var tempList = new List<Item>();

        foreach (var item in _runes)
        {
            if (runes_forge_exclude.Contains(item.id)) continue;

            if (item.baseRarity == rarity)
                tempList.Add(item);
        }

        return tempList;
    }

    private List<string> runes_forge_exclude = new List<string>()
    {
        "rune_shard",

        "rune_attack2",
        "rune_defense2",
        "rune_health2",
        "rune_mana2",

        "rune_attack3",
        "rune_defense3",
        "rune_health3",
        "rune_mana3",

        "rune_recharge2",
        "rune_toughness2",
        "rune_tranquility2",
        "rune_wind2",

        "rune_transmutation"
    };


    #region Editor Helper
#if UNITY_EDITOR
    [FoldoutGroup("Helper"), SerializeField]
    private string pathEquipment, pathMaterial, pathConsumable, pathKey, pathRune, pathCostume;
    [FoldoutGroup("Helper"), Button]
    private void RebuildAllItems()
    {
        _equipments = RebuildCategory(pathEquipment);
        _materials = RebuildCategory(pathMaterial);
        _consumables = RebuildCategory(pathConsumable);
        _keyItems = RebuildCategory(pathKey);
        _runes = RebuildCategory(pathRune).ToList();
        _costumes = RebuildCategory(pathCostume).ToList();

        EditorUtility.SetDirty(this);
    }
    private Item[] RebuildCategory(string path)
    {
        var itemData = new List<Item>();
        string[] guids = AssetDatabase.FindAssets("t:Item", new[] { path });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Item item = AssetDatabase.LoadAssetAtPath<Item>(assetPath);
            if (item != null)
            {
                itemData.Add(item);
            }
        }
        return itemData.ToArray();
    }

    [FoldoutGroup("Helper")]
    public Item[] listA, listB, listDiff;
    [FoldoutGroup("Helper"), Button]
    private void GetDiff()
    {
        var onlyInA = listA.Except(listB).ToList();
        var onlyInB = listB.Except(listA).ToList();
        listDiff = onlyInA.Union(onlyInB).ToArray();
    }
    [FoldoutGroup("Helper"), Button]
    private void LogDuplicatesInListA()
    {
        var indexMap = new Dictionary<Item, List<int>>();

        for (int i = 0; i < listA.Length; i++)
        {
            var item = listA[i];
            if (!indexMap.ContainsKey(item))
                indexMap[item] = new List<int>();

            indexMap[item].Add(i);
        }

        foreach (var kvp in indexMap)
        {
            if (kvp.Value.Count > 1)
            {
                Debug.Log($"Duplicate Item: {kvp.Key} at indices: {string.Join(", ", kvp.Value)}");
            }
        }
    }


#endif
    #endregion
}