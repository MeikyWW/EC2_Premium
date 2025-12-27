using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using CodeStage.AntiCheat.Storage;
using CodeStage.AntiCheat.ObscuredTypes;
using I2.Loc;
using MEC;
using Sirenix.OdinInspector;

public class GlobalUserData : MonoBehaviour
{
    public static int key = 135; //key for decode cipher XOR
    public User data;
    [HideInInspector] public User[] userProfile;
    public Profile[] profile;  //using struct for readonly purposes

    public Settings settings = new Settings();
    public UserAdventureData heroesData = new UserAdventureData();

    public int activeSlot;
    public string profileName;

    GameManager gm;
    CutsceneHandler ch;
    TimestampManager timeStamp;
    [HideInInspector] public bool saveLoaded = false;

    [HideInInspector] public ObscuredInt gdt;
    [HideInInspector] public ObscuredInt rdt;

    string newDevicePassphrase;

    public void Load()
    {
        if (saveLoaded) return;

        LoadProfile();
        LoadHeroes();
        LoadSetting(true);

        saveLoaded = true;
    }
    public void InitStartScreen()
    {
        if (saveLoaded) return;
#if UNITY_STANDALONE
        userProfile = new User[3];
        profile = new Profile[3];
        LoadProfileThumbnailData();
#endif
        if (SaveFileExist())
            LoadProfile();

        LoadSetting(false);
        saveLoaded = true;
    }
#if UNITY_STANDALONE
    public string LatestCheckpoint(string profilename)
    {
        var prefData = ObscuredPrefs.GetString(EC2Constant.EC2_PROFILE_KEY, "");
        if(!string.IsNullOrEmpty(prefData))
        {
            var savedData = EncryptDecryptSaveFile(prefData);
            data = EC2Utils.FromJson<User>(savedData);
            return data.latestCheckpoint;
        }
        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            + "\\Epic Conquest 2\\" + profilename + "\\" + profilename + ".save";
        if (File.Exists(filename))
        {
            byte[] saveFile = File.ReadAllBytes(filename);
            string savedata = EncryptDecryptSaveFile(System.Text.Encoding.UTF8.GetString(saveFile));
            data = EC2Utils.FromJson<User>(savedata);

            return data.latestCheckpoint;
        }

        return "Capital_Park";
    }
#endif
    public void Save()
    {
        // profile properties
        activeSlot = PlayerPrefs.GetInt("active", 0);
        profileName = "user_" + activeSlot;
        gm = GameManager.instance;
        ch = CutsceneHandler.instance;
        data.deviceID = SystemInfo.deviceUniqueIdentifier;
        data.gold = gdt;
        data.ruby = rdt;
        //set playtime on latest timestamp
        if (gm)
        {
            //Debug.Log(timeStamp.timestamp);
            data.playTime = timeStamp.timestamp;
        }
        string savedata = EC2Utils.ToJson(data);
        string settingdata = EC2Utils.ToJson(settings);

        SaveProfile(profileName, savedata);
        SaveSetting(settingdata);
    }

    void SaveProfile(string profilename, string savedata)
    {
#if UNITY_ANDROID || UNITY_IOS
        ObscuredPrefs.SetString(EC2Constant.EC2_PROFILE_KEY, savedata);

        if (ObscuredPrefs.HasKey(EC2Constant.EC2_PROFILE_KEY_OLD))
            ObscuredPrefs.DeleteKey(EC2Constant.EC2_PROFILE_KEY_OLD);

        ObscuredPrefs.Save();
#else
        DirectoryInfo info = Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
           + "\\Epic Conquest 2\\" + profilename + "\\");
        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
          + "\\Epic Conquest 2\\" + profilename + "\\" + profilename + ".save";
        System.IO.File.WriteAllText(filename, savedata);
#endif
    }

    public void SaveUserHero(string profilename, string[] herodata, string inventory)
    {
#if UNITY_ANDROID || UNITY_IOS
        DirectoryInfo info = Directory.CreateDirectory(Application.persistentDataPath
     + "\\Epic Conquest 2\\" + profilename + "\\");
        string filename = Application.persistentDataPath
          + "\\Epic Conquest 2\\" + profilename + "\\" + "user_hero.save";
#else
        DirectoryInfo info = Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
     + "\\Epic Conquest 2\\" + profilename + "\\");
        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
          + "\\Epic Conquest 2\\" + profilename + "\\" + "user_hero.save";
#endif

        string hero = herodata[0] + "\n#"
                     + herodata[1] + "\n#"
                     + herodata[2] + "\n#"
                     + herodata[3];

        System.IO.File.WriteAllText(filename, EncryptDecryptSaveFile(hero + "\n$" + inventory));
    }

    public void SaveAdventureData(UserAdventureData adventureData)
    {
        var encryptedData = EC2Utils.ToJson(adventureData);
        //save new data
        ObscuredPrefs.SetString(EC2Constant.EC2_HERO_KEY, encryptedData);
        //delete old data
        if (ObscuredPrefs.HasKey(EC2Constant.EC2_HERO_KEY_OLD))
            ObscuredPrefs.DeleteKey(EC2Constant.EC2_HERO_KEY_OLD);

        ObscuredPrefs.Save();

    }

    public void SaveSetting(string settingdata)
    {
#if UNITY_ANDROID || UNITY_IOS
        s_isOutlineInited = false;
        ObscuredPrefs.SetString(EC2Constant.EC2_SETTING_KEY, settingdata);
        ObscuredPrefs.Save();
#else
        DirectoryInfo info = Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            + "\\Epic Conquest 2");
        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            + "\\Epic Conquest 2\\settings.save";
        System.IO.File.WriteAllText(filename, settingdata);
#endif
    }
    public void SaveSetting()
    {
        string settingdata = EC2Utils.ToJson(settings);

#if UNITY_ANDROID || UNITY_IOS
        s_isOutlineInited = false;
        ObscuredPrefs.SetString(EC2Constant.EC2_SETTING_KEY, settingdata);
        ObscuredPrefs.Save();
#else
        DirectoryInfo info = Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            + "\\Epic Conquest 2");
        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            + "\\Epic Conquest 2\\settings.save";
        System.IO.File.WriteAllText(filename, settingdata);
