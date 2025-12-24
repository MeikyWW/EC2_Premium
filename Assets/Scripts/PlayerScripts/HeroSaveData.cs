using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using MEC;
using System;
using CodeStage.AntiCheat.Storage;
using CodeStage.AntiCheat.ObscuredTypes;
using Sirenix.OdinInspector;
using System.Linq;

public class HeroSaveData : MonoBehaviour
{
    HeroStatus _status;
    SheetDataReferences dataSheet;
    public bool saveDataLoaded;

    public int heroIndex;
    //public bool physicalDmg; //increase dmg by STR, otherwise by INT

    public HeroData data;
    [HideInInspector] public ObscuredInt currentLevel;
    [HideInInspector] public ObscuredInt currentEXP, currentSTR, currentINT, currentDEX, currentAGI, currentVIT;
    [HideInInspector] public string savedata;

    public static int key = 135;
    private void Awake()
    {
        _status = GetComponent<HeroStatus>();
    }
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
    public void Load()
    {
        if (saveDataLoaded) return;
        LoadDesktop();
        saveDataLoaded = true;
    }
    public void Save()
    {
        savedata = EC2Utils.ToJson(data);

        //GameManager.instance.userData.SetHeroThumbnail(data.LEVEL);
    }
    void LoadDesktop()
    {
        // profile properties

        string prefData;
        bool isLoadingOldData;

        if (ObscuredPrefs.HasKey(EC2Constant.EC2_HERO_KEY_OLD))
        {
            isLoadingOldData = true;
            prefData = ObscuredPrefs.GetString(EC2Constant.EC2_HERO_KEY_OLD, "");
        }

        else
        {
            isLoadingOldData = false;
            prefData = ObscuredPrefs.GetString(EC2Constant.EC2_HERO_KEY, "");
        }

        if (!string.IsNullOrEmpty(prefData))
        {
            string savedata;
            int heroIndex;
            if (isLoadingOldData)
            {
                savedata = EncryptDecryptSaveFile(prefData);
                var dataSaved = EC2Utils.FromJson<UserAdventureData_OLD>(savedata);
                heroIndex = dataSaved.heroes.FindIndex(x => x.HERO == _status.heroReference.hero);

                if (heroIndex != -1)
                {
                    data = dataSaved.heroes[heroIndex];
                }
            }

            else
            {
                savedata = prefData;
                var dataSaved = EC2Utils.FromJson<UserAdventureData>(savedata);
                heroIndex = dataSaved.heroes.FindIndex(x => x.HERO == _status.heroReference.hero);

                if (heroIndex != -1)
                {
                    data = dataSaved.heroes[heroIndex];
                }
            }

            if (data.cosuTransmog == null || data.cosuTransmog.Count == 0)
            {
                //fill all empty equipment slots with empty ItemInstance object
                data.cosuTransmog = new List<CostumeInstance>();
                for (int i = 0; i < 7; i++) if (data.cosuTransmog.Count < 7) data.cosuTransmog.Add(new CostumeInstance());
            }

            if (heroIndex != -1)
            {
                currentLevel = data.LEVEL;
                currentEXP = data.EXP;
                currentSTR = data.STR;
                currentINT = data.INT;
                currentDEX = data.DEX;
                currentAGI = data.AGI;
                currentVIT = data.VIT;
                SetWeaponModel();
                CreateMasteryData();
            }
            else
            {
                CreateHeroBaseData();
            }
        }
        else
        {
            //if no saved data, create the base data
            CreateHeroBaseData();
        }

        //ApplyAuraWeapon();
    }

    public void AssignHeroData()
    {
        _status = GetComponent<HeroStatus>();

        if (data.equipment == null) data.equipment = new List<ItemInstance>();
        for (int i = data.equipment.Count; i < 7; i++) if (data.equipment.Count < 7) data.equipment.Add(new ItemInstance());

        if (data.costumes == null) data.costumes = new List<CostumeInstance>();
        for (int i = data.costumes.Count; i < EC2Utils.GetTotalCostumeSlot(); i++) if (data.costumes.Count < 7) data.costumes.Add(new CostumeInstance());

        if (data.cosuTransmog == null) data.cosuTransmog = new List<CostumeInstance>();
        for (int i = data.cosuTransmog.Count; i < 7; i++) if (data.cosuTransmog.Count < 7) data.cosuTransmog.Add(new CostumeInstance());

        currentLevel = data.LEVEL;
        currentEXP = data.EXP;
        currentSTR = data.STR;
        currentINT = data.INT;
        currentDEX = data.DEX;
        currentAGI = data.AGI;
        currentVIT = data.VIT;
        SetWeaponModel();
        CreateMasteryData();

        saveDataLoaded = true;
    }
    public void ApplyAuraWeapon()
    {
        if (!string.IsNullOrEmpty(data.costumes[0].itemData.id))
        {
            if (!data.costumes[0].isHid)
                return;
        }

        if (!string.IsNullOrEmpty(data.equipment[0].id))
        {
            GetComponent<CostumeChanger>().ApplyAura(data.equipment[0].enhancementLevel);
        }
    }

    public static HeroInfo LoadHeroSaveData(Hero hero)
    {
        // profile properties
        HeroInfo temp = new HeroInfo();

        string prefData;
        bool isLoadingOldData;

        if (ObscuredPrefs.HasKey(EC2Constant.EC2_HERO_KEY_OLD))
        {
            isLoadingOldData = true;
            prefData = ObscuredPrefs.GetString(EC2Constant.EC2_HERO_KEY_OLD, "");
        }

        else
        {
            isLoadingOldData = false;
            prefData = ObscuredPrefs.GetString(EC2Constant.EC2_HERO_KEY, "");
        }

        if (!string.IsNullOrEmpty(prefData))
        {
            HeroData data = new HeroData();
            string savedata;
            int heroIndex;
            if (isLoadingOldData)
            {
                savedata = EncryptDecryptSaveFile(prefData);
                var dataSaved = EC2Utils.FromJson<UserAdventureData_OLD>(savedata);
                heroIndex = dataSaved.heroes.FindIndex(x => x.HERO == hero);

                if (heroIndex != -1)
                {
                    data = dataSaved.heroes[heroIndex];
                }
            }

            else
            {
                savedata = prefData;
                var dataSaved = EC2Utils.FromJson<UserAdventureData>(savedata);
                heroIndex = dataSaved.heroes.FindIndex(x => x.HERO == hero);

                if (heroIndex != -1)
                {
                    data = dataSaved.heroes[heroIndex];
                }
            }

            if (heroIndex != -1)
            {
                temp.level = data.LEVEL;
            }
        }

        if (temp.level == 0)
        {
            TextAsset baseStat = Resources.Load(hero.ToString().ToLower() + "_base") as TextAsset;
            var data = EC2Utils.FromJson<HeroData>(baseStat.text);

            temp.level = data.LEVEL;
            if (hero != Hero.Claris)
            {
                if (GameManager.instance.userData.CurrentQuestID <
                    GameManager.instance.heroDatabase.GetHeroReference(hero).joinedPartyQuestID)
                {
                    var heroInfo = LoadHeroSaveData(Hero.Claris);
                    if (heroInfo != null)
                    {
                        if (heroInfo.level < temp.level)
                            temp.level = heroInfo.level;
                    }
                }
            }
        }

        return temp;
    }

