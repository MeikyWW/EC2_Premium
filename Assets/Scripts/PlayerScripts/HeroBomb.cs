using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using MEC;

public class HeroBomb : MonoBehaviour
{
    public float explosionRadius;
    public LayerMask explosionMask;
    public ShakeLevel camShake;
    public StatusEffects effects;
    [ShowIf("@effects != StatusEffects.none")]
    public float effectDuration;
    public bool waitForVfx;

    Transform source;
    DamageRequest req;

    public float damageDelay = 0f;

    public CustomDamagePopUp customDmgHud;

    public void Explode(Transform src, DamageRequest damageRequest)
    {
        source = src;
        req = damageRequest;

        if (damageDelay > 0)
            Timing.RunCoroutine(DamageDelay().CancelWith(gameObject));
        else Boom();
    }
    IEnumerator<float> DamageDelay()
    {
        yield return Timing.WaitForSeconds(damageDelay);

        Boom();
    }

    void Boom()
    {
        CombatCamera.instance.Shake(camShake);

        Collider[] victims;
        victims = Physics.OverlapSphere(transform.position, explosionRadius, explosionMask);
        foreach (Collider c in victims)
        {
            if (c.CompareTag("enemy"))
            {
                req.statusEffectDamage = req.damage;

                req.statusEffect = effects;
                req.statusEffectDuration = effectDuration;

                req.useCustomDmgPopUp = true;
                req.customDmgPopUp = customDmgHud;

                c.GetComponent<EnemyHealth>().TakeDamage(transform, req, out DamageResult result);
                if (req.isCritical) GameManager.instance.vfxManager.Crit(transform.position, AttackType.blow);
                if (effects != StatusEffects.none) StatusEffectManager.instance.SetStatusEffect(c.transform, req);
            }
            // if (c.CompareTag("Ore"))
            //     c.GetComponent<GatheringPoint>().Hit();
        }
        if (!waitForVfx) Destroy(gameObject);
    }
}
