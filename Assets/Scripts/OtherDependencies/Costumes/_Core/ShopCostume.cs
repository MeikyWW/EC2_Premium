using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class OnSaleCostume
{
    public Hero hero;
    public List<SetOfCostume> setOfCostumes;
}
public class ShopCostume : MonoBehaviour, IOpenableMenu
{
    [Header("GUI")]
    public UILabel price;
    public UIGrid costumeGrid;
    public GameObject listerPrefab;
    public GameObject buyButton;
    public Transform highlight;

    [Header("Hero Selector")]
    UISprite[] heroHeaderSprites;
    public Transform changeHeroHighlight;
    public GameObject heroHeaderPrefab;
    public UIGrid heroHeaderGrid;
    int selectedHeroIndex;
    Hero selectedHero;

    [Header("On Sale Costume")]
    public List<OnSaleCostume> onSaleCostumes;
    private int selectedIndex;

    private GameManager gm;
    private SFXManager sfx;

    bool inited;

    private void OnEnable()
    {
        ShopCostumeUIButton.OnListClick += OnClickInspect;
    }

    private void OnDisable()
    {
        ShopCostumeUIButton.OnListClick -= OnClickInspect;
    }

    private void Init()
    {
        if (inited) return;
        gm = GameManager.instance;
        sfx = gm.sfx;
        inited = true;
    }

    public void Open()
    {
        Init();

        selectedIndex = 0;
        selectedHeroIndex = 0;
        selectedHero = onSaleCostumes[selectedHeroIndex].hero;

        RefreshHeader();

        gm.pauseMenu.Activate3DViewer(selectedHero);
        ShowListing();
    }

    public void Close()
    {
        highlight.gameObject.SetActive(false);
        changeHeroHighlight.gameObject.SetActive(false);
        var headerList = heroHeaderGrid.GetChildList();
        foreach (var item in headerList)
            Destroy(item.gameObject);
    }

    public List<SetOfCostume> GetSetOfCostumeByHero()
    {
        var set = onSaleCostumes.Find(x => x.hero == selectedHero);
        return set == null ? new List<SetOfCostume>() : set.setOfCostumes;
    }

    public void ShowListing()
    {
        var listerGrid = costumeGrid.GetChildList();
        foreach (var item in listerGrid)
            item.gameObject.SetActive(false);

        for (int i = 0; i < GetSetOfCostumeByHero().Count; i++)
        {
            var setData = GetSetOfCostumeByHero()[i];
            ShopCostumeUIButton setUI;

            if (i < listerGrid.Count)
            {
                listerGrid[i].gameObject.SetActive(true);
                setUI = listerGrid[i].GetComponent<ShopCostumeUIButton>();
            }

            else
            {
                setUI = NGUITools.AddChild(costumeGrid.gameObject, listerPrefab).
                    GetComponent<ShopCostumeUIButton>();
            }

            bool isOwned = CheckOwnedStatus(setData);
            setUI.SetInfo(i, isOwned, setData.SetName());
        }
        costumeGrid.Reposition();

        InspectCostume(selectedIndex);
    }

    bool CheckOwnedStatus(SetOfCostume setOfCostume)
    {
        bool check = true;
        foreach (var cosu in setOfCostume.costumes)
        {
            if (!gm.userData.data.costumes.ContainsKey(cosu.id))
            {
                check = false;
                break;
            }
        }

        return check;
    }

    public void InspectCostume(int index)
    {
        selectedIndex = index;
        SetHighlight();
        var setData = GetSetOfCostumeByHero()[selectedIndex];
        
        var cosuViewer = gm.pauseMenu.GetHeroes3D(selectedHero);
        if (cosuViewer)
        {
            cosuViewer.DisableAll();
            cosuViewer.ClearAllSpawned();

            foreach (var item in setData.costumes)
                SetPart(cosuViewer, item.equipment.costume, 0);
        }

        price.text = setData.price.ToString();
        buyButton.SetActive(!CheckOwnedStatus(setData));
    }

