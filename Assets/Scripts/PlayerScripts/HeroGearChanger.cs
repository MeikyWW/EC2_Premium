using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class HeroGearChanger : MonoBehaviour
{
    [System.Serializable]
    public struct HeroChangeableEquip
    {
        [ShowIf("@string.IsNullOrEmpty(id)")]
        public Item item;
        [ShowIf("@item == null")]
        public string id;
        public GameObject weaponPrefab;
    }

    public GameObject weaponHolder;
    [ListDrawerSettings(ShowIndexLabels = false), LabelText("Equipments")]
    public List<HeroChangeableEquip> weaponModelData;

    [ListDrawerSettings(ShowIndexLabels = false), LabelText("Costume")]
    public List<HeroChangeableEquip> cosWeaponModelData;

    private Dictionary<string, HeroChangeableEquip> weaponDatabaseDictionary;
    bool isEqInit;

    void InitEquipmentChanger()
    {
        if (isEqInit) return;

        weaponDatabaseDictionary = new Dictionary<string, HeroChangeableEquip>();
        foreach (HeroChangeableEquip e in weaponModelData)
        {
            string id = e.item == null ? "base" : e.item.id;
            weaponDatabaseDictionary.Add(id, e);
        }
        foreach (HeroChangeableEquip e in cosWeaponModelData)
        {
            weaponDatabaseDictionary.Add(e.id, e);
        }

        isEqInit = true;
    }
    public void ChangeWeapon(string itemId)
    {
        InitEquipmentChanger();

        HeroChangeableEquip selected;
        if (!string.IsNullOrEmpty(itemId) && weaponDatabaseDictionary.ContainsKey(itemId))
            selected = weaponDatabaseDictionary[itemId];
        else selected = weaponModelData[0];

        //if there's already spawned equipment under weaponHolder, delete it
        if (weaponHolder.transform.childCount > 0)
            Destroy(weaponHolder.transform.GetChild(0).gameObject);

        //spawn selected weapon
        Instantiate(selected.weaponPrefab, weaponHolder.transform);
    }

}
