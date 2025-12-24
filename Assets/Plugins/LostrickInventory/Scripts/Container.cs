using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using System.Text;
using CodeStage.AntiCheat.Storage;
using MEC;

//Player's Inventory Data
//Item's custom attribute (effects, amount, etc) is saved here

public class Container : MonoBehaviour
{
    private const string saveId = "inventory_";
    public Dictionary<string, Item> database;

    [Header("Inventory Limit")]
    public int maxEquipment;
    public int maxMaterial;
    public int maxConsumable;
    public int maxKeyItem;
    public int maxRuneItem;
    public int maxPetItem = 20;

    [Header("Inventory List")]
    [SerializeField]
    public List<ItemInstance> equipments;
    public List<ItemInstance> materials, consumables, keyItems;
    public List<ItemInstance> runes;
    public List<ItemInstance> pets;

    public InventoryType inventoryType;

    bool isInited;
    Inventory inventory;

    [HideInInspector] public string savedata;

    void Start()
    {
        Init();
    }

    public void Init()
    {
        if (isInited) return;
        inventory = Inventory.instance;
        database = inventory.allItems;

        LoadInventory();

        Inventory.instance.AddInventory(inventoryType, this);
        if (inventoryType == InventoryType.CharacterInventory) Inventory.instance.SetSelectedContainer(inventoryType);

        isInited = true;
    }

    public void LoadInventory()
    {
        switch (inventoryType)
        {
            case InventoryType.CharacterInventory:
                LoadMainInventory();
                break;

            case InventoryType.BankInventory:
                LoadStorage();
                break;
        }
    }

    public static int key = 135;
    public static string EncryptDecryptSaveFile(string text)
    {
        StringBuilder inSb = new StringBuilder(text);
        StringBuilder outSb = new StringBuilder(text.Length);
        char c;
        for (int i = 0; i < text.Length; i++)
        {
            c = inSb[i];
            c = (char)(c ^ key);
            outSb.Append(c);
        }
        return outSb.ToString();
    }

    void LoadMainInventory()
    {
        if (ObscuredPrefs.HasKey(EC2Constant.EC2_HERO_KEY_OLD))
        {
            var prefData = ObscuredPrefs.GetString(EC2Constant.EC2_HERO_KEY_OLD, "");

            LoadOldData(prefData);
        }
        else
        {
            var equipmentPrefData = ObscuredPrefs.GetString(EC2Constant.EC2_INVENTORY_EQ_KEY, "");
            var matPrefData = ObscuredPrefs.GetString(EC2Constant.EC2_INVENTORY_MATS_KEY, "");
            var consumPrefData = ObscuredPrefs.GetString(EC2Constant.EC2_INVENTORY_CONSUM_KEY, "");
            var keyPrefData = ObscuredPrefs.GetString(EC2Constant.EC2_INVENTORY_KEYS_KEY, "");

            if (!string.IsNullOrEmpty(equipmentPrefData) &&
                !string.IsNullOrEmpty(matPrefData) &&
                !string.IsNullOrEmpty(consumPrefData) &&
                !string.IsNullOrEmpty(keyPrefData))
            {
                var equipmentData = EC2Utils.FromJson<List<ItemInstance>>(equipmentPrefData);
                var matData = EC2Utils.FromJson<List<ItemInstance>>(matPrefData);
                var consumData = EC2Utils.FromJson<List<ItemInstance>>(consumPrefData);
                var keyData = EC2Utils.FromJson<List<ItemInstance>>(keyPrefData);

                equipments = equipmentData;
                materials = matData;
                consumables = consumData;
                keyItems = keyData;

                CheckLoadedEquipment();
                CheckLoadedMaterials();
                CheckLoadedConsumables();
                CheckLoadedKeys();

                CheckAcquiredEquipments();
            }

            else
            {
                InitNew();
            }

            var runePrefData = ObscuredPrefs.GetString(EC2Constant.EC2_INVENTORY_RUNES_KEY, "");
            if (!string.IsNullOrEmpty(runePrefData))
            {
                var runeData = EC2Utils.FromJson<List<ItemInstance>>(runePrefData);
                runes = runeData;
                CheckLoadedRunes();
            }

            else InitNewRunes();

            var petPrefData = ObscuredPrefs.GetString(EC2Constant.EC2_INVENTORY_PETS_KEY, "");
            if (!string.IsNullOrEmpty(petPrefData))
            {
                var petsData = EC2Utils.FromJson<List<ItemInstance>>(petPrefData);
                pets = petsData;
                CheckLoadedPets();
            }

            else InitNewPets();

            var move = GameManager.instance.inventory.GetComponent<MoveContainer>();
            var Migrator = GameManager.instance.inventory.GetComponent<Migrator>();
            Timing.RunCoroutine(EC2Utils.DelayAndDo(Timing.WaitForOneFrame, () =>
            {
                if (move)
                {
                    move.RemoveAll();
                    move.Migrate();
                }

                if (Migrator)
                {
                    Migrator.CheckMigration();
                }


                ConvertOldKeyItems();
            }));
        }
    }