    public void GetHeroLevelAndSkillLevel(out int _level, out int _skillLevel)
    {
        _level = 0;
        _skillLevel = 0;
        string prefData;
        bool isLoadingOldData;

        if (ObscuredPrefs.HasKey(EC2Constant.EC2_HERO_KEY_OLD))
        {
            isLoadingOldData = true;
            prefData = ObscuredPrefs.GetString(EC2Constant.EC2_HERO_KEY_OLD, "");
        }

        else
        {
            isLoadingOldData = false;
            prefData = ObscuredPrefs.GetString(EC2Constant.EC2_HERO_KEY, "");
        }

        for (int i = 0; i < System.Enum.GetValues(typeof(Hero)).Length; i++)
        {
            if (!string.IsNullOrEmpty(prefData))
            {
                int _tempLevel = 0;
                int _tempSkillLevel = 0;

                HeroData data = new HeroData();
                string savedata;
                int heroIndex;
                if (isLoadingOldData)
                {
                    savedata = EncryptDecryptSaveFile(prefData);
                    var dataSaved = EC2Utils.FromJson<UserAdventureData_OLD>(savedata);
                    heroIndex = dataSaved.heroes.FindIndex(x => x.HERO == (Hero)i);

                    if (heroIndex != -1)
                    {
                        data = dataSaved.heroes[heroIndex];
                    }
                }

                else
                {
                    savedata = prefData;
                    var dataSaved = EC2Utils.FromJson<UserAdventureData>(savedata);
                    heroIndex = dataSaved.heroes.FindIndex(x => x.HERO == (Hero)i);

                    if (heroIndex != -1)
                    {
                        data = dataSaved.heroes[heroIndex];
                    }
                }

                if (heroIndex != -1)
                {
                    _tempLevel = data.LEVEL;

                    for (int levelIndex = 0; levelIndex < data.skillLevels.Length; levelIndex++)
                    {
                        _tempSkillLevel += data.skillLevels[levelIndex];
                    }
                }
                else
                {
                }

                _level += _tempLevel;
                _skillLevel += _tempSkillLevel;
            }
        }
    }

    void CreateHeroBaseData()
    {
        //Debug.Log("create base data #" + heroIndex);
        TextAsset baseStat = Resources.Load(_status.heroReference.hero.ToString().ToLower() + "_base") as TextAsset;
        data = EC2Utils.FromJson<HeroData>(baseStat.text);

        if (data.equipment == null || data.equipment.Count == 0)
        {
            //fill all empty equipment slots with empty ItemInstance object
            data.equipment = new List<ItemInstance>();
            for (int i = 0; i < 7; i++) if (data.equipment.Count < 7) data.equipment.Add(new ItemInstance());
        }

        if (data.costumes == null || data.costumes.Count == 0)
        {
            //fill all empty equipment slots with empty ItemInstance object
            data.costumes = new List<CostumeInstance>();
            for (int i = 0; i < 7; i++) if (data.costumes.Count < 7) data.costumes.Add(new CostumeInstance());
        }

        if (data.cosuTransmog == null || data.cosuTransmog.Count == 0)
        {
            //fill all empty equipment slots with empty ItemInstance object
            data.cosuTransmog = new List<CostumeInstance>();
            for (int i = 0; i < 7; i++) if (data.cosuTransmog.Count < 7) data.cosuTransmog.Add(new CostumeInstance());
        }

        data.HERO = _status.heroReference.hero;
        currentLevel = data.LEVEL;

        if (_status.heroReference.hero != Hero.Claris)
        {
            if (GameManager.instance.userData.CurrentQuestID < _status.heroReference.joinedPartyQuestID)
            {
                var heroInfo = LoadHeroSaveData(Hero.Claris);
                if (heroInfo != null)
                {
                    if (heroInfo.level < currentLevel)
                        currentLevel = heroInfo.level;
                }
            }
        }

        currentEXP = data.EXP;
        currentSTR = data.STR;
        currentINT = data.INT;
        currentDEX = data.DEX;
        currentAGI = data.AGI;
        currentVIT = data.VIT;
        SetWeaponModel();
        CreateMasteryData();
        Save();
    }
    public void ResetLevel()
    {
        TextAsset baseStat = Resources.Load(_status.heroReference.hero.ToString().ToLower() + "_base") as TextAsset;
        HeroData temp = EC2Utils.FromJson<HeroData>(baseStat.text);

        currentLevel = temp.LEVEL;
        currentEXP = 0;

        data.skillLevels = temp.skillLevels;
        data.skillExp = temp.skillExp;
        data.skillSlot = temp.skillSlot;

        _status.control.SetSkillButtons();
        ResetAttribute();
    }
    public void ResetAttribute()
    {
        TextAsset baseStat = Resources.Load(_status.heroReference.hero.ToString().ToLower() + "_base") as TextAsset;
        HeroData temp = EC2Utils.FromJson<HeroData>(baseStat.text);

        currentSTR = temp.STR;
        currentINT = temp.INT;
        currentAGI = temp.AGI;
        currentDEX = temp.DEX;
        currentVIT = temp.VIT;

        data.STR = currentSTR;
        data.INT = currentINT;
        data.AGI = currentAGI;
        data.DEX = currentDEX;
        data.VIT = currentVIT;

        _status.CalculateHeroAttributes();
        //data.masteryLvl = new int[8];
        Save();
    }
    public void ResetMastery()
    {
        data.masteryLvl = new int[8];
        GameManager.instance.pauseMenu.SelectedHero.control.InitAllMasteries();
        GameManager.instance.pauseMenu.SelectedHero.CalculateHeroAttributes();
        Save();
    }
    void SetWeaponModel()
    {
        //Set main weapon
        if (data.equipment == null || data.equipment.Count == 0)
        {
            //fill all empty equipment slots with empty ItemInstance object
            data.equipment = new List<ItemInstance>();
            for (int i = 0; i < 7; i++) if (data.equipment.Count < 7) data.equipment.Add(new ItemInstance());
        }
        //else GetComponent<HeroGearChanger>().ChangeWeapon(data.equipment[0].id);
        SetCostumeData();
    }

