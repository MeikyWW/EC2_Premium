using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EZ_Pooling;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using MEC;

public enum DamageModifierType
{
    BasicAttack,
    SkillAttack
}

public class HeroWeaponHitbox : MonoBehaviour
{
    [Title("Effects")]
    public GameObject hitVfx;
    [LabelText("slash-bleeding")]
    public GameObject hitVfx2;
    public float vfxOffsetY;

    [Title("Attribute")]
    public AttackType attackType;
    public DamageModifierType damageType;
    public bool timeStopOnHit;
    public float timeStopDuration = 0.1f;
    public bool shakeOnHit;
    public ShakeLevel shakeLevel;
    public float attackKnockback = 1;
    public float stunDuration = 0;
    public float addedStunDuration = 0f;

    [Title("Status Attack")]
    public StatusEffects statusAttack;
    public float statusPotency;
    public float statusDuration;
    public float applyChance = 100f;

    [Title("Custom Damage Popup")]
    public bool useCustomHud;
    public CustomDamagePopUp customDmg;

    private AudioSource _audio;
    private HeroControl hero;
    private bool timeStopOnce, shakeOnce;

    [Title("Special")]
    public LayerMask layerMask = 1 << 10 | 1 << 13;
    public bool callBeforeDamage;
    public bool onlyFireOnce;
    [ShowIf("@this.onlyFireOnce")]
    public float delayDisable = 0.5f;
    [ShowIf("@this.onlyFireOnce")]
    public UnityEvent OnEnemyHitFireOnce;
    public UnityEvent OnEnemyHit;
    private bool fired;
    private BoxCollider boxCollider;
    private SphereCollider sphereCollider;