    private void CheckLoadedPets()
    {
        //===== Key Items =====//
        if (pets == null || pets.Count == 0) pets = new List<ItemInstance>();

        for (int i = 0; i < maxPetItem; i++)
        {
            if (pets.Count < maxPetItem) pets.Add(new ItemInstance());
        }
    }

    private void LoadOldData(string prefData)
    {
        if (!string.IsNullOrEmpty(prefData))
        {
            string saveData;

            saveData = EncryptDecryptSaveFile(prefData);
            var data = EC2Utils.FromJson<UserAdventureData_OLD>(saveData);

            equipments = data.inventoryData.equipments;
            materials = data.inventoryData.materials;
            consumables = data.inventoryData.consumables;
            keyItems = data.inventoryData.keys;
            CheckLoadedEquipment();
            CheckLoadedMaterials();
            CheckLoadedConsumables();
            CheckLoadedKeys();
            CheckLoadedRunes();
            CheckLoadedPets();
        }
        else
        {
            InitNew();
            InitNewRunes();
            InitNewPets();
        }
    }

    private void CheckLoadedKeys()
    {
        //===== Key Items =====//
        if (keyItems == null || keyItems.Count == 0) keyItems = new List<ItemInstance>();
        for (int i = 0; i < maxKeyItem; i++) if (keyItems.Count < maxKeyItem) keyItems.Add(new ItemInstance());
    }

    private void CheckLoadedRunes()
    {
        //===== Key Items =====//
        if (runes == null || runes.Count == 0) runes = new List<ItemInstance>();

        for (int i = 0; i < maxRuneItem; i++)
        {
            if (runes.Count < maxRuneItem) runes.Add(new ItemInstance());
        }

        CheckOldRunes();
    }

    public void CheckOldRunes()
    {
        for (int i = 0; i < runes.Count; i++)
        {
            if (runes[i].id == "rune_shard")
            {
                var t = AddItem(runes[i].id, runes[i].quantity, null);
                runes[i] = new ItemInstance();
            }
        }
    }
    private void CheckLoadedConsumables()
    {
        //===== Consumables =====//
        if (consumables == null || consumables.Count == 0) consumables = new List<ItemInstance>();
        for (int i = 0; i < maxConsumable; i++) if (consumables.Count < maxConsumable) consumables.Add(new ItemInstance());
    }

    private void CheckLoadedMaterials()
    {
        //===== Materials =====//
        if (materials == null || materials.Count == 0) materials = new List<ItemInstance>();
        for (int i = 0; i < maxMaterial; i++) if (materials.Count < maxMaterial) materials.Add(new ItemInstance());
    }

