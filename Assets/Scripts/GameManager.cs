using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Hero Stuffs")] 
    public List<GameObject> heroPrefabs;
    public Dictionary<Hero, GameObject> heroHandler;
    public static System.Action OnPartyChanged;
    [HideInInspector] public SheetDataReferences dataSheet;
    [HideInInspector] public VFXManager vfxManager;
    public int heroMaxLevel = 50;

    void Awake()
    {
        instance = this;
    }

    public bool AllowParty
    {
        get
        {
            return HeroesInCharge.Count > 1;
        }
    }
    
    public List<HeroStatus> HeroesInCharge;

    public HeroStatus ActiveHero
    {
        get
        {
            return HeroesInCharge[SelectedHeroIndex];
        }
    }

    private int selectedHeroIndex;
    public int SelectedHeroIndex
    {
        get { return selectedHeroIndex; }
        set
        {
            selectedHeroIndex = value;
        }
    }
}


