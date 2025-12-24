using MEC;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyManager : MonoBehaviour
{
    private GameManager gm;

    public EC2HeroSkill switchSkill;
    public static event Action OnHeroSwitch;
    public static event Action OnPerfectEvasionSuccess;
    public const float SHARED_EXP = 0.20f;

    #region SUBSCRIBTIONS 
    private void OnEnable()
    {
        HeroHealth.OnForceSwitch += ForceSwitch;
        HeroStatus.OnShareEXP += ShareEXP;
    }

    private void OnDisable()
    {
        HeroHealth.OnForceSwitch -= ForceSwitch;
        HeroStatus.OnShareEXP -= ShareEXP;
    }
    #endregion

    private void Start()
    {
        gm = GameManager.instance;
    }

    private void Update()
    {
        if (gm.STATE == GameState.PAUSE) return;
        if (switchSkill.runningCooldown > 0)
            switchSkill.runningCooldown -= Time.deltaTime;
    }

    public void Switch()
    {
        if (gm.fishMechanic.isFishing) return;
        if (gm.ActiveHero.control.IsSiege) return;
        if (gm.ActiveHero.heroHealth.GetFrozenState()) return;
        if (gm.ActiveHero.control.IsMovementRestricted()) return;
        if (gm.ActiveHero.control.isStunned) return;
        if (gm.ActiveHero.control.isStrongPulled) return;
        if (gm.ActiveHero.control.IsUsingSkill()) return;
        if (gm.AliveHeroes().Count < 2) return;
        if (switchSkill.runningCooldown > 0) return;
        if (gm.activeAreaNode.disableSwitch) return;

        gm.sfx.SwitchCharacter();
        switchSkill.runningCooldown = switchSkill.appliedCooldown;
        SetSwitchedCharacterData(gm.ActiveHero.transform, out Vector3 oldPos, out Quaternion oldRotation, out int oldIndex);

        var newIndex = gm.SelectedHeroIndex == 1 ? 0 : 1;
        SwitchProcess(oldPos, oldRotation, oldIndex, newIndex, false);
    }

    public static void PerfectEvasionSuccess()
    {
        OnPerfectEvasionSuccess?.Invoke();
    }

    // Only for the Overlord battle 2!!
    private bool overlordBattle2 = false;
    private System.Action OnOverlordBattle2Switch;
    public void SetOverlordBattle2Switch(System.Action action)
    {
        overlordBattle2 = true;
        OnOverlordBattle2Switch = action;
    }
    public void ClearOverlordBattle2Switch()
    {
        overlordBattle2 = false;
        OnOverlordBattle2Switch = null;
    }
    // ================================

    public void ForceSwitch()
    {
        var aliveHeroes = gm.AliveHeroes();
        if (aliveHeroes.Count <= 0)
        {
            if (overlordBattle2)
            {
                OnOverlordBattle2Switch?.Invoke();
                ClearOverlordBattle2Switch();
                return;
            }
            else
            {
                gm.SetGameOver();
                return;
            }
        }

        SetSwitchedCharacterData(gm.ActiveHero.transform, out Vector3 oldPos, out Quaternion oldRotation, out int oldIndex);
        SwitchProcess(oldPos, oldRotation, oldIndex, aliveHeroes[0], false);// true);
    }

    public void SwitchProcess(Vector3 oldPos, Quaternion oldRotation, int oldIndex, int newIndex, bool isKilled)
    {
        if (!gm.activeAreaNode.nonCombatArea)
            gm.ActiveHero.control.BeforeSwitch();

        gm.ActiveHero.currentlyActive = false;
        gm.SelectedHeroIndex = newIndex;

        gm.ActiveHero.heroProps.SwitchEffect();

        gm.MoveHero(gm.ActiveHero.control, oldPos, oldRotation);

        if (!gm.activeAreaNode.nonCombatArea)
            gm.ActiveHero.control.CommenceSwitch();

        HeroStatusBar temp = gm.HeroesInCharge[oldIndex].statusBar;
        gm.HeroesInCharge[oldIndex].statusBar = gm.ActiveHero.statusBar;
        gm.ActiveHero.statusBar = temp;
        gm.ActiveHero.statusBar.ResetScale();
        gm.ActiveHero.currentlyActive = true;

        OnHeroSwitch?.Invoke();

        gm.combatCam.SetTarget(gm.ActiveHero.transform, false);

        if (!isKilled)
        {
            gm.MoveHero(gm.HeroesInCharge[oldIndex].control, new Vector3(0, 1000f, 0), gm.HeroesInCharge[oldIndex].transform.rotation);
        }

        gm.ActiveHero.costumeChanger.ResetPos();
    }

    public void SetSwitchedCharacterData(Transform activeHero, out Vector3 oldPos, out Quaternion oldRotation, out int oldIndex)
    {
        oldPos = activeHero.position;
        oldRotation = activeHero.rotation;
        oldIndex = gm.SelectedHeroIndex;
    }

    //===== EXP SHARING =====//
    public void ShareEXP(int exp)
    {
        var sharedEXP = exp / (gm.CountHeroesInCharge() - 1); //exclude the active hero
        for (int i = 0; i < gm.CountHeroesInCharge(); i++)
        {
            if (i != gm.SelectedHeroIndex)
                gm.HeroesInCharge[i].GainExp(sharedEXP);
        }
    }

    //===== HEAL & REVIVE =====//
    public void HealParty()
    {
        for (int i = 0; i < gm.CountHeroesInCharge(); i++)
        {
            if (gm.HeroesInCharge[i])
            {
                if (gm.HeroesInCharge[i].heroHealth.die) gm.HeroesInCharge[i].Revive();
                else gm.HeroesInCharge[i].HealCheckpoint();
            }
        }
    }
    public void RestoreMana()
    {
        for (int i = 0; i < gm.CountHeroesInCharge(); i++)
        {
            if (gm.HeroesInCharge[i])
            {
                gm.HeroesInCharge[i].FullManaRestore();
            }
        }
    }
}