    private void CheckLoadedEquipment()
    {
        //===== Equipment =====//
        if (equipments == null || equipments.Count == 0) equipments = new List<ItemInstance>(); //if no saved data, create new List
        for (int i = 0; i < maxEquipment; i++) if (equipments.Count < maxEquipment) equipments.Add(new ItemInstance()); //fill empty slots with blank items

        // var converter = GameManager.instance.inventory.GetComponent<OldItemConverter>();
        // for (int i = 0; i < maxEquipment; i++)
        // {
        //     equipments[i] = CheckOldItem(equipments[i]); //convert old items
        //     if (converter)
        //         equipments[i] = converter.HalvesMPR(equipments[i]);
        // }

        for (int i = 0; i < maxEquipment; i++)
        {
            //set randomstat to match database
            if (!string.IsNullOrEmpty(equipments[i].id))
            {
                if (equipments[i].randomStat == null) equipments[i].randomStat = inventory.SetSecondStat(equipments[i].id);
                else equipments[i].randomStat = inventory.SetSecondStat(equipments[i].id, equipments[i].randomStat.stats);
            }
        }
    }
    private void CheckAcquiredEquipments()
    {
        for (int i = 0; i < maxEquipment; i++)
        {
            //set randomstat to match database
            if (!string.IsNullOrEmpty(equipments[i].id))
            {
                //check equipment unlocks
                GameManager.instance.userData.data.AddAcquiredItem(inventory.allItems[equipments[i].id]);
            }
        }
    }

    void LoadStorage()
    {
        // profile properties
        var equipmentPrefData = ObscuredPrefs.GetString(EC2Constant.EC2_STORAGE_EQ_KEY, "");
        if (!string.IsNullOrEmpty(equipmentPrefData))
        /*&&
        !string.IsNullOrEmpty(matPrefData) &&
        !string.IsNullOrEmpty(consumPrefData))*/
        {
            var equipmentData = EC2Utils.FromJson<List<ItemInstance>>(equipmentPrefData);
            //var matData = EC2Utils.FromJson<List<ItemInstance>>(matPrefData);
            //var consumData = EC2Utils.FromJson<List<ItemInstance>>(consumPrefData);

            equipments = equipmentData;
            //materials = matData;
            //consumables = consumData;

            CheckLoadedEquipment();
            //CheckLoadedMaterials();
            //CheckLoadedConsumables();
        }
        else
        {
            //print("save not found: container");
            InitNewStorage();
        }
    }

    public string GetJsonData()
    {
        string json = JsonUtility.ToJson(this);
        return json;
    }

    void InitNew()
    {
        //===== Equipment =====//
        equipments = new List<ItemInstance>();
        for (int i = 0; i < maxEquipment; i++) if (equipments.Count < maxEquipment) equipments.Add(new ItemInstance()); //fill all empty slots with empty ItemInstance object

        //===== Materials =====//
        materials = new List<ItemInstance>();
        for (int i = 0; i < maxMaterial; i++) if (materials.Count < maxMaterial) materials.Add(new ItemInstance());

        //===== Consumables =====//
        consumables = new List<ItemInstance>();
        for (int i = 0; i < maxConsumable; i++) if (consumables.Count < maxConsumable) consumables.Add(new ItemInstance());

        //===== Key Items =====//
        keyItems = new List<ItemInstance>();
        for (int i = 0; i < maxKeyItem; i++) if (keyItems.Count < maxKeyItem) keyItems.Add(new ItemInstance());

    }

    void InitNewRunes()
    {
        //===== Rune Items =====//
        runes = new List<ItemInstance>();
        for (int i = 0; i < maxRuneItem; i++) if (runes.Count < maxRuneItem) runes.Add(new ItemInstance());
    }
    void InitNewPets()
    {
        //===== Rune Items =====//
        pets = new List<ItemInstance>();
        for (int i = 0; i < maxPetItem; i++) if (pets.Count < maxPetItem) pets.Add(new ItemInstance());
    }

    void InitNewStorage()
    {
        //===== Equipment =====//
        equipments = new List<ItemInstance>();
        for (int i = 0; i < maxEquipment; i++) if (equipments.Count < maxEquipment) equipments.Add(new ItemInstance()); //fill all empty slots with empty ItemInstance object

        ////===== Materials =====//
        //materials = new List<ItemInstance>();
        //for (int i = 0; i < maxMaterial; i++) if (materials.Count < maxMaterial) materials.Add(new ItemInstance());

        ////===== Consumables =====//
        //consumables = new List<ItemInstance>();
        //for (int i = 0; i < maxConsumable; i++) if (consumables.Count < maxConsumable) consumables.Add(new ItemInstance());
    }

