using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Hero Stuffs")] 
    public List<GameObject> heroPrefabs;
    public Dictionary<Hero, GameObject> heroHandler;

    void Awake()
    {
        instance = this;
    }
}