    [LabelText("Hero")]
    public Hero heroName;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        sphereCollider = GetComponent<SphereCollider>();
    }

    private void OnEnable()
    {
        fired = false;
    }
    private void Start()
    {
        _audio = GetComponent<AudioSource>();
        hero = transform.root.GetComponent<HeroControl>();
    }
    public void ResetTimestop()
    {
        timeStopOnce = false;
        shakeOnce = false;
    }

    DamageRequest statusDmgReq;
    public void SetStatusAttack(DamageRequest dmgReq, float _chance)
    {
        statusDmgReq = dmgReq;

        statusAttack = dmgReq.statusEffect;
        statusDuration = dmgReq.statusEffectDuration;
        statusPotency = dmgReq.statusEffectDamage;
        applyChance = _chance;

        statusDmgReq.hero = heroName;
    }
    public void SetCustomHUD(CustomDamagePopUp customHUD)
    {
        if (customHUD == null)
        {
            useCustomHud = false;
        }
        else
        {
            useCustomHud = true;
            customDmg = customHUD;
        }
    }


    public void SetAddedStunDuration(float _duration)
    {
        addedStunDuration = _duration;
    }
    public void ResetStunDuration()
    {
        addedStunDuration = 0;
    }

    bool ApplyRandomize(float applyChance) => applyChance >= Random.Range(0f, 100f);
    private void OnTriggerEnter(Collider other)
    {
        CommenceAttack(other);
    }

    public void AttackWithCollider(out HitboxResult result)
    {
        result = new HitboxResult() { victims = new List<Collider>() };
        Collider[] victims = new Collider[0];
        try
        {
            if (boxCollider)
            {
                Vector3 center = boxCollider.transform.TransformPoint(boxCollider.center);
                Vector3 halfExtent = Vector3.Scale(boxCollider.size, boxCollider.transform.lossyScale) * 0.5f;
                victims = Physics.OverlapBox(center, halfExtent, transform.rotation, layerMask);

                //victims = Physics.OverlapBox(boxCollider.bounds.center, boxCollider.size, transform.rotation, layerMask);
            }
            else if (sphereCollider)
            {
                victims = Physics.OverlapSphere(transform.position, sphereCollider.radius, layerMask);
            }
            result.isHit = victims.Length > 0;
            result.totalEnemyHit = victims.Length;

            foreach (Collider c in victims)
            {
                result.victims.Add(c);
                CommenceAttack(c);
            }
        }
        catch
        {
        }
    }
    public Collider[] GetVictimsInCollider()
    {
        Collider[] victims = new Collider[0];
        try
        {
            if (boxCollider)
            {
                Vector3 center = boxCollider.transform.TransformPoint(boxCollider.center);
                Vector3 halfExtent = Vector3.Scale(boxCollider.size, boxCollider.transform.lossyScale) * 0.5f;
                victims = Physics.OverlapBox(center, halfExtent, transform.rotation, layerMask);
            }
            else if (sphereCollider)
            {
                victims = Physics.OverlapSphere(transform.position, sphereCollider.radius, layerMask);
            }
        }
        catch
        {
        }
        return victims;
    }
    public void CommenceAttack(Collider other)
    {
        if (other.CompareTag("enemy"))
        {
            if (callBeforeDamage)
            {
                OnEnemyHit?.Invoke();

                if (onlyFireOnce)
                {
                    if (!fired)
                    {
                        Timing.RunCoroutine(DisableFireOnce(delayDisable).CancelWith(gameObject));
                        OnEnemyHitFireOnce?.Invoke();
                    }
                }
            }

            //Debug.Log(transform.name + " damagin enemy");

            DamageRequest dmgReq = new DamageRequest()
            {
                attackType = attackType,
                knockback = attackKnockback,
                stunDuration = stunDuration + addedStunDuration,
                damageModifierType = damageType,

                useCustomDmgPopUp = useCustomHud,
                customDmgPopUp = customDmg,
                hero = heroName
            };

            var miss = hero.DamageEnemy(other.GetComponent<EnemyHealth>(), dmgReq);

            if (!miss)
            {
                _audio.Play();
                SpawnVFX(other.transform);
                hero.AddEnemyHit();

                if (ApplyRandomize(applyChance))
                {
                    if (statusDuration > 0)
                        StatusEffectManager.instance.SetStatusEffect(other.transform, statusDmgReq);
                }

                if (!callBeforeDamage)
                {
                    OnEnemyHit?.Invoke();

                    if (onlyFireOnce)
                    {
                        if (!fired)
                        {
                            Timing.RunCoroutine(DisableFireOnce(delayDisable).CancelWith(gameObject));
                            OnEnemyHitFireOnce?.Invoke();
                        }
                    }
                }

                if (timeStopOnHit && !timeStopOnce)
                {
                    timeStopOnce = true;
                    if (timeStopDuration > 0)
                        GameManager.instance.TimeBreak(timeStopDuration);
                }

                if (shakeOnHit && !shakeOnce)
                {
                    shakeOnce = true;
                    CombatCamera.instance.Shake(shakeLevel);
                }
            }
        }
        else if (other.CompareTag("Ore"))
        {
            _audio.Play();
            other.GetComponent<GatheringPoint>().Hit();
            SpawnVFX(other.transform);
            hero.AddEnemyHit();
        }
    }

    void SpawnVFX(Transform hitPos)
    {
        Vector3 pos = hitPos.position;
        pos.y = transform.position.y + vfxOffsetY;

        if (hero.affectBleeding)
        {
            if (hitVfx2) Instantiate(hitVfx2, pos, Quaternion.identity);
        }
        else
        {
            if (hitVfx) Instantiate(hitVfx, pos, Quaternion.identity);
        }
    }

    IEnumerator<float> DisableFireOnce(float delay)
    {
        fired = true;
        yield return Timing.WaitForSeconds(delay);
        fired = false;
    }

    public void PlayHitAudio()
    {
        _audio.Play();
    }

    public void DisableCollider()
    {
        GetComponent<Collider>().enabled = false;
    }
}

[System.Serializable]
public class HitboxResult
{
    public bool isHit;
    public int totalEnemyHit;
    public List<Collider> victims;
}