    public bool AddItem(string itemId, int quantity, ItemInstance attribute)
    {
        ItemInstance itemInstance = new ItemInstance();

        bool hasAttribute = false;
        if (attribute != null)
        {
            hasAttribute = !string.IsNullOrEmpty(attribute.id);
        }

        if (!hasAttribute)
        {
            //pick up a fresh item
            itemInstance.id = itemId;
            itemInstance.quantity = quantity;
            //If it's equipment, init the extra status
            Item pickedItem = database[itemId];
            itemInstance.currentRarity = pickedItem.baseRarity;

            if (pickedItem.itemType == ItemType.Equipment)
            {
                Inventory inv = Inventory.instance;

                itemInstance.quantity = 1;
                itemInstance.randomStat = inv.SetSecondStat(pickedItem);
                itemInstance.fixedStats = inv.SetFixedStat(pickedItem, pickedItem.baseRarity, 0);
                itemInstance.socketStats = inv.UnlockSocket(pickedItem, itemInstance);
                //itemInstance.setCategory = inv.GetRandomSetEffect(pickedItem);
            }

            if (pickedItem.itemType == ItemType.Pet)
            {
                itemInstance.quantity = 1;

                if (GameManager.instance)
                {
                    Random.InitState(GameManager.instance.userData.data.randomPetSeed);
                    itemInstance.seed = Random.Range(0, 1234567);
                    GameManager.instance.userData.data.randomPetSeed++;
                    Random.InitState(PlayfabManager.instance.serverTime.Second);
                }
            }
        }
        else
        {
            //pick up player-dropped item
            //itemInstance = attribute;

            itemInstance.id = attribute.id;
            itemInstance.enhancementLevel = attribute.enhancementLevel;
            itemInstance.currentRarity = attribute.currentRarity;
            itemInstance.randomStat = attribute.randomStat;
            itemInstance.fixedStats = attribute.fixedStats;
            itemInstance.socketStats = attribute.socketStats;
            //itemInstance.setCategory = attribute.setCategory;
            itemInstance.currentEXP = attribute.currentEXP;
            itemInstance.seed = attribute.seed;

            if (inventory.allItems[itemInstance.id].itemType == ItemType.Equipment || inventory.allItems[itemInstance.id].itemType == ItemType.Rune
                 || inventory.allItems[itemInstance.id].itemType == ItemType.Pet)
                itemInstance.quantity = 1;
            else itemInstance.quantity += quantity;
        }

        bool addSucceed = AddItem(itemInstance);
        //SaveInventory();


        //if it's consumable, refresh the quickslot gui
        if (database[itemId].itemType == ItemType.Consumable)
        {
            GameManager.instance.RefreshQuickSlot();
        }

        return addSucceed;
    }
    public bool AddItem(ItemInstance itemInstance)
    {
        Item item = database[itemInstance.id];
        int inventoryCount = GetInventoryMaxSlot(item.itemType);
        if (item.itemType != ItemType.Equipment && item.itemType != ItemType.Rune
            && item.itemType != ItemType.Pet)
        {
            //If the added item is Consumable/Material/Key, increase the amount of said item in inventory.
            for (int i = 0; i < inventoryCount; i++)
            {
                if (InventoryList(item.itemType)[i] != null && InventoryList(item.itemType)[i].id == item.id)
                {
                    if (GameManager.instance)
                    {
                        GameManager.instance.userData.HistoryItem_Added(item.id, itemInstance.quantity);
                    }
                    InventoryList(item.itemType)[i].quantity += itemInstance.quantity;

                    return true;
                }
            }
            //If the said item is not available inside inventory, add a new item
        }

        //Add this itemID to it's corresponding Inventory Category
        for (int i = 0; i < inventoryCount; i++)
        {
            if (string.IsNullOrEmpty(InventoryList(item.itemType)[i].id))
            {
                //fill the empty object with new object
                if (GameManager.instance)
                {
                    GameManager.instance.userData.HistoryItem_Added(item.id, itemInstance.quantity);
                }
                InventoryList(item.itemType)[i] = itemInstance;
                return true;
            }
        }

        return false;
    }

