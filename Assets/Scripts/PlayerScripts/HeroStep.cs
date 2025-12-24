using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public enum GroundType
{
    normal, grass, water, snow, sand
}
public enum FootStep
{
    left, right
}

public class HeroStep : MonoBehaviour
{
    public Transform stepCaster;
    public AudioClip[] groundStep, waterStep, grassStep, snowStep, sandStep;

    [Header("Water")]
    public ParticleSystem waterSplash;
    public GameObject waterRipple;
    ObjectFollow rippleObj;
    River river;
    float rippleHeight;

    [Header("Sand")]
    public ParticleSystem sandSplash;

    [Header("Snow")]
    public ParticleSystem snowSplash;

    [Header("Decal Step")]
    public bool useDecalStep;
    [ShowIf("@useDecalStep")]
    public GameObject decalStep_left, decalStep_right;
    [ShowIf("@useDecalStep")]
    public Transform leftFoot, rightFoot;
    FootStep footStep;

    GroundType type;
    AudioSource _audio;

    private void Start()
    {
        if (stepCaster == null) stepCaster = transform;
        _audio = stepCaster.GetComponent<AudioSource>();
        //SubsEvent();
    }

    public void Step(int id) //0:left foot | 1:right foot
    {
        footStep = (FootStep)id;

        RaycastHit hit;
        if (Physics.Raycast(stepCaster.position, Vector3.down, out hit, 2, 1 << 0))
        {
            if (hit.transform.CompareTag("water"))
            {
                type = GroundType.water;

                //set ripple height
                if (river == null)
                {
                    river = hit.transform.GetComponent<River>();
                    if (river)
                    {
                        rippleHeight = river.height.position.y;

                        GameObject g = Instantiate(waterRipple, new Vector3(transform.position.x, rippleHeight, transform.position.z), Quaternion.identity);
                        rippleObj = g.GetComponent<ObjectFollow>();
                        rippleObj.SetTargetWithLockedY(transform, rippleHeight, 999);
                    }
                }
            }
            else if (hit.transform.CompareTag("grass"))
            {
                type = GroundType.grass;
            }
            else if (hit.transform.CompareTag("snow"))
            {
                type = GroundType.snow;
                SpawnDecalStep();
            }
            else if (hit.transform.CompareTag("sand"))
            {
                type = GroundType.sand;
                SpawnDecalStep();
            }
            else
            {
                type = GroundType.normal;

                //return water state
                river = null;
                if (rippleObj)
                {
                    rippleObj.GetComponent<ParticleSystem>().Stop();
                    rippleObj.DelayedDestroy(1.2f);
                }
            }

            PlayStepSound();
        }
        else
        {

        }
    }

    void PlayStepSound()
    {
        AudioClip select = null;
        switch (type)
        {
            case GroundType.normal:
                if (groundStep.Length > 0) select = groundStep[Random.Range(0, groundStep.Length)];
                break;

            case GroundType.grass:
                if (grassStep.Length > 0) select = grassStep[Random.Range(0, grassStep.Length)];
                break;

            case GroundType.water:
                if (waterStep.Length > 0) select = waterStep[Random.Range(0, waterStep.Length)];
                if (waterSplash) waterSplash.Play();
                break;

            case GroundType.sand:
                if (sandStep.Length > 0) select = sandStep[Random.Range(0, sandStep.Length)];
                if (sandSplash) sandSplash.Play();
                break;

            case GroundType.snow:
                if (snowStep.Length > 0) select = snowStep[Random.Range(0, snowStep.Length)];
                if (snowSplash) snowSplash.Play();
                break;
        }
        if (select) _audio.PlayOneShot(select);
    }

    void SpawnDecalStep()
    {
        if (!useDecalStep) return;
        //if (!decalEnabled) return;

        var pos = footStep == FootStep.left ? leftFoot.position : rightFoot.position;
        var step = footStep == FootStep.left ? decalStep_left : decalStep_right;

        Instantiate(step, pos, transform.rotation);
    }

    /*
    //Only enable Decal Step when Environment Detail (Settings) is ON
    bool decalEnabled;
    #region Subs
    private void SubsEvent()
    {
        MenuOptions.OnChangeFoliageSetting += SetDecalStep;
        GlobalUserData.OnFoliageSettingLoaded += SetDecalStep;
    }

    private void OnDestroy()
    {
        MenuOptions.OnChangeFoliageSetting -= SetDecalStep;
        GlobalUserData.OnFoliageSettingLoaded -= SetDecalStep;
    }
    #endregion

    public void SetDecalStep(bool enableDecalStep)
    {
        decalEnabled = enableDecalStep;
    }*/
}
