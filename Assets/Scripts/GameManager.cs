using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get, private set};

    [Header("Hero Stuffs")] 
    public List<GameObject> heroPrefabs;
    public Dictionary<Hero, GameObject> heroHandler;

    
}