    int sortId = 0;
    public string Sort(ItemType activeCategory)
    {
        string result = "";

        switch (sortId)
        {
            case 0: SortAscending(activeCategory); result = "Sort"; break;
            case 1: SortDescending(activeCategory); result = "Sort"; break;
        }

        sortId++;
        if (sortId >= 2) sortId = 0;

        //SaveInventory(activeCategory);

        return result;
    }

    int FinalSortingPriority(ItemInstance item)
    {
        Item i = database[item.id];
        int basePriority = i.sortingPriority;
        //int rarityDifference = item.currentRarity - i.baseRarity;
        int mod = 0;
        switch (item.currentRarity)
        {
            case Rarity.Legendary: mod = 200; break;
            case Rarity.Epic: mod = 100; break;
            case Rarity.Rare: mod = 50; break;
            default: mod = 0; break;
        }
        mod += item.enhancementLevel * 2;

        return basePriority + mod;
    }

    void SortAscending(ItemType type)
    {
        switch (type)
        {
            case ItemType.Equipment:
                equipments = equipments.OrderBy(e => (e != null && e.id != null) ? FinalSortingPriority(e) : 10000)
                    //(e => e != null ? string.IsNullOrEmpty(e.id) : true)
                    //.ThenBy(e => (e != null && e.id != null) ? database[e.id].sortingPriority : 0)
                    //.ThenBy(e => (e != null) ? e.id : string.Empty)
                    .ToList(); break;
            case ItemType.Material:
                materials = materials.OrderBy(e => (e != null && e.id != null) ? database[e.id].sortingPriority : 10000)
                    .ToList(); break;
            case ItemType.Consumable:
                consumables = consumables.OrderBy(e => (e != null && e.id != null) ? database[e.id].sortingPriority : 10000)
                    .ToList(); break;
            case ItemType.Rune:
                runes = runes.OrderBy(e => (e != null && e.id != null) ? FinalSortingPriority(e) : 10000)
                    .ToList(); break;
            case ItemType.KeyItem:
                keyItems = keyItems.OrderBy(e => (e != null && e.id != null) ? database[e.id].sortingPriority : 10000)
                    .ToList(); break;
            case ItemType.Pet:
                pets = pets.OrderBy(e => (e != null && e.id != null) ? database[e.id].sortingPriority : 10000)
                    .ToList(); break;
        }
    }
    public void SortDescending(ItemType type)
    {
        switch (type)
        {
            case ItemType.Equipment:
                equipments = equipments.OrderByDescending(e => (e != null && e.id != null) ? FinalSortingPriority(e) : 0)
                    /*equipments.OrderBy(e => e != null ? string.IsNullOrEmpty(e.id) : true)
                    .ThenByDescending(e => (e != null && e.id != null) ? database[e.id].sortingPriority : 0)
                    .ThenBy(e => (e != null) ? e.id : string.Empty)*/
                    .ToList(); break;
            case ItemType.Material:
                materials = materials.OrderByDescending(e => (e != null && e.id != null) ? database[e.id].sortingPriority : 0)
                    .ToList(); break;
            case ItemType.Consumable:
                consumables = consumables.OrderByDescending(e => (e != null && e.id != null) ? database[e.id].sortingPriority : 0)
                    .ToList(); break;
            case ItemType.Rune:
                runes = runes.OrderByDescending(e => (e != null && e.id != null) ? FinalSortingPriority(e) : 0)
                    .ToList(); break;
            case ItemType.KeyItem:
                keyItems = keyItems.OrderByDescending(e => (e != null && e.id != null) ? database[e.id].sortingPriority : 0)
                    .ToList(); break;
            case ItemType.Pet:
                pets = pets.OrderByDescending(e => (e != null && e.id != null) ? database[e.id].sortingPriority : 0)
                    .ToList(); break;
        }
    }
    public void ResetSortID()
    {
        sortId = 0;
    }