    private void SetPart(CostumeChanger cosuChanger, EC2Costume item, int index)
    {
        var weaponPart = cosuChanger.ChangeCostume(item, index);
        if (weaponPart)
        {
            cosuChanger.SetSpawned(weaponPart, null);
            var followWeaponPos = weaponPart.GetComponent<FollowObjectLerp>();
            if (followWeaponPos)
            {
                weaponPart.transform.position = cosuChanger.weaponPos.position;
                followWeaponPos.SetTarget(cosuChanger.weaponPos);
            }
        }
    }
    public void OnClickInspect(int index)
    {
        if (selectedIndex == index) return;
        sfx.GUINavigate();
        InspectCostume(index);
    }

    public void Buy()
    {
        EventDelegate evt = new EventDelegate(this, "Confirm");
        gm.notification.AssignConfirmEvent(evt);
        gm.notification.OpenNotification("buy_costume");
    }

    public void RefreshHeader()
    {
        heroHeaderSprites = new UISprite[onSaleCostumes.Count];

        for (int i = 0; i < onSaleCostumes.Count; i++)
        {
            UISprite heroSelector;
            var heroRef = gm.heroDatabase.GetHeroReference(onSaleCostumes[i].hero);
            var obj = NGUITools.AddChild(heroHeaderGrid.gameObject, heroHeaderPrefab);

            var portrait = obj.transform.Find("Portrait");

            if (portrait)
            {
                heroSelector = portrait.GetComponent<UISprite>();

                heroSelector.spriteName = heroRef.miniPortrait;
                heroHeaderSprites[i] = heroSelector;
            }

            EventDelegate evt = new EventDelegate(this, "OnClickPortraitSelectorHero");
            evt.parameters[0].value = i;
            evt.parameters[1].value = heroRef.hero;
            EventDelegate.Set(obj.GetComponent<UIButton>().onClick, evt);
        }

        heroHeaderGrid.Reposition();
        changeHeroHighlight.gameObject.SetActive(true);
        changeHeroHighlight.position = heroHeaderSprites[selectedHeroIndex].transform.parent.position;
        highlight.localPosition = Vector3.zero;
    }

    void OnClickPortraitSelectorHero(int index, Hero hero)
    {
        if (selectedHeroIndex == index) return;
        gm.pauseMenu.Reset3DViewer();
        sfx.GUINavigate();
        selectedHeroIndex = index;
        this.selectedHero = hero;

        gm.pauseMenu.Activate3DViewer(selectedHero);
        changeHeroHighlight.gameObject.SetActive(true);
        changeHeroHighlight.position = heroHeaderSprites[selectedHeroIndex].transform.parent.position;
        highlight.localPosition = Vector3.zero;

        selectedIndex = 0;
        ShowListing();
    }

    int zblm;
    public void Confirm()
    {
        var setData = GetSetOfCostumeByHero()[selectedIndex];
        zblm = gm.userData.rdt;
        if (gm.userData.kRdt(setData.price, OutcomeType.Premium))
        {
            ProceedBuy();
        }
        else
        {
            SnackbarManager.instance.PopSnackbar("insufficient_ruby");
            sfx.ItemFull();
        }
    }

    public void SetHighlight()
    {
        highlight.gameObject.SetActive(true);
        highlight.transform.parent = costumeGrid.GetChild(selectedIndex);
        highlight.localPosition = Vector3.zero;
    }
    
    public void ProceedBuy()
    {
        if(gm.notification.isOpen)
           gm.notification.CloseNotification(false);

        var setData = GetSetOfCostumeByHero()[selectedIndex];
        List<Reward> rewards = new List<Reward>();

        foreach (var item in setData.costumes)
        {
            rewards.Add(new Reward()
            {
                item = item,
                rewardType = RewardType.Item,
                amount = 1,
            });
        }

        foreach (var reward in rewards)
            gm.GetReward(reward, false, IncomeType.Etc);

        GameManager.instance.multipleRewardNotif.Notify(rewards);

        gm.userData.Touch(new PH() { zblm = zblm, zzdh = gm.userData.rdt });

        ShowListing();

        gm.SaveGame();
    }
}