    public void SetCostumeData()
    {
        if (data.costumes == null || data.costumes.Count == 0)
        {
            data.costumes = new List<CostumeInstance>();
            for (int i = 0; i < EC2Utils.GetTotalCostumeSlot(); i++)
                data.costumes.Add(new CostumeInstance());
        }

        if (data.cosuTransmog == null || data.cosuTransmog.Count == 0)
        {
            data.cosuTransmog = new List<CostumeInstance>();
            for (int i = 0; i < EC2Utils.GetTotalCostumeSlot(); i++)
                data.cosuTransmog.Add(new CostumeInstance());
        }

        var cosuChanger = GetComponent<CostumeChanger>();
        cosuChanger.DisableAll();
        cosuChanger.ClearAllSpawned();

        for (int i = 0; i < EC2Utils.GetTotalCostumeSlot(); i++)
        {
            string gearID = string.Empty;
            int encLevel = 0;
            int index = 0;

            CostumeInstance transmog = data.cosuTransmog[i];
            CostumeInstance costume = data.costumes[i];
            ItemInstance equipment = data.equipment[i];

            try
            {
                encLevel = equipment.enhancementLevel;
            }
            catch { }

            if (!string.IsNullOrEmpty(transmog.itemData.id))
            {
                Item item = Inventory.instance.allItems[transmog.itemData.id];
                if (item)
                {
                    gearID = item.id;
                    index = transmog.textureIndex;
                }
            }

            else
            {
                if (!string.IsNullOrEmpty(costume.itemData.id) && !costume.isHid)
                {
                    Item item = Inventory.instance.allItems[costume.itemData.id];
                    Color rarityColor = new Color(252f, 255f, 126f, 255f);
                    if (item)
                    {
                        gearID = item.id;
                        index = costume.textureIndex;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(equipment.id))
                    {
                        if (Inventory.instance.allItems.ContainsKey(equipment.id))
                        {
                            Item item = Inventory.instance.allItems[equipment.id];
                            if (item.equipment.equipmentType == EquipmentCostumeType.EquipmentCostume)
                            {
                                gearID = item.id;
                                index = 0;
                            }

                            else
                            {
                                gearID = SetDefaultCostume(i);
                                //encLevel = 0;
                                index = 0;
                            }
                        }
                    }

                    else
                    {
                        gearID = SetDefaultCostume(i);
                        //encLevel = 0;
                        index = 0;
                    }
                }
            }

            if (Inventory.instance.allItems.ContainsKey(gearID))
            {
                Item item = Inventory.instance.allItems[gearID];
                if (item)
                    SetPart(item.equipment.costume, index);
            }
            else
            {
                SetPart(new EC2Costume()
                {
                    costumePart = EC2Utils.GetCostumeSlot(i),
                }, 0);
            }

            //apply Aura weapon
            if (i == 0)
            {
                if (!string.IsNullOrEmpty(gearID.ToString()))
                    GetComponent<CostumeChanger>().ApplyAura(encLevel);
            }
        }
    }

    private void SetPart(EC2Costume item, int index)
    {
        //Debug.Log($"set {_status.heroReference.hero} costume        ");

        var cosuChanger = GetComponent<CostumeChanger>();
        var weaponPart = GetComponent<CostumeChanger>().ChangeCostume(item, index);
        if (weaponPart)
        {
            cosuChanger.SetSpawned(weaponPart, () =>
            {
                if (_status)
                    if (_status.control) _status.control.Disengage();
            });

            //Edna
            if (cosuChanger.weaponGloballySpawned)
            {
                var followWeaponPos = weaponPart.GetComponent<FollowObjectLerp>();
                if (followWeaponPos)
                {
                    weaponPart.transform.position = cosuChanger.weaponPos.position;
                    followWeaponPos.SetTarget(cosuChanger.weaponPos);
                }
            }
        }

        if (GameManager.instance)
            GameManager.instance.SetStance();
    }

    public string SetDefaultCostume(int i)
    {
        string itemKey = "default_" + GetComponent<HeroStatus>().heroReference.
            hero.ToString().ToLower() + "_" + EC2Utils.GetCostumeSlot(i).ToString().ToLower();
        return itemKey;
    }

    void CreateMasteryData()
    {
        if (data.masteryLvl == null || data.masteryLvl.Length <= 0)
        {
            //create base mastery level (0)
            data.masteryLvl = new int[8];
        }

    }
    public void SetSkillSlot(int slot, int skillId)
    {
        try
        {
            data.skillSlot[slot] = skillId;
        }
        catch
        {
            data.skillSlot = new int[4] { -1, -1, -1, -1 };
            data.skillSlot[slot] = skillId;
        }
    }
    public bool SetEquipment(Item item, ItemInstance itemData, int equipSlot)
    {
        if (string.IsNullOrEmpty(itemData.id))
        {
            //equip removal
            data.equipment[equipSlot] = new ItemInstance();
            SetWeaponModel();

            return true;
        }

        bool[] checks = new bool[2];

        //check character requirement
        var heroReq = item.equipment.characterRequirement;
        if (heroReq != Hero.None)
        {
            if (heroReq == _status.heroReference.hero) checks[0] = true;
            else checks[0] = false;
        }
        else checks[0] = true;

        //check level requirement
        int levelReq = item.equipment.levelRequirement;
        if (currentLevel < levelReq) checks[1] = false;
        else checks[1] = true;


        int checksum = 0;
        for (int i = 0; i < checks.Length; i++)
            if (checks[i]) checksum++;

        if (checksum == checks.Length)
        {
            //equip success
            data.equipment[equipSlot] = itemData;
            SetWeaponModel();

            return true;
        }
        else
        {
            return false;
        }
    }