    public int GetInventoryMaxSlot(ItemType category)
    {
        int result = 0;
        switch (category)
        {
            case ItemType.Equipment: result = maxEquipment; break;
            case ItemType.Material: result = maxMaterial; break;
            case ItemType.Consumable: result = maxConsumable; break;
            case ItemType.KeyItem: result = maxKeyItem; break;
            case ItemType.Rune: result = maxRuneItem; break;
            case ItemType.Pet: result = maxPetItem; break;
        }
        return result;
    }
    public List<ItemInstance> InventoryList(ItemType type)
    {
        switch (type)
        {
            case ItemType.Equipment: return equipments;
            case ItemType.Material: return materials;
            case ItemType.Consumable: return consumables;
            case ItemType.KeyItem: return keyItems;
            case ItemType.Rune: return runes;
            case ItemType.Pet: return pets;
            default: return equipments;
        }
    }
    public int GetInventoryCount(ItemType type)
    {
        int sum = 0;

        for (int i = 0; i < InventoryList(type).Count; i++)
        {
            if (!string.IsNullOrEmpty(InventoryList(type)[i].id))
            {
                sum++;
            }
        }

        return sum;
    }
    public bool IsInventoryFull(ItemType type)
    {
        return GetInventoryCount(type) >= GetInventoryMaxSlot(type);
    }
    public bool IsInventoryFull(ItemType type, int addedItem)
    {
        return GetInventoryCount(type) + addedItem >= GetInventoryMaxSlot(type);
    }

    public void RemoveItem(string itemId)
    {
        RemoveItem(itemId, 1);
    }
    public bool RemoveItem(string ItemId, int quantity)
    {
        Item item = database[ItemId];
        ItemType type = item.itemType;
        float maxItemSlot = GetInventoryMaxSlot(type);

        for (int i = 0; i < maxItemSlot; i++)
        {
            if (InventoryList(type)[i] != null && InventoryList(type)[i].id == ItemId)
            {
                //if (type == ItemType.KeyItem) InventoryList(type)[i].quantity = 0;
                //else InventoryList(type)[i].quantity -= quantity;
                if (GameManager.instance)
                {
                    GameManager.instance.userData.HistoryItem_Removed(ItemId, quantity);
                }
                InventoryList(type)[i].quantity -= quantity;
                if (InventoryList(type)[i].quantity <= 0)
                {
                    //quantity below 0, remove from inventory
                    // InventoryList(type)[i] = new ItemInstance();
                    InventoryList(type)[i].id = string.Empty;
                }
                return true;
            }
        }

        return false;
    }
    public void RemoveItem(string ItemId, ItemType types, int removeQty)
    {
        ItemType type = types;
        float maxItemSlot = GetInventoryMaxSlot(type);

        for (int i = 0; i < maxItemSlot; i++)
        {
            if (InventoryList(type)[i] != null && InventoryList(type)[i].id == ItemId)
            {
                InventoryList(type)[i].quantity -= removeQty;
                if (InventoryList(type)[i].quantity <= 0) InventoryList(type)[i] = new ItemInstance();
                return;
            }
        }
    }
    public bool RemoveItem(int index, ItemType category)
    {
        return RemoveItem(index, category, false);
    }
    public bool RemoveItem(int index, ItemType category, bool disableItemDrop)
    {
        return RemoveItem(index, category, disableItemDrop, true);
    }
    public bool RemoveItem(int index, ItemType category, bool disableItemDrop, bool checkIfSocketed)
    {
        if (category == ItemType.Equipment)
        {
            if (checkIfSocketed)
                if (IsSocketed(index))
                {
                    GameManager.instance.sfx.ItemFull();
                    SnackbarManager.instance.PopSnackbar("eq_has_socket");
                    return false;
                }
        }

        if (!disableItemDrop)
        {
            //get item attributes
            ItemInstance attribute = new ItemInstance();
            attribute = InventoryList(category)[index];

            Item item = database[attribute.id];

            //drop item
            ItemDropManager.instance.SpawnItem(GameManager.instance.ActiveHero.transform.position + Vector3.up * 2, item, attribute,
                isDropFromBag: true);
        }

        //delete item from inventory
        InventoryList(category)[index] = new ItemInstance();

        return true;
        //SaveInventory(category);
    }

