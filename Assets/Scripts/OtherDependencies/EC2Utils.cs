using UnityEngine;
using I2.Loc;
using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.Storage;
using Newtonsoft.Json;
using MEC;

[System.Serializable]
public struct SpawnFx
{
    public string _name;
    public Transform prefab;
    public Vector3 pos;
    public Vector3 rot;
}
public class EC2Utils
{
    public static string GetVersion()
    {
        var key = EC2Constant.EC2_ALLOWED_GAME_VERSION;
        return key;
    }
    public static bool IsBeta()
    {
        return Application.version.Contains("beta");
    }
    public static DateTime GetNextDay(DateTime currentTime)
    {
        //currentTime = PlayfabManager.instance.serverTime;

        Debug.Log(currentTime);

        DateTime tomorrow = currentTime.AddDays(1).Date;

        Debug.Log(tomorrow);
        Debug.Log("time left " + tomorrow.Subtract(currentTime).TotalSeconds);

        return tomorrow;//.Subtract(currentTime);

        //return tomorrow;
    }
    public static DateTime GetNextWeekday(DayOfWeek dayOfWeek, DateTime currentTime)
    {
        return GetNextWeekday(dayOfWeek, currentTime, true);
    }
    public static DateTime GetNextWeekday(DayOfWeek dayOfWeek, DateTime currentTime, bool excludeToday)
    {
        // (1 - 1 + 7) % 7 = 1
        int daysUntilNextWeekday = ((int)dayOfWeek - (int)currentTime.DayOfWeek + 7) % 7;

        int toAdd = daysUntilNextWeekday;
        if (excludeToday)
        {
            if (toAdd == 0)
            {
                toAdd = 7;
            }
        }
        DateTime nextWeekday = currentTime.AddDays(toAdd).Date;

        return nextWeekday;
    }

    public static string GetMappedRewardTypeToString(Reward reward)
    {
        var key = string.Empty;
        switch (reward.rewardType)
        {
            case RewardType.Item:
                key = reward.item.id;
                break;

            case RewardType.Gold:
                key = "gold";
                break;

            case RewardType.Ruby:
                key = "ruby";
                break;
        }

        return key;
    }
    public static string GetIcon(Item item)
    {
        if (item == null) return "--";
        if (item.itemType == ItemType.Consumable || item.itemType == ItemType.Material) return item.IconId;
        else if (item.itemType == ItemType.Rune) return "icon_rune_" + item.socket.type.ToString().ToLower();
        else return "icon_" + item.ItemIcon;
    }

    public static string GetIcon(RewardType type)
    {
        return GetIcon(type, null);
    }
    public static string GetIcon(RewardType type, Item item)
    {
        switch (type)
        {
            case RewardType.Gold:
                return "menu_gold";

            case RewardType.Ruby:
                return "menu_ruby";

            case RewardType.Item:
                if (item == null) return "--";
                return GetIcon(item);

            case RewardType.Special_Premium:
                return "icon_key";

            case RewardType.Special_Hero:
                return "icon_cos_hair";

            default:
                return "icon_key";
        }
    }

    public static string GetStatIcon(EC2.Stats stat)
    {
        switch (stat)
        {
            case EC2.Stats.AttackSpeed:
            case EC2.Stats.Crit:
            case EC2.Stats.CritDamage:
            case EC2.Stats.BasicAtkDamage:
            case EC2.Stats.SkillAtkDamage:
            case EC2.Stats.Accuracy:
            default:
                return "icon_cos_weapon";


            case EC2.Stats.HealthPercentage:
            case EC2.Stats.Recovery:
            case EC2.Stats.ElementalResistance:
            case EC2.Stats.PhysicalResistance:
            case EC2.Stats.Evasion:
                return "substat_defense";


            case EC2.Stats.Speciality:
            case EC2.Stats.ManaReduce:
            case EC2.Stats.CooldownReduce:
            case EC2.Stats.ManaGain:
            case EC2.Stats.SPRegen:
            case EC2.Stats.ConsumablePlus:
                return "substat_utility";
        }
    }
    public static string GetRewardName(Reward reward)
    {
        string itemName = string.Empty;
        switch (reward.rewardType)
        {
            case RewardType.Gold:
                itemName = LocalizationManager.GetTranslation("general/gold");
                break;

            case RewardType.Ruby:
                itemName = LocalizationManager.GetTranslation("general/ruby");
                break;

            case RewardType.Item:
                itemName = reward.item.ItemName();
                //textColor = EC2Utils.GetRarityColor(rewards[i].item.baseRarity);
                break;

            case RewardType.Special_Premium:
                itemName = "Premium Pass";
                break;

            case RewardType.Special_Hero:
                itemName = "Unlock " + GameManager.instance.heroDatabase.GetHeroReference(reward.hero).HeroName();
                break;

            case RewardType.Special_Welkin:
                itemName = "Daily Ruby";
                break;

            case RewardType.Special_DailyMaterial:
                itemName = "Daily Material";
                break;

            case RewardType.Developer_Buff:
                itemName = reward.buffData.addedBuff.BuffName();
                break;

            case RewardType.FishingNet:
                itemName = "Fishing Net";
                break;

            case RewardType.UnlimitedFishing:
                itemName = "Unlimited Fishing";
                break;

            case RewardType.RentHelper:
                itemName = "Rent a Helper!";
                break;

            case RewardType.NoBoss:
                itemName = "No Boss";
                break;

            case RewardType.FreeRift:
                itemName = "Free Rift Entry";
                break;

            default:
                itemName = reward.rewardType.ToString();
                break;
        }

        return itemName;
    }