    public HeroAttributes CombinedStatus()
    {
        Timing.RunCoroutine(ConvertOldEquipment().CancelWith(gameObject));

        HeroAttributes stat = new HeroAttributes();
        dataSheet = GameManager.instance.dataSheet;

        //Define base status (Hero Lv1 no equipment)
        stat.AddStatus(EC2.Stats.MaxHP, dataSheet.statData[0].baseValue);
        stat.AddStatus(EC2.Stats.MaxMP, dataSheet.statData[1].baseValue);
        stat.AddStatus(EC2.Stats.Attack, dataSheet.statData[2].baseValue);
        stat.AddStatus(EC2.Stats.Defense, dataSheet.statData[3].baseValue);
        stat.AddStatus(EC2.Stats.Crit, dataSheet.statData[4].baseValue);
        stat.AddStatus(EC2.Stats.CritDamage, dataSheet.statData[5].baseValue);
        stat.AddStatus(EC2.Stats.AttackSpeed, dataSheet.statData[6].baseValue);
        stat.AddStatus(EC2.Stats.Speciality, dataSheet.statData[7].baseValue);
        stat.AddStatus(EC2.Stats.Evasion, dataSheet.statData[8].baseValue);
        stat.AddStatus(EC2.Stats.Recovery, dataSheet.statData[9].baseValue);
        stat.AddStatus(EC2.Stats.CooldownReduce, dataSheet.statData[10].baseValue);
        stat.AddStatus(EC2.Stats.ManaReduce, dataSheet.statData[11].baseValue);
        stat.AddStatus(EC2.Stats.PhysicalResistance, dataSheet.statData[12].baseValue);
        stat.AddStatus(EC2.Stats.ElementalResistance, dataSheet.statData[13].baseValue);
        stat.AddStatus(EC2.Stats.BasicAtkDamage, dataSheet.statData[15].baseValue);
        stat.AddStatus(EC2.Stats.SkillAtkDamage, dataSheet.statData[16].baseValue);
        stat.AddStatus(EC2.Stats.Accuracy, dataSheet.statData[17].baseValue);
        stat.AddStatus(EC2.Stats.ManaGain, dataSheet.statData[18].baseValue);
        stat.AddStatus(EC2.Stats.SPRegen, dataSheet.statData[19].baseValue);
        stat.AddStatus(EC2.Stats.ConsumablePlus, dataSheet.statData[20].baseValue);
        //stat.AddStatus(Stats.Break, dataSheet.statData[4].baseValue);
        //stat.AddStatus(Stats.Pierce, dataSheet.statData[5].baseValue);
        //stat.AddStatus(Stats.ManaGain, dataSheet.statData[12].baseValue);
        //stat.AddStatus(Stats.ConsumablePlus, dataSheet.statData[16].baseValue);
        //stat.AddStatus(Stats.DropRatePlus, dataSheet.statData[17].baseValue);

        //stat.MaxMP = 200;

        //Hero gets additional attribute on each level (this value is not saved)
        ObscuredInt STR = currentSTR + currentLevel - 1;
        ObscuredInt INT = currentINT + currentLevel - 1;
        ObscuredInt DEX = currentDEX + currentLevel - 1;
        ObscuredInt AGI = currentAGI + currentLevel - 1;
        ObscuredInt VIT = currentVIT + currentLevel - 1;

        //Calculates the status given by attributes
        //STR//
        //if (physicalDmg) 
        stat.AddStatus(EC2.Stats.BasicAtkDamage, STR * EC2Constant.STR_TO_BASICATK);
        stat.AddStatus(EC2.Stats.CritDamage, STR * EC2Constant.STR_TO_CDM);
        stat.AddStatus(EC2.Stats.Attack, STR * EC2Constant.STR_TO_ATK);
        stat.AddStatus(EC2.Stats.PhysicalResistance, STR * EC2Constant.STR_TO_PHYRES);

        //INT//
        //if (!physicalDmg) 
        stat.AddStatus(EC2.Stats.ManaGain, INT * EC2Constant.INT_TO_MPGAIN);
        stat.AddStatus(EC2.Stats.ManaReduce, INT * EC2Constant.INT_TO_MPRED);
        stat.AddStatus(EC2.Stats.SkillAtkDamage, INT * EC2Constant.INT_TO_SKILLATK);
        stat.AddStatus(EC2.Stats.ElementalResistance, INT * EC2Constant.INT_TO_ELERES);
        stat.AddStatus(EC2.Stats.MaxMP, INT * EC2Constant.INT_TO_MAXMP);
        stat.AddStatus(EC2.Stats.Attack, INT * EC2Constant.INT_TO_ATK);

        //DEX//
        stat.AddStatus(EC2.Stats.Crit, DEX * EC2Constant.DEX_TO_CRITRATE);
        stat.AddStatus(EC2.Stats.Speciality, DEX * EC2Constant.DEX_TO_SPECIALITY);
        stat.AddStatus(EC2.Stats.Accuracy, DEX * EC2Constant.DEX_TO_ACCURACY);
        stat.AddStatus(EC2.Stats.Attack, DEX * EC2Constant.DEX_TO_ATK);

        //AGI//
        stat.AddStatus(EC2.Stats.AttackSpeed, AGI * EC2Constant.AGI_TO_ASPD);
        stat.AddStatus(EC2.Stats.Evasion, AGI * EC2Constant.AGI_TO_EVA);
        stat.AddStatus(EC2.Stats.CooldownReduce, AGI * EC2Constant.AGI_TO_CDR);
        stat.AddStatus(EC2.Stats.SPRegen, AGI * EC2Constant.AGI_TO_SPREGEN);
        stat.AddStatus(EC2.Stats.Attack, AGI * EC2Constant.AGI_TO_ATK);

        //VIT//
        stat.AddStatus(EC2.Stats.Recovery, VIT * EC2Constant.VIT_TO_RECOVERY);
        stat.AddStatus(EC2.Stats.PhysicalResistance, VIT * EC2Constant.VIT_TO_PHYRES);
        stat.AddStatus(EC2.Stats.ElementalResistance, VIT * EC2Constant.VIT_TO_ELERES);
        stat.AddStatus(EC2.Stats.MaxHP, VIT * EC2Constant.VIT_TO_MAXHP);
        stat.AddStatus(EC2.Stats.Defense, VIT * EC2Constant.VIT_TO_DEF);

        //Calculates the status given by Equipments
        for (int i = 0; i < data.equipment.Count; i++)
        {
            ItemInstance equipAttribute = data.equipment[i];

            if (string.IsNullOrEmpty(equipAttribute.id)) continue;
            if (!Inventory.instance.allItems.ContainsKey(equipAttribute.id)) continue;

            Item equip = Inventory.instance.allItems[equipAttribute.id];

            /*
            //Base stat is modified by enhancement level. each point increase stats by 5%
            foreach (EquipStats s in equip.equipment.baseStats)
            {
                float modifiedValue = EC2Utils.GetEnhancedValue(s.value, equipAttribute.enhancementLevel);
                stat.AddStatus(s.stats, modifiedValue);
            }*/

            //Base Stat
            EquipStats s_base = equip.equipment.baseStats;
            float upgradedValue = EC2Utils.GetUpgradeValue(s_base.value, equip.baseRarity, equipAttribute.currentRarity);
            stat.AddStatus(s_base.stats, EC2Utils.GetEnhancedValue(upgradedValue, equipAttribute.enhancementLevel));

            //Randomized Stat
            EquipStats s_random = equipAttribute.randomStat;
            upgradedValue = EC2Utils.GetUpgradeValue(s_random.value, equip.baseRarity, equipAttribute.currentRarity);
            stat.AddStatus(s_random.stats, EC2Utils.GetEnhancedValue(upgradedValue, equipAttribute.enhancementLevel));

            //Fixed and Socket stats
            if (equipAttribute.fixedStats.Count > 0)
            {
                foreach (EquipStats s in equipAttribute.fixedStats)
                {
                    stat.AddStatus(s.stats, s.value);
                }
            }

        }

        for (int i = 0; i < data.costumes.Count; i++)
        {
            CostumeInstance equipAttribute = data.costumes[i];

            if (string.IsNullOrEmpty(equipAttribute.itemData.id)) continue;
            if (!Inventory.instance.allItems.ContainsKey(equipAttribute.itemData.id)) continue;

            Item equip = Inventory.instance.allItems[equipAttribute.itemData.id];

            //Base Stat
            EquipStats s_base = equip.equipment.baseStats;
            stat.AddStatus(s_base.stats, s_base.value);

        }

        return stat;
    }
    public HeroAttributes GetCosuUnlockedStat()
    {
        HeroAttributes stat = new HeroAttributes();

        if (!GameManager.instance) return stat;

        var userCosu = GameManager.instance.userData.data.costumes;

        foreach (var key in userCosu.Keys.ToList())
        {
            if (!userCosu.ContainsKey(key)) continue;
            if (!Inventory.instance.allItems.ContainsKey(key)) continue;

            var cosuData = Inventory.instance.allItems[key].equipment.costume;

            if (cosuData.hero != Hero.None)
                if (cosuData.hero != _status.heroReference.hero) continue;

            //Base Stat
            stat.AddStatus(cosuData.unlockStat.stats, cosuData.unlockStat.value);
        }

        return stat;
    }

