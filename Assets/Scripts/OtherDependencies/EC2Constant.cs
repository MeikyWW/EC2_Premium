using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections.Generic;

public class EC2Constant
{
    #region Stats
    public const int MAX_REPORT_DAILY = 8;
    public const int LB_DIVIDER = 100000;

    //STR
    public const float STR_TO_BASICATK = 2.0f;
    public const float STR_TO_CDM = 2.5f;
    public const float STR_TO_ATK = 0.6f;
    public const float STR_TO_PHYRES = 1.5f;

    //INT
    public const float INT_TO_MPGAIN = 2.0f;
    public const float INT_TO_MPRED = 1.5f;
    public const float INT_TO_SKILLATK = 1.5f;
    public const float INT_TO_MAXMP = 1;
    public const float INT_TO_ELERES = 1.5f;
    public const float INT_TO_ATK = 0.3f;

    //DEX
    public const float DEX_TO_CRITRATE = 1.5f;
    public const float DEX_TO_SPECIALITY = 1.5f;
    public const float DEX_TO_ACCURACY = 1.0f;
    public const float DEX_TO_ATK = 0.5f;

    //AGI
    public const float AGI_TO_ASPD = 3;
    public const float AGI_TO_EVA = 1;
    public const float AGI_TO_CDR = 2f;
    public const float AGI_TO_SPREGEN = 2f;
    public const float AGI_TO_ATK = 0.4f;

    //VIT
    public const float VIT_TO_RECOVERY = 2.5f;
    public const float VIT_TO_PHYRES = 2.0f;
    public const float VIT_TO_ELERES = 2.0f;
    public const float VIT_TO_MAXHP = 8f;
    public const float VIT_TO_DEF = 0.5f;

    public const float FIRST_TIER_STAT = 20;
    public const float SECOND_TIER_STAT = 35f;
    public const float THIRD_TIER_STAT = 45;
    #endregion

    #region Values
    public const string STATE_DEPENDENT = "stateDependent";
    public const float PREMIUM_BONUS_ATTENDANCE = 0.3f;
    public const float PREMIUM_BONUS_DROP_RATE = 30f;
    public const float PREMIUM_BONUS_EXP = 0.3f;

    public const float HH_DR = 15f;
    public const float HH_E = 0.20f;
    public const float HH_G = 0.20f;

    public const bool EC2_OBS_NO = false;
    public const bool EC2_OBS_YES = true;

    public const string NPC_QUEST_MANAGER = "Knight_Hall/npc_melissa";

    public const int CHAPTER_1_END_QUEST_ID = 39;
    public const int V1_0_END_QUEST_ID = 36;
    public const int COST_CHANGE_NAME = 50;
    public const string Force_Tele_V1_1 = "Force_Tele_V1_1";

    public const int DILATION_BASIC = 12;
    public const int DILATION_PREM = 8;
    public const int MAX_BOX_ITEM = 8;
    public const int MAX_WATCH_ADS = 99;
    public const int MAX_DAILY_BOX_OPEN = 10;

    public const float MAX_PROTECTION_CD = 300f;
    public const int MAX_DAILY_MISSION = 3;

    public const int MAX_REPUTATION_LEVEL = 13;
    public const float REPUTATION_BONUS_EXP = 0.2f;
    public const float REPUTATION_BONUS_DR = 25f;

    public const string WORLDTIME_URL = "http://worldclockapi.com/api/json/est/now";
    public const string PING_URL = "https://www.google.com/";
    public const string ONLINECOUPON_URL = "https://textuploader.com/18tsd/raw";
    public const string NEWS_URL = "https://textuploader.com/18rmi/raw";

    public const string MAILREWARDS_URL = "https://textuploader.com/18tsd/raw"; //gajadi kepake

    public const string BOSS_RUSH = "boss_rush";
    public const string CHAMBER_OF_TRIAL = "CoT_3.0";
    public const string COT_SCENE = "ChamberOfTrial";

    public const float REDUCTION_ARMOR_PER_STACK = 5;
    #endregion

