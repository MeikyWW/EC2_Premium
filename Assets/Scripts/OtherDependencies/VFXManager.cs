using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public enum AttackType
{
    slash, blow, none
}

public class VFXManager : MonoBehaviour
{    
    public GameObject m_parent;

    [Header("Critical Effect")]
    public GameObject critFlash;
    public GameObject critLinePrefab;
    int loopCount = 2;
    float delay = 0.06f;
    bool isFlashing;
    GameManager manager;

    void Start()
    {
        manager = GameManager.instance;
    }

    public void Crit(Vector3 pos, AttackType type)
    {
        if (type == AttackType.none)
        {
            return;
        }

        switch (type)
        {
            case AttackType.slash:
                Transform critLine = NGUITools.AddChild(m_parent, critLinePrefab).transform;
                NGUIMath.OverlayPosition(critLine.transform, pos, Camera.main, UICamera.mainCamera);
                critLine.localPosition = new Vector3(critLine.localPosition.x, critLine.localPosition.y, 0);
                critLine.transform.localRotation = Quaternion.Euler(Vector3.forward * (Random.Range(0, 360)));
                break;
            case AttackType.blow: break;
            default: break;
        }

        if (!isFlashing)
        {
            //prevents too many flashes
            Timing.RunCoroutine(Flashes());
        }
    }
    public void Flash()
    {
        Timing.RunCoroutine(Flashes());
    }
    IEnumerator<float> Flashes()
    {
        for (int i = 0; i < loopCount; i++)
        {
            critFlash.SetActive(true);
            yield return Timing.WaitForSeconds(delay);
            critFlash.SetActive(false);
            yield return Timing.WaitForSeconds(delay);
        }
        isFlashing = false;
    }
    public void FlashLong()
    {
        Timing.RunCoroutine(Flashes_Long());
    }
    IEnumerator<float> Flashes_Long()
    {
        critFlash.SetActive(true);
        yield return Timing.WaitForSeconds(0.3f);
        critFlash.SetActive(false);
        isFlashing = false;
    }

    [Header("Vignette")]
    public UITweener vignette;
    public void VignetteON(float duration)
    {
        vignette.duration = duration;
        vignette.PlayForward();
    }
    public void VignetteOFF(float duration)
    {
        vignette.duration = duration;
        vignette.PlayReverse();
    }

    [Header("[Alaster] Azure Flash")]
    public GameObject azureSlash;
    public GameObject azureCross;
    public void AzureCross(Vector3 pos)
    {
        Transform critLine = NGUITools.AddChild(m_parent, azureCross).transform;
        NGUIMath.OverlayPosition(critLine.transform, pos, Camera.main, UICamera.mainCamera);
        critLine.localPosition = new Vector3(critLine.localPosition.x, critLine.localPosition.y, 0);
    }
    public void AzureSlash(Vector3 pos)
    {
        Transform critLine = NGUITools.AddChild(m_parent, azureSlash).transform;
        NGUIMath.OverlayPosition(critLine.transform, pos, Camera.main, UICamera.mainCamera);
        critLine.localPosition = new Vector3(critLine.localPosition.x, critLine.localPosition.y, 0);
    }

    [Header("[Louisa] Assassination")]
    public GameObject assCrosshair;
    public GameObject assTargetLock;
    public Transform AssassinateCrosshair(Transform follow)
    {
        Transform crosshair = NGUITools.AddChild(m_parent, assCrosshair).transform;
        crosshair.GetComponent<GuiFollow>().SetTarget(follow, 0);

        return crosshair;
    }
    public Transform AssassinateLockon(Transform follow)
    {
        Transform crosshair = NGUITools.AddChild(m_parent, assTargetLock).transform;
        crosshair.GetComponent<GuiFollow>().SetTarget(follow, 2);

        return crosshair;
    }


    [Header("Stun Effect")]
    public GameObject stunFx;
    public GameObject Stun(Transform follow, float y_offset, float duration)
    {
        GameObject stun = Instantiate(stunFx, follow.position + Vector3.up * y_offset, Quaternion.identity);
        stun.GetComponent<ObjectFollow>().SetTarget(follow, y_offset, duration);

        return stun;
    }

    [Header("Potion Effect")]
    public GameObject potion_hp;
    public GameObject potion_mp, potion_cure;
    public void PotionHP(Transform follow, float y_offset)
    {
        GameObject fx = Instantiate(potion_hp, follow.position + Vector3.up * y_offset, potion_hp.transform.rotation);
    }
    public void PotionMP(Transform follow, float y_offset)
    {
        GameObject fx = Instantiate(potion_mp, follow.position + Vector3.up * y_offset, potion_mp.transform.rotation);
    }
    public void PotionCure(Transform follow, float y_offset)
    {
        GameObject fx = Instantiate(potion_cure, follow.position + Vector3.up * y_offset, potion_cure.transform.rotation);
    }
    public void CheckPointHeal(Transform follow, float y_offset, float duration)
    {
        GameObject fx = Instantiate(potion_mp, follow.position + Vector3.up * y_offset, potion_mp.transform.rotation);
        ObjectFollow o = fx.AddComponent<ObjectFollow>();
        o.SetTarget(follow, y_offset, duration);
    }

    [Header("Etc Effect")]
    public GameObject enemy_aggro;
    public void EnemyAggro(Transform follow, float y_offset, float duration)
    {
        GameObject stun = Instantiate(enemy_aggro, follow.position + Vector3.up * (y_offset + 2), potion_hp.transform.rotation);
        stun.GetComponent<ObjectFollow>().SetTarget(follow, y_offset + 2, 2);
    }
}