    public static Color GetRewardColor(Reward reward)
    {
        Color textColor = Color.white;
        switch (reward.rewardType)
        {
            case RewardType.Item:
                textColor = EC2Utils.GetRarityColor(reward.item.baseRarity);
                break;

            case RewardType.Ruby:
            case RewardType.Gold:
                textColor = EC2Utils.GetRarityColor(Rarity.Rare);
                break;

            default:
                textColor = EC2Utils.GetRarityColor(Rarity.Epic);
                break;
        }

        return textColor;
    }

    public static T FromJson<T>(string serializedJson)
    {
        try
        {
            JsonSerializerSettings config = new JsonSerializerSettings()
            {
                Converters = new[] { new ObscuredConverter() },
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            return JsonConvert.DeserializeObject<T>(serializedJson, config);
        }
        catch
        {
            JsonSerializerSettings config = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            return JsonConvert.DeserializeObject<T>(serializedJson, config);
        }
    }

    public static string ToJson(object obj)
    {
        JsonSerializerSettings config = new JsonSerializerSettings()
        {
            Converters = new[] { new ObscuredConverter() },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        return JsonConvert.SerializeObject(obj, config);
    }

    public static int GetTotalCostumeSlot()
    {
        return System.Enum.GetValues(typeof(CostumeSlot)).Length + 3;
    }
    public static string SelectedCostumeIcon(int index)
    {
        string result = "";
        switch (index)
        {
            case 0: result = "eq_cos_weapon"; break;
            case 1: result = "eq_cos_hair"; break;
            case 2: result = "eq_cos_suit"; break;
            case 3:
            case 4:
            case 5:
            case 6: result = "eq_cos_mask"; break;
        }
        return result;
    }

    public static CostumeSlot GetCostumeSlot(int index)
    {
        switch (index)
        {
            case 0: return CostumeSlot.Weapon;
            case 1: return CostumeSlot.Hair;
            case 2: return CostumeSlot.Suit;
            case 3:
            case 4:
            case 5:
            case 6: return CostumeSlot.Accessory;
            default: return CostumeSlot.Weapon;
        }
    }
    public static string GetCurrentEmail()
    {
        var gplayEmail = ObscuredPrefs.GetString(EC2Constant.EC2_LINKED_GMAIL, "");
        var pfEmail = ObscuredPrefs.GetString(EC2Constant.EC2_PLAYFAB_EMAIL, "");

        if (!string.IsNullOrEmpty(pfEmail))
            return pfEmail;
        else if (!string.IsNullOrEmpty(gplayEmail))
            return gplayEmail;
        else return "";
    }
    public static void DeleteAllPrefsExceptSettings()
    {
        var oldSettings = GlobalUserData.GetUserSetting();
        var touch = ObscuredPrefs.GetBool(EC2Constant.EC2_ZABI);
        var oldFanart = PlayerPrefs.GetString(EC2Constant.EC2_SHUFFLED_FANART, "");

        string widgetconfig = "";
        if (PlayerPrefs.HasKey(EC2Constant.EC2_WIDGET_CONFIG))
            widgetconfig = PlayerPrefs.GetString(EC2Constant.EC2_WIDGET_CONFIG);

        var lastPFID = PlayerPrefs.GetString(EC2Constant.LAST_PFID, "");

        PlayerPrefs.DeleteAll();
        ObscuredPrefs.DeleteAll();
        PlayerPrefs.Save();
        ObscuredPrefs.Save();

        if (!string.IsNullOrEmpty(widgetconfig))
            PlayerPrefs.SetString(EC2Constant.EC2_WIDGET_CONFIG, widgetconfig);

        if (!string.IsNullOrEmpty(oldFanart))
            ObscuredPrefs.SetString(EC2Constant.EC2_SHUFFLED_FANART, oldFanart);
        ObscuredPrefs.SetString(EC2Constant.EC2_SETTING_KEY, EC2Utils.ToJson(oldSettings));
        ObscuredPrefs.SetString(EC2Constant.LAST_PFID, EC2Utils.ToJson(lastPFID));
        ObscuredPrefs.SetBool(EC2Constant.EC2_ZABI, touch);
        ObscuredPrefs.Save();
    }

    public static void DelayDo(float delay, System.Action Callback)
    {
        Timing.RunCoroutine(DelayAndDo(delay, Callback));
    }
    public static IEnumerator<float> DelayAndDo(float delay, System.Action Callback)
    {
        yield return Timing.WaitForSeconds(delay);
        Callback?.Invoke();
    }
    public static string GetRewardIcon(RewardType rewardType, Item item)
    {
        switch (rewardType)
        {
            case RewardType.Gold:
                return "menu_gold";

            case RewardType.Ruby:
                return "menu_ruby";

            case RewardType.Item:
                return GetIcon(item);

            case RewardType.Special_Premium:
                return "icon_key";

            case RewardType.Special_Hero:
                return "icon_cos_hair";

            default:
                return "--";
        }

    }
    public static string ToRoman(int number)
    {
        if ((number < 0) || (number > 3999)) return string.Empty;
        if (number < 1) return string.Empty;
        if (number >= 1000) return "M" + ToRoman(number - 1000);
        if (number >= 900) return "CM" + ToRoman(number - 900);
        if (number >= 500) return "D" + ToRoman(number - 500);
        if (number >= 400) return "CD" + ToRoman(number - 400);
        if (number >= 100) return "C" + ToRoman(number - 100);
        if (number >= 90) return "XC" + ToRoman(number - 90);
        if (number >= 50) return "L" + ToRoman(number - 50);
        if (number >= 40) return "XL" + ToRoman(number - 40);
        if (number >= 10) return "X" + ToRoman(number - 10);
        if (number >= 9) return "IX" + ToRoman(number - 9);
        if (number >= 5) return "V" + ToRoman(number - 5);
        if (number >= 4) return "IV" + ToRoman(number - 4);
        if (number >= 1) return "I" + ToRoman(number - 1);
        return string.Empty;
    }
    public static int GetReputationKillBonus(ActiveMission data, int reputationLevel)
    {
        if
        (
            (
                data.mission.objective.type == MissionType.KillByClass ||
                data.mission.objective.type == MissionType.KillByFamily ||
                data.mission.objective.type == MissionType.SpecifiedKill
            )
            && reputationLevel >= 8
        )
            return Mathf.FloorToInt(data.mission.objective.quantity * 0.5f);
        else return 0;
    }

    public static string GetLanguageId(SupportedLanguages lang)
    {
        string s = "";

        switch (lang)
        {
            case SupportedLanguages.english: s = "Default"; break;
            case SupportedLanguages.indonesia: s = "id"; break;
            case SupportedLanguages.portuguese: s = "ptbr"; break;
            case SupportedLanguages.spanish: s = "sp"; break;
            case SupportedLanguages.chinese_simplified: s = "cnsimp"; break;
            case SupportedLanguages.japanese: s = "jp"; break;
        }

        return s;
    }

    //==== General Translation ====//
    public static string LocalizedGoldLabel()
    {
        return LocalizationManager.GetTranslation("general/gold");
    }

    public static string LocalizedRubyLabel()
    {
        return LocalizationManager.GetTranslation("general/ruby");
    }

    //==== Status Translation ====//
    public static string EquipStatusLabel(EC2.Stats s)
    {
        string result;

        switch (s)
        {
            case EC2.Stats.Attack: result = Stat_Attack(); break;
            case EC2.Stats.AttackSpeed: result = Stat_Aspd(); break;
            case EC2.Stats.Crit: result = Stat_Crit(); break;
            case EC2.Stats.CritDamage: result = Stat_CritDmg(); break;
            //case Stats.Pierce: result = Stat_Pierce(lang); break;
            //case Stats.Break: result = Stat_Break(lang); break;

            case EC2.Stats.MaxHP: result = Stat_MaxHP(); break;
            case EC2.Stats.Defense: result = Stat_Defense(); break;
            case EC2.Stats.Recovery: result = Stat_Recovery(); break;
            case EC2.Stats.ElementalResistance: result = Stat_EleResist(); break;
            case EC2.Stats.PhysicalResistance: result = Stat_PhyResist(); break;
            case EC2.Stats.Evasion: result = Stat_Evasion(); break;

            //case Stats.ManaGain: result = Stat_ManaGain(lang); break;
            case EC2.Stats.ManaReduce: result = Stat_ManaReduce(); break;
            case EC2.Stats.CooldownReduce: result = Stat_CooldownReduce(); break;
            case EC2.Stats.Speciality: result = Stat_Speciality(); break;

            case EC2.Stats.HealthPercentage: result = Stat_MaxHP(); break;
            case EC2.Stats.MaxMP: result = Stat_MaxMP(); break;
            //case Stats.ConsumablePlus: result = Stat_Consumable(lang); break;
            //case Stats.DropRatePlus: result = Stat_DropRate(lang); break;

            case EC2.Stats.BasicAtkDamage: result = Stat_BasicAtkDamage(); break;
            case EC2.Stats.SkillAtkDamage: result = Stat_SkillAtkDamage(); break;
            case EC2.Stats.Accuracy: result = Stat_Accuracy(); break;
            case EC2.Stats.ManaGain: result = Stat_ManaGain(); break;
            case EC2.Stats.SPRegen: result = Stat_SPRegen(); break;
            case EC2.Stats.ConsumablePlus: result = Stat_ConsumablePlus(); break;
            case EC2.Stats.AttackPercentage: result = Stat_Attack(); break;
            case EC2.Stats.DefensePercentage: result = Stat_Defense(); break;
            default: result = "-----"; break;
        }

        return result;
    }
    public static string EquipSocketLabel(SocketType s, SupportedLanguages lang)
    {
        return "-----";
    }

    //-- offensive
    static string Stat_Attack()
    {
        return LocalizationManager.GetTranslation("label/character/stats/attack");
    }
    static string Stat_Aspd()
    {
        return LocalizationManager.GetTranslation("label/character/stats/aspd");
    }
    static string Stat_Crit()
    {
        return LocalizationManager.GetTranslation("label/character/stats/crit");
    }
    static string Stat_CritDmg()
    {
        return LocalizationManager.GetTranslation("label/character/stats/critDmg");
    }
    //-- defensive
    static string Stat_MaxHP()
    {
        return LocalizationManager.GetTranslation("label/character/stats/maxhp");
    }
    static string Stat_Defense()
    {
        return LocalizationManager.GetTranslation("label/character/stats/defense");
    }
    static string Stat_Recovery()
    {
        return LocalizationManager.GetTranslation("label/character/stats/recovery");
    }
    static string Stat_EleResist()
    {
        return LocalizationManager.GetTranslation("label/character/stats/eleResistance");
    }
    static string Stat_PhyResist()
    {
        return LocalizationManager.GetTranslation("label/character/stats/phyResistance");
    }
    static string Stat_Evasion()
    {
        return LocalizationManager.GetTranslation("label/character/stats/evasion");
    }
    //-- utility
    static string Stat_MaxMP()
    {
        return LocalizationManager.GetTranslation("label/character/stats/maxmp");
    }
    static string Stat_ManaReduce()
    {
        return LocalizationManager.GetTranslation("label/character/stats/mpcost");
    }
    static string Stat_CooldownReduce()
    {
        return LocalizationManager.GetTranslation("label/character/stats/cooldown");
    }
    static string Stat_Speciality()
    {
        return LocalizationManager.GetTranslation("label/character/stats/spgauge");
    }

    static string Stat_BasicAtkDamage()
    {
        return LocalizationManager.GetTranslation("label/character/stats/basicAtkDamage");
    }

    static string Stat_SkillAtkDamage()
    {
        return LocalizationManager.GetTranslation("label/character/stats/skillAtkDamage");
    }

    static string Stat_Accuracy()
    {
        return LocalizationManager.GetTranslation("label/character/stats/accuracy");
    }

    static string Stat_ManaGain()
    {
        return LocalizationManager.GetTranslation("label/character/stats/manaGain");
    }

    static string Stat_SPRegen()
    {
        return LocalizationManager.GetTranslation("label/character/stats/spRegen");
    }

    static string Stat_ConsumablePlus()
    {
        return LocalizationManager.GetTranslation("label/character/stats/consumablePlus");
    }

    //==== Hardcoded Translation ====//
    public static string Gold()
    {
        return LocalizationManager.GetTranslation("general/gold");
    }
    public static string Ruby()
    {
        return LocalizationManager.GetTranslation("general/ruby");
    }
    public static string Stage()
    {
        return LocalizationManager.GetTranslation("general/stage");
    }
    public static string FormatMinuteSeconds(TimeSpan my)
    {
        return string.Format("{0}:{1}",
                Mathf.FloorToInt((float)my.Minutes).ToString("00"),
                my.Seconds.ToString("00"));
    }
    public static string TrialChallengeByType(TrialChallengeType type, int amount)
    {
        var result = LocalizationManager.GetTranslation("texts/trial/" + type.ToString().ToLower());
        if (amount > 0)
        {
            try
            {
                var format = string.Format(result, amount);
                result = format;
            }
            catch { }
        }

        return result;
    }

    public static string GetDefenseSystem(Monolith_Attack type, MonolithValues values)
    {
        string result = LocalizationManager.GetTranslation("texts/trial_defense/" + type.ToString().ToLower());
        try
        {
            var args = new List<object>();
            // if (values.attackRepeat > 0)
            //     args.Add(ToRoman(Mathf.RoundToInt(values.attackRepeat)));

            if (values.attackPotency > 0)
                args.Add(values.attackPotency);

            result = string.Format(result, args.ToArray());
        }
        catch
        {
            result = LocalizationManager.GetTranslation("texts/trial_defense/" + type.ToString().ToLower());
        }

        return result;
    }

    public static string Inventory_Acquire(SupportedLanguages lang)
    {
        return LocalizationManager.GetTranslation("texts/item_acquire");
    }
    public static string Inventory_Full_Warning(SupportedLanguages lang)
    {
        return LocalizationManager.GetTranslation("texts/inventory_full");
    }
    public static string Equipment_Unequip(SupportedLanguages lang)
    {
        return LocalizationManager.GetTranslation("texts/unequip");
    }
    public static string Skill_Cooldown(SupportedLanguages lang)
    {
        return LocalizationManager.GetTranslation("texts/skill_cooldown");
    }
    public static string Skill_Manacost(SupportedLanguages lang)
    {
        return LocalizationManager.GetTranslation("texts/skill_mpcost");
    }
    public static string Skill_NotAssigned(SupportedLanguages lang)
    {
        return LocalizationManager.GetTranslation("texts/not_assigned");
    }


    public static string Interact_Talk()
    {
        return LocalizationManager.GetTranslation("texts/interact/talk");
    }
    public static string Interact_Examine()
    {
        return LocalizationManager.GetTranslation("texts/interact/examine");
    }
    public static string Interact_Checkpoint()
    {
        return LocalizationManager.GetTranslation("texts/interact/checkpoint");
    }
    public static string Interact_Open()
    {
        return LocalizationManager.GetTranslation("texts/interact/open");
    }
    public static string Interact_Proceed()
    {
        return LocalizationManager.GetTranslation("texts/interact/proceed");
    }
    public static string Interact_Fish()
    {
        return LocalizationManager.GetTranslation("texts/interact/fish");
    }
    public static string Interact_Campsite()
    {
        return LocalizationManager.GetTranslation("texts/interact/campsite");
    }

    public static string Text_On()
    {
        return LocalizationManager.GetTranslation("label/button/On");
    }
    public static string Text_Off()
    {
        return LocalizationManager.GetTranslation("label/button/Off");
    }
    public static string Text_HardShadow()
    {
        return LocalizationManager.GetTranslation("menu/option/graphic/hardShadow");
    }
    public static string Text_SoftShadow()
    {
        return LocalizationManager.GetTranslation("menu/option/graphic/softShadow");
    }
    public static string Text_Preset(int lv)
    {
        return LocalizationManager.GetTranslation("menu/option/graphic/preset/" + lv);
    }
    public static string Text_CrowdAll()
    {
        return LocalizationManager.GetTranslation("menu/option/graphic/crowd/all");
    }
    public static string Text_CrowdNearby()
    {
        return LocalizationManager.GetTranslation("menu/option/graphic/crowd/closest");
    }
    public static string Text_NoLimitFPS()
    {
        return LocalizationManager.GetTranslation("menu/option/graphic/fps_noLimit");
    }
    public static string Text_Claimed()
    {
        return LocalizationManager.GetTranslation("label/button/Claimed");
    }
    public static string Text_Claim()
    {
        return LocalizationManager.GetTranslation("label/button/Claim");
    }
    public static string Text_Activated()
    {
        return LocalizationManager.GetTranslation("label/button/activated");
    }


    //==== Other ====//
    public static string StatValueCharacterInfo(EC2.Stats s, float val)
    {
        string result = "";// val >= 0 ? " +" : " -";

        switch (s)
        {
            //Offensive
            case EC2.Stats.Attack: result += Mathf.Floor(val).ToString(); break;
            case EC2.Stats.AttackSpeed: result += val.ToString("F2") + "%"; break;
            case EC2.Stats.Crit: result += val.ToString("F2") + "%"; break;
            case EC2.Stats.CritDamage: result += val.ToString("F2") + "%"; break;
            //case Stats.Pierce: result += val.ToString("F2") + "%"; break;
            //case Stats.Break: result += val.ToString("F2") + "%"; break;

            //Defensive
            case EC2.Stats.MaxHP: result += Mathf.Floor(val).ToString(); break;
            case EC2.Stats.Defense: result += Mathf.Floor(val).ToString(); break;
            case EC2.Stats.Recovery: result += val.ToString("F2") + "%"; break;
            case EC2.Stats.ElementalResistance: result += val.ToString("F2") + "%"; break;
            case EC2.Stats.PhysicalResistance: result += Mathf.Floor(val).ToString(); break;
            case EC2.Stats.Evasion: result += val.ToString("F2") + "%"; break;

            //Utility
            //case Stats.ManaGain: result += val.ToString("F2") + "%"; break;
            case EC2.Stats.ManaReduce: result += val.ToString("F2") + "%"; break;
            case EC2.Stats.CooldownReduce: result += val.ToString("F2") + "%"; break;
            case EC2.Stats.Speciality: result += val.ToString("F2") + "%"; break;
            //case Stats.ConsumablePlus: result += val.ToString("F2") + "%"; break;
            //case Stats.DropRatePlus: result += val.ToString("F2") + "%"; break;
            default: result = ""; break;
        }

        return result;
    }
    public static string ConvertedValue(EC2.Stats s, float val)
    {
        if (s == EC2.Stats.Attack || s == EC2.Stats.MaxHP || s == EC2.Stats.Defense || s == EC2.Stats.MaxMP)
        {
            if (val < 10) return val.ToString();
            return string.Format("{0:0,0}", val);
        }

        return string.Format("{0}%", (val / 10).ToString("F2"));
    }

    public static string ConvertedValue(SocketType s, float val)
    {
        return string.Format("{0}%", val.ToString("F2"));
    }
    public static Color GetRarityColor(Rarity rarity)
    {
        Color result = Color.white;
        switch (rarity)
        {
            case Rarity.Common: result = Color.white; break; //[ffffff]
            case Rarity.Uncommon: result = new Color(0.62f, 1.0f, 0.62f); break; //[9EFF9E]
            case Rarity.Rare: result = new Color(0.4f, 0.7f, 1.0f); break; //[66B2FF]
            case Rarity.Epic: result = new Color(1.0f, 0.47f, 0.43f); break; //[FF786E]
            case Rarity.Legendary: result = new Color(1.0f, 0.55f, 1.0f); break; //[FF8CFF]
        }
        return result;
    }
    public static string GetRarityColorHEX(Rarity rarity)
    {
        string result = "FFFFFF";
        switch (rarity)
        {
            case Rarity.Common: result = "FFFFFF"; break; //[ffffff]
            case Rarity.Uncommon: result = "9EFF9E"; break; //[9EFF9E]
            case Rarity.Rare: result = "66B2FF"; break; //[66B2FF]
            case Rarity.Epic: result = "FF786E"; break; //[FF786E]
            case Rarity.Legendary: result = "FF8CFF"; break; //[FF8CFF]
        }
        return result;
    }
    public static float GetRuneEXPToNextLevel(Rarity rarity, int currentLevel)
    {
        return GetRuneValue(rarity) * (currentLevel + 1);
    }

    public static double ParseDouble(string s)
    {
        double result = 0;

        try
        {
            if (string.IsNullOrEmpty(s))
                result = 0;
            else
                result = double.Parse(s);
        }
        catch
        {

        }

        return result;
    }
    public static int GetRuneEXPPoint(Rarity rarity, int runeLevel)
    {
        int res = GetRuneValue(rarity);
        res += Mathf.RoundToInt(EC2Utils.GetRuneEXPByEnhancementLevel(rarity, runeLevel));
        return res;
    }
    public static int GetRuneValue(Rarity rarity)
    {
        int res = 0;
        switch (rarity)
        {
            case Rarity.Uncommon:
                res = 1;
                break;

            case Rarity.Rare:
                res = 3;
                break;

            case Rarity.Epic:
                res = 6;
                break;

            case Rarity.Legendary:
                res = 10;
                break;

            default:
                res = 1;
                break;
        }
        return res;
    }
    public static int GetRuneEXPByEnhancementLevel(Rarity rarity, int runeLevel)
    {
        int res = 0;
        switch (runeLevel)
        {
            case 1:
                res = 1; break;
            case 2:
                res = 2; break;
            case 3:
                res = 4; break;
            case 4:
                res = 7; break;
            case 5:
                res = 10; break;
            default:
                res = 0; break;
        }

        return res * GetRuneValue(rarity);
    }
    public static bool IsRarityAbove(Rarity source, Rarity requirement)
    {
        return (int)source >= (int)requirement;
    }
    public static float GetUpgradeValue(float baseVal, Rarity baseRarity, Rarity targetRarity)
    {
        int dif = (int)targetRarity - (int)baseRarity;
        float modifier = 0.25f * dif;

        float finalVal = baseVal + (modifier * baseVal);
        finalVal = Mathf.Ceil(finalVal);

        return finalVal;
    }
    public static float GetEnhancedValue(float baseVal, int enhancementLevel)
    {
        float modifier = 1;

        if (enhancementLevel <= 5) modifier += enhancementLevel * 0.1f;
        else if (enhancementLevel > 5 && enhancementLevel <= 10)
            modifier += 0.5f + ((enhancementLevel - 5) * 0.15f);
        else modifier += 1.25f + ((enhancementLevel - 10) * 0.2f);

        return Mathf.Floor(baseVal * modifier);
    }
    public static float GetComparation(float previousVal, float newVal)
    {
        return Mathf.Floor(newVal - previousVal);
    }
    public static float GetDefenseDamageReduction(float defense, int enemyLevel)
    {
        /*
        //FORMULA (A)
        float trueDef = defense * 2;
        float modifier = 2;
        float dmgReduction = (1 / modifier) * trueDef / Mathf.Sqrt(enemyLevel);
        */

        //FORMULA (B)
        float defMod = 5f;
        float levelMod = 5f;
        float dmgReduction = (defense * defMod) / (Mathf.Sqrt(enemyLevel) * levelMod);
        float baseDmgReduction = dmgReduction;
        //if reduction value is greater than 70%, any excess value will be cut

        //Debug.Log("Base DR : " + dmgReduction);

        float excess95 = 0;
        if (dmgReduction >= 95)
        {
            float excess = Mathf.Abs(95f - dmgReduction);
            excess95 = excess * 0.3f;

            dmgReduction -= excess;

            //Debug.Log(string.Format("DR higher than 95. cut excess {0} by 0.35 => {1} --- DR is now {2}", excess, excess95, dmgReduction));
        }

        float excess85 = 0;
        if (dmgReduction >= 85)
        {
            float excess = Mathf.Abs(85f - dmgReduction);
            excess85 = excess * 0.4f;

            dmgReduction -= excess;

            //Debug.Log(string.Format("DR higher than 85. cut excess {0} by 0.5 => {1} --- DR is now {2}", excess, excess85, dmgReduction));
        }

        float excess70 = 0;
        if (dmgReduction >= 70)
        {
            float excess = Mathf.Abs(70f - dmgReduction);
            excess70 = excess * 0.5f;

            dmgReduction -= excess;

            //Debug.Log(string.Format("DR higher than 70. cut excess {0} by 0.65 => {1} --- DR is now {2}", excess, excess70, dmgReduction));
        }

        if (excess70 > 0)
            dmgReduction = 70f + excess70 + excess85 + excess95;
        else dmgReduction = baseDmgReduction;

        //Debug.Log(string.Format("DR = 70 + {0} + {1} + {2} = {3}", excess70, excess85, excess95, dmgReduction));

        return dmgReduction;
    }
    public static int GetSkillMaxExp(int level)
    {
        if (level >= 10) return 0;

        int[] expTable = new int[10] { 0, 10, 25, 50, 100, 150, 200, 300, 400, 500 };
        return expTable[level];
    }

    public static float Scaling(float oldMin, float oldMax, float newMin, float newMax, float value)
    {
        float oldRange = (oldMax - oldMin);
        float newRange = (newMax - newMin);
        float newValue = (((value - oldMin) * newRange) / oldRange) + newMin;

        return newValue;
    }

    //==== Audio ====//
    public static float GetMasterVol(int i)
    {
        float[] masterVolRange = new float[11] { -40, -18, -16, -14, -12, -10, -8, -6, -4, -2, 0 };
        return masterVolRange[i];
    }
    public static float GetOtherVol(int i)
    {
        float[] otherVolRange = new float[11] { -80, -30, -28, -26, -20, -15, -13, -8, -5, -3, 0 };
        return otherVolRange[i];
    }

    //==== PlayerPrefs ====//
    public static string GetCurrentProfilePrefs(string prefs)
    {
        return GetCurrentProfilePrefs(PlayerPrefs.GetInt("active"), prefs);
    }
    public static string GetCurrentProfilePrefs(int index, string prefs)
    {
        return index + "_" + prefs;
    }

    //This PlayerPrefs can't be missing from progress
    public static void CreateImportantPrefs(int questId)
    {
        if (questId >= 4) PlayerPrefs.SetInt(GetCurrentProfilePrefs("q3_gift"), 1);
        if (questId >= 4) PlayerPrefs.SetInt(GetCurrentProfilePrefs("cs007_seen"), 1);
        if (questId >= 23) PlayerPrefs.SetInt(GetCurrentProfilePrefs("cs021_seen"), 1);
        if (questId >= 36) PlayerPrefs.SetInt(GetCurrentProfilePrefs("cs031_seen"), 1);
    }

    //This PlayerPrefs are to be deleted on New Game
    public static string[] deletedPrefsOnNewGame = new string[13]
    {
        "quickSlot",        //player's quick slot data
        "cs003_seen",       //player has seen Cutscene003, skip to Outskirts battle
        "cs007_seen",       //player has seen Cutscene007, skip to Boar battle
        "rift_tutorial",    //player has triggered the Dimension Rift Tutorial (right after Cutscene015)
        "q3_gift",          //player has received Nina's Gift in QuestId 3
        "craft_tutorial",   //player has triggered the Blacksmith - Crafting Tutorial (in front of Jessie's Worker)
        "enhance_tutorial", //player has triggered the Blacksmith - Enhance/Upgrade Tutorial (right after Cutscene009)
        "transfer_tutorial",//player has triggered the Blacksmith - Transfer Tutorial
        "cs021_seen",       //player has seen Cutscene021, skip to DemonWolf_Phase2
        "newpower_tutorial",//player has triggered the A New Power Tutorial (right after Cutscene025)
        "navigation_tutorial",//player has triggered the Navigation Tutorial (right after Cutscene002)
        "cs031_seen",        //player has seen Cutscene031, skip to Mino_1
        "fish_tutorial"    //player has triggered the Fishing Tutorial (Outskirts)
    };

    public static void SetPortalIndex(int index)
    {
        if (index >= 0)
            PlayerPrefs.SetInt("nextPortalIndex", index);
        else PlayerPrefs.DeleteKey("nextPortalIndex");
    }
    public static int PortalIndex()
    {
        if (!PlayerPrefs.HasKey("nextPortalIndex"))
            return -1;
        return PlayerPrefs.GetInt("nextPortalIndex");
    }

    public static int Pengurangan(int a, int b)
    {
        return a - b;
    }

    public static int Penjumlahan(int a, int b)
    {
        return a + b;
    }

    public static bool IsMine()
    {
        return true;
    }
}