#endif
    }

    public void SetSaveSlotIndex(int index)
    {
        PlayerPrefs.SetInt("active", index);
    }

    public void LoadProfile()
    {
        // profile properties
        //int activeSlot = PlayerPrefs.GetInt("active", 0);
        //string profilename = "user_" + activeSlot;

        gm = GameManager.instance;
        ch = CutsceneHandler.instance;
#if UNITY_ANDROID || UNITY_IOS
        string prefData;
        bool isLoadingOldData;
        if (ObscuredPrefs.HasKey(EC2Constant.EC2_PROFILE_KEY_OLD))
        {
            isLoadingOldData = true;
            prefData = ObscuredPrefs.GetString(EC2Constant.EC2_PROFILE_KEY_OLD, "");
        }

        else
        {
            isLoadingOldData = false;
            prefData = ObscuredPrefs.GetString(EC2Constant.EC2_PROFILE_KEY, "");
        }

        if (!string.IsNullOrEmpty(prefData))
        {
            string savedata;

            if (isLoadingOldData) savedata = EncryptDecryptSaveFile(prefData);
            else savedata = prefData;
#else
        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            + "\\Epic Conquest 2\\" + profilename + "\\" + profilename + ".save";
        if (File.Exists(filename))
        {
            byte[] saveFile = File.ReadAllBytes(filename);
            string savedata = EncryptDecryptSaveFile(System.Text.Encoding.UTF8.GetString(saveFile));
#endif
            data = EC2Utils.FromJson<User>(savedata);
            gdt = data.gold;
            rdt = data.ruby;

            data.CheckSeed();

            /*
            if (CurrentQuestID == 6 && gm) // Spawn at fixed point if claris/player current quest id is at 6 // Part 2 Initializer   
            {
                SetTargetScene("Capital_Central");
                PlayerPrefs.SetInt("spawnToCheckpoint", 0);
                PlayerPrefs.SetInt("noBgmOnLoad", 1);
                EC2Utils.SetPortalIndex(4);
            }*/
            if (CurrentQuestID == 23 && gm) // Part 3 Initializer   
            {
                SetTargetScene("Forest_10");
                SetCheckpoint("Forest_14");
                PlayerPrefs.SetInt("spawnToCheckpoint", 2);
            }
        }
        else
        {
            //Create new save
            CreateNewUserData();
        }

        if (data.heroesUnlocked.Count == 0)
        {
            data.heroesUnlocked.Add(Hero.Claris);
        }

        if (data.heroesInCharge.Count == 0)
        {
            data.heroesInCharge.Add(Hero.Claris);
        }

        if (!data.migratedonamy)
        {
            data.migratedonamy = true;
            if (data.premiumUser)
            {
                //if (data.purchasedItems.Find(x => x == "") == null)
                data.purchasedItems.Add("premium_mode");
            }
        }

        if (gm)
        {
            timeStamp = gm.timeStamp;
            timeStamp.timestamp = data.playTime;
        }
    }

    public void LoadHeroes()
    {
        var prefData = ObscuredPrefs.GetString(EC2Constant.EC2_HERO_KEY, "");

        if (!string.IsNullOrEmpty(prefData))
        {
            heroesData = EC2Utils.FromJson<UserAdventureData>(prefData);
        }

        else heroesData = new UserAdventureData();
    }

    public bool IsMyDevice()
    {
        var prefData = ObscuredPrefs.GetString(EC2Constant.EC2_PROFILE_KEY_OLD, "");
        if (!string.IsNullOrEmpty(prefData))
        {
            var savedata = EncryptDecryptSaveFile(prefData);

            data = EC2Utils.FromJson<User>(savedata);

            if (!string.IsNullOrEmpty(data.deviceID)) //check kalo pake save2an orang
            {
                if (data.deviceID != SystemInfo.deviceUniqueIdentifier)
                {
                    Debug.Log("DeviceID is different. Force create new user data");
                    return false;
                }
            }
        }

        return true;
    }

    //    void LoadProfileThumbnailData()
    //    {
    //        LoadThumbnailUser0();
    //        LoadThumbnailUser1();
    //        LoadThumbnailUser2();
    //    }

    //    void LoadThumbnailUser0()
    //    {
    //        string profilename = "user_" + 0;
    //#if UNITY_ANDROID || UNITY_IOS
    //        string filename = Application.persistentDataPath
    //       + "\\Epic Conquest 2\\" + profilename + "\\" + profilename + ".save";
    //#else
    //        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
    //       + "\\Epic Conquest 2\\" + profilename + "\\" + profilename + ".save";
    //#endif
    //        if (File.Exists(filename))
    //        {
    //            //print("load profile thumbnail: " + profilename);
    //            byte[] saveFile = File.ReadAllBytes(filename);
    //            string savedata = EncryptDecryptSaveFile(System.Text.Encoding.UTF8.GetString(saveFile));

    //            //userProfile[0] = new User();
    //            userProfile[0] = EC2Utils.FromJson<User>(savedata);
    //            //print("Playtime 1 : " + userProfile[0].playTime);
    //            decimal dec = new decimal(userProfile[0].playTime);
    //            double d = (double)dec;

    //            profile[0] = new Profile();
    //            profile[0].heroLevel = userProfile[0].heroLevels.Count != 0 ? userProfile[0].heroLevels : new List<int>();
    //            //profile[0].lastCheckpoint = userProfile[0].latestCheckpoint;
    //            profile[0].playtime = TimeSpan.FromSeconds(d).ToString(@"dd\:hh\:mm\:ss");
    //        }
    //    }
    //    void LoadThumbnailUser1()
    //    {
    //        string profilename = "user_" + 1;

    //#if UNITY_ANDROID || UNITY_IOS
    //        string filename = Application.persistentDataPath
    //       + "\\Epic Conquest 2\\" + profilename + "\\" + profilename + ".save";
    //#else
    //        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
    //       + "\\Epic Conquest 2\\" + profilename + "\\" + profilename + ".save";
    //#endif
    //        if (File.Exists(filename))
    //        {
    //            //print("load profile thumbnail: " + profilename);
    //            byte[] saveFile = File.ReadAllBytes(filename);
    //            string savedata = EncryptDecryptSaveFile(System.Text.Encoding.UTF8.GetString(saveFile));

    //            //userProfile[1] = new User();
    //            userProfile[1] = EC2Utils.FromJson<User>(savedata);
    //            //print("Playtime 2 : " + userProfile[1].playTime);
    //            decimal dec = new decimal(userProfile[1].playTime);
    //            double d = (double)dec;

    //            profile[1] = new Profile();
    //            profile[1].heroLevel = userProfile[1].heroLevels.Count != 0 ? userProfile[1].heroLevels : new List<int>();
    //            //profile[1].lastCheckpoint = userProfile[1].latestCheckpoint;
    //            profile[1].playtime = TimeSpan.FromSeconds(d).ToString(@"dd\:hh\:mm\:ss");
    //        }
    //    }
    //    void LoadThumbnailUser2()
    //    {
    //        string profilename = "user_" + 2;

    //#if UNITY_ANDROID || UNITY_IOS
    //        string filename = Application.persistentDataPath
    //       + "\\Epic Conquest 2\\" + profilename + "\\" + profilename + ".save";
    //#else
    //        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
    //       + "\\Epic Conquest 2\\" + profilename + "\\" + profilename + ".save";
    //#endif
    //        if (File.Exists(filename))
    //        {
    //            //print("load profile thumbnail: " + profilename);
    //            byte[] saveFile = File.ReadAllBytes(filename);
    //            string savedata = EncryptDecryptSaveFile(System.Text.Encoding.UTF8.GetString(saveFile));

    //            //userProfile[2] = new User();
    //            userProfile[2] = EC2Utils.FromJson<User>(savedata);
    //            //print("Playtime 3 : " + userProfile[2].playTime);
    //            decimal dec = new decimal(userProfile[2].playTime);
    //            double d = (double)dec;

    //            profile[2] = new Profile();
    //            profile[2].heroLevel = userProfile[2].heroLevels.Count != 0 ? userProfile[2].heroLevels : new List<int>();
    //            //profile[2].lastCheckpoint = userProfile[2].latestCheckpoint;
    //            profile[2].playtime = TimeSpan.FromSeconds(d).ToString(@"dd\:hh\:mm\:ss");
    //        }
    //    }
    public void DeleteSaveFile(string profilename)
    {
#if UNITY_ANDROID || UNITY_IOS
        string filename = Application.persistentDataPath
          + "\\Epic Conquest 2\\" + profilename + "\\";
        string renderedMap = Application.persistentDataPath + "/Resources/RenderedMap/" + profilename + "/";
#else
        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
          + "\\Epic Conquest 2\\" + profilename + "\\";
        string renderedMap = Application.dataPath + "/Resources/RenderedMap/" + profilename + "/";
#endif
        //delete save file
        if (Directory.Exists(filename))
        {
            string[] files = Directory.GetFiles(filename);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            //delete rendered map
            if (Directory.Exists(renderedMap))
            {
                string[] map = Directory.GetFiles(renderedMap);
                foreach (string m in map)
                {
                    File.SetAttributes(m, FileAttributes.Normal);
                    File.Delete(m);
                }
            }

            //InitActiveRequest();
            print("Save Data Deleted: " + filename);
        }
    }
    public void DeleteRenderedMap(string profilename)
    {
#if UNITY_ANDROID || UNITY_IOS
        string renderedMap = Application.persistentDataPath + "/Resources/RenderedMap/" + profilename + "/";
#else
        string renderedMap = Application.dataPath + "/Resources/RenderedMap/" + profilename + "/";
#endif
        if (Directory.Exists(renderedMap))
        {
            string[] map = Directory.GetFiles(renderedMap);
            foreach (string m in map)
            {
                File.SetAttributes(m, FileAttributes.Normal);
                File.Delete(m);
            }
            print("rendered map folder: deleted");
        }
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

    float modifier;
    public static event System.Action<ShadowQuality> OnShadowSettingLoaded;
    public static event System.Action<bool> OnOutlineSettingLoaded;
    public static event System.Action<bool> OnFoliageSettingLoaded;
    //public static event System.Action<bool> OnDynamicPhysicsSettingLoaded;
    public static event System.Action<CrowdSetting> OnCrowdSettingLoaded;
    public void LoadSetting(bool applyNow)
    {
#if UNITY_ANDROID || UNITY_IOS
        var prefData = ObscuredPrefs.GetString(EC2Constant.EC2_SETTING_KEY, "");
        if (!string.IsNullOrEmpty(prefData))
        {
            var settingdata = prefData;
#else
        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            + "\\Epic Conquest 2\\settings.save";
        if (File.Exists(filename))
        {
            byte[] saveFile = File.ReadAllBytes(filename);
            string settingdata = System.Text.Encoding.UTF8.GetString(saveFile);
#endif

            settings = EC2Utils.FromJson<Settings>(settingdata);

            //testing
            //data.questId = 35;
        }
        else
        {
            settings = new Settings();
            settings.refreshRate = Screen.currentResolution.refreshRate <= 30 ? 30 : 60;
            SaveSetting();
        }

        ApplyUserLanguage(settings.languageID);

        if (applyNow)
        {
            Application.targetFrameRate = settings.refreshRate;// settings.refreshRate;
            SetResolution();
            LoadEnvironmentSetting();
        }
    }

    public static Settings GetUserSetting()
    {
        Settings settings;
        var prefData = ObscuredPrefs.GetString(EC2Constant.EC2_SETTING_KEY, "");
        if (!string.IsNullOrEmpty(prefData))
        {
            var settingdata = prefData;
            settings = EC2Utils.FromJson<Settings>(settingdata);

        }
        else
        {
            settings = new Settings();
            settings.refreshRate = Screen.currentResolution.refreshRate <= 30 ? 30 : 60;
        }

        OnCrowdSettingLoaded?.Invoke(settings.crowdSetting);
        OnShadowSettingLoaded?.Invoke(settings.shadowQuality);
        OnOutlineSettingLoaded?.Invoke(settings.useOutline);
        OnFoliageSettingLoaded?.Invoke(settings.useFoliage);
        //OnDynamicPhysicsSettingLoaded?.Invoke(settings.useDynamicPhysics);

        return settings;
    }
    public static User GetUserData()
    {
        User user;
        var prefData = ObscuredPrefs.GetString(EC2Constant.EC2_PROFILE_KEY, "");
        if (!string.IsNullOrEmpty(prefData))
        {
            var userdata = prefData;
            user = EC2Utils.FromJson<User>(userdata);

        }
        else
        {
            user = new User();
        }

        return user;
    }

    public static bool s_isOutlineInited;
    public static bool s_useOutline;
    public static bool GetOutlineSetting()
    {
        if (s_isOutlineInited) return s_useOutline;
        var prefData = ObscuredPrefs.GetString(EC2Constant.EC2_SETTING_KEY, "");
        if (!string.IsNullOrEmpty(prefData))
        {
            var settingdata = prefData;
            var settings = EC2Utils.FromJson<Settings>(settingdata);
            s_useOutline = settings.useOutline;
        }
        else s_useOutline = new Settings().useOutline;

        s_isOutlineInited = true;
        return s_useOutline;
    }

    public void LoadEnvironmentSetting()
    {
        OnShadowSettingLoaded?.Invoke(settings.shadowQuality);
        OnOutlineSettingLoaded?.Invoke(settings.useOutline);
        OnFoliageSettingLoaded?.Invoke(settings.useFoliage);
    }

    public void ResetUserData(int val)
    {
        data = new User(val);
        gdt = val;
        rdt = 0;
    }
    public void AdvanceQuest()
    {
        if (data.questId <= gm.questManager.questContainer.quest.Length)
            data.questId++;
    }

    public void ApplyUserLanguage(SupportedLanguages lang)
    {
        Debug.Log(LocalizationManager.CurrentLanguage);
        switch (lang)
        {
            case SupportedLanguages.english:
                LocalizationManager.CurrentLanguage = "English";
                break;

            case SupportedLanguages.indonesia:
                LocalizationManager.CurrentLanguage = "Indonesian";
                break;

            case SupportedLanguages.portuguese:
                LocalizationManager.CurrentLanguage = "Portuguese (Brazil)";
                break;

            case SupportedLanguages.chinese_simplified:
                if (LocalizationManager.Sources[0].FindAsset(EC2Constant.CN_FONT_BOLD) == null ||
                    LocalizationManager.Sources[0].FindAsset(EC2Constant.CN_FONT_REGULAR) == null)
                {
                    if (AddressableManager.instance)
                        Timing.RunCoroutine(AddressableManager.instance.GetAssetInfo(EC2Constant.CN_FONT_BOLD, CheckFont, SetDefaultLanguage));
                }
                LocalizationManager.CurrentLanguage = "Chinese (Simplified)";
                break;


            case SupportedLanguages.spanish:
                LocalizationManager.CurrentLanguage = "Spanish";
                break;

            case SupportedLanguages.french:
                LocalizationManager.CurrentLanguage = "French";
                break;

            default:
                LocalizationManager.CurrentLanguage = "English";
                break;
        }
    }

    void CheckFont(long size)
    {
        if (size > 0f)
            SetDefaultLanguage();
        else
        {
            IList<object> keys = new List<object>()
            {
                EC2Constant.CN_FONT_REGULAR, EC2Constant.CN_FONT_BOLD
            };

            Timing.RunCoroutine(AddressableManager.instance.LoadAssets(keys, AssignFont));
        }
    }

    void AssignFont(IList<object> objs)
    {
        if (objs.Count == 0)
        {
            SetDefaultLanguage();
            return;
        }

        Debug.Log("assigning font");
        foreach (var loadedObj in objs)
        {
            var font = loadedObj as Font;
            if (font != null)
                LocalizationManager.Sources[0].AddAsset(font);
        }
        LocalizationManager.CurrentLanguage = "Chinese (Simplified)";
    }

    void SetDefaultLanguage()
    {
        Debug.Log("set default language");
        settings.languageID = SupportedLanguages.english;
        LocalizationManager.CurrentLanguage = "English";
    }

    public void AdvanceQuest(int targetQuestId)
    {
        if (targetQuestId > CurrentQuestID)
            data.questId++;
    }

    //Properties
    public int CurrentQuestID
    {
        get => data.questId;
    }
    public int LanguageID
    {
        get => (int)settings.languageID;
    }
    public int z_gdt
    {
        get => gdt;
    }

    public static string GetLatestCheckPoint(User user)
    {
        var loadedScene = "Capital_Park";
        if (user == null) return loadedScene;

        var prefData = ObscuredPrefs.GetString(EC2Constant.EC2_LATEST_CHECKPOINT, "");
        if (!string.IsNullOrEmpty(prefData))
            loadedScene = prefData;

        if (!string.IsNullOrEmpty(user.lastCheckpoint))
            loadedScene = user.lastCheckpoint;

        return loadedScene;
    }
    public bool SaveFileExist()
    {
        return ObscuredPrefs.HasKey(EC2Constant.EC2_PROFILE_KEY_OLD) ||
            ObscuredPrefs.HasKey(EC2Constant.EC2_PROFILE_KEY);
    }
    public bool SaveFileExist(int index)
    {
#if UNITY_ANDROID || UNITY_IOS
        return ObscuredPrefs.HasKey(EC2Constant.EC2_PROFILE_KEY_OLD) ||
            ObscuredPrefs.HasKey(EC2Constant.EC2_PROFILE_KEY);
#else
        profileName = "user_" + index;
        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            + "\\Epic Conquest 2\\" + profileName + "\\" + profileName + ".save";

        if (File.Exists(filename)) return true;
        return false;
#endif
    }
    public bool SettingFileExist()
    {
#if UNITY_ANDROID || UNITY_IOS
        return ObscuredPrefs.HasKey(EC2Constant.EC2_SETTING_KEY);
#else
        string filename = Application.persistentDataPath
            + "\\Epic Conquest 2\\" + "settings.save";

        string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            + "\\Epic Conquest 2\\" + "settings.save";
        if (File.Exists(filename)) return true;
        return false;
#endif
    }
    public void CreateNewUserData()
    {
        SetCheckpoint("Capital_Park");
        print("new user data created");

        data.questId = 0;
        data.CheckSeed();
        //testing
        //data.questId = 20;
    }

    //Achievements
    public void EnemyKill()
    {
        data.enemyKilled++;
        EC2AchievementManager.AddProgression(AchievementType.enemyKilled, EC2AchievementDatabaseType.Combat, 1);
        //switch (data.enemyKilled)
        //{
        //    case 1:
        //        gm.achievements.SendAchievementToServer("combat_enemykill_1"); //first kill
        //        break;
        //    case 100:
        //        gm.achievements.SendAchievementToServer("combat_enemykill_100"); //100th kill
        //        break;
        //    case 500:
        //        gm.achievements.SendAchievementToServer("combat_enemykill_500"); //500th kill
        //        break;
        //    case 1000:
        //        gm.achievements.SendAchievementToServer("combat_enemykill_1000"); //1000th kill
        //        break;
        //}
    }
    public void EliteKill()
    {
        data.eliteKilled++;
        EC2AchievementManager.AddProgression(AchievementType.eliteKilled, EC2AchievementDatabaseType.Combat, 1);
        //switch (data.eliteKilled)
        //{
        //    case 10:
        //        gm.achievements.SendAchievementToServer("combat_elitekill_10"); //10th kill
        //        break;
        //    case 50:
        //        gm.achievements.SendAchievementToServer("combat_elitekill_50"); //50th kill
        //        break;
        //    case 100:
        //        gm.achievements.SendAchievementToServer("combat_elitekill_100"); //100th kill
        //        break;
        //}
    }
    public void ItemCrafted()
    {
        data.craftingCount++;
        EC2AchievementManager.AddProgression(AchievementType.craftingCount, EC2AchievementDatabaseType.Exploration, 1);
        //switch (data.craftingCount)
        //{
        //    case 1:
        //        gm.achievements.SendAchievementToServer("blacksmith_craft_1"); //first craft
        //        break;
        //    case 10:
        //        //10th craft
        //        break;
        //    case 100:
        //        //100th craft
        //        break;
        //}
    }

    public static System.Action<RenderTexture> OnResolutionChanged;
    public void SetResolution()
    {
        if (PlayerPrefs.GetInt("nores") == 1) return;

        if (!gm) return;
        switch (settings.renderQuality)
        {
            case RenderQuality.VeryLow: modifier = 0.4f; break;
            case RenderQuality.Low: modifier = 0.6f; break;
            case RenderQuality.Medium: modifier = 0.8f; break;
            case RenderQuality.High: modifier = 1f; break;
        }

        var cam = Camera.main;
        if (cam)
        {
            if (cam.targetTexture != null)
                cam.targetTexture.Release();

            var rtex = new RenderTexture(
                Mathf.RoundToInt(gm.width * modifier),
                Mathf.RoundToInt(gm.height * modifier), 24);
            cam.targetTexture = rtex;
            OnResolutionChanged?.Invoke(rtex);
            Debug.Log("Set resolution to : " +
                Mathf.RoundToInt(gm.width * modifier) + " x " +
                Mathf.RoundToInt(gm.height * modifier));
        }

        //Screen.SetResolution(Mathf.RoundToInt(gm.width), Mathf.RoundToInt(gm.height), true);
    }

    public static RenderTexture SetResolution(Settings settings, float width, float height)
    {
        float modifier = 1f;
        switch (settings.renderQuality)
        {
            case RenderQuality.VeryLow: modifier = 0.4f; break;
            case RenderQuality.Low: modifier = 0.6f; break;
            case RenderQuality.Medium: modifier = 0.8f; break;
            case RenderQuality.High: modifier = 1f; break;
        }

        var rtex = new RenderTexture(
            Mathf.RoundToInt(width * modifier),
            Mathf.RoundToInt(height * modifier), 24);

        //Screen.SetResolution(Mathf.RoundToInt(width), Mathf.RoundToInt(height), true);

        Debug.Log("Set resolution to : " +
            Mathf.RoundToInt(width * modifier) + " x " +
            Mathf.RoundToInt(height * modifier));

        return rtex;
    }

    public static RenderTexture GetRTX(float width, float height)
    {
        var rtex = new RenderTexture(
            Mathf.RoundToInt(width),
            Mathf.RoundToInt(height), 24);

        return rtex;
    }

    public void LevelUpChar(Hero hero, int currentLevel)
    {
        //data.heroLevels[(int)hero]++;
        EC2AchievementManager.AddCharacterProgression(AchievementType.maxLevelReached, currentLevel, hero);
        //switch (data.heroLevels[index])
        //{
        //    case 2:
        //        gm.achievements.SendAchievementToServer("char_level_1");
        //        break;
        //    case 10:
        //        gm.achievements.SendAchievementToServer("char_level_10");
        //        break;
        //    case 20:
        //        gm.achievements.SendAchievementToServer("char_level_20");
        //        break;
        //    case 30:
        //        gm.achievements.SendAchievementToServer("char_level_30");
        //        break;
        //    case 40:
        //        gm.achievements.SendAchievementToServer("char_level_40");
        //        break;
        //    case 50:
        //        gm.achievements.SendAchievementToServer("char_level_50");
        //        break;
        //}
    }
    public void SecretTreasurePointFound()
    {
        data.treasurePointGet++;
        EC2AchievementManager.AddProgression(AchievementType.treasurePointGet, EC2AchievementDatabaseType.Exploration, 1);
    }
    public void DepleteGather()
    {
        data.gatherDepleted++;
        EC2AchievementManager.AddProgression(AchievementType.gatherDepleted, EC2AchievementDatabaseType.Exploration, 1);
        //switch (data.gatherDepleted)
        //{
        //    case 1:
        //        gm.achievements.SendAchievementToServer("explore_gather_1"); //First Time Gathering
        //        break;
        //    case 50:
        //        gm.achievements.SendAchievementToServer("explore_gather_50"); //50th time gathering
        //        break;
        //    case 100:
        //        gm.achievements.SendAchievementToServer("explore_gather_100"); //100th time gathering
        //        break;
        //    case 500:
        //        gm.achievements.SendAchievementToServer("explore_gather_500"); //500th time gathering
        //        break;
        //}
    }
    public void OreDepleted()
    {
        EC2AchievementManager.AddProgression(AchievementType.oreDepleted, EC2AchievementDatabaseType.Exploration, 1);
    }

    public void RiftEnter()
    {
        data.riftEntered++;
        EC2AchievementManager.AddProgression(AchievementType.riftEntered, EC2AchievementDatabaseType.Combat, 1);
        //switch (data.riftEntered)
        //{
        //    case 1:
        //        gm.achievements.SendAchievementToServer("combat_riftenter_1");
        //        break;
        //}
    }
    public void EnhanceItem()
    {
        /*
        data.enhanceCount++;
        switch (data.enhanceCount)
        {
            case 6:
                gm.am.SendAchievementToServer("enhance_item_plus6");
                break;
            case 9:
                gm.am.SendAchievementToServer("enhance_item_plus9");
                break;
        }*/
    }

    public int ReputationLevel
    {
        get => data.reputation.level;
        set => data.reputation.level = value;
    }

    public int ReputationEXP
    {
        get => data.reputation.currentEXP;
        set => data.reputation.currentEXP = value;
    }

    public int GetExtraMission(int level)
    {
        int extras = 0;

        if (level >= 2) extras++;
        if (level >= 4) extras++;
        if (level >= 10) extras++;

        return extras;
    }

    public static event System.Action<string, string> OnReputationLevelUp;
    public void AddEXPReputation(int exp)
    {
        if (ReputationLevel >= EC2Constant.MAX_REPUTATION_LEVEL) return;

        int toNext = ExpToLevelUpReputation(ReputationLevel);

        ReputationEXP += exp;

        if (ReputationEXP >= toNext)
        {
            ReputationLevel++;
            ReputationEXP -= toNext;
            OnReputationLevelUp?.Invoke(LocalizationManager.GetTranslation("mission/reputation_up"),
                LocalizationManager.GetTranslation("mission/reputation_bonus"));
        }

        if (ReputationLevel >= EC2Constant.MAX_REPUTATION_LEVEL)
            ReputationEXP = 0;
    }

    public string GetReputationBonus()
    {
        string bonus = LocalizationManager.GetTranslation("mission/reputation_bonus");

        if (string.IsNullOrEmpty(bonus) && ReputationLevel > 0)
            bonus = LocalizationManager.GetTranslation("mission/rep/lv" + ReputationLevel);

        return bonus;
    }

    public int ExpToLevelUpReputation(int level)
    {
        int toNext = 80;
        switch (level)
        {
            case 0:
            case 1:
                toNext = 60;
                break;

            case 2:
                toNext = 60;
                break;

            case 3:
                toNext = 80;
                break;

            case 4:
                toNext = 110;
                break;

            case 5:
                toNext = 130;
                break;

            case 6:
                toNext = 150;
                break;

            case 7:
                toNext = 180;
                break;

            case 8:
                toNext = 200;
                break;

            case 9:
                toNext = 250;
                break;

            case 10:
                toNext = 300;
                break;

            case 11:
                toNext = 350;
                break;

            case 12:
                toNext = 400;
                break;

            case 13:
                toNext = 450;
                break;
        }

        return toNext;
    }


    //Func

    public static event System.Action<int> gdtDel;
    public static event System.Action<int> rdtDel;
    public bool kGdt(ObscuredInt yes)
    {
        if (yes < 0) return EC2Constant.EC2_OBS_NO;
        if (EC2Utils.Pengurangan(gdt, yes) < 0) return EC2Constant.EC2_OBS_NO;

        gdt = EC2Utils.Pengurangan(gdt, yes);
        gdtDel?.Invoke(gdt);
        return EC2Constant.EC2_OBS_YES;
    }
    public void tGdt(ObscuredInt yes)
    {
        gdt = EC2Utils.Penjumlahan(gdt, yes);
        gdtDel?.Invoke(gdt);
    }
    public bool checkGdtEnuf(ObscuredInt yes)
    {
        if (yes < 0) return EC2Constant.EC2_OBS_NO;
        if (EC2Utils.Pengurangan(gdt, yes) < 0) return EC2Constant.EC2_OBS_NO;
        else return EC2Constant.EC2_OBS_YES;
    }

    public bool checkRdtEnuf(ObscuredInt yes)
    {
        if (yes < 0) return EC2Constant.EC2_OBS_NO;
        if (EC2Utils.Pengurangan(rdt, yes) < 0) return EC2Constant.EC2_OBS_NO;
        else return EC2Constant.EC2_OBS_YES;
    }
    public bool kRdt(ObscuredInt yes, OutcomeType type)
    {
        if (yes < 0) return EC2Constant.EC2_OBS_NO;
        if (EC2Utils.Pengurangan(rdt, yes) < 0) return EC2Constant.EC2_OBS_NO;

        rdt = EC2Utils.Pengurangan(rdt, yes);
        data.outcome.Add(new EC2Outcome() { type = type, value = yes });
        rdtDel?.Invoke(rdt);
        data.totalOutcome += yes;
        return EC2Constant.EC2_OBS_YES;
    }
    public void tRdt(ObscuredInt yes, IncomeType type)
    {
        rdt = EC2Utils.Penjumlahan(rdt, yes);
        if (type != IncomeType.Etc)
            data.income.Add(new EC2Income() { type = type, value = yes });
        data.totalIncome += yes;
        rdtDel?.Invoke(rdt);
    }

    public void SetQuestID(int questId)
    {
        data.questId = questId;
    }
    public void SetTargetScene(string scene)
    {
        PlayerPrefs.SetString("sceneToLoad", scene);
    }
    public void SetLatestScene(string scene)
    {
        data.latestScene = scene;
    }
    /*public void SetPortalIndex(int portal)
    {
        data.indexPortal = portal;
    }*/
    public void SetLatestPosition(Vector3 position, Quaternion rotation)
    {
        data.latestPosition = position;
        data.latestRotation = rotation.eulerAngles;
    }
    public void SetBackToGameplay(bool value)
    {
        data.isBackToGameplay = value;
    }
    public void SetSpawnToLatestLocation(bool value)
    {
        data.teleportBack = value;
    }

    public void SetCheckpoint(string scene)
    {
        data.lastCheckpoint = scene;
        ObscuredPrefs.SetString(EC2Constant.EC2_LATEST_CHECKPOINT, scene);
        ObscuredPrefs.Save();
    }

    public void SetLanguageID(int index)
    {
        settings.languageID = (SupportedLanguages)index;
    }

    public static void AssignDataFromCloud(string loadedData)
    {
        var data = EC2Utils.FromJson<CloudData>(loadedData);
        ObscuredPrefs.SetString(EC2Constant.EC2_PROFILE_KEY, EC2Utils.ToJson(data.userData));
        ObscuredPrefs.SetString(EC2Constant.EC2_HERO_KEY, EC2Utils.ToJson(data.heroesData));

        ObscuredPrefs.SetString(EC2Constant.EC2_INVENTORY_EQ_KEY, EC2Utils.ToJson(data.equipments));
        ObscuredPrefs.SetString(EC2Constant.EC2_INVENTORY_MATS_KEY, EC2Utils.ToJson(data.materials));
        ObscuredPrefs.SetString(EC2Constant.EC2_INVENTORY_CONSUM_KEY, EC2Utils.ToJson(data.consumables));
        ObscuredPrefs.SetString(EC2Constant.EC2_INVENTORY_RUNES_KEY, EC2Utils.ToJson(data.runeItems));
        ObscuredPrefs.SetString(EC2Constant.EC2_INVENTORY_KEYS_KEY, EC2Utils.ToJson(data.keyItems));
        ObscuredPrefs.SetString(EC2Constant.EC2_STORAGE_EQ_KEY, EC2Utils.ToJson(data.storagedEquipments));

        ObscuredPrefs.SetString(EC2Constant.EC2_ATTENDANCE_KEY, EC2Utils.ToJson(data.attendanceData));
        ObscuredPrefs.SetString(EC2Constant.EC2_MISSION_KEY, EC2Utils.ToJson(data.dailyMission));
        ObscuredPrefs.SetString(EC2Constant.EC2_DAILY_BOX_KEY, EC2Utils.ToJson(data.dailyBox));
        ObscuredPrefs.SetString(EC2Constant.EC2_ACHIEVEMENT_KEY, EC2Utils.ToJson(data.savedAchievement));
        ObscuredPrefs.SetString(EC2Constant.EC2_BOSS_RUSH_KEY, EC2Utils.ToJson(data.bossRushNew));
        ObscuredPrefs.SetString(EC2Constant.EC2_TRIAL_KEY, EC2Utils.ToJson(data.trialProgression));
        ObscuredPrefs.SetString(EC2Constant.EC2_COUPON_KEY, EC2Utils.ToJson(data.claimedCoupons));

        ObscuredPrefs.SetString(EC2Constant.EC2_GENERATED_DEALS, EC2Utils.ToJson(data.generatedDeals));
        ObscuredPrefs.SetString(EC2Constant.EC2_DEATHNOTE_ENEMY_KEY, EC2Utils.ToJson(data.enemyList));
        ObscuredPrefs.SetString(EC2Constant.EC2_DEATHNOTE_GATHER_KEY, EC2Utils.ToJson(data.gatherableList));
        ObscuredPrefs.SetString(EC2Constant.EC2_FISHING_SPOT_KEY, EC2Utils.ToJson(data.fishingSpots));

        ObscuredPrefs.SetString(EC2Constant.EC2_CLOUD_ALL_DATA_KEY, loadedData);
        ObscuredPrefs.Save();
    }
    public static void AssignEventData(string key, string loadedData)
    {
        var data = EC2Utils.FromJson<Dictionary<string, Dictionary<string, EventProgressionValue>>>(loadedData);
        ObscuredPrefs.SetString(key, EC2Utils.ToJson(data));
        ObscuredPrefs.Save();
    }

    public static void AssignSideQuestData(string key, string loadedData)
    {
        var data = EC2Utils.FromJson<Dictionary<string, SideQuestProgression>>(loadedData);
        ObscuredPrefs.SetString(key, EC2Utils.ToJson(data));
        ObscuredPrefs.Save();
    }
    public static void AssignMysticMinesData(string key, string loadedData)
    {
        var data = EC2Utils.FromJson<MysticMineWeeklyStatus>(loadedData);
        ObscuredPrefs.SetString(key, EC2Utils.ToJson(data));
        ObscuredPrefs.Save();
    }
    public static string BuildCloudData()
    {
        var cloudData = new CloudData();

        var userData = ObscuredPrefs.GetString(EC2Constant.EC2_PROFILE_KEY, "");
        if (!string.IsNullOrEmpty(userData))
            cloudData.userData = EC2Utils.FromJson<User>(userData);

        var userAdventureData = ObscuredPrefs.GetString(EC2Constant.EC2_HERO_KEY, "");
        if (!string.IsNullOrEmpty(userAdventureData))
            cloudData.heroesData = EC2Utils.FromJson<UserAdventureData>(userAdventureData);

        var eq = ObscuredPrefs.GetString(EC2Constant.EC2_INVENTORY_EQ_KEY, "");
        if (!string.IsNullOrEmpty(eq))
            cloudData.equipments = EC2Utils.FromJson<List<ItemInstance>>(eq);

        var mat = ObscuredPrefs.GetString(EC2Constant.EC2_INVENTORY_MATS_KEY, "");
        if (!string.IsNullOrEmpty(mat))
            cloudData.materials = EC2Utils.FromJson<List<ItemInstance>>(mat);

        var cons = ObscuredPrefs.GetString(EC2Constant.EC2_INVENTORY_CONSUM_KEY, "");
        if (!string.IsNullOrEmpty(cons))
            cloudData.consumables = EC2Utils.FromJson<List<ItemInstance>>(cons);

        var rune = ObscuredPrefs.GetString(EC2Constant.EC2_INVENTORY_RUNES_KEY, "");
        if (!string.IsNullOrEmpty(rune))
            cloudData.keyItems = EC2Utils.FromJson<List<ItemInstance>>(rune);

        var key = ObscuredPrefs.GetString(EC2Constant.EC2_INVENTORY_KEYS_KEY, "");
        if (!string.IsNullOrEmpty(key))
            cloudData.keyItems = EC2Utils.FromJson<List<ItemInstance>>(key);

        var storage = ObscuredPrefs.GetString(EC2Constant.EC2_STORAGE_EQ_KEY, "");
        if (!string.IsNullOrEmpty(storage))
            cloudData.storagedEquipments = EC2Utils.FromJson<List<ItemInstance>>(storage);

        var coupon = ObscuredPrefs.GetString(EC2Constant.EC2_COUPON_KEY, "");
        if (!string.IsNullOrEmpty(coupon))
            cloudData.claimedCoupons = EC2Utils.FromJson<Dictionary<string, DateTime>>(coupon);

        var attendance = ObscuredPrefs.GetString(EC2Constant.EC2_ATTENDANCE_KEY, "");
        if (!string.IsNullOrEmpty(attendance))
            cloudData.attendanceData = EC2Utils.FromJson<AttendanceStatus>(attendance);

        var achievement = ObscuredPrefs.GetString(EC2Constant.EC2_ACHIEVEMENT_KEY, "");
        if (!string.IsNullOrEmpty(achievement))
            cloudData.savedAchievement = EC2Utils.FromJson<Dictionary<string, AchievementProgression>>(achievement);

        var dailyBox = ObscuredPrefs.GetString(EC2Constant.EC2_DAILY_BOX_KEY, "");
        if (!string.IsNullOrEmpty(dailyBox))
            cloudData.dailyBox = EC2Utils.FromJson<DailyBoxStatus>(dailyBox);

        var dailyMission = ObscuredPrefs.GetString(EC2Constant.EC2_MISSION_KEY, "");
        if (!string.IsNullOrEmpty(dailyMission))
            cloudData.dailyMission = EC2Utils.FromJson<DailyMission>(dailyMission);

        var gatherableList = ObscuredPrefs.GetString(EC2Constant.EC2_DEATHNOTE_GATHER_KEY, "");
        if (!string.IsNullOrEmpty(gatherableList))
            cloudData.gatherableList = EC2Utils.FromJson<Dictionary<string, GatherableRespawnInfo>>(gatherableList);

        var fishingSpots = ObscuredPrefs.GetString(EC2Constant.EC2_FISHING_SPOT_KEY, "");
        if (!string.IsNullOrEmpty(fishingSpots))
            cloudData.fishingSpots = EC2Utils.FromJson<Dictionary<string, GatherableRespawnInfo>>(fishingSpots);

        var generatedDeals = ObscuredPrefs.GetString(EC2Constant.EC2_GENERATED_DEALS, "");
        if (!string.IsNullOrEmpty(generatedDeals))
            cloudData.generatedDeals = EC2Utils.FromJson<List<GeneratedDeal>>(generatedDeals);

        var enemyList = ObscuredPrefs.GetString(EC2Constant.EC2_DEATHNOTE_ENEMY_KEY, "");
        if (!string.IsNullOrEmpty(enemyList))
            cloudData.enemyList = EC2Utils.FromJson<Dictionary<string, DateTime>>(enemyList);

        var bossRush = ObscuredPrefs.GetString(EC2Constant.EC2_BOSS_RUSH_KEY, "");
        if (!string.IsNullOrEmpty(bossRush))
            cloudData.bossRushNew = EC2Utils.FromJson<BossRushWeeklyStatus>(bossRush);

        var trialProgression = ObscuredPrefs.GetString(EC2Constant.EC2_TRIAL_KEY, "");
        if (!string.IsNullOrEmpty(trialProgression))
            cloudData.trialProgression = EC2Utils.FromJson<TrialProgression>(trialProgression);

        ObscuredPrefs.SetString(EC2Constant.EC2_CLOUD_ALL_DATA_KEY, EC2Utils.ToJson(cloudData));

        ObscuredPrefs.Save();

        return EC2Utils.ToJson(cloudData);
    }

    public static string BuildCloudEventData()
    {
        // string temp = string.Empty;
        string eventProgression = ObscuredPrefs.GetString(EC2Constant.EC2_MY_EVENT_KEY, "");
        // if (!string.IsNullOrEmpty(eventProgression))
        //     temp = EC2Utils.ToJson(eventProgression);

        return eventProgression;
    }

    public static string BuildCloudSideQuestData()
    {
        // string temp = string.Empty;
        var sideQuestProgression = ObscuredPrefs.GetString(EC2Constant.EC2_SIDE_QUEST_KEY, "");
        // if (!string.IsNullOrEmpty(sideQuestProgression))
        //     temp = EC2Utils.ToJson(sideQuestProgression);

        return sideQuestProgression;
    }

    public static string BuildMysticMineData()
    {
        // string temp = string.Empty;
        var data = ObscuredPrefs.GetString(EC2Constant.EC2_MYSTIC_MINE_KEY, "");
        // if (!string.IsNullOrEmpty(data))
        //     temp = EC2Utils.ToJson(data);

        return data;
    }

    public static double GetUserPlayTime()
    {
        var userData = ObscuredPrefs.GetString(EC2Constant.EC2_PROFILE_KEY, "");
        if (!string.IsNullOrEmpty(userData))
        {
            var user = EC2Utils.FromJson<User>(userData);
            return user.playTime;
        }

        else return 0f;
    }

    public void Touch(PH ph)
    {
        if (!PlayfabManager.instance) return;
        if (PlayfabManager.instance.isCurrentPlayerBonked) return;
        if (PlayfabManager.instance.isCurrentPlayerSus) return;
        data.phs.Add(ph);
    }

    private List<string> itemToCheck = new List<string>()
    {
        "key_substat_selector",
        "key_mat_selector",
        "key_rune_selector",
        "key_soul_selector",
        "key_silverkey",
        "key_mysticmines",
        "key_christmaswish",
        "key_santa_select"
    };

    public void InitBeforeValue()
    {
        if (!GameManager.instance) return;

        foreach (var keyToCheck in itemToCheck)
        {
            if (GameManager.instance.inventory.allItems.ContainsKey(keyToCheck))
            {
                if (!data.valueBeforeNewestUpdate.ContainsKey(keyToCheck))
                {
                    var toCheck = GameManager.instance.GetItemTouchData(keyToCheck);
                    data.valueBeforeNewestUpdate.Add(keyToCheck, toCheck.quantity);
                }
            }
        }
    }

    public void HistoryItem_Added(string id, int quantity)
    {
        try
        {
            if (!itemToCheck.Contains(id)) return;
            if (!data.valueBeforeNewestUpdate.ContainsKey(id))
            {
                InitBeforeValue();
            }

            if (!data.historyAdded.ContainsKey(id))
            {
                data.historyAdded.Add(id, 0);
            }

            data.historyAdded[id] += quantity;
#if UNITY_EDITOR
            Debug.Log(id + " Added ->" + quantity);
#endif
        }
        catch { Debug.Log("History add failed"); }
    }

    public void HistoryItem_Removed(string id, int quantity)
    {
        try
        {
            if (!itemToCheck.Contains(id)) return;
            if (!data.valueBeforeNewestUpdate.ContainsKey(id))
            {
                InitBeforeValue();
            }

            if (!data.historyRemoved.ContainsKey(id))
            {
                data.historyRemoved.Add(id, 0);
            }

            data.historyRemoved[id] += quantity;
#if UNITY_EDITOR
            Debug.Log(id + " Removed ->" + quantity);
#endif
        }
        catch { Debug.Log("History remove failed"); }
    }
    public void AnnPre(System.Action pre, System.Action nPre)
    {
        bool x = false;
        if (data.premiumUser && data.migratedonamy)
        {
            if (!data.purchasedItems.Contains("premium_mode"))
            {
                x = true;
            }
        }

        if (x) pre?.Invoke();
        else nPre?.Invoke();
    }

    public void AnnT(System.Action t, System.Action nT)
    {
        bool x = false;
        foreach (var h in data.phs)
        {
            if (h.zblm <= h.zzdh)
            {
                x = true;
                break;
            }
        }
        if (x) t?.Invoke();
        else nT?.Invoke();
    }


    //For Character Banner
    public bool IsBannerEligible(string bannerId)
    {
        if (CurrentQuestID < 4) return false;

        data.InitBanner(bannerId);
        return data.characterBanner[bannerId].IsEligible();
    }
    public void BannerPurchased(string bannerId, int index)
    {
        data.InitBanner(bannerId);
        data.characterBanner[bannerId].Purchase(index);
    }
    public void ResetBannerPurchase()
    {
        data.characterBanner.Clear();
    }
}

[System.Serializable]
public class PH
{
    public int zblm;
    public int zzdh;
}

[System.Serializable]
public class EC2Income
{
    public IncomeType type;
    public int value;
}


[System.Serializable]
public class EC2Outcome
{
    public OutcomeType type;
    public int value;
}

[System.Serializable]
public class EC2BannerData
{
    public string bannerId;
    public List<bool> bought;

    public void Purchase(int index)
    {
        while (bought.Count < index + 1)
            bought.Add(false);

        bought[index] = true;
    }
    public bool IsPurchased(int index)
    {
        if (bought == null)
        {
            bought = new List<bool>();
            return false;
        }

        if (bought.Count < index + 1)
        {
            return false;
        }

        return bought[index];
    }
    public bool IsEligible()
    {
        //it's eligible until player purchased all offers
        if (bought == null) return true;
        if (bought.Count < 3) return true;
        foreach (bool b in bought)
            if (b == false) return true;

        return false;
    }
}

[System.Serializable]
public class User
{
    public User()
    {

    }

    public User(int gold)
    {
        this.gold = gold;
        //this.heroLevels[0] = 1;
    }

    [Header("General")]
    public string deviceID;
    //public string displayName;
    public int questId;
    public double playTime;
    public int ruby;
    public int gold;
    public bool firstTimePlay = true;
    public bool premiumUser;
    public bool boughtAltPass;
    public bool boughtStarterPack;
    public bool boughtOffer1, boughtOffer2, boughtOffer3, boughtOffer4, boughtOffer5;

    //Banner - Louisa
    //public bool louisaBannerEligible;
    public Dictionary<string, EC2BannerData> characterBanner;
    public void InitBanner(string _bannerId)
    {
        if (characterBanner == null)
            characterBanner = new Dictionary<string, EC2BannerData>();

        if (!characterBanner.ContainsKey(_bannerId))
        {
            EC2BannerData bd = new EC2BannerData() { bannerId = _bannerId };
            characterBanner.Add(_bannerId, bd);
        }
    }

    //Event Pass
    public List<string> eventpass_ads;


    //Event Items
    public Dictionary<string, int> eventExclusiveItems;
    public void AddEventItem(Item eventItem, int amt)
    {
        AddEventItem(eventItem.id, amt);
    }
    public void AddEventItem(string id, int amt)
    {
        if (eventExclusiveItems == null)
            eventExclusiveItems = new Dictionary<string, int>();

        if (eventExclusiveItems.ContainsKey(id))
            eventExclusiveItems[id] += amt;
        else eventExclusiveItems.Add(id, amt);
    }
    public int GetEventItemProgression(Item eventItem)
    {
        return GetEventItemProgression(eventItem.id);
    }
    public int GetEventItemProgression(string id)
    {
        if (eventExclusiveItems == null)
            eventExclusiveItems = new Dictionary<string, int>();

        if (eventExclusiveItems.ContainsKey(id))
            return eventExclusiveItems[id];
        else return 0;
    }


    public WelkinStatus welkinStatus = new WelkinStatus();
    public WelkinStatus welkinMaterialStatus = new WelkinStatus();
    public PetData petData = new PetData();

    [Header("Seed")]
    public Dictionary<Rarity, int> randomCraftingSeeds = new Dictionary<Rarity, int>();
    public Dictionary<Rarity, Dictionary<int, int>> randomAppraiseSeeds = new Dictionary<Rarity, Dictionary<int, int>>();

    public int randomUpgradeSeed;
    public int randomGachaSeed;
    public int randomRuneSeed;
    public int randomImprintSeed;
    public int randomPetSeed;

    //FORTUNE WHEEL
    public Dictionary<Hero, int> randomWheelSeeds = new Dictionary<Hero, int>();

    public string lastCheckpoint;
    public void CheckSeed()
    {
        //Fortune Wheel Seed
        int heroTotal = System.Enum.GetValues(typeof(Hero)).Length;

        int heroIndex = 0;
        if (randomWheelSeeds == null) randomWheelSeeds = new Dictionary<Hero, int>();

        if (randomWheelSeeds.Count < heroTotal)
        {
            heroIndex = randomWheelSeeds.Count;
            for (int i = heroIndex; i < heroTotal; i++)
                randomWheelSeeds.Add((Hero)i, UnityEngine.Random.Range(1, 1234567));
        }

        for (int i = 0; i < heroTotal; i++)
        {
            if (!randomWheelSeeds.ContainsKey((Hero)i)) continue;
            if (randomWheelSeeds[(Hero)i] > 20000000)
                randomWheelSeeds[(Hero)i] = UnityEngine.Random.Range(1, 1234567);
        }


        //Crafting
        int rarityTotal = System.Enum.GetValues(typeof(Rarity)).Length;

        int rarityIndex = 0; //common
        if (randomCraftingSeeds == null) randomCraftingSeeds = new Dictionary<Rarity, int>();

        if (randomCraftingSeeds.Count < rarityTotal)
        {
            rarityIndex = randomCraftingSeeds.Count;
            for (int i = rarityIndex; i < rarityTotal; i++)
                randomCraftingSeeds.Add((Rarity)i, UnityEngine.Random.Range(1, 1234567));
        }

        for (int i = 0; i < rarityTotal; i++)
        {
            if (!randomCraftingSeeds.ContainsKey((Rarity)i)) continue;
            if (randomCraftingSeeds[(Rarity)i] > 20000000)
                randomCraftingSeeds[(Rarity)i] = UnityEngine.Random.Range(1, 1234567);
        }

        //Appraisal
        int maxTotalLock = 4;
        int appraisalRarityIndex = 0;
        int appraisalIndex = 0;
        if (randomAppraiseSeeds == null) randomAppraiseSeeds = new Dictionary<Rarity, Dictionary<int, int>>();

        if (randomAppraiseSeeds.Count < rarityTotal)
        {
            appraisalRarityIndex = randomAppraiseSeeds.Count;
            for (int i = appraisalRarityIndex; i < rarityTotal; i++)
            {
                randomAppraiseSeeds.Add((Rarity)i, new Dictionary<int, int>());
            }
        }

        for (int i = 0; i < rarityTotal; i++)
        {
            var raritySeed = randomAppraiseSeeds[(Rarity)i];

            if (raritySeed.Count < maxTotalLock)
            {
                appraisalIndex = raritySeed.Count;
                for (int j = appraisalIndex; j < maxTotalLock; j++)
                    raritySeed.Add(j, UnityEngine.Random.Range(1, 1234567));
            }
        }

        for (int i = 0; i < rarityTotal; i++)
        {
            if (!randomAppraiseSeeds.ContainsKey((Rarity)i)) continue;

            var raritySeed = randomAppraiseSeeds[(Rarity)i];

            for (int j = 0; j < maxTotalLock; j++)
            {
                if (raritySeed[j] > 20000000)
                    raritySeed[j] = UnityEngine.Random.Range(1, 1234567);
            }
        }

        //Upgrade
        if (randomUpgradeSeed == 0)
        {
            randomUpgradeSeed = UnityEngine.Random.Range(1, 1234567);
        }

        else if (randomUpgradeSeed > 20000000)
        {
            randomUpgradeSeed = UnityEngine.Random.Range(1, 1234567);
        }

        //Gacha
        if (randomGachaSeed == 0)
        {
            randomGachaSeed = UnityEngine.Random.Range(1, 1234567);
        }

        else if (randomGachaSeed > 20000000)
        {
            randomGachaSeed = UnityEngine.Random.Range(1, 1234567);
        }

        //Rune
        if (randomRuneSeed == 0)
        {
            randomRuneSeed = UnityEngine.Random.Range(1, 1234567);
        }

        else if (randomRuneSeed > 20000000)
        {
            randomRuneSeed = UnityEngine.Random.Range(1, 1234567);
        }

        if (randomImprintSeed == 0)
        {
            randomImprintSeed = UnityEngine.Random.Range(1, 1234567);
        }

        else if (randomImprintSeed > 20000000)
        {
            randomImprintSeed = UnityEngine.Random.Range(1, 1234567);
        }

        //Pet
        if (randomPetSeed == 0)
        {
            randomPetSeed = UnityEngine.Random.Range(1, 1234567);
        }

        else if (randomPetSeed > 20000000)
        {
            randomPetSeed = UnityEngine.Random.Range(1, 1234567);
        }
    }


    //Food - Feast Buff
    public string activeFeastBuff;
    public float feastDuration;
    public DateTime feastActivationTime;
    public string ActiveFeastBuff
    {
        get => activeFeastBuff;
        set => activeFeastBuff = value;
    }
    public float FeastBuffTimeLeft
    {
        get
        {
            float timeleft = feastDuration - (float)(DateTime.Now - feastActivationTime).TotalSeconds;

            if (timeleft <= 0 || feastDuration <= 0)
            {
                activeFeastBuff = string.Empty;
                feastDuration = 0;
                timeleft = 0;
                feastActivationTime = DateTime.Now;
            }

            //return which is lower, timeleft or duration
            if (timeleft < feastDuration)
                return timeleft;
            else return feastDuration;
        }
    }
    public void ActivateFeastBuff(Recipe food, bool overrideDuration)
    {
        activeFeastBuff = food.recipeId;

        if (overrideDuration)
        {
            feastDuration = (food.effectDuration * 60) + 4;
            feastActivationTime = DateTime.Now;
        }

    }

    //Abyssal Tear
    public int acquiredAbyssalTear;
    public DateTime lastAbyssalTear;
    public List<string> abyssalClearedAreas = new List<string>();


    public bool migratedonamy;

    [Header("PD")]
    public List<PH> phs = new List<PH>();
    public List<EC2Outcome> outcome = new List<EC2Outcome>();
    public List<EC2Income> income = new List<EC2Income>();
    public Dictionary<string, int> valueBeforeNewestUpdate = new Dictionary<string, int>();
    public Dictionary<string, int> historyAdded = new Dictionary<string, int>();
    public Dictionary<string, int> historyRemoved = new Dictionary<string, int>();
    public long totalOutcome = 0;
    public long totalIncome = 0;

    //Heroes owned
    public List<Hero> heroesUnlocked = new List<Hero>();
    public List<Hero> heroesInCharge = new List<Hero>();

    public List<Hero> TrialParty = new List<Hero>();

    //public int indexPortal; //for fixed spawn after cutscene

    //Profile Thumbnail

    //===== ACHIEVEMENTS PROGRESSION =====//

    [Header("Statistics - Combat")]
    public int enemyKilled;
    public int eliteKilled, riftEntered, riftCleared;
    public int maxDmgReached;

    [Header("Statistics - Exploration")]
    public int treasurePointGet;
    public int gatherDepleted;

    [Header("Statistics - Blacksmith")]
    public int craftingCount;
    public int enhanceCount, upgradeCount, transferCount;//, socketCount, dismantleCount;

    [Header("Statistics - Character Profile")]
    public LevelEXPData reputation = new LevelEXPData();
    public int maxLevelReached;
    public int attributeAssigned;

    //========================//


    /*[Header("Save Points")]
    public string latestCheckpoint;*/

    [Header("Latest Properties Before Cutscene")]
    public string latestScene;
    public Vector3 latestPosition;
    public Vector3 latestRotation;
    public bool isBackToGameplay;
    public bool teleportBack;

    public void AddOpenedMap(string scene)
    {
        if (!openedMap.ContainsKey(scene))
            openedMap.Add(scene, 1);
    }
    public void AddOpenedTutorial(string name)
    {
        //print(name);
        if (!tutorial.ContainsKey(name))
        {
            tutorial.Add(name, 1);
        }
    }
    public void AddAcquiredItem(Item item)
    {
        //add to owned weapons
        //Debug.Log("adding " + item.ItemName());
        if (item.equipment.equipSlot == EquipSlot.MainWeapon)
        {
            if (!collectedWeapons.Contains(item.id))
            {
                //Debug.Log(item.ItemName() + " added");
                collectedWeapons.Add(item.id);
            }
        }
    }
    public bool WeaponAcquired(Item item)
    {
        if (item.equipment.equipSlot != EquipSlot.MainWeapon)
            return false;

        if (collectedWeapons.Contains(item.id))
            return true;
        else return false;
    }

    public void AddAcquiredRecipe(Recipe recipe)
    {
        if (!collectedRecipes.Contains(recipe.recipeId))
            collectedRecipes.Add(recipe.recipeId);
    }
    public bool HasRecipe(Recipe recipe)
    {
        //initialize default recipe
        if (collectedRecipes == null || collectedRecipes.Count == 0)
            collectedRecipes = new List<string>();

        if (!collectedRecipes.Contains("fish_skewer_1"))
            collectedRecipes.Add("fish_skewer_1");

        if (!collectedRecipes.Contains("nasigoreng"))
            collectedRecipes.Add("nasigoreng");

        if (collectedRecipes.Contains(recipe.recipeId))
            return true;
        else return false;
    }

    //Field Boss
    public void RecordFieldBossKillTime(string name, DateTime time)
    {
        if (fieldBossKill.ContainsKey(name)) fieldBossKill[name] = time; //Replace Death Time
        else fieldBossKill.Add(name, time);
    }
    public void RemoveFieldBossKillTime(string name)
    {
        fieldBossKill.Remove(name);
    }
    public DateTime lastTrade = new DateTime();
    public DateTime lastBoughtRunePack = new DateTime();
    public DateTime lastBoughtSoulPack = new DateTime();
    public DateTime lastBoughtEXPPack = new DateTime();
    public DateTime lastBoughtReforgePack = new DateTime();
    public DateTime lastBoughtLazuliPack = new DateTime();
    public int soulPackCounter;

    //EliteMonster
    public Dictionary<string, bool> posEliteMons = new Dictionary<string, bool>();
    public float lastSpawn;

    //BattlePortal
    public float lastSpawnPortal;

    //Gacha completed list
    public List<string> completedGachas = new List<string>();
    public List<string> purchasedItems = new List<string>();
    public Dictionary<string, Dictionary<string, int>> gachaProgression = new Dictionary<string, Dictionary<string, int>>();

    //Unlockables
    public Dictionary<string, int> costumes = new Dictionary<string, int>();
    public Dictionary<string, int> tutorial = new Dictionary<string, int>();
    public Dictionary<string, int> openedMap = new Dictionary<string, int>();
    public Dictionary<string, int> permachest = new Dictionary<string, int>();
    public List<string> oneTimeNPCs = new List<string>();
    public List<string> collectedWeapons = new List<string>();
    public List<string> collectedRecipes = new List<string>();

    //Field Boss
    public Dictionary<string, DateTime> fieldBossKill = new Dictionary<string, DateTime>();
    public Dictionary<string, TimeSpan> fieldBossBestTime_Normals = new Dictionary<string, TimeSpan>();
    public Dictionary<string, TimeSpan> fieldBossBestTime_Hards = new Dictionary<string, TimeSpan>();
    public Dictionary<string, TimeSpan> fieldBossBestTime_Extremes = new Dictionary<string, TimeSpan>();

    [Title("Monet")]
    public DateTime unlimitedPondUntil = new DateTime();
    public DateTime fishingNetUntil = new DateTime();
    public int freeBossCount = 0;
    public DateTime helperCompletedOn = new DateTime();
    public DailyPackSaveData dailyPackSaveData = new DailyPackSaveData();
    public Dictionary<string, DateTime> dateTimeKey = new Dictionary<string, DateTime>();

    public List<DeveloperBuff> developerBuffs = new List<DeveloperBuff>();
    public DeveloperBuff FindBuff(string id)
    {
        return developerBuffs.Find(x => x.buffID == id);
    }

    public float CalculateBonusEXPBuff()
    {
        float temp = 0f;
        foreach (var buff in developerBuffs)
        {
            if (buff.IsActive(PlayfabManager.instance.serverTime))
            {
                temp += buff.buffedEXP;
            }
        }

        return temp;
    }

    public float CalculateBonusDropBuff()
    {
        float temp = 0f;
        foreach (var buff in developerBuffs)
        {
            if (buff.IsActive(PlayfabManager.instance.serverTime))
            {
                temp += buff.buffedDrop;
            }
        }

        return temp;
    }
    public bool HasFishingNet(DateTime currentTimer)
    {
        return fishingNetUntil.Subtract(currentTimer).TotalSeconds > 0;
    }

    public bool HasUnlimitedPond(DateTime currentTimer)
    {
        return unlimitedPondUntil.Subtract(currentTimer).TotalSeconds > 0;
    }
}

[System.Serializable]
public class Profile
{
    public List<int> heroLevel = new List<int>();
    public string lastCheckpoint;
    public string playtime;
}

[System.Serializable]
public class UserAdventureData_OLD
{
    public List<HeroData> heroes;
    public UserInventoryData inventoryData;
}

[System.Serializable]
public class UserAdventureData
{
    public List<HeroData> heroes = new List<HeroData>();

    public void ChangeData(HeroData heroData)
    {
        var data = heroes.Find(x => x.HERO == heroData.HERO);
        if (data == null) return;

        data = heroData;
    }

    public int HeroIndex(HeroData heroData)
    {
        var data = heroes.FindIndex(x => x.HERO == heroData.HERO);
        return data;
    }
}

[System.Serializable]
public class LevelEXPData
{
    public int level;
    public int currentEXP;

    public LevelEXPData()
    {
        level = 0;
        currentEXP = 0;
    }
}

[System.Serializable]
public class CloudData
{
    public User userData = new User();
    public UserAdventureData heroesData = new UserAdventureData();
    public List<GeneratedDeal> generatedDeals = new List<GeneratedDeal>();
    public Dictionary<string, DateTime> claimedCoupons = new Dictionary<string, DateTime>();
    public Dictionary<string, AchievementProgression> savedAchievement = new Dictionary<string, AchievementProgression>();
    public AttendanceStatus attendanceData = new AttendanceStatus();
    public DailyMission dailyMission = new DailyMission();
    public DailyBoxStatus dailyBox = new DailyBoxStatus();
    public BossRushStatus bossRush = new BossRushStatus(); //old prevent bolak balik 
    public BossRushWeeklyStatus bossRushNew = new BossRushWeeklyStatus();
    public TrialProgression trialProgression = new TrialProgression();

    public List<ItemInstance> equipments = new List<ItemInstance>();
    public List<ItemInstance> materials = new List<ItemInstance>();
    public List<ItemInstance> consumables = new List<ItemInstance>();
    public List<ItemInstance> runeItems = new List<ItemInstance>();
    public List<ItemInstance> keyItems = new List<ItemInstance>();
    public List<ItemInstance> storagedEquipments = new List<ItemInstance>();

    public Dictionary<string, DateTime> enemyList = new Dictionary<string, DateTime>();
    public Dictionary<string, GatherableRespawnInfo> fishingSpots = new Dictionary<string, GatherableRespawnInfo>();
    public Dictionary<string, GatherableRespawnInfo> gatherableList = new Dictionary<string, GatherableRespawnInfo>();
}

public enum IncomeType
{
    Etc,
    Drop,
    IAP,
    Daily,
    Achievement,
    NPCPurchase,
    CoT,
    SideQuest,
    Evt,
    OX
}
public enum OutcomeType
{
    None,
    BlacksmithThings,
    Premium, //Cosu / AMY
    NPCPurchase,
    Resetter //Reset Status / Mastery / Display Name / Bosses
}

[System.Serializable]
public class WelkinStatus
{
    public DateTime lastClaim = new DateTime();
    public DateTime validUntil = new DateTime();
}