    public bool IsSocketed(int index)
    {
        ItemInstance item = InventoryList(ItemType.Equipment)[index];

        if (item.socketStats == null) return false;
        if (item.socketStats.Count == 0) return false;

        //get item attributes
        return !string.IsNullOrEmpty(item.socketStats[0].socketID);
    }

    public ItemInstance GetItemData(string itemId)
    {
        ItemType type = database[itemId].itemType;
        foreach (ItemInstance item in InventoryList(type))
            if (item != null && item.id == itemId)
                return item;

        return new ItemInstance();
    }

    public List<int> GetEquipmentIndexes(string itemID)
    {
        var tempList = new List<int>();

        for (int i = 0; i < equipments.Count; i++)
        {
            var data = equipments[i];
            if (data.id == itemID) tempList.Add(i);
        }

        return tempList;
    }

    public List<int> GetEquipmentIndexes(string itemID, Rarity rarity)
    {
        var tempList = new List<int>();

        for (int i = 0; i < equipments.Count; i++)
        {
            var data = equipments[i];
            if (data.id == itemID && data.currentRarity == rarity) tempList.Add(i);
        }

        return tempList;
    }
    public List<int> GetEquipmentIndexes(string itemID, Rarity rarity, int excludedIndex)
    {
        var tempList = new List<int>();

        for (int i = 0; i < equipments.Count; i++)
        {
            var data = equipments[i];
            if (data.id == itemID && data.currentRarity == rarity)
            {
                if (i == excludedIndex) continue;
                tempList.Add(i);
            }
        }

        return tempList;
    }

    public List<ItemInstance> GetSocketableItemDataBySlot(EquipSlot equipSlot)
    {
        List<ItemInstance> _temp = new List<ItemInstance>();
        foreach (var rune in runes)
        {
            if (string.IsNullOrEmpty(rune.id)) continue;
            var _itemData = database[rune.id];
            if (_itemData.socket.type != SocketType.None && _itemData.socket.socketableWith.Contains(equipSlot))
                _temp.Add(rune);
        }

        return _temp;
    }

    public List<ItemInstance> GetWeaponByHero(Hero hero)
    {
        var tempList = new List<ItemInstance>();
        foreach (var item in equipments)
        {
            if (string.IsNullOrEmpty(item.id)) continue;
            if (!inventory.allItems.ContainsKey(item.id)) continue;
            var itemData = inventory.allItems[item.id];

            if (itemData.equipment.characterRequirement == hero)
                tempList.Add(item);
        }

        return tempList;
    }
    public int GetQuantity(string itemId)
    {
        //print("fin : " + itemId);
        int result = 0;
        try
        {
            ItemType type = database[itemId].itemType;
            foreach (ItemInstance item in InventoryList(type))
                if (item != null && item.id == itemId)
                    result = item.quantity;
        }
        catch
        {
            print("Item not found: " + itemId);
        }

        return result;
    }

    public int GetEquipmentPossesion(Item item)
    {
        return equipments.FindAll(itemData => itemData.id == item.id).Count;
    }
    public void PrintAmount(string itemId)
    {
        //print(database[itemId].itemName + " amount = " + GetQuantity(itemId));
    }

    public void SaveInventoryData()
    {
        var equipmentData = EC2Utils.ToJson(equipments);
        var matData = EC2Utils.ToJson(materials);
        var consumData = EC2Utils.ToJson(consumables);
        var keyData = EC2Utils.ToJson(keyItems);
        var runeData = EC2Utils.ToJson(runes);

        ObscuredPrefs.SetString(EC2Constant.EC2_INVENTORY_EQ_KEY, equipmentData);
        ObscuredPrefs.SetString(EC2Constant.EC2_INVENTORY_MATS_KEY, matData);
        ObscuredPrefs.SetString(EC2Constant.EC2_INVENTORY_CONSUM_KEY, consumData);
        ObscuredPrefs.SetString(EC2Constant.EC2_INVENTORY_KEYS_KEY, keyData);
        ObscuredPrefs.SetString(EC2Constant.EC2_INVENTORY_RUNES_KEY, runeData);

        if (ObscuredPrefs.HasKey(EC2Constant.EC2_HERO_KEY_OLD))
            ObscuredPrefs.DeleteKey(EC2Constant.EC2_HERO_KEY_OLD);

        ObscuredPrefs.Save();

    }

