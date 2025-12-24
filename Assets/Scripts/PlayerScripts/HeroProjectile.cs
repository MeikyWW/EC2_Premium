using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EZ_Pooling;
using MEC;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.Events;


public class HeroProjectile : MonoBehaviour
{
    public bool USE_RIGIDBODY;
    public Transform hitFx;
    public float travelSpeed;
    public float knockback;
    public bool doNotDestroyOnContact, hitFxPooling, timeBreakOnContact;
    [ShowIf("@timeBreakOnContact")]
    public float timeBreakDuration = 0.1f;
    public AttackType critType;
    public float destroyTime;
    public GameObject m_renderer;
    public int pierce;
    public ManaRegenInfo regen;

    [Title("Exploding Projectile")]
    public bool explodeOnContact;
    [ShowIf("@explodeOnContact")]
    public bool nullifyContactDamage;
    [ShowIf("@explodeOnContact")]
    public float explodeRange, explodeDelay, explodeKnockback;
    [ShowIf("@explodeOnContact")]
    public LayerMask explosionMask;
    [ShowIf("@explodeOnContact")]
    public ShakeLevel shake;

    [Title("Special Atributes")]
    public bool damageArmorPct;
    public float stunDuration;
    public bool noAudioOnHit;
    public int louisaFocusBuildup = 0;
    public bool disableDrain;

    //privates
    Transform projSource;
    Collider col;
    AudioSource _audio;
    HeroStatus hero;
    Rigidbody rb;
    
    float lifetime;
    bool traveling;
    //attributes
    int pierceCount, enemyHit;
    //shield-piercing

    bool stopTravelling;
    DamageRequest damageRequest;
    public UnityEvent onHittingEnemy;
    public UnityEvent<Vector3> onDestroy;

    #region SUBSCRIBTIONS
    public void OnEnable()
    {
        GameManager.OnGameStateChanged += TravelOverState;
    }
    public void OnDisable()
    {
        GameManager.OnGameStateChanged -= TravelOverState;
        stopTravelling = false;

    }
    private void TravelOverState(GameState state)
    {
        switch (state)
        {
            case GameState.PAUSE:
                stopTravelling = true;
                if (USE_RIGIDBODY) rb.linearVelocity = Vector3.zero;
                break;

            case GameState.PLAYING:
                stopTravelling = false;
                if (USE_RIGIDBODY) rb.linearVelocity = transform.forward * travelSpeed;
                break;
        }
    }
    #endregion

    void Awake()
    {
        col = GetComponent<Collider>();
        _audio = GetComponent<AudioSource>();
        if (USE_RIGIDBODY) rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (stopTravelling) return;
        if (!traveling) return;

        lifetime += Time.deltaTime;
        if (lifetime > destroyTime) Timing.RunCoroutine(Despawn().CancelWith(gameObject));

        if (!USE_RIGIDBODY)
            transform.Translate(new Vector3(0, 0, travelSpeed * Time.deltaTime));
    }
    public void Shoot(Transform source, DamageRequest req)
    {
        onDestroy.RemoveAllListeners();

        pierceCount = 0;
        hero = source.GetComponent<HeroStatus>();
        projSource = source;

        alrdDestroyed = false;
        col.enabled = true;
        audioPlayed = false;
        RendererVisible(true);

        totalManaRegen = 0;
        enemyHit = 0;
        damageRequest = req;
        damageRequest.pierce = pierce;

        bullseye_popped = false;

        traveling = true;
        if (USE_RIGIDBODY) rb.linearVelocity = transform.forward * travelSpeed;
    }

    bool audioPlayed, hit;

    void ApplyElement(Transform target)
    {
        //DamageRequest elementReq = new DamageRequest(damageRequest);

        switch (damageRequest.elemental)
        {
            case Elemental.Thunder:
            case Elemental.Fire:
            case Elemental.Wind:
                /*
                elementReq.statusEffect =  damageRequest.statusEffect;
                elementReq.statusEffectDuration = damageRequest.statusEffectDuration;
                elementReq.statusEffectDamage = damageRequest.statusEffectDamage;*/
                StatusEffectManager.instance.SetStatusEffect(target, damageRequest);
                break;
            case Elemental.Focus:
                target.GetComponent<EnemyAI>().LouisaFocusBuildUp(louisaFocusBuildup, damageRequest.statusEffectDuration, damageRequest.statusEffectDamage, projSource.GetComponent<Hero_Louisa>());
                break;
        }
    }


    void OnTriggerEnter(Collider c)
    {
        hit = false;

        if (!nullifyContactDamage)
        {
            if (c.CompareTag("enemy"))
            {
                hit = true;

                damageRequest.knockback = knockback;
                c.GetComponent<EnemyHealth>().TakeDamage(projSource, damageRequest, out DamageResult result);

                if (!result.isMiss)
                {
                    if (damageRequest.isCritical)
                    {
                        if (!disableDrain)
                        {
                            if (hero)
                                hero.control.ApplyDrain(damageRequest.damage);
                        }
                    }

                    if (!result.isDead)
                    {
                        if (stunDuration > 0) c.GetComponent<EnemyAI>().Stun(stunDuration);
                        //c.GetComponent<EnemyHealth>().DamageArmor(s_break, true);
                    }

                    if (damageRequest.louisa_bullseye)
                        PopLouisaBullseye(transform);

                    ManaRegen();
                    enemyHit++;

                    if (hero)
                        hero.control.UniqueAttackProcs(c.transform.position);

                    if (!explodeOnContact && damageRequest.elemental != Elemental.None)
                    {
                        var statusEffect = c.GetComponent<IStatusEffect>();
                        if (statusEffect != null) ApplyElement(c.transform);
                    }

                    if (doNotDestroyOnContact)
                    {
                        if (hitFx)
                        {
                            if (hitFxPooling) EZ_PoolManager.Spawn(hitFx, transform.position, transform.rotation);
                            else Instantiate(hitFx, transform.position, transform.rotation);
                        }
                    }
                }
            }
            if (c.CompareTag("Ore"))
            {
                hit = true;
                c.GetComponent<GatheringPoint>().Hit();

                ManaRegen();
                enemyHit++;
            }

            if (hit && !audioPlayed)
            {
                if (timeBreakOnContact)
                {
                    CombatCamera.instance.MediumShake();
                    GameManager.instance.TimeBreak(timeBreakDuration / 2);
                }

                if (!noAudioOnHit) _audio.Play();
                audioPlayed = true;
            }
        }

        if (explodeOnContact)
        {
            Timing.RunCoroutine(CommenceExplosion(), EC2Constant.STATE_DEPENDENT);
        }

        if (!doNotDestroyOnContact) //destroys on contact. check piercing here.
        {
            if (hitFx)
            {
                if (hitFxPooling) EZ_PoolManager.Spawn(hitFx, transform.position, transform.rotation);
                else Instantiate(hitFx, transform.position, transform.rotation);
            }

            pierceCount++;
            if (pierceCount > pierce)
                Timing.RunCoroutine(Despawn().CancelWith(gameObject));
        }

        if(enemyHit > 0)
            onHittingEnemy?.Invoke();
    }
    public System.Action<Vector3> onExplode;
    IEnumerator<float> CommenceExplosion()
    {
        yield return Timing.WaitForSeconds(explodeDelay);

        if (timeBreakOnContact)
        {
            CombatCamera.instance.MediumShake();
            GameManager.instance.TimeBreak(timeBreakDuration);
        }

        if (shake != ShakeLevel.none)
            CombatCamera.instance.Shake(shake);

        onExplode?.Invoke(transform.position);

        Collider[] victims;
        victims = Physics.OverlapSphere(transform.position, explodeRange, explosionMask);
        foreach (Collider c in victims)
        {
            if (c.CompareTag("enemy"))
            {
                damageRequest.knockback = explodeKnockback;
                c.GetComponent<EnemyHealth>().TakeDamage(projSource, damageRequest, out DamageResult result);
                if (!result.isMiss)
                {
                    if (damageRequest.elemental != Elemental.None)
                        ApplyElement(c.transform);


                    if (damageRequest.isCritical)
                    {
                        GameManager.instance.vfxManager.Crit(transform.position, AttackType.blow);
                        if (!disableDrain)
                        {
                            if (hero)
                                hero.control.ApplyDrain(damageRequest.damage);
                        }
                    }

                    if (hero)
                        hero.control.UniqueAttackProcs(c.transform.position);
                }

                if (damageRequest.louisa_bullseye)
                    PopLouisaBullseye(transform);
            }
            if (c.CompareTag("Ore"))
            {
                c.GetComponent<GatheringPoint>().Hit();
            }
        }

        //if (nullifyContactDamage)
        //{
        ManaRegen(victims.Length);
        //}
    }

    bool bullseye_popped;
    void PopLouisaBullseye(Transform t)
    {
        if (bullseye_popped) return;
        bullseye_popped = true;

        HudManager.instance.PopBullseye(t);
    }


    bool alrdDestroyed;

    IEnumerator<float> Despawn()
    {
        if (alrdDestroyed)
        {
            yield return Timing.WaitForOneFrame;
        }
        else
        {
            alrdDestroyed = true;

            onDestroy?.Invoke(transform.position);
            onDestroy.RemoveAllListeners();

            traveling = false;
            if (USE_RIGIDBODY) rb.linearVelocity = Vector3.zero;

            col.enabled = false;
            RendererVisible(false);

            if (visualFx)
                visualFx.Stop(true, vfxDestroy);

            yield return Timing.WaitForSeconds(destroyWait);

            /*
            while (_audio.isPlaying)
                yield return Timing.WaitForSeconds(0.1f);
            */

            lifetime = 0;
            if (hitFxPooling) EZ_PoolManager.Despawn(transform);
            else Destroy(gameObject);
        }
    }
    void RendererVisible(bool visible)
    {
        if (m_renderer == null) return;

        if (visible)
            m_renderer.SetActive(true);
        else m_renderer.SetActive(false);
    }

    float totalManaRegen;
    void ManaRegen()
    {
        if (totalManaRegen > regen.maximumManaIncrease) return;

        float result = enemyHit == 0 ? regen.manaIncreaseOnInitialHit : regen.manaIncreasePerExtraEnemy;
        totalManaRegen += result;

        if (totalManaRegen > regen.maximumManaIncrease)
            result = regen.maximumManaIncrease - totalManaRegen;


        if (hero)
        {
            //if(hero.socketEffect.manaDrain.count > 0)
            //    result += result * hero.socketEffect.manaDrain.values[0];

            hero.AddManaValue(result);
        }
    }
    void ManaRegen(int enemyHit)
    {
        if (enemyHit <= 0) return;

        float result = regen.manaIncreaseOnInitialHit;

        if (enemyHit > 1)
            result += regen.manaIncreasePerExtraEnemy * (enemyHit - 1);

        if (result > regen.maximumManaIncrease)
            result = regen.maximumManaIncrease;

        if (hero)
            hero.AddManaValue(result);
    }


    [Title("Force Destroy")]
    public Transform destroyFx;
    public bool destroyFxPooling;
    public ParticleSystem visualFx;
    [ShowIf("@visualFx != null")]
    public ParticleSystemStopBehavior vfxDestroy = ParticleSystemStopBehavior.StopEmitting;
    public float destroyWait;
    public void ForceDestroy()
    {
        if (destroyFx)
        {
            if (destroyFxPooling) EZ_PoolManager.Spawn(destroyFx, transform.position, transform.rotation);
            else Instantiate(destroyFx, transform.position, transform.rotation);
        }

        Timing.RunCoroutine(Despawn().CancelWith(gameObject));
    }
}
