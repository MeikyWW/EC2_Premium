using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuCostume : MonoBehaviour
{
    public UILabel heroName;
    public UILabel transmogLabel;
    public GameObject transmogButton;

    [Header("Default Viewer")]
    public UIWidget costumeViewer;
    public Transform[] costumeSlots;
    public Transform[] costumeHides;
    UIButton[] costumeHideButtons;
    UISprite[] costumeHideSprites;
    UIButton[] costumeTextureNext;
    bool[] isCostumeHid;

    public Transform highlight;
    UIButton[] costumeButtons;

    [Header("Selector")]
    public UIWidget costumeSelector;
    public Transform selectedEquipSlot;
    public Transform[] selectionList;
    public Transform selectorHighlight;
    public UIScrollBar scrollBar;

    [Header("Selector Mobile Support")]
    public UIGrid selectorGrid;
    public GameObject selectorItemPrefab;

    [Header("Inspector")]
    public UIWidget statusDetail;
    public ItemInspector costumeInspector;
    CharacterStatusViewer statusViewer;

    [Header("Options")]
    public GameObject optBrowse;
    public GameObject optChange;

    UILabel[] slotLabels;
    UISprite selectedIcon;
    UILabel selectedLabel;

    GameManager gm;
    SFXManager sfx;
    Inventory inventoryData;
    HeroSaveData heroSaveData;

    ItemInstance selectedCostume;

    bool isInited, browse, eqSelection;
    int browseIndex, selectionIndex, selectorSlotCount;

    #region SUBSCRIBTIONS 
    private void OnEnable()
    {
        CostumeUIButton.OnSelectCostume += OnClickCostumeSelector;
    }

    private void OnDisable()
    {
        CostumeUIButton.OnSelectCostume -= OnClickCostumeSelector;
    }
    #endregion
    
    void Init()
    {
        if (isInited) return;

        gm = GameManager.instance;
        sfx = gm.sfx;
        inventoryData = Inventory.instance;
        statusViewer = statusDetail.GetComponent<CharacterStatusViewer>();

        //init default viewer
        slotLabels = new UILabel[costumeSlots.Length];
        costumeButtons = new UIButton[costumeSlots.Length];

        for (int i = 0; i < costumeSlots.Length; i++)
        {
            costumeButtons[i] = costumeSlots[i].GetComponent<UIButton>();
            EventDelegate evt = new EventDelegate(this, "OnClickCostumeViewer");
            evt.parameters[0].value = i;
            EventDelegate.Set(costumeButtons[i].onClick, evt);
            slotLabels[i] = costumeSlots[i].Find("Label").GetComponent<UILabel>();
        }

        //init hider
        costumeHideButtons = new UIButton[costumeHides.Length];
        costumeHideSprites = new UISprite[costumeHides.Length];
        isCostumeHid = new bool[costumeHides.Length];

        for (int i = 0; i < costumeHides.Length; i++)
        {
            costumeHideButtons[i] = costumeHides[i].GetComponent<UIButton>();
            EventDelegate evt2 = new EventDelegate(this, nameof(ToggleHideCostume));
            evt2.parameters[0].value = i;
            EventDelegate.Set(costumeHideButtons[i].onClick, evt2);

            costumeHideSprites[i] = costumeHides[i].GetComponent<UISprite>();
            isCostumeHid[i] = false;
        }

        costumeTextureNext = new UIButton[costumeSlots.Length];
        for (int i = 0; i < costumeSlots.Length; i++)
        {
            costumeTextureNext[i] = costumeSlots[i].Find("ToggleTexture").GetComponent<UIButton>();
            EventDelegate evt3 = new EventDelegate(this, nameof(GetNextTexture));
            evt3.parameters[0].value = i;
            EventDelegate.Set(costumeTextureNext[i].onClick, evt3);
        }
        //init selector 
        selectedIcon = selectedEquipSlot.Find("Icon").GetComponent<UISprite>();
        selectedLabel = selectedEquipSlot.Find("Label").GetComponent<UILabel>();
        isInited = true;
    }

    public void ShowCostumes()
    {
        Init();
        SetTransmogButtonText();
        browse = false; eqSelection = false;
        highlight.gameObject.SetActive(false);
        selectorHighlight.gameObject.SetActive(false);
        costumeViewer.alpha = 1;
        costumeSelector.alpha = 0;
        statusDetail.alpha = 1;

        HideInspector();

        var stat = gm.pauseMenu.SelectedHero;
        heroName.text = stat.heroReference.HeroName();
        heroSaveData = gm.pauseMenu.SelectedHero.GetComponent<HeroSaveData>();

        //Set Costume based on Save Data
        if(isTransmogging)
        {
            for (int i = 0; i < costumeSlots.Length; i++)
            {
                ItemInstance eq = heroSaveData.data.cosuTransmog[i].itemData;
                if (i < costumeHides.Length)
                {
                    costumeHides[i].gameObject.SetActive(false);
                }

                if (!string.IsNullOrEmpty(eq.id))
                {
                    Item item = inventoryData.allItems[eq.id];
                    slotLabels[i].color = Color.white;
                    slotLabels[i].text = item.ItemName();

                    if (eq.enhancementLevel > 0) slotLabels[i].text += " +" + eq.enhancementLevel;

                    /*
                    if (i < costumeTextureNext.Length)
                    {
                        ColorChangeButtonShow(item, i);
                    }*/
                }
                else
                {
                    slotLabels[i].color = Color.gray;
                    slotLabels[i].text = "---";
                }
            }
        }

        else
        {
            for (int i = 0; i < costumeSlots.Length; i++)
            {
                CostumeInstance eq = heroSaveData.data.costumes[i];

                if (!string.IsNullOrEmpty(eq.itemData.id))
                {
                    Item item = inventoryData.allItems[eq.itemData.id];
                    slotLabels[i].color = Color.white;
                    slotLabels[i].text = item.ItemName();

                    if (eq.itemData.enhancementLevel > 0) slotLabels[i].text += " +" + eq.itemData.enhancementLevel;
                    if (i < costumeHides.Length)
                    {
                        costumeHides[i].gameObject.SetActive(true);
                        SetHideStatus(i, eq.isHid);
                    }

                    if(i < costumeTextureNext.Length)
                    {
                        ColorChangeButtonShow(item, i);
                    }
                }
                else
                {
                    slotLabels[i].color = Color.gray;
                    slotLabels[i].text = "---";
                    if (i < costumeHides.Length)
                    {
                        SetHideStatus(i, false);
                        costumeHides[i].gameObject.SetActive(false);
                    }

                    if (i < costumeTextureNext.Length)
                    {
                        costumeTextureNext[i].gameObject.SetActive(false);
                        eq.textureIndex = 0;
                    }
                }
            }
        }
        
        gm.pauseMenu.Set3DViewerCostume(gm.pauseMenu.SelectedHero);
        statusViewer.Refresh();
    }
    void ColorChangeButtonShow(Item item, int index)
    {
        int checksum = 0;

        if (isTransmogging)
            checksum++;

        if (gm.HasAltCostumePass())
            checksum++;

        if (item.equipment.costume.HasOtherMaterials)
            checksum++;

        costumeTextureNext[index].gameObject.SetActive(checksum == 3);
    }

    public void CloseCostume()
    {
        Init();
        SetTransmogButtonText();
        browse = false; eqSelection = false;
        highlight.gameObject.SetActive(false);
        selectorHighlight.gameObject.SetActive(false);
        costumeViewer.alpha = 1;
        costumeSelector.alpha = 0;
        //statusDetail.alpha = 1;

        HideInspector();
        SetBottomOptions();
    }

    public void SetHideStatus(int index, bool isHid)
    {
        isCostumeHid[index] = isHid;
        if (!isHid)
        {
            costumeHideSprites[index].spriteName = "costume_show";
        }

        else
        {
            costumeHideSprites[index].spriteName = "costume_hide";
        }
    }

    public void ToggleHideCostume(int index)
    {
        sfx.GUINavigate();
        SetHideStatus(index, !isCostumeHid[index]);

        heroSaveData.data.costumes[index].isHid = isCostumeHid[index];
        gm.pauseMenu.SelectedHero.heroData.SetCostumeData();
        gm.pauseMenu.Set3DViewerCostume(gm.pauseMenu.SelectedHero);

        /*
        if (!gm.HasAltCostumePass())
        {

        }
        else
        {
            sfx.GUINavigate();
            SetHideStatus(index, !isCostumeHid[index]);

            heroSaveData.data.costumes[index].isHid = isCostumeHid[index];
            gm.pauseMenu.SelectedHero.heroData.SetCostumeData();
            gm.pauseMenu.Set3DViewerCostume(gm.pauseMenu.SelectedHero);

            //gm.pauseMenu.SelectedHero.heroData.ApplyAuraWeapon();
        }*/
    }

    public void GetNextTexture(int index)
    {
        sfx.GUINavigate();
        var eq = isTransmogging ? heroSaveData.data.cosuTransmog[index] : heroSaveData.data.costumes[index];
        if (string.IsNullOrEmpty(eq.itemData.id)) return;
        if (!inventoryData.allItems.ContainsKey(eq.itemData.id)) return;

        Item item = inventoryData.allItems[eq.itemData.id];
        heroSaveData.data.costumes[index].textureIndex = item.equipment.costume.NextTexture(heroSaveData.data.costumes[index].textureIndex);
        Debug.Log(heroSaveData.data.costumes[index].textureIndex);
        gm.pauseMenu.SelectedHero.heroData.SetCostumeData();
        gm.pauseMenu.Set3DViewerCostume(gm.pauseMenu.SelectedHero);
        //gm.pauseMenu.SelectedHero.heroData.ApplyAuraWeapon();
    }

    public void HideInspector()
    {
        costumeInspector.Close();
    }

    public void ShowInspector()
    {
        if (isTransmogging) return;
        costumeInspector.gameObject.SetActive(true);
    }

    void BrowseCostumeViewer()
    {
        int index = browseIndex;
        highlight.gameObject.SetActive(true);
        highlight.transform.parent = costumeSlots[index].transform;
        highlight.transform.localPosition = Vector3.zero;


        ItemInstance itemData = isTransmogging ? heroSaveData.data.cosuTransmog[index].itemData : heroSaveData.data.costumes[index].itemData;
        if (string.IsNullOrEmpty(itemData.id))
        {
            HideInspector();
            statusDetail.alpha = 1;
        }
        else
        {
            Item item = inventoryData.allItems[itemData.id];
            statusDetail.alpha = 0;
            ShowInspector();
            costumeInspector.DisplayCostume(item, false);
        }
    }

    void SetBottomOptions()
    {
        transmogButton.SetActive(!eqSelection);
        optBrowse.SetActive(browse && !eqSelection);
        optChange.SetActive(eqSelection);
    }

    List<ItemInstance> costumeList;
    void OpenSelector()
    {
        //select item
        selectedCostume = isTransmogging ? heroSaveData.data.cosuTransmog[browseIndex].itemData : heroSaveData.data.costumes[browseIndex].itemData;
        costumeList = new List<ItemInstance>();

        //get specific item list from inventory
        CostumeSlot selectedSlot = EC2Utils.GetCostumeSlot(browseIndex);
        var costumes = new List<Item>();

        if(isTransmogging)
        {
            costumes.AddRange(inventoryData.ListCostumesDefault(selectedSlot, gm.pauseMenu.SelectedHero.heroReference.hero));
            costumes.AddRange(inventoryData.ListUnlockedWeapons(selectedSlot, gm.pauseMenu.SelectedHero.heroReference.hero));
        }

        costumes.AddRange(inventoryData.ListCostumesByType(selectedSlot, gm.pauseMenu.SelectedHero.heroReference.hero));

        foreach (var item in costumes)
        {
            ItemInstance instancedCostume = new ItemInstance()
            {
                id = item.id
            };

            costumeList.Add(instancedCostume);
        }

        if(string.IsNullOrEmpty(selectedCostume.id) && costumeList.Count == 0)
        {
            sfx.ItemFull();
            SnackbarManager.instance.PopSnackbar("costume_locked");

            return;
        }

        eqSelection = true; selectionIndex = 0;
        selectorHighlight.gameObject.SetActive(false);

        costumeSelector.alpha = 1;
        costumeViewer.alpha = 0;
        statusDetail.alpha = 0;
        sfx.GUINavigate();

        selectedIcon.spriteName = EC2Utils.SelectedCostumeIcon(browseIndex);

        if (string.IsNullOrEmpty(selectedCostume.id))
        {
            selectedLabel.color = Color.gray;
            selectedLabel.text = "---";
        }
        else
        {
            Item item = inventoryData.allItems[selectedCostume.id];
            selectedLabel.color = EC2Utils.GetRarityColor(selectedCostume.currentRarity);
            selectedLabel.text = item.ItemName();
        }

        ShowSelectorListing();
        SetBottomOptions();
    }
    void ShowSelectorListing()
    {
        var selectorSlots = selectorGrid.GetChildList();
        for (int selectorIndex = 0; selectorIndex < selectorSlots.Count; selectorIndex++)
        {
            if (selectorIndex == 0) continue;

            selectorSlots[selectorIndex].gameObject.SetActive(false);
        }

        for (int i = 0; i <= costumeList.Count; i++)
        {
            if (i == 0) continue;
            var data = costumeList[i - 1];
            CostumeUIButton selectorItemInfo;

            if (i < selectorSlots.Count)
            {
                selectorSlots[i].gameObject.SetActive(true);
                selectorItemInfo = selectorSlots[i].GetComponent<CostumeUIButton>();
            }

            else
            {
                selectorItemInfo = NGUITools.AddChild(selectorGrid.gameObject, selectorItemPrefab)
                    .GetComponent<CostumeUIButton>();
            }

            if (!string.IsNullOrEmpty(data.id))
            {
                Item item = inventoryData.allItems[data.id];
                selectorItemInfo.SetInfo(item, i, gm.userData.data.costumes.ContainsKey(item.id) 
                    || item.equipment.costume.costumeType == CostumeSet.Default);
            }
        }
        selectorGrid.Reposition();
        InspectSelector();
    }

    public static event System.Action<EC2Costume> OnChangeCostume;
    void InspectSelector()
    {
        int index = selectionIndex;

        selectorHighlight.gameObject.SetActive(true);
        selectorHighlight.transform.parent = selectorGrid.GetChild(index);
        selectorHighlight.transform.localPosition = Vector3.zero;

        ItemInstance itemData;

        if (index == 0)
        {
            gm.pauseMenu.Set3DViewerCostume(gm.pauseMenu.SelectedHero);

            if (!isTransmogging)
            {
                if (!string.IsNullOrEmpty(heroSaveData.data.costumes[browseIndex].itemData.id))
                {
                    itemData = heroSaveData.data.costumes[browseIndex].itemData;

                    Item item = inventoryData.allItems[itemData.id];

                    ShowInspector();
                    costumeInspector.DisplayCostume(item, false);
                }
                else
                {
                    HideInspector();
                }
            }
        }
        else
        {
            itemData = costumeList[index - 1];

            Item item = inventoryData.allItems[itemData.id];
            gm.pauseMenu.Set3DViewerCostume(gm.pauseMenu.SelectedHero);
            gm.pauseMenu.SetDefaultCostume(gm.pauseMenu.SelectedHero.heroReference.hero, browseIndex);

            var oldId = isTransmogging ? 
                heroSaveData.data.cosuTransmog[browseIndex].itemData.id : heroSaveData.data.costumes[browseIndex].itemData.id;
            if (!string.IsNullOrEmpty(oldId))
            {
                Item oldAcc = inventoryData.allItems[oldId];
                gm.pauseMenu.heroes3D[gm.pauseMenu.SelectedHero.heroReference.hero].DisableAccessory(oldAcc.equipment.costume);
            }
            else
            {
                oldId = isTransmogging ?
                heroSaveData.data.costumes[browseIndex].itemData.id : heroSaveData.data.cosuTransmog[browseIndex].itemData.id;

                if (!string.IsNullOrEmpty(oldId))
                {
                    Item oldAcc = inventoryData.allItems[oldId];
                    gm.pauseMenu.heroes3D[gm.pauseMenu.SelectedHero.heroReference.hero].DisableAccessory(oldAcc.equipment.costume);
                }
            }

            SetPart(gm.pauseMenu.heroes3D[gm.pauseMenu.SelectedHero.heroReference.hero], item.equipment.costume, 
                heroSaveData.data.costumes[browseIndex].textureIndex);

            if (!isTransmogging)
            {
                ShowInspector();
                costumeInspector.DisplayCostume(item, true);
            }
        }
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

    void EquipSelectedItem()
    {
        int slot = browseIndex;
        int index = selectionIndex;
        ItemInstance itemData;

        if (index == 0)
        {
            itemData = isTransmogging ? heroSaveData.data.cosuTransmog[slot].itemData : heroSaveData.data.costumes[slot].itemData;
            if (string.IsNullOrEmpty(itemData.id))
            {
                sfx.ItemFull();
                SnackbarManager.instance.PopSnackbar("unequip_fail");
            }

            else
            {
                sfx.Unequip();

                if (isTransmogging)
                {
                    heroSaveData.data.cosuTransmog[slot] = new CostumeInstance();
                }

                else
                {
                    heroSaveData.data.costumes[slot] = new CostumeInstance()
                    {
                        itemData = new ItemInstance(),
                        isHid = false
                    };
                    gm.pauseMenu.SelectedHero.CalculateHeroAttributes();
                }
                gm.pauseMenu.SelectedHero.heroData.SetCostumeData();

                ShowCostumes();
                EnableBrowse();
            }
        }
        else
        {
            itemData = costumeList[index - 1];

            if(gm.inventory.allItems.ContainsKey(itemData.id))
            {
                if (!gm.userData.data.costumes.ContainsKey(itemData.id) 
                    && gm.inventory.allItems[itemData.id].equipment.costume.costumeType != CostumeSet.Default)
                {
                    sfx.ItemFull();
                    SnackbarManager.instance.PopSnackbar("costume_locked");
                }

                else
                {
                    var dupli = isTransmogging ?
                        heroSaveData.data.cosuTransmog.Find(x => x.itemData.id == itemData.id) != null 
                        :
                        heroSaveData.data.costumes.Find(x => x.itemData.id == itemData.id) != null;
                    if (dupli)
                    {
                        sfx.ItemFull();
                        SnackbarManager.instance.PopSnackbar("costume_duplicate");
                    }

                    else
                    {
                        if (isTransmogging)
                        {
                            heroSaveData.data.cosuTransmog[slot].itemData = itemData;
                        }

                        else
                        {
                            heroSaveData.data.costumes[slot] = new CostumeInstance()
                            {
                                itemData = itemData,
                                isHid = false
                            };
                        }

                        sfx.Equip();
                        gm.pauseMenu.SelectedHero.heroData.SetCostumeData();
                        gm.pauseMenu.SelectedHero.CalculateHeroAttributes();
                        ShowCostumes();
                        EnableBrowse();
                    }
                    
                }
            }
           
        }

    }
    public void CloseSelector()
    {
        selectorHighlight.gameObject.SetActive(false);
        eqSelection = false;
        costumeSelector.alpha = 0;
        costumeViewer.alpha = 1;
        sfx.GuiClose();
        SetBottomOptions();
        ShowCostumes();
    }
    void EnableBrowse()
    {
        browse = true; browseIndex = 0;
        SetBottomOptions();
    }
    void DisableBrowse()
    {
        browse = false;
        SetBottomOptions();
    }
    public bool Button_B()
    {
        if (eqSelection)
        {
            CloseSelector();
            return false;
        }

        if (browse)
        {
            DisableBrowse();
            return true;
        }

        return true;
    }

    public void OnClickCostumeViewer(int index)
    {
        browse = true;
        SetBottomOptions();
        sfx.GUINavigate();
        browseIndex = index;
        BrowseCostumeViewer();
    }
    public void OnClickCostumeSelector(int index)
    {
        sfx.GUINavigate();
        selectionIndex = index;
        InspectSelector();
    }

    public void OnClickChangeCostume()
    {
        if (browse) OpenSelector();
    }

    [HideInInspector] public bool isTransmogging;
    public void OnToggleTransmog()
    {
        isTransmogging = !isTransmogging;
        SetTransmogButtonText();
        sfx.GUINavigate();
        ShowCostumes();
    }

    private void SetTransmogButtonText()
    {
        if (isTransmogging)
            transmogLabel.text = "Transmog";
        else
            transmogLabel.text = "Costumes";
    }

    public void OnClickEquip()
    {
        if (eqSelection)
        {
            EquipSelectedItem();
        }
    }
}
public enum CostumeSlot
{
    Weapon,
    Hair,
    Suit,
    Accessory
}