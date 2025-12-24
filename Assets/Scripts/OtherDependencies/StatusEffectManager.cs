using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

public enum StatusEffects
{
    bleed,
    blast,
    atkUp,
    critUp,
    durUp,
    spdUp,
    recUp,
    poison,
    freeze,
    burn,
    paralyze,
    silence,
    none,
    armorDown,
    eleResUp,
    phyResUp,
    exhaust,
    damageDown,
    skillReset,
    ensnare,
    cursed,
    blind,
    tremor,
    slow,
    critResDown,
    mysticFlame,
    snowSlow,
    fireShield,
    focused,
    chill,
    frostbiteFever,
    tropicalSquash,
    zeravCurse,
    windVeil
}

public class StatusEffectManager : MonoBehaviour
{
    public static StatusEffectManager instance;
    public List<DebuffSpecialPosition> debuffPositions;
    public List<StatusEffects> buffList;
    private void Awake()
    {
        instance = this;

        buffList = new List<StatusEffects>()
        {
            StatusEffects.atkUp,
            StatusEffects.phyResUp,
            StatusEffects.eleResUp,
            StatusEffects.critUp,
            StatusEffects.durUp,
            StatusEffects.spdUp,
            StatusEffects.recUp,
            StatusEffects.fireShield,
            StatusEffects.tropicalSquash,
        };

    }

    public bool IsBuff(StatusEffects effect)
    {
        return buffList.Contains(effect);
    }


    [Title("Status Effect Prefabs (Positive)")]
    public GameObject buff_atkUp;
    public GameObject buff_critUp, buff_durUp, buff_spdUp, buff_recUp;
    public GameObject buff_eleResUp;
    public GameObject buff_phyResUp;
    public GameObject buff_fireShield;
    public GameObject buff_tropicalSquash;
    public GameObject buff_abyssal_windveil;

    [Title("Status Effect Prefabs (Negative)")]
    public GameObject debuff_bleeding;
    public GameObject debuff_blast, debuff_para, debuff_poison;
    public GameObject debuff_freeze;
    public GameObject debuff_burn;
    public GameObject debuff_stacked_burn;
    public GameObject debuff_armor;
    public GameObject debuff_damage;
    public GameObject debuff_exhaust;
    public GameObject debuff_ensnare;
    public GameObject debuff_silence;
    public GameObject debuff_cursed;
    public GameObject debuff_blind;
    public GameObject debuff_tremor;
    public GameObject debuff_slow;
    public GameObject debuff_mysticFlame;
    public GameObject debuff_snowSlow;
    public GameObject debuff_focused;
    public GameObject debuff_chill;
    public GameObject debuff_frostbiteFever;
    public GameObject debuff_zeravcurse;

    [Title("Special Status - Freeze")]
    public Material debuff_freeze_mat;
    public List<string> EXCLUDE = new List<string>(new string[] { "Shadow" });
    public void Freeze(Transform victim)
    {
        GameObject t = victim.gameObject;
        List<Renderer> allRenderers = new List<Renderer>();

        MeshRenderer[] props = t.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < props.Length; i++)
        {
            if (ContainsExcludedTransform(props[i].transform.name)) { }
            else allRenderers.Add(props[i]);
        }

        SkinnedMeshRenderer[] bodypart = t.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < bodypart.Length; i++)
        {
            if (ContainsExcludedTransform(bodypart[i].transform.name)) { }
            else allRenderers.Add(bodypart[i]);
        }

        foreach (Renderer r in allRenderers)
        {
            List<Material> mats = r.materials.ToList();
            mats.Add(debuff_freeze_mat);
            r.materials = mats.ToArray();
        }
    }
    public void Unfreeze(Transform victim)
    {
        GameObject t = victim.gameObject;
        List<Renderer> allRenderers = new List<Renderer>();

        MeshRenderer[] props = t.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < props.Length; i++)
        {
            if (ContainsExcludedTransform(props[i].transform.name)) { }
            else allRenderers.Add(props[i]);
        }

        SkinnedMeshRenderer[] bodypart = t.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < bodypart.Length; i++)
        {
            if (ContainsExcludedTransform(bodypart[i].transform.name)) { }
            else allRenderers.Add(bodypart[i]);
        }
        foreach (Renderer r in allRenderers)
        {
            List<Material> mats = r.materials.ToList();

            for (int i = 0; i < mats.Count; i++)
            {
                if (mats[i].name.Contains(debuff_freeze_mat.name))
                    mats.RemoveAt(i);
            }
            r.materials = mats.ToArray();
        }
    }
    bool ContainsExcludedTransform(string input)
    {
        foreach (string s in EXCLUDE)
        {
            if (input.Contains(s))
                return true;
        }
        return false;
    }


    public void SetStatusEffect(Transform target, StatusEffects effect, float duration, float damage, Hero heroSource = Hero.None)
    {
        DamageRequest req = new DamageRequest()
        {
            statusEffect = effect,
            statusEffectDuration = duration,
            statusEffectDamage = damage,
            unresistable = false,
            hero = heroSource
        };

        SetStatusEffect(target, req);
    }
    public void SetStatusEffect(Transform target, DamageRequest damageRequest)
    {
        try
        {
            target.GetComponent<IStatusEffect>().SetStatusEffect(new DamageRequest(damageRequest));
        }
        catch (System.Exception e)
        {
            // Debug.LogWarning("failed applying status effect : " + e.Message);
        }
    }
    public GameObject GetObject(StatusEffects fx, Transform source, bool stacking)
    {
        GameObject sel;
        switch (fx)
        {
            case StatusEffects.bleed: sel = debuff_bleeding; break;
            case StatusEffects.blast: sel = debuff_blast; break;
            case StatusEffects.paralyze: sel = debuff_para; break;
            case StatusEffects.poison: sel = debuff_poison; break;
            case StatusEffects.atkUp: sel = buff_atkUp; break;
            case StatusEffects.critUp: sel = buff_critUp; break;
            case StatusEffects.eleResUp: sel = buff_eleResUp; break;
            case StatusEffects.phyResUp: sel = buff_phyResUp; break;
            case StatusEffects.freeze: sel = debuff_freeze; break;
            case StatusEffects.burn: sel = stacking ? debuff_stacked_burn : debuff_burn; break;
            case StatusEffects.armorDown: sel = debuff_armor; break;
            case StatusEffects.damageDown: sel = debuff_damage; break;
            case StatusEffects.exhaust: sel = debuff_exhaust; break;
            case StatusEffects.ensnare: sel = debuff_ensnare; break;
            case StatusEffects.silence: sel = debuff_silence; break;
            case StatusEffects.cursed: sel = debuff_cursed; break;
            case StatusEffects.blind: sel = debuff_blind; break;
            case StatusEffects.tremor: sel = debuff_tremor; break;
            case StatusEffects.slow: sel = debuff_slow; break;
            case StatusEffects.mysticFlame: sel = debuff_mysticFlame; break;
            case StatusEffects.snowSlow: sel = debuff_snowSlow; break;
            case StatusEffects.fireShield: sel = buff_fireShield; break;
            case StatusEffects.focused: sel = debuff_focused; break;
            case StatusEffects.chill: sel = debuff_chill; break;
            case StatusEffects.frostbiteFever: sel = debuff_frostbiteFever; break;
            case StatusEffects.zeravCurse: sel = debuff_zeravcurse; break;
            case StatusEffects.windVeil: sel = buff_abyssal_windveil; break;
            default: sel = null; break;
        }

        GameObject g;
        var special = debuffPositions.Find(x => x.status == fx);
        if (special == null)
            g = Instantiate(sel, source.position + Vector3.up * 2, Quaternion.identity);
        else
            g = Instantiate(sel, source.position + Vector3.up * special.y_offset, Quaternion.identity);
        return g;
    }

}

[System.Serializable]
public class DebuffSpecialPosition
{
    public StatusEffects status;
    public float y_offset;
}