    public HeroCostumeSetEffect GetCostumeSetEffect(out HeroAttributes attributeModifier)
    {
        _status.costumeSets = new Dictionary<CostumeSet, int>();

        foreach (var key in System.Enum.GetValues(typeof(CostumeSet)))
        {
            var cosuKey = (CostumeSet)key;
            _status.costumeSets.Add(cosuKey, 0);
        }

        HeroCostumeSetEffect _temp = new HeroCostumeSetEffect();
        attributeModifier = new HeroAttributes();


        List<string> cosuList = new List<string>();
        for (int i = 0; i < data.costumes.Count; i++)
        {
            CostumeInstance cosu = data.costumes[i];

            if (string.IsNullOrEmpty(cosu.itemData.id)) continue;
            if (!cosuList.Contains(cosu.itemData.id))
                cosuList.Add(cosu.itemData.id);
        }


        foreach (var costumeSet in _status.costumeSets.Keys.ToList())
        {
            var costumeSetEffect = GameManager.instance.
                equipmentSetEffectDatabase.GetCostumeSetEffect(costumeSet);

            if (costumeSetEffect != null)
            {
                foreach (var cosu in cosuList)
                {
                    if (costumeSetEffect.IsUnderSet(cosu))
                        _status.costumeSets[costumeSet]++;
                }

                foreach (var requirement in costumeSetEffect.setEffects)
                {
                    if (_status.costumeSets[costumeSet] >= requirement.piecesNeeded)
                    {
                        foreach (var effect in requirement.effects)
                        {
                            if (effect.isUniqueModifier)
                                _temp.Add(effect.setEffectModifier, effect.value);
                            else attributeModifier.AddStatus(effect.stat, effect.value);
                        }
                    }
                }
            }
        }

        return _temp;
    }

    public HeroSetEffect GetEquipmentSetEffect(out HeroAttributes attributeModifier)
    {
        _status.setCategories = new Dictionary<EquipSet, int>();

        foreach (var key in System.Enum.GetValues(typeof(EquipSet)))
        {
            var eqKey = (EquipSet)key;
            _status.setCategories.Add(eqKey, 0);
        }

        HeroSetEffect _temp = new HeroSetEffect();
        attributeModifier = new HeroAttributes();

        List<string> equipListDistinct = new List<string>();
        for (int i = 0; i < data.equipment.Count; i++)
        {
            ItemInstance equipAttribute = data.equipment[i];

            if (string.IsNullOrEmpty(equipAttribute.id)) continue;
            if (!equipListDistinct.Contains(equipAttribute.id))
                equipListDistinct.Add(equipAttribute.id);
        }

        foreach (var equipSet in _status.setCategories.Keys.ToList())
        {
            var equipmentSetEffect = GameManager.instance.
                equipmentSetEffectDatabase.GetEquipmentSetEffect(equipSet);

            if (equipmentSetEffect != null)
            {
                foreach (var eq in equipListDistinct)
                {
                    if (equipmentSetEffect.IsUnderSet(eq))
                        _status.setCategories[equipSet]++;
                }

                foreach (var requirement in equipmentSetEffect.setEffects)
                {
                    if (_status.setCategories[equipSet] >= requirement.piecesNeeded)
                    {
                        foreach (var effect in requirement.effects)
                        {
                            if (effect.isUniqueModifier)
                                _temp.Add(effect.setEffectModifier, effect.value);
                            else attributeModifier.AddStatus(effect.stat, effect.value);
                        }
                    }
                }
            }

        }
        return _temp;
    }

    public HeroSocketEffect GetEquipmentSocketEffect()
    {
        HeroSocketEffect _temp = new HeroSocketEffect();
        for (int i = 0; i < data.equipment.Count; i++)
        {
            ItemInstance equipAttribute = data.equipment[i];

            if (string.IsNullOrEmpty(equipAttribute.id)) continue;
            if (!Inventory.instance.allItems.ContainsKey(equipAttribute.id)) continue;

            if (equipAttribute.socketStats.Count > 0)
            {
                foreach (EquipSocket data in equipAttribute.socketStats)
                {
                    if (string.IsNullOrEmpty(data.socketID)) continue;
                    if (!Inventory.instance.allItems.ContainsKey(data.socketID)) continue;
                    var socketItemData = Inventory.instance.allItems[data.socketID];
                    var tempLevel = _temp.GetValueByType(socketItemData.socket.type).highestLevel;

                    _temp.SetHighestLevel(socketItemData.socket.type, data.currentLevel);
                    _temp.Add(socketItemData.socket.type, 1);

                    if (!socketItemData.socket.unstackable)
                    {
                        _temp.Add(socketItemData.socket.type,
                            socketItemData.socket.GetSocketValue(data.currentLevel)
                        );
                    }

                    else
                    {
                        _temp.Set(socketItemData.socket.type,
                            socketItemData.socket.GetSocketValue
                                        (_temp.GetValueByType(socketItemData.socket.type).highestLevel)
                        );
                    }

                    _temp.SetHighest(socketItemData.socket.type,
                        socketItemData.socket.GetRuneValues(_temp.GetValueByType(socketItemData.socket.type).highestLevel));
                }
            }

        }
        return _temp;
    }

    public float ConvertedValue(EC2.Stats stat, float rawValue)
    {
        if (!GetComponent<HeroStatus>().IsMine()) return 0;
        return dataSheet.ConvertedValue(stat, rawValue);
    }

    public float GetRawValue(EC2.Stats stat, float convertedValue)
    {
        if (!GetComponent<HeroStatus>().IsMine()) return 0;
        return dataSheet.GetRawValue(stat, convertedValue);
    }

    public StatusCap GetStatusCap(EC2.Stats stat, float convertedValue)
    {
        if (!GetComponent<HeroStatus>().IsMine()) return 0;
        return dataSheet.GetStatusCap(stat, convertedValue);
    }

    public void UpgradeMastery(int index)
    {
        data.masteryLvl[index]++;
        GetComponent<HeroControl>().InitAllMasteries();
    }

    IEnumerator<float> ConvertOldEquipment()
    {
        GameManager gm = GameManager.instance;
        Inventory inventory = gm.inventory;

        //wait for init
        while (gm == null)
        {
            gm = GameManager.instance;
            yield return Timing.WaitForOneFrame;
        }

        while (gm.inventory.allItems == null)
        {
            yield return Timing.WaitForOneFrame;
        }

        //check old equipment and convert
        var converter = GameManager.instance.inventory.GetComponent<OldItemConverter>();
        for (int i = 0; i < data.equipment.Count; i++)
        {
            data.equipment[i] = CheckOldItem(data.equipment[i]);
            if (converter)
                data.equipment[i] = converter.HalvesMPR(data.equipment[i]);
        }

        //match secondary status with database
        for (int i = 0; i < data.equipment.Count; i++)
        {
            if (!string.IsNullOrEmpty(data.equipment[i].id))
            {
                if (data.equipment[i].randomStat == null) data.equipment[i].randomStat = inventory.SetSecondStat(data.equipment[i].id);
                else data.equipment[i].randomStat = inventory.SetSecondStat(data.equipment[i].id, data.equipment[i].randomStat.stats);
            }
        }
    }
    ItemInstance CheckOldItem(ItemInstance source)
    {
        OldItemConverter convert = GameManager.instance.inventory.GetComponent<OldItemConverter>();
        return convert.GetConvertedItem(source);
    }

    public ObscuredInt AttributeSum()
    {
        return currentSTR + currentINT + currentDEX + currentAGI + currentVIT;
    }

    public int TotalMasteryPoint()
    {
        //Hero gets mastery point starting from Lv11
        int point = currentLevel - 10;
        if (point < 0) return 0;
        else return point;
    }

    public int RemainingMasteryPoint()
    {
        //Remaining Mastery Point a hero can spend
        return TotalMasteryPoint() - AllocatedMasteryPoint();
    }

    public int AllocatedMasteryPoint()
    {
        //Returns all allocated mastery points
        int pointSum = 0;

        for (int i = 0; i < data.masteryLvl.Length; i++)
        {
            switch (data.masteryLvl[i])
            {
                case 1: pointSum += 1; break;
                case 2: pointSum += 3; break;
                case 3: pointSum += 6; break;
                default: break;
            }
        }

        return pointSum;
    }
}

[System.Serializable]
public class HeroData
{
    [Header("Base Status")]
    public Hero HERO;
    public int LEVEL;
    public int EXP, STR, INT, DEX, AGI, VIT;

    [Header("Skill Data")]
    public int[] skillSlot;
    public int[] skillLevels;
    public int[] skillExp;

    [Header("Mastery Data")]
    public int[] masteryLvl;

    [Header("Equipment Data")]
    public List<ItemInstance> equipment;
    public List<CostumeInstance> costumes;
    public List<CostumeInstance> cosuTransmog = new List<CostumeInstance>();

    public List<string> GetEquipmentByString()
    {
        List<string> equipListDistinct = new List<string>();
        for (int i = 0; i < equipment.Count; i++)
        {
            ItemInstance equipAttribute = equipment[i];

            if (string.IsNullOrEmpty(equipAttribute.id)) continue;
            if (!equipListDistinct.Contains(equipAttribute.id))
                equipListDistinct.Add(equipAttribute.id);
        }

        return equipListDistinct;
    }

}

[System.Serializable]
public class HeroLeaderboardData
{
    public HeroData heroData;
    public float maxHP, maxMP, attack, defense, critRate, critDmg,
                 attackSpeed, speciality, evasion, recovery, cdr,
                 mpCostRed, phyRes, eleRes,
                 basicAtkDamage, skillAtkDamage, accuracy, manaGain, spRegen, consumablePlus;
}

[System.Serializable]
public class CostumeInstance
{
    public ItemInstance itemData;
    public bool isHid;
    public int textureIndex;

    public CostumeInstance()
    {
        itemData = new ItemInstance();
        isHid = false;
        textureIndex = 0;
    }
}

[System.Serializable]
public class HeroAttributes
{
    //Offensive
    public ObscuredFloat Attack;
    public ObscuredFloat AttackSpeed;
    public ObscuredFloat Crit;
    public ObscuredFloat CritDamage;
    public ObscuredFloat Break;
    public ObscuredFloat Pierce;

    //Defensive
    public ObscuredFloat MaxHP;
    public ObscuredFloat Defense;
    public ObscuredFloat Recovery;
    public ObscuredFloat ElementalResistance;
    public ObscuredFloat Evasion;
    public ObscuredFloat PhysicalResistance;

    //Utility
    public ObscuredFloat ManaReduce;
    public ObscuredFloat CooldownReduce;
    public ObscuredFloat Speciality;
    public ObscuredFloat DropRatePlus;

    //Special Attributes
    public ObscuredFloat RefreshSkill;
    public ObscuredFloat Lifesteal;
    public ObscuredFloat Deflect;
    public ObscuredFloat DamageAmplifier;

    //hidden
    public ObscuredFloat MaxMP;
    public ObscuredFloat HealthPercentage;

    //new
    public ObscuredFloat BasicAtkDamage;
    public ObscuredFloat SkillAtkDamage;
    public ObscuredFloat Accuracy;
    public ObscuredFloat ManaGain;
    public ObscuredFloat SPRegen;
    public ObscuredFloat ConsumablePlus;

    //new special
    public ObscuredFloat AttackPercentage;
    public ObscuredFloat DefensePercentage;

    public void Merge(HeroAttributes other)
    {
        Attack += other.Attack;
        AttackSpeed += other.AttackSpeed;
        Crit += other.Crit;
        CritDamage += other.CritDamage;
        Break += other.Break;
        Pierce += other.Pierce;
        MaxHP += other.MaxHP;
        Defense += other.Defense;
        Recovery += other.Recovery;
        ElementalResistance += other.ElementalResistance;
        Evasion += other.Evasion;
        PhysicalResistance += other.PhysicalResistance;
        ManaReduce += other.ManaReduce;
        CooldownReduce += other.CooldownReduce;
        Speciality += other.Speciality;
        DropRatePlus += other.DropRatePlus;
        RefreshSkill += other.RefreshSkill;
        Lifesteal += other.Lifesteal;
        Deflect += other.Deflect;
        DamageAmplifier += other.DamageAmplifier;
        MaxMP += other.MaxMP;
        HealthPercentage += other.HealthPercentage;
        BasicAtkDamage += other.BasicAtkDamage;
        SkillAtkDamage += other.SkillAtkDamage;
        Accuracy += other.Accuracy;
        ManaGain += other.ManaGain;
        SPRegen += other.SPRegen;
        ConsumablePlus += other.ConsumablePlus;
        AttackPercentage += other.AttackPercentage;
        DefensePercentage += other.DefensePercentage;
    }

    public void AddStatus(EC2.Stats stat, float value)
    {
        switch (stat)
        {
            case EC2.Stats.Attack: Attack += value; break;
            case EC2.Stats.Defense: Defense += value; break;

            case EC2.Stats.AttackSpeed: AttackSpeed += value; break;
            case EC2.Stats.Crit: Crit += value; break;
            case EC2.Stats.CritDamage: CritDamage += value; break;
            //case Stats.Pierce: Pierce += value; break;
            //case Stats.Break: Break += value; break;

            case EC2.Stats.MaxHP: MaxHP += value; break;
            case EC2.Stats.Recovery: Recovery += value; break;
            case EC2.Stats.ElementalResistance: ElementalResistance += value; break;
            case EC2.Stats.PhysicalResistance: PhysicalResistance += value; break;
            case EC2.Stats.Evasion: Evasion += value; break;

            //case Stats.ManaGain: ManaGain += value; break;
            case EC2.Stats.MaxMP: MaxMP += value; break;
            case EC2.Stats.Speciality: Speciality += value; break;
            case EC2.Stats.ManaReduce: ManaReduce += value; break;
            case EC2.Stats.CooldownReduce: CooldownReduce += value; break;
            case EC2.Stats.HealthPercentage: HealthPercentage += value; break;
            //case Stats.ConsumablePlus: ConsumablePlus += value; break;
            //case Stats.DropRatePlus: DropRatePlus += value; break;

            case EC2.Stats.BasicAtkDamage: BasicAtkDamage += value; break;
            case EC2.Stats.SkillAtkDamage: SkillAtkDamage += value; break;
            case EC2.Stats.Accuracy: Accuracy += value; break;
            case EC2.Stats.ManaGain: ManaGain += value; break;
            case EC2.Stats.SPRegen: SPRegen += value; break;
            case EC2.Stats.ConsumablePlus: ConsumablePlus += value; break;

            case EC2.Stats.AttackPercentage: AttackPercentage += value; break;
            case EC2.Stats.DefensePercentage: DefensePercentage += value; break;
        }
    }
}

[System.Serializable]
public class HeroSetEffect
{
    public ObscuredFloat refreshSkill;
    public ObscuredFloat lifesteal;
    public ObscuredFloat valorDamageAmplifier;
    public ObscuredFloat vengeance;
    public ObscuredFloat demonicCurse;
    public ObscuredFloat mardukVigor;
    public ObscuredFloat abyssVoid, abyssGale, abyssGlacial, abyssInferno;
    public void Add(SetEffectModifier setEffect, float value)
    {
        switch (setEffect)
        {
            case SetEffectModifier.RefreshSkill: refreshSkill += value; break;
            case SetEffectModifier.Lifesteal: lifesteal += value; break;
            case SetEffectModifier.ValorDamageAmplifier: valorDamageAmplifier += value; break;
            case SetEffectModifier.Vengeance: vengeance += value; break;
            case SetEffectModifier.Demonic: demonicCurse += value; break;
            case SetEffectModifier.MardukVigor: mardukVigor += value; break;
            case SetEffectModifier.Entropy: abyssVoid += value; break;
            case SetEffectModifier.WindVeil: abyssGale += value; break;
            case SetEffectModifier.FrostNova: abyssGlacial += value; break;
            case SetEffectModifier.PhoenixTrigger: abyssInferno += value; break;
        }
    }
}

[System.Serializable]
public class HeroCostumeSetEffect
{
    public ObscuredFloat crovenBlast;
    public ObscuredFloat schoolManaSteal;
    public ObscuredFloat maidValue;
    public ObscuredFloat ogClarisValue;
    public ObscuredFloat santaValue;
    public ObscuredFloat eclipseValue;
    public ObscuredFloat summerValue;
    public ObscuredFloat midnightValue;

    public void Add(SetEffectModifier set, float value)
    {
        switch (set)
        {
            case SetEffectModifier.Croven: crovenBlast += value; break;
            case SetEffectModifier.School: schoolManaSteal += value; break;
            case SetEffectModifier.Maid: maidValue += value; break;
            case SetEffectModifier.OG_Claris: ogClarisValue += value; break;
            case SetEffectModifier.Santa: santaValue += value; break;
            case SetEffectModifier.Eclipse: eclipseValue += value; break;
            case SetEffectModifier.Summer: summerValue += value; break;
            case SetEffectModifier.Midnight: midnightValue += value; break;
        }
    }
}

