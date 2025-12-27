using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameState STATE = GameState.PLAYING;
    private float baseTimeScale;
    public float BaseTimeScale
    {
        get => baseTimeScale;
    }

    [Header("Hero Stuffs")] 
    public List<GameObject> heroPrefabs;
    public Dictionary<Hero, GameObject> heroHandler;
    public static System.Action OnPartyChanged;
    [HideInInspector] public SheetDataReferences dataSheet;
    [HideInInspector] public VFXManager vfxManager;
    public GlobalUserData userData;
    public static System.Action<GameState> OnGameStateChanged;

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
    public bool AllowMasterySkill
    {
        get => userData.CurrentQuestID >= 24;
    }

    public bool IsEngagedInCombat()
    {
        bool result = false;

        //ada musuh di sekitar
        // if (detectedEnemies > 0)
        //     result = true;

        // //ada musuh yg narget player
        // if (aggroedEnemies > 0)
        //     result = true;

        // //masih menunggu status engage hilang
        // if (disengaging)
        //     result = true;

        return result;
    }

#region Gameover

    [HideInInspector] public bool disableGameOverScreen;
    bool gameOver, gameOverInputEnabled;
    public bool IsGameOver()
    {
        return gameOver;
    }
    public static event System.Action OnGameOver;
    public async void SetGameOver()
    {
        if (gameOver) return;

        gameOver = true;

        if (disableGameOverScreen)
        {
            disableGameOverScreen = false;
            return;
        }

        //gameOverText = gameOverUI.transform.Find("Label").GetComponent<UILabel>();
        //CameraPlay.CurrentCamera = UICamera.mainCamera;

        OnGameOver?.Invoke();
        //RecordTimer.instance.GameOver();
        gameOverInputEnabled = false;
        //if (fog) fog.SaveTextureData();

        //Timing.RunCoroutine(EnableGameOverInput());
        /*
        if (usingFusion)
        { 
            await Task.Delay(1000);
            if(_currentRunner)
            {
                LeaveSession(() =>
                {
                });
            }
        }*/
    }
#endregion

}