    public void SaveStorageData(bool cloudBatch)
    {
        var equipmentData = EC2Utils.ToJson(equipments);
        //var matData = EC2Utils.ToJson(materials);
        //var consumData = EC2Utils.ToJson(consumables);

        ObscuredPrefs.SetString(EC2Constant.EC2_STORAGE_EQ_KEY, equipmentData);
        //ObscuredPrefs.SetString(EC2Constant.EC2_STORAGE_MATS_KEY, matData);
        //ObscuredPrefs.SetString(EC2Constant.EC2_STORAGE_CONSUM_KEY, consumData);
        ObscuredPrefs.Save();

        if (PlayfabManager.instance)
        {
            if (cloudBatch)
            {
                //PlayfabManager.instance.AddToBatchSave(EC2Constant.EC2_STORAGE_EQ_KEY, equipmentData);
                //PlayfabManager.instance.AddToBatchSave(EC2Constant.EC2_STORAGE_MATS_KEY, matData);
                //PlayfabManager.instance.AddToBatchSave(EC2Constant.EC2_STORAGE_CONSUM_KEY, consumData);
            }

            else
            {
                //PlayfabManager.instance.SaveDataByKey(EC2Constant.EC2_STORAGE_EQ_KEY, equipmentData);
                //PlayfabManager.instance.SaveDataByKey(EC2Constant.EC2_STORAGE_MATS_KEY, matData);
                //PlayfabManager.instance.SaveDataByKey(EC2Constant.EC2_STORAGE_CONSUM_KEY, consumData);
            }
        }
    }

    public void CreateStorageSaveData()
    {
#if UNITY_STANDALONE

        savedata =
         EC2Utils.ToJson(equipments, Formatting.Indented)
         + "\n#" + EC2Utils.ToJson(materials, Formatting.Indented)
         + "\n#" + EC2Utils.ToJson(consumables, Formatting.Indented);
#else

        var data = new UserStorageData()
        {
            equipments = equipments,
            consumables = consumables,
            materials = materials
        };

        savedata = EC2Utils.ToJson(data);
        //PlayerPrefs.SetString(saveId + "equipments", EC2Utils.ToJson(equipments, Formatting.Indented));
        //PlayerPrefs.SetString(saveId + "materials", EC2Utils.ToJson(materials, Formatting.Indented));
        //PlayerPrefs.SetString(saveId + "consumables", EC2Utils.ToJson(consumables, Formatting.Indented));
#endif
    }

    private void ConvertOldKeyItems()
    {
        OldItemConverter convert = GameManager.instance.inventory.GetComponent<OldItemConverter>();
        foreach (var keyitem in keyItems)
        {
            if (string.IsNullOrEmpty(keyitem.id)) continue;
            convert.ConvertKeyItem(keyitem);
        }
    }

    ItemInstance CheckOldItem(ItemInstance source)
    {
        OldItemConverter convert = GameManager.instance.inventory.GetComponent<OldItemConverter>();
        return convert.GetConvertedItem(source);
    }

    public ItemInstance FindItem(ItemType type, string id)
    {
        var items = InventoryList(type);

        foreach (var item in items)
        {
            if (item.id == id)
                return item;
        }

        return new ItemInstance();
    }
}

[System.Serializable]
public class UserInventoryData
{
    public List<ItemInstance> equipments;
    public List<ItemInstance> materials;
    public List<ItemInstance> consumables;
    public List<ItemInstance> keys;
}

[System.Serializable]
public class UserStorageData
{
    public List<ItemInstance> equipments;
    public List<ItemInstance> materials;
    public List<ItemInstance> consumables;
}