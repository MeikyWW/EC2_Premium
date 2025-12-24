using EZ_Pooling;
using MEC;
using System.Collections.Generic;
using UnityEngine;

public class Claris_CrossSlash : MonoBehaviour
{
    public Transform hitFx;
    public float travelSpeed;
    public float knockback;
    public float hitDelay = 0.05f;
    public float endTime;
    public bool dummy;
    public ManaRegenInfo regen;
    public AttackType atkType = AttackType.slash;

    public bool timebreak;

    Transform source, blackhole;
    AudioSource m_audio;
    GameManager manager;
    HeroStatus hero;
    float timer;
    bool activ;
    Collider col;
    float divide = 1;
    int totalEnemyHit;
    DamageRequest damageRequest;

    private void Start()
    {
        manager = GameManager.instance;
        m_audio = GetComponent<AudioSource>();
        timer = endTime;
    }
    private void Update()
    {
        if (!activ) return;
        if (manager.STATE == GameState.PAUSE) return;

        transform.Translate(new Vector3(0, 0, travelSpeed * Time.deltaTime));

        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= endTime - 0.8f) divide = 2;
            if (timer <= 0) DamageEnd();
        }
    }

    public void SetDamage(Transform src, DamageRequest req)
    {
        SetDamage(src, req, false, 0);
    }
    public void SetDamage(Transform src, DamageRequest req, bool isExSaber, float bleedChance)
    {
        activ = true;
        if (dummy) return;

        hero = src.GetComponent<HeroStatus>();
        col = GetComponent<Collider>();
        source = src;
        damageRequest = req;

        if (!isExSaber)
        {
            blackhole = transform.Find("blackhole");
            Timing.RunCoroutine(CommenceAttack().CancelWith(gameObject), Segment.Update);
        }
        else
        {
            if (Random.Range(0, 100) > bleedChance)
            {
                req.statusEffect = StatusEffects.none;
            }
        }
    }
    void DamageEnd()
    {
        if (!dummy) col.enabled = false;
        activ = false;

        Destroy(gameObject, 1);
    }

    public float pull_distance = 1;
    public float pull_maxDistance = 6;
    public float pull_speed = 10;

    bool hit, critShown;
    void OnTriggerEnter(Collider other)
    {
        hit = other.CompareTag("enemy") || other.CompareTag("Chest") || other.CompareTag("Ore");

        if (hit)
        {
            try
            {
                Transform target = other.transform;
                if (target.CompareTag("enemy"))
                {
                    target.GetComponent<EnemyAI>().PullEffect(blackhole, pull_distance, pull_maxDistance, pull_speed);
                    damageRequest.knockback = knockback;
                    target.GetComponent<EnemyHealth>().TakeDamage(source, damageRequest, out DamageResult result);
                    if (!result.isMiss)
                    {
                        totalEnemyHit++;

                        //damageRequest.damageSourceKey = "other";

                        StatusEffectManager.instance.SetStatusEffect(target, damageRequest);
                        StatusEffectManager.instance.SetStatusEffect(target, damageRequest);

                        if (timebreak) GameManager.instance.TimeBreak(0.04f);
                    }
                }
                if (target.CompareTag("Ore"))
                {
                    target.GetComponent<GatheringPoint>().Hit();
                    totalEnemyHit++;
                }

                m_audio.Play();

                if (hitFx)
                    EZ_PoolManager.Spawn(hitFx, target.position + Vector3.up * 3, hitFx.transform.rotation);
            }
            catch { }

            //showing critflash once
            if(totalEnemyHit > 0)
            {
                if (damageRequest.isCritical)
                {
                    if (!critShown)
                    {
                        critShown = true;
                        GameManager.instance.vfxManager.Crit(transform.position, atkType);
                    }
                }
            }
        }
    }
    IEnumerator<float> CommenceAttack()
    {
        while (activ)
        {
            col.enabled = true;
            critShown = false;
            yield return Timing.WaitForSeconds(hitDelay / divide);

            col.enabled = false;
            ManaRegen();
            totalEnemyHit = 0;
            yield return Timing.WaitForSeconds(hitDelay / divide);
        }
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

    public ParticleSystem vfx;
    public void ForceEnd()
    {
        vfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(blackhole.gameObject);
        DamageEnd();
    }
}
