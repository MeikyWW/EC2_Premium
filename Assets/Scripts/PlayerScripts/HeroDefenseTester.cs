using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[System.Serializable]
public class DefenseTesterData
{
    public int level;
    public float damage;
    public float DR;
}


public class HeroDefenseTester : MonoBehaviour
{
    public float extraMaxHp = 1000000;
    public float extraDefense;

    HeroStatus status;
    [Button]
    public void Set()
    {
        status = GetComponent<HeroStatus>();

        status.tester_extraHp = extraMaxHp;
        status.heroHealth.RestoreHpPercent(100, false);

        status.tester_extraDefense = extraDefense;


    }

    public List<DefenseTesterData> damageInfo;
    public void RegisterDamage(int srcLevel, float finalDmg, float reduction)
    {
        bool dataInserted = false;
        foreach (var item in damageInfo)
        {
            if (item.level == srcLevel)
                dataInserted = true;
        }

        if (!dataInserted)
        {
            damageInfo.Add(new DefenseTesterData()
            {
                level = srcLevel,
                damage = finalDmg,
                DR = reduction
            });
        }
    }

    [Button]
    void Clear()
    {
        damageInfo.Clear();
    }

}