    #region Player Preferences Key
    public const string EC2_CUTSCENE_TRIGGER_SIDEQUEST = "On_End_Trigger_SideQuest";
    public const string EC2_FORCE_PARTY = "EC_FORCE_PARTY";
    public const string EC2_UNPROCCESSED_IAP = "EC2_UNPROCCESSED_IAP";
    public const string EC2_SELECTED_DIFFICULTY = "EC2_SELECTED_DIFFICULTY";
    public const string EC2_ACHIEVEMENT_KEY = "EC2_ACHIEVEMENT";
    public const string EC2_MY_EVENT_KEY = "EC2_EVENT_PROGRESSION_NEW_2";
    public const string EC2_MISSION_KEY = "EC2_DAILY_MISSION";
    public const string EC2_SIDE_QUEST_KEY = "EC2_SIDE_QUEST";
    public const string EC2_DAILY_BOX_KEY = "EC2_DAILY_BOX";
    public const string EC2_COUPON_KEY = "EC2_COUPON";
    public const string EC2_RANDOM_TREASURE_KEY = "EC2_TREASURE_PROGRESSION";
    public const string EC2_TRIAL_KEY = "EC2_TRIAL_V3.0";
    public const string EC2_REPORT_SUBMISSION = "EC2_REPORT_SUBMISSION";
    public const string EC2_FIRST_PURCHASES = "EC2_FIRST_PURCHASES";
    public const string EC2_PENDING_TX = "EC2_PENDING_TX";
    public const string EC2_HUNGRYNESS = "EC2_HUNGRYNESS";
    public const string EC2_LOVEMETER = "EC2_LOVEMETER";

    //Old Version
    public const string EC2_PROFILE_KEY_OLD = "EC2_PROFILE";
    public const string EC2_HERO_KEY_OLD = "EC2_HERO";

    public const string EC2_PROFILE_KEY = "EC2_PROFILE_NEW";
    public const string EC2_HERO_KEY = "EC2_HERO_NEW";

    public const string EC2_CL_OBS_K = "EC2_RUBY";

    public const string EC2_INVENTORY_EQ_KEY = "EC2_INVENTORY_EQ";
    public const string EC2_INVENTORY_MATS_KEY = "EC2_INVENTORY_MATS";
    public const string EC2_INVENTORY_CONSUM_KEY = "EC2_INVENTORY_CONSUM";
    public const string EC2_INVENTORY_KEYS_KEY = "EC2_INVENTORY_KEYS";
    public const string EC2_INVENTORY_RUNES_KEY = "EC2_INVENTORY_RUNES";
    public const string EC2_INVENTORY_PETS_KEY = "EC2_INVENTORY_PETS";

    public const string EC2_STORAGE_EQ_KEY = "EC2_STORAGE_EQ";
    public const string EC2_STORAGE_MATS_KEY = "EC2_STORAGE_MATS";
    public const string EC2_STORAGE_CONSUM_KEY = "EC2_STORAGE_CONSUM";
    public const string EC2_BREEDING_KEY = "EC2_BREED";

    public const string EC2_SETTING_KEY = "EC2_SETTING";

    public const string EC2_ATTENDANCE_KEY = "EC2_ATTENDANCE";
    public const string EC2_BOSS_RUSH_KEY = "EC2_BOSS_RUSH_NEW";
    public const string EC2_MYSTIC_MINE_KEY = "EC2_MYSTIC_MINE";
    public const string EC2_SERVER_TIME_KEY = "EC2_SERVER_TIME_2022";
    public const string EC2_BACKUP_TIME_KEY = "EC2_BACKUP_TIME";

    public const string EC2_LATEST_CHECKPOINT = "EC2_LATEST_CHECKPOINT";

    public const string EC2_DEATHNOTE_ENEMY_KEY = "EC2_DEATH_ENEMIES";
    public const string EC2_FISHING_SPOT_KEY = "EC2_FISHING_SPOT";
    public const string EC2_DEATHNOTE_GATHER_KEY = "EC2_GATHERABLE";

    public const string EC2_LEADERBOARD_KEY = "EC2_LEADERBOARD";

    public const string EC2_REMINDER_CHANGE_NAME = "EC2_REMINDER_CHANGE_NAME";

    public const string ACCOUNT_CREATED = "EC2_ACCOUNT_CREATED";
    public const string LAST_PFID = "LAST_PFID";
    //public const string SCREEN_HEIGHT = "EC2_SCREEN_HEIGHT";
    //public const string SCREEN_WIDTH = "EC2_SCREEN_WIDTH";

    public const string EC2_SHUFFLED_FANART = "EC2_SHUFFLED_FANART";
    public const string EC2_CURRENT_QUEST = "EC2_CURRENT_QUEST";

