using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HeroHUD : MonoBehaviour
{
    GameManager gm;
    public TweenAlpha widget;

    public List<HeroStatusBar> instantiatedStatusBars = new List<HeroStatusBar>();

    public HeroStatusBar MainStatusBar
    {
        get
        {
            return instantiatedStatusBars[0];
        }
    }

    //=========================================//
    //========== HERO CONSUMABLE BAR ==========//
    //=========================================//
    [Header("Consumable Slot")]
    public ConsumableQuickSlot consumableQuickSlot;
    public ConsumableQuickSlot quickSlotKeyboard;
    public ConsumableQuickSlot mobileQuickSlot;
    int kbUsedQuickSlot;
    //[HideInInspector]
    public string[] quickSlotIds;
    //[HideInInspector]
    public Item[] quickSlotObserve;
    //[HideInInspector]
    public ItemInstance[] quickSlotData;
    public Transform quickSlotHighlight; //keyboard
    bool isUsingItem;


#if UNITY_ANDROID || UNITY_IOS
    #region SUBSCRIBTIONS 
    private void OnEnable()
    {
        ConsumableUIButton.OnClickConsumable += UseQuickSlot;
    }

    private void OnDisable()
    {
        ConsumableUIButton.OnClickConsumable -= UseQuickSlot;
    }
    #endregion
#endif

    private void Start()
    {
        gm = GameManager.instance;
        InitQuickSlot();
    }

    private void Update()
    {
#if UNITY_STANDALONE
        QuickSlotInputUpdate();
#endif
    }
    void InitQuickSlot()
    {
#if UNITY_STANDALONE
        consumableQuickSlot.Init(this);
        quickSlotKeyboard.Init(this);
#endif
        mobileQuickSlot.Init(this);
        if (PlayerPrefs.HasKey(EC2Utils.GetCurrentProfilePrefs("quickSlot")))
        {
            LoadQuickSlot();
        }
        else
        {
            //create empty
            quickSlotIds = new string[4];
            quickSlotObserve = new Item[4];
            quickSlotData = new ItemInstance[4];
            SaveQuickSlot();
        }
        mobileQuickSlot.ResetQuickSlot();

#if UNITY_STANDALONE
        consumableQuickSlot.ResetQuickSlot();
        quickSlotKeyboard.ResetQuickSlot();
        quickSlotHighlight.position = quickSlotKeyboard.icons[0].transform.position;
#endif
    }
    public void AssignQuickSlot(int slot, string _item)
    {
        Inventory inventoryData = Inventory.instance;

        if (string.IsNullOrEmpty(_item))
        {
            quickSlotIds[slot] = "";
            quickSlotObserve[slot] = null;
            quickSlotData[slot] = new ItemInstance();
        }
        else
        {
            var itemData = inventoryData.allItems[_item];
            var indexItem = IsItemAssigned(itemData);
            if (indexItem != -1)
            {
                quickSlotIds[indexItem] = "";
                quickSlotObserve[indexItem] = null;
                quickSlotData[indexItem] = new ItemInstance();
            }

            quickSlotIds[slot] = _item;
            quickSlotObserve[slot] = itemData;
            quickSlotData[slot] = inventoryData.CharacterInventory().GetItemData(_item);
        }
        SaveQuickSlot();

#if UNITY_ANDROID || UNITY_IOS
        mobileQuickSlot.ResetQuickSlot();
#else
        consumableQuickSlot.ResetQuickSlot();
        quickSlotKeyboard.ResetQuickSlot();
#endif

    }

    public int IsItemAssigned(Item item)
    {
        for (int i = 0; i < quickSlotObserve.Length; i++)
        {
            var data = quickSlotObserve[i];
            if (data == item) return i;
        }

        return -1;
    }

    public void LoadQuickSlot()
    {
        string[] split = PlayerPrefs.GetString(EC2Utils.GetCurrentProfilePrefs("quickSlot")).Split('|');

        Inventory inventoryData = Inventory.instance;
        quickSlotIds = new string[split.Length];
        quickSlotObserve = new Item[split.Length];
        quickSlotData = new ItemInstance[split.Length];

        for (int i = 0; i < quickSlotIds.Length; i++)
        {
            if (!string.IsNullOrEmpty(split[i]))
            {
                quickSlotIds[i] = split[i];
                quickSlotObserve[i] = inventoryData.allItems[split[i]];
                quickSlotData[i] = inventoryData.CharacterInventory().GetItemData(split[i]);
            }
        }
    }
    void SaveQuickSlot()
    {
        string save = string.Join("|", quickSlotIds);
        PlayerPrefs.SetString(EC2Utils.GetCurrentProfilePrefs("quickSlot"), save);
    }

    void QuickSlotInputUpdate()
    {
        if (isUsingGamepad) return;
        if (gm.STATE == GameState.PAUSE) return;
        if (isUsingItem) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) UseQuickSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) UseQuickSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) UseQuickSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) UseQuickSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) UseQuickSlot(4);
        else if (Input.GetKeyDown(KeyCode.Alpha6)) UseQuickSlot(5);
    }
    public void QuickSlotNavigate(DPadDirection d)
    {
        if (!isUsingGamepad) return;
        if (gm.STATE == GameState.PAUSE) return;
        if (isUsingItem) return;

        switch (d)
        {
            case DPadDirection.up: BrowseUp(); break;
            case DPadDirection.down: BrowseDown(); break;
            case DPadDirection.right: UseQuickSlot(); break;
            default: break;
        }
    }
    void BrowseUp()
    {
        if (isUsingItem) return;
        consumableQuickSlot.Up();
    }
    void BrowseDown()
    {
        if (isUsingItem) return;
        consumableQuickSlot.Down();
    }
    void UseQuickSlot()
    {
        if (isUsingItem) return;
        if (consumableQuickSlot.CanConsume())
        {
            isUsingItem = gm.ActiveHero.control.StartUseConsumable();

            kbUsedQuickSlot = consumableQuickSlot.qsIndex;
        }
    }
    void UseQuickSlot(int index)
    {
        if (gm.IsGameOver()) return; //bug fix use item at gameover
        //if (isUsingItem) return;

#if UNITY_ANDROID || UNITY_IOS
        if (mobileQuickSlot.CanConsume(index))
        {
            isUsingItem = gm.ActiveHero.control.StartUseConsumable();
            kbUsedQuickSlot = index;
        }
        else
        {
            //No item. 
            //Show deals
        }
#else
        if (quickSlotKeyboard.CanConsume(index))
        {
            //print("Use Item");
            isUsingItem = gm.ActiveHero.control.StartUseConsumable();

            kbUsedQuickSlot = index;
            consumableQuickSlot.qsIndex = index;
        }
#endif
    }

    public bool CanUseLastItem()
    {
        return mobileQuickSlot.CanConsume(kbUsedQuickSlot);
    }

    public void SetVisible(bool visible)
    {
        if (visible)
        {
            widget.PlayForward();
        }
        else
        {
            widget.PlayReverse();
            //ClarisRageVfx(false);
        }
    }

    public void SetVisibleOnStart(bool visible)
    {
        if (visible) widget.PlayForward();
        else widget.PlayReverse();
    }

    public Item ConsumeSelectedItem()
    {
        isUsingItem = false;
        int itemIndex;

#if UNITY_ANDROID || UNITY_IOS
        itemIndex = kbUsedQuickSlot;
        mobileQuickSlot.Consume(itemIndex);
#else
        if (isUsingGamepad)
        {
            consumableQuickSlot.Consume();
            itemIndex = consumableQuickSlot.qsIndex;
        }
        else
        {
            quickSlotKeyboard.Consume(kbUsedQuickSlot);
            itemIndex = kbUsedQuickSlot;
            quickSlotHighlight.position = quickSlotKeyboard.icons[itemIndex].transform.position;
        }
#endif

        return quickSlotObserve[itemIndex];
    }
    public void ItemUseInterrupted()
    {
        isUsingItem = false;
    }

    bool isUsingGamepad;
    // public void ControllerChange(Rewired.ControllerType type)
    // {
    //     isUsingGamepad = type == Rewired.ControllerType.Joystick;

    //     consumableQuickSlot.SetVisible(isUsingGamepad, isUsingGamepad);
    //     consumableQuickSlot.ResetQuickSlot();

    //     quickSlotKeyboard.SetVisible(!isUsingGamepad, !isUsingGamepad);
    //     quickSlotKeyboard.ResetQuickSlot();
    // }

    //====================================//
    //========== HERO SKILL BAR ==========//
    //====================================//
    [Header("Skill Slot")]
    public HeroSkillButton evasion;
    public HeroSkillButton skillA, skillB, skillC, skillD;
}
