using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using DG.Tweening;

public class Claris_LeapStrike : MonoBehaviour
{
    public GameObject groundSlam;
    public LayerMask layerMask;
    public EC2Decal groundCrackDecal;

    Transform source;
    bool critical;
    DamageRequest damageRequest;
    float stunDuration;
    private void Update()
    {
        if (source)
            transform.position = source.position;
    }

    bool hit, critShown;
    public void SetDamage(Transform src, float stun, DamageRequest req)
    {
        source = src;
        damageRequest = req;
        stunDuration = stun;
    }

    public void FinisherAttack(float stunDuration, out int enemyHit)
    {
        //Spawn fx at ground below
        Vector3 spawnPos;
        //Quaternion spawnRot;
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 2, Vector3.down, out hit, 3, 1 << 0))
        {
            spawnPos = hit.point;
            //spawnRot = Quaternion.LookRotation(hit.normal);
        }
        else
        {
            spawnPos = transform.position;
            //spawnRot = groundSlam.transform.rotation;
        }
        Instantiate(groundSlam, spawnPos, groundSlam.transform.rotation);//spawnRot); 
        if (groundCrackDecal)
        {
            //decal = groundCrackDecal.CreatePrefab();
            Timing.RunCoroutine(DecalSeq());
        }
        
        Collider[] victims;
        victims = Physics.OverlapSphere(transform.position, 6f, layerMask);
        enemyHit = victims.Length;

        foreach (Collider c in victims)
        {
            if (c.CompareTag("enemy"))
            {
                c.GetComponent<EnemyHealth>().TakeDamage(source, damageRequest, out DamageResult result);
                if(!result.isMiss)
                {
                    if (critical) GameManager.instance.vfxManager.Crit(transform.position, AttackType.blow);
                    if (!result.isDead)
                    {
                        if (stunDuration > 0) c.GetComponent<EnemyAI>().Stun(stunDuration);
                    }
                }
            }
            if (c.CompareTag("Ore"))
                c.GetComponent<GatheringPoint>().Hit();
        }

        //Destroy(gameObject);
    }

    Material mat;
    GameObject decal;
    IEnumerator<float> DecalSeq()
    {
        decal = groundCrackDecal.CreatePrefab();
        mat = decal.GetComponent<Renderer>().material;

        //yield return Timing.WaitForSeconds(0.3f);
        FadeIn();
        yield return Timing.WaitForSeconds(2.0f);
        FadeOut();
    }
    public void FadeIn()
    {
        DOTween.To(FadeMaterials, 0, 3f, 0.5f);
    }
    public void FadeOut()
    {
        DOTween.To(FadeMaterials, 3f, 0, 2f).OnComplete(DestroyObj);
    }
    void FadeMaterials(float alpha)
    {
        mat.SetFloat("_Opacity", alpha);
    }
    void DestroyObj()
    {
        Destroy(decal);
        Destroy(gameObject);
    }
}
