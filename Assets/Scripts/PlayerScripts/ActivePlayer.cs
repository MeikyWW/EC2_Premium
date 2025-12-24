using UnityEngine;
using System.Collections;

public class ActivePlayer : MonoBehaviour {

    public static ActivePlayer instance;
    [HideInInspector]
    public HeroStatus status;

    public void Awake()
    {
        instance = this;
        status = GetComponent<HeroStatus>();
        DontDestroyOnLoad(gameObject);
    }
}
