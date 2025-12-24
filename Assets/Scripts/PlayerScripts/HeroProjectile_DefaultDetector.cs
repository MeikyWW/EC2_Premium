using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class HeroProjectile_DefaultDetector : MonoBehaviour
{
    private HeroProjectile projectile;

    public bool isEdnaProjectile;
    [ShowIf("@isEdnaProjectile")]
    private Edna_Projectile ednaProjectile;

    private void Awake()
    {
        if (isEdnaProjectile)
            ednaProjectile = transform.parent.GetComponent<Edna_Projectile>();
        else projectile = transform.parent.GetComponent<HeroProjectile>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isEdnaProjectile) ednaProjectile.ForceDestroy();
        else projectile.ForceDestroy();
    }
}
