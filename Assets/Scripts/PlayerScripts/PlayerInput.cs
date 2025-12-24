using UnityEngine;

public enum AxisType
{
    arrow, wasd
}
public enum ActionButton
{
    BasicAtk, UniqueAtk,
    Evade,
    None
}
namespace EC2
{
    public enum GameMode
    {
        Combat, Pause
    }
}

public class PlayerInput : MonoBehaviour
{
    private float h, v;
    private GameManager gm;
    // private Player _player;

    bool disableAllInput, isPs4Con;

    //Dpad hold
    bool dpadDown, rapid;
    float dpadDownTime, rapidCount;
    DPadDirection pressedDir;


#if UNITY_STANDALONE
    private void Start()
    {
        gm = GameManager.instance;
        _player = ReInput.players.GetPlayer(0);
        ChangeGameMode(GameMode.Combat);

        ReInput.controllers.AddLastActiveControllerChangedDelegate(ChangeController);
        Timing.RunCoroutine(ChangeRemappedKeys());
    }
    void ChangeController(Controller con)
    {
        if (con == null)
        {
            //show default gui as xbox gui
            ChangeToXbox();
            return;
        }

        //print(con.name);
 
        //check if ps4 controller
        isPs4Con = con.name.Contains("DualShock");
        ChangeController(con.type);
    }
    void ChangeController(ControllerType type)
    {
        switch (type)
        {
            case ControllerType.Joystick:
                if (isPs4Con) ChangeToPS4();
                else ChangeToXbox();
                break;

            case ControllerType.Keyboard: 
                ChangeToKeyboard();
                break;
        }
    }
    IEnumerator<float> ChangeRemappedKeys()
    {
        while (!gm.userData.saveLoaded)
            yield return Timing.WaitForOneFrame;

        SetRemapKeys();
    }

    private void Update()
    {
        if (disableAllInput) return;

        if (gm.IsGameOver())
        {
            if (_player.GetButtonDown("Action"))
                gm.ProceedGameOver();
            return;
        }

        //Movement Input
        h = _player.GetAxisRaw("MoveH");
        v = _player.GetAxisRaw("MoveV");

        //Main Button Press
        if (_player.GetButtonDown("Action")) gm.ActionButtonPressed(); //A
        if (_player.GetButtonDown("BasicAtk")) gm.SquareButtonPressed(); //X
        else if (_player.GetButtonDown("SpecialAtk")) gm.TriangleButtonPressed(); //Y -- Interact
        if (_player.GetButtonDown("Cancel")) gm.CancelButtonPressed(); //B -- Use Special Attack

        if (_player.GetButtonUp("BasicAtk")) gm.SquareButtonRelease(); //X release

        //Centre Button Press
        if (_player.GetButtonDown("Start")) gm.StartButtonPressed();
        if (_player.GetButtonDown("Select"))
        {
            gm.SelectButtonPressed();
            if (gm.activeAreaNode.fogOfWar) gm.activeAreaNode.fogOfWar.CompareMap();
        }

        if (_player.GetButtonDown("L3")) gm.LeftStickButtonPressed();
        if (_player.GetButtonDown("R3")) gm.RightStickButtonPressed();

        //Shoulders Button Press
        if (_player.GetButtonDown("LB")) gm.LeftShoulderPressed();
        if (_player.GetButtonDown("RB")) gm.RightShoulderPressed();
        if (_player.GetButtonDown("LT")) gm.LeftTriggerPressed();
        if (_player.GetButtonDown("RT")) gm.RightTriggerPressed();
        //Shoulders Button Release
       
        if (_player.GetButtonUp("LB")) gm.LeftShoulderReleased();
        if (_player.GetButtonUp("RB")) gm.RightShoulderReleased();
        if (_player.GetButtonUp("LT")) gm.LeftTriggerReleased();
        if (_player.GetButtonUp("RT")) gm.RightTriggerReleased();

        //D-Pad Input
        if (_player.GetButtonDown("Up"))
        {
            gm.DpadAxisPressed(DPadDirection.up);
            pressedDir = DPadDirection.up;
            dpadDown = true;
        }
        else if (_player.GetButtonDown("Down"))
        {
            gm.DpadAxisPressed(DPadDirection.down);
            pressedDir = DPadDirection.down;
            dpadDown = true;
        }
        else if (_player.GetButtonDown("Left"))
        {
            gm.DpadAxisPressed(DPadDirection.left);
            pressedDir = DPadDirection.left;
            dpadDown = true;
        }
        else if (_player.GetButtonDown("Right"))
        {
            gm.DpadAxisPressed(DPadDirection.right);
            pressedDir = DPadDirection.right;
            dpadDown = true;
        }

        //D-Pad Hold
        if (_player.GetButtonUp("Up") || _player.GetButtonUp("Down") || _player.GetButtonUp("Left") || _player.GetButtonUp("Right"))
        {
            dpadDown = rapid = false;
            dpadDownTime = 0;
        }
        if (dpadDown)
        {
            dpadDownTime += Time.unscaledDeltaTime;
            if (dpadDownTime >= 0.25f) rapid = true;

            if (rapid)
            {
                rapidCount += Time.unscaledDeltaTime;
                if (rapidCount >= 0.05f)
                {
                    gm.DpadAxisPressed(pressedDir);
                    rapidCount = 0;
                }
            }
        }

        //Scroll Input
        float scroll = _player.GetAxisRaw("Scroll");
        if (scroll != 0) gm.Scroll(-scroll);
    }

    public float HorizontalAxis()
    {
        return h;
    }
    public float VerticalAxis()
    {
        return v;
    }

    public void ForceStopRapid()
    {
        dpadDown = rapid = false;
        dpadDownTime = rapidCount = 0;
    }
    public void ChangeGameMode(GameMode mode)
    {
        _player.controllers.maps.SetAllMapsEnabled(false);

        int categoryId = mode == GameMode.Combat ? 0 : 1;
        _player.controllers.maps.SetMapsEnabled(true, categoryId);
    }


    //==== INTERFACE CHANGE ====//
    public Ec2AdaptiveButton[] allButtons;

    void ChangeToXbox()
    {
        foreach (Ec2AdaptiveButton s in allButtons)
            s.SetControl(0);

        gm.heroHud.ControllerChange(ControllerType.Joystick);
    }
    void ChangeToKeyboard()
    {
        foreach (Ec2AdaptiveButton s in allButtons)
            s.SetControl(1);

        gm.heroHud.ControllerChange(ControllerType.Keyboard);
    }
    void ChangeToPS4()
    {
        foreach (Ec2AdaptiveButton s in allButtons)
            s.SetControl(2);

        gm.heroHud.ControllerChange(ControllerType.Joystick);
    }
    public void DisableAllInput(bool state)
    {
        disableAllInput = state;
    }


    //==== KEYBOARD CUSTOM MAPPING ====//
    [Header("Explore Key Remap")]
    public UILabel[] defaultInteract;
    public UILabel[] defaultSkillA, defaultSkillB, defaultSkillC, defaultSkillD;

    [Header("GUI Key Remap")]
    public UILabel[] guiConfirm;
    public UILabel[] guiReturn, guiLB, guiRB, guiDrop, guiRemove, guiSort;
    
    public void SetRemapKeys()
    {
#if UNITY_STANDALONE
        //init remap data from settings
        Settings s = gm.userData.settings;
        OptionControl con = gm.pauseMenu.menuOpt.optionControl;
        con.ShowMappedKeys();


        //Interact Button : Default - E
        foreach (UILabel l in defaultInteract) l.text = con.GetKeyName(9, true);
        //Skill Button A : Default - A
        foreach (UILabel l in defaultSkillA) l.text = con.GetKeyName(10, true);
        //Skill Button B : Default - S
        foreach (UILabel l in defaultSkillB) l.text = con.GetKeyName(11, true);
        //Skill Button C : Default - D
        foreach (UILabel l in defaultSkillC) l.text = con.GetKeyName(12, true);
        //Skill Button D : Default - F
        foreach (UILabel l in defaultSkillD) l.text = con.GetKeyName(13, true);


        //Gui Confirm : Default - Z
        foreach (UILabel l in guiConfirm) l.text = con.GetKeyName(4, false);
        //Gui Return : Default - X
        foreach (UILabel l in guiReturn) l.text = con.GetKeyName(5, false);
        //Gui LB : Default - LB
        foreach (UILabel l in guiLB) l.text = con.GetKeyName(9, false);
        //Gui RB : Default - RB
        foreach (UILabel l in guiRB) l.text = con.GetKeyName(10, false);
        //Drop Item : Default - D
        foreach (UILabel l in guiDrop) l.text = con.GetKeyName(6, false);
        //Remove/Sell : Default - R
        foreach (UILabel l in guiRemove) l.text = con.GetKeyName(7, false);
        //Sort Bag : Default - ~
        foreach (UILabel l in guiSort) l.text = con.GetKeyName(13, false);
#endif
    }
#endif
}
