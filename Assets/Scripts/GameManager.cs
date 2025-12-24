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

    void Awake()
    {
        instance = this;
    }


    
}


