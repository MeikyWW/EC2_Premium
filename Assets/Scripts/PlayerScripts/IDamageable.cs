using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable 
{
    void TakeDamage(Transform source, DamageRequest request, out DamageResult result);
    public void TakePercentageDamage(float pct);
    void Stun(float duration);

    public bool IsDead();
}