[System.Serializable]
public class HeroSocketEffect
{
    public RuneInstance attack = new RuneInstance();
    public RuneInstance defense = new RuneInstance();
    public RuneInstance maxHP = new RuneInstance();
    public RuneInstance maxMP = new RuneInstance();

    public RuneInstance recharge = new RuneInstance();
    public RuneInstance tranquility = new RuneInstance();
    public RuneInstance toughness = new RuneInstance();
    public RuneInstance wind = new RuneInstance();

    public RuneInstance burst = new RuneInstance();
    public RuneInstance bravery = new RuneInstance();
    public RuneInstance protection = new RuneInstance();
    public RuneInstance drain = new RuneInstance();

    //ex rune
    public RuneInstance transmutation = new RuneInstance();
    public RuneInstance ruin = new RuneInstance();
    public RuneInstance omni = new RuneInstance();
    public RuneInstance magna = new RuneInstance();

    //public ObscuredFloat fireAspect;
    //public ObscuredFloat iceAspect;
    //public ObscuredFloat thunderAspect;
    //public ObscuredFloat venomAspect;
    //public ObscuredFloat quickConsume;
    //public ObscuredFloat thorn;

    public RuneInstance GetValueByType(SocketType type)
    {
        switch (type)
        {
            case SocketType.Attack: return attack;
            case SocketType.Defense: return defense;
            case SocketType.MaxHP: return maxHP;
            case SocketType.MaxMP: return maxMP;

            case SocketType.Recharge: return recharge;
            case SocketType.Tranquility: return tranquility;
            case SocketType.Toughness: return toughness;
            case SocketType.Wind: return wind;

            case SocketType.Burst: return burst;
            case SocketType.Bravery: return bravery;
            case SocketType.Protection: return protection;
            case SocketType.Drain: return drain;

            case SocketType.Transmutation: return transmutation;
            case SocketType.Ruin: return ruin;
            case SocketType.Omni: return omni;
            case SocketType.Magna: return magna;

            default: return null;
                //case SocketType.FireAspect: fireAspect += value; break;
                //case SocketType.IceAspect: iceAspect += value; break;
                //case SocketType.VenomAspect: venomAspect += value; break;
                //case SocketType.ThunderAspect: thunderAspect += value; break;
                //case SocketType.QuickConsume: quickConsume += value; break;
                //case SocketType.Thorn: thorn += value; break;
        }
    }

    public void Add(SocketType socket, int count)
    {
        var runeInstance = GetValueByType(socket);
        if (runeInstance == null) return;

        runeInstance.count += count;
    }

    public void Add(SocketType socket, List<float> runeValues)
    {
        var runeInstance = GetValueByType(socket);
        if (runeInstance == null) return;

        if (runeInstance.values == null)
        {
            runeInstance.values = new List<float>();
        }

        for (int i = 0; i < runeValues.Count; i++)
        {
            if (i > runeInstance.values.Count - 1)
                runeInstance.values.Add(runeValues[i]);

            else
                runeInstance.values[i] += runeValues[i];
        }
    }

    public void Set(SocketType socket, List<float> runeValues)
    {
        var runeInstance = GetValueByType(socket);
        if (runeInstance == null) return;

        if (runeInstance.values == null)
        {
            runeInstance.values = new List<float>();
        }

        for (int i = 0; i < runeValues.Count; i++)
        {
            if (i > runeInstance.values.Count - 1)
                runeInstance.values.Add(runeValues[i]);

            else
                runeInstance.values[i] = runeValues[i];
        }
    }

    public void SetHighest(SocketType socket, List<BoolFloatPair> runeValues)
    {
        var runeInstance = GetValueByType(socket);
        if (runeInstance == null) return;

        if (runeInstance.values == null)
        {
            runeInstance.values = new List<float>();
        }

        for (int i = 0; i < runeValues.Count; i++)
        {
            if (i > runeInstance.values.Count - 1)
                runeInstance.values.Add(runeValues[i].value);

            else
            {
                var selectedVal = runeValues[i];
                if (selectedVal.basedOnHighestLevel)
                {
                    runeInstance.values[i] = selectedVal.value;
                }
            }
        }
    }

    public void SetHighestLevel(SocketType socket, int level)
    {
        var runeInstance = GetValueByType(socket);
        if (runeInstance == null) return;
        runeInstance.highestLevel = runeInstance.highestLevel < level ? level : runeInstance.highestLevel;
    }
}

[System.Serializable]
public class HeroInfo
{
    public HeroReference heroReference;
    public ObscuredInt level;
}

[System.Serializable]
public class RuneInstance
{
    public int highestLevel;
    public int count;
    public List<float> values;

    public float GetValue(int index)
    {
        if (values == null) return 0f;

        if (index > values.Count - 1) return 0f;

        return values[index];
    }
}
public class ItemInstance_Simplified
{
    public string id;
    public int level;

    public ItemInstance_Simplified Copy(ItemInstance item)
    {
        var temp = new ItemInstance_Simplified();

        temp.id = item.id;
        temp.level = item.enhancementLevel;

        return temp;
    }
    public List<ItemInstance_Simplified> Copy(List<ItemInstance> itemCollection)
    {
        var temp = new List<ItemInstance_Simplified>();

        foreach (var item in itemCollection)
        {
            var temp2 = new ItemInstance_Simplified();

            temp2.id = item.id;
            temp2.level = item.enhancementLevel;

            temp.Add(temp2);
        }
        return temp;
    }
}
public class CostumeInstance_Simplified
{
    public string id;
    public bool isHid;

    public CostumeInstance_Simplified Copy(CostumeInstance item)
    {
        var temp = new CostumeInstance_Simplified();

        temp.id = item.itemData.id;
        temp.isHid = item.isHid;

        return temp;
    }
    public List<CostumeInstance_Simplified> Copy(List<CostumeInstance> itemCollection)
    {
        var temp = new List<CostumeInstance_Simplified>();

        foreach (var item in itemCollection)
        {
            var temp2 = new CostumeInstance_Simplified();

            temp2.id = item.itemData.id;
            temp2.isHid = item.isHid;

            temp.Add(temp2);
        }
        return temp;
    }
}

public struct GearInfo
{
    public string id;
    public int level;
    public int index;

    public GearInfo(string id, int lvl, int index)
    {
        this.id = id;
        level = lvl;
        this.index = 0;
    }
}