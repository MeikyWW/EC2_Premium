using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using MyBox;

public class FishingTools : MonoBehaviour
{
    public GameObject fishingRod, Weapon, fishingBobber;
    private GameObject bobber;
    public GameObject premiumFishingRod;


    public void ToggleFishingRod(bool value)
    {
        if (Weapon) Weapon.SetActive(!value);

        bool premiumRod = GetComponent<HeroStatus>().IsUsingPremiumRod();
        if (premiumRod)
        {
            if (premiumFishingRod) premiumFishingRod.SetActive(value);
        }
        else
        {
            if (fishingRod) fishingRod.SetActive(value);
        }

        if (value)
        {
            GetComponent<HeroControl>().HideWeapons();
        }
        else
        {
            GetComponent<HeroControl>().ShowWeapons();
        }
    }

    public void SetFishingBobberNewPos(Transform parent)
    {
        if (bobber) Destroy(bobber);

        bobber = Instantiate(fishingBobber, parent.position, fishingBobber.transform.rotation, parent);
        bobber.transform.localPosition = Vector3.zero;
    }

    DG.Tweening.Core.TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions> tweener;
    public void FishingBobberFallMovement()
    {
        tweener = bobber.transform.DOLocalMoveY(-0.75f, 0.5f).SetEase(Ease.InOutBounce);
    }

    public void ResetFishingBobber()
    {
        if(tweener != null)
        {
            if (tweener.IsPlaying())
                tweener.onComplete = () => Destroy(bobber);
        }

        else Destroy(bobber);
    }

}
