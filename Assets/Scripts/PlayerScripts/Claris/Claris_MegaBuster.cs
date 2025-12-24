using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using DG.Tweening;

public class Claris_MegaBuster : MonoBehaviour
{
    public float knockback;
    public float hitDelay = 0.05f;
    public int maxHit = 3;
    public EC2Decal groundCrackDecal;
    public ManaRegenInfo regen;

    Transform source;
    float timer, stun;
    bool enableStun;
    Collider col;
    GameObject decal;
    HeroStatus hero;
    int totalEnemyHit;
    DamageRequest damageRequest;

    public void SetDamage(Transform src, float stunDur, DamageRequest req)
    {
        col = GetComponent<Collider>();
        hero = src.GetComponent<HeroStatus>();
        source = src; stun = stunDur;
        damageRequest = req;

        Timing.RunCoroutine(CommenceAttack().CancelWith(gameObject), Segment.Update);
        if (groundCrackDecal)
        {
            //decal = groundCrackDecal.CreatePrefab();
            Timing.RunCoroutine(DecalSeq());
        }
    }
    void DamageEnd()
    {
        Destroy(gameObject, 1);
    }

    void OnTriggerEnter(Collider other)
    {
        try
        {
            Transform target = other.transform;
            if (target.CompareTag("enemy"))
            {
                damageRequest.knockback = knockback;
                target.GetComponent<EnemyHealth>().TakeDamage(source, damageRequest, out DamageResult result);

                if(!result.isMiss)
                {
                    totalEnemyHit++;
                    if (enableStun) if (!result.isDead) target.GetComponent<EnemyAI>().Stun(stun);
                }
            }
            if (target.CompareTag("Ore"))
            {
                target.GetComponent<GatheringPoint>().Hit();
                totalEnemyHit++;
            }

        }
        catch { }

        //shpwing critflash once
        if(totalEnemyHit > 0)
        {
            if (damageRequest.isCritical)
                GameManager.instance.vfxManager.Crit(transform.position, AttackType.blow);
        }
    }
    IEnumerator<float> CommenceAttack()
    {
        for (int i = 0; i < maxHit; i++)
        {
            if (i == 2) enableStun = true;
            col.enabled = true;
            yield return Timing.WaitForSeconds(hitDelay);

            col.enabled = false;
            ManaRegen();
            totalEnemyHit = 0;
            yield return Timing.WaitForSeconds(hitDelay);
        }
    }

    Material mat;
    IEnumerator<float> DecalSeq()
    {
        decal = groundCrackDecal.CreatePrefab();
        mat = decal.GetComponent<Renderer>().material;
        //Debug.Log(mat);
        yield return Timing.WaitForSeconds(0.3f);
        FadeIn();
        yield return Timing.WaitForSeconds(1.5f);
        FadeOut();
    }
    public void FadeIn()
    {
        //Func : FadeMaterials(float alpha) , StartAlpha, EndAlpha, Duration
        DOTween.To(FadeMaterials, 0, 1, 0.5f);
    }
    public void FadeOut()
    {
        DOTween.To(FadeMaterials, 1, 0, 1f).OnComplete(DestroyObj);
    }
    void FadeMaterials(float alpha)
    {
        mat.SetFloat("_Opacity", alpha);
    }
    void DestroyObj()
    {
        Destroy(decal);
    }
    void ManaRegen()
    {
        if (totalEnemyHit == 0) return;

        float result = regen.manaIncreaseOnInitialHit +
            regen.manaIncreasePerExtraEnemy * (totalEnemyHit - 1);
        if (result > regen.maximumManaIncrease)
            result = regen.maximumManaIncrease;

        hero.AddManaValue(result);
        totalEnemyHit = 0;
    }
}