    public const string EC2_ONLINE_COUPON = "EC2_COUPONS_NEW";
    public const string EC2_TRIAL_STAGE = "EC2_TRIAL_STAGE_3.0";
    public const string EC2_ONLINE_NEWS = "newsData";
    public const string EC2_CLOUD_EVENT_KEY = "event_test_internal";
    // public const string EC2_CLOUD_EVENT_KEY = "EC2_EVENT_NEW_WITH_VC";

    public const string EC2_LINKED_GMAIL = "EC2_LINKED_EMAIL"; // FOR GOOGLE PLAY LOGIN
    public const string EC2_PLAYFAB_USERNAME = "EC2_PLAYFAB_USERNAME";
    public const string EC2_PLAYFAB_EMAIL = "EC2_PLAYFAB_EMAIL";
    public const string EC2_PLAYFAB_PASSWORD = "EC2_PLAYFAB_PASSWORD";
    public const string EC2_REFRESH_TOKEN = "EC2_REFRESH_TOKEN";
    public const string EC2_CHECK_AFTER_LINK = "EC2_IS_CLOUD_LOADED";

    public const string EC2_PURCHASE_HISTORY_KEY = "EC2_PURCHASE_HISTORY";
    public const string EC2_MAIL_REWARDS = "EC2_MAIL_REWARDS";

    public const string EC2_WIDGET_CONFIG = "EC2_WIDGET_CONFIG_NEW";

    public const string EC2_CLOUD_ALL_DATA_KEY = "EC2_CLOUD_ALL_DATA";
    public const string EC2_BONKAS = "EC2_BONK";
    public const string EC2_ONE_TIME = "EC2_ONE_TIME_LIST";

    public const string EC2_SEEN_NEWS_KEY = "EC2_SEEN_NEWS";

    public const string EC2_PLAYTIME = "PlayTime";
    public const string EC2_DEVICE_ID = "DeviceID";
    public const string EC2_FORCE_LOAD_ON_LOGIN = "Force";

    public const string EC2_SUS = "EC2_SUS";
    public const string EC2_ZABI = "ZABI";
    public const string EC2_CURRENT_DEALS = "EC2_CURRENT_DEALS_NEW"; //server
    public const string EC2_GENERATED_DEALS = "EC2_GENERATED_DEALS"; //server
    public const string EC2_BANNER_END = "EC2_BANNER_END"; //server
    public const string EC2_DEALS_REMINDER = "EC2_DEALS_REMINDER"; //local
    public const string EC2_DAILY_PACK = "EC2_DAILY_PACK"; //local
    #endregion

    #region Icon
    public const string TALK_ICON = "interact_talk";
    public const string SQ_ICON = "interact_quest";
    public const string LEAVE_ICON = "interact_leave";
    public const string OPEN_SHOP_ICON = "interact_openshop";
    public const string PICK_ICON = "interact_pick";
    public const string OPEN_CHEST_ICON = "interact_openchest";
    public const string RUNESMITH_ICON = "RuneUpgrade";
    public const string BG_WHITE = "buttonbg-white";
    public const string BG_GRAY = "buttonbg-gray";
    public const string SWITCH_ICON = "btn_change";
    #endregion

    #region Server Code 
    public const int SUCCESS_CODE = 200;
    public const int INVALID_VERSION = 300;
    public const int SUBMISSION_PAUSED = 100;
    public const int REPORT_FAILED = 404;
    public const int ANNOMALY_FOUND = 69;

    public const string ROOM_NAME = "Room_Name";
    public const string ROOM_VISIBILITY = "Room_Visibility";
    public const string ROOM_READY = "Room_Ready";
    public const string ROOM_MAP = "Room_Map";

    public const string EC2_ALLOWED_GAME_VERSION = "allowed_game_versions";
    public const string EC2_ALLOWED_GAME_BETA_VERSION = "allowed_game_beta_versions";
    #endregion

    #region Others
    public const string MALF_TEST = "Hack / Mod detected !";
    #endregion

    #region Asset Bundle / Addressable
    public const string CN_FONT_BOLD = "CN_Font_Bold";
    public const string CN_FONT_REGULAR = "CN_Font_Regular";
    #endregion

    #region Photon Event
    public const byte PHOTONEVENT_SET_STANCE = 101;
    public const byte PHOTONEVENT_CHAT = 102;
    public const byte PHOTONEVENT_CHAT_RTL = 103;
    public const byte PHOTONEVENT_CHAT_NEWPLAYER = 104;
    #endregion
}
