using MEC;
using System.Collections.Generic;
using UnityEngine;

public class Claris_ViolentAssault : MonoBehaviour
{
    public float knockback;
    public float hitDelay = 0.05f;
    public int totalHits;
    public ManaRegenInfo regen;

    AudioSource m_audio;
    Transform source;
    bool critical, audioPlayed;
    Collider col;
    HeroStatus hero;
    int totalEnemyHit;
    DamageRequest damageRequest;

    public void SetDamage(Transform src, DamageRequest req)
    {
        m_audio = GetComponent<AudioSource>();
        col = GetComponent<Collider>();
        hero = src.GetComponent<HeroStatus>();
        damageRequest = req;
        source = src;

        Timing.RunCoroutine(CommenceAttack().CancelWith(gameObject), Segment.Update);
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
                if (!result.isMiss) totalEnemyHit++;
            }
            else if (target.CompareTag("Ore"))
            {
                target.GetComponent<GatheringPoint>().Hit();
                totalEnemyHit++;
            }

        }
        catch { }

        if (totalEnemyHit > 0 && !audioPlayed)
        {
            audioPlayed = true;
            m_audio.Play();
            if (critical) GameManager.instance.vfxManager.Crit(transform.position, AttackType.slash);
            GameManager.instance.TimeBreak(0.05f);
        }

    }
    IEnumerator<float> CommenceAttack()
    {
        for (int i = 0; i < totalHits; i++)
        {
            col.enabled = true;
            yield return Timing.WaitForSeconds(hitDelay);

            col.enabled = false;
            ManaRegen();
            totalEnemyHit = 0;
            audioPlayed = false;
            yield return Timing.WaitForSeconds(hitDelay);
        }
        DamageEnd();
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
