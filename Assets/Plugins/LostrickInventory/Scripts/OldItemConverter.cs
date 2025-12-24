using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class OldItemConverter : MonoBehaviour
{
    [System.Serializable]
    public struct ItemConversion
    {
        public Item oldItem;
        public Item newItem;
        public bool ignoreRarity;
        public int amtMultiplier;
        public Rarity rarity;
    }

    [ListDrawerSettings(ShowIndexLabels = false, NumberOfItemsPerPage = 3)]
    public List<ItemConversion> oldItems;

    private GameManager gm;
    private Dictionary<string, ItemConversion> database = new Dictionary<string, ItemConversion>();
    private bool inited;

    private void Init()
    {
        if (inited) return;

        gm = GameManager.instance;

        foreach (ItemConversion i in oldItems)
            database.Add(i.oldItem.id, i);

        inited = true;
    }

    public ItemInstance GetConvertedItem(ItemInstance source)
    {
        Init();

        if (string.IsNullOrEmpty(source.id)) return source;
        if (gm.inventory.allItems.ContainsKey(source.id)) return source;
        if (!database.ContainsKey(source.id)) return source;

        //is old item -- convert to new item (FOR EQUIPMENT)
        ItemInstance converted = new ItemInstance();
        ItemConversion item = database[source.id];
        string newId = item.newItem.id;
        Rarity newRarity = item.ignoreRarity ? source.currentRarity : item.rarity;

        print(string.Format("Convert : '{0}' to '{1}'", source.id, newId));

        converted.id = newId;
        converted.currentRarity = newRarity;
        converted.enhancementLevel = source.enhancementLevel;
        converted.quantity += source.quantity * item.amtMultiplier;

        converted.randomStat = gm.inventory.SetSecondStat(newId);
        converted.fixedStats = gm.inventory.SetFixedStat(newId, newRarity, converted.enhancementLevel);

        if (source.socketStats.Count > 0)
        {
            converted.socketStats = new List<EquipSocket>(source.socketStats);
        }
        else
        {
            converted.socketStats = gm.inventory.UnlockSocket(newId, converted);
        }
        return converted;
    }

    public void ConvertKeyItem(ItemInstance source)
    {
        Init();

        if (!database.ContainsKey(source.id)) return;

        ItemConversion item = database[source.id];

        //remove item and add
        var inv = GameManager.instance.inventory;
        inv.CharacterInventory().AddItem(item.newItem.id, source.quantity * item.amtMultiplier, new ItemInstance());
        inv.CharacterInventory().RemoveItem(source.id, 99999);
    }

    public ItemInstance RemoveRareSocket(ItemInstance source)
    {
        if (string.IsNullOrEmpty(source.id)) return source;
        if (!gm.inventory.allItems.ContainsKey(source.id)) return source;

        if (source.currentRarity == Rarity.Rare)
            source.socketStats = new List<EquipSocket>();

        return source;
    }

    public ItemInstance HalvesMPR(ItemInstance source)
    {
        if (string.IsNullOrEmpty(source.id)) return source;
        if (!gm.inventory.allItems.ContainsKey(source.id)) return source;

        if (source.fixedStats.Count > 0)
        {
            foreach (var stat in source.fixedStats)
            {
                if (stat.stats == EC2.Stats.ManaReduce)
                {
                    if (stat.value > 60f)
                        stat.value = stat.value / 2f;
                }
            }
        }

        return source;
    }
}