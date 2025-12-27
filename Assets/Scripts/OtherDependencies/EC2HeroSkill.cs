using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

public enum StatusValueType
{
    percent,
    second,
    none
}

[System.Serializable]
public struct AttributeInfo
{
    public float baseValue;
    public float valueIncreasePerLevel;
    public float valueAtMaxLevel;
    public StatusValueType valType;
    public bool isHidden;
}

[CreateAssetMenu(fileName = "Skill", menuName = "EC2/HeroSkill", order = 1)]
public class EC2HeroSkill : ScriptableObject
{

    #region EDITOR DISPLAY
    [BoxGroup("Info"), ShowInInspector, PropertyOrder(-1), DisplayAsString, HideLabel, GUIColor(0,1,0)]
    private string Editor_Skill_Name
    {
        get { return "[" + I2.Loc.LocalizationManager.GetTranslation("skill/" + id + "/name") + "]"; }
        set { }
    }
    [BoxGroup("Info"), ShowInInspector, PropertyOrder(-1), DisplayAsString(false), HideLabel]
    private string Editor_Skill_Desc
    {
        get { return I2.Loc.LocalizationManager.GetTranslation("skill/" + id + "/desc"); }
        set { }
    }
    [BoxGroup("Attributes"), ShowInInspector, PropertyOrder(-1), DisplayAsString(false), HideLabel]
    private string Editor_Skill_Attributes
    {
        get 
        {
            string build = "";
            for (int i = 0; i < skillAttributes.Length; i++)
            {
                string num = "[" + i + "] ";
                string detail = I2.Loc.LocalizationManager.GetTranslation("skill/" + id + "/atb_" + i);

                build += num + detail;
                if (skillAttributes[i].isHidden) build += " - hidden";

                build += " : " + skillAttributes[i].baseValue;

                switch (skillAttributes[i].valType)
                {
                    case StatusValueType.percent: build += "%"; break;
                    case StatusValueType.second: build += "s"; break;
                    case StatusValueType.none: break;
                }

                if (i != skillAttributes.Length - 1) build += "\n";
            }

            return build;
        }
        set { }
    }
    #endregion

    [PropertyOrder(-2)]
    public string id;

    [BoxGroup("Usage"), LabelText("Mana Cost")]
    public float baseManaCost;
    
    [BoxGroup("Usage"), LabelText("Cooldown")]
    public float baseCooldown;

    [BoxGroup("Usage"), LabelText("Disabled By Ensnare")]
    public bool disabledByEnsnare;

    [BoxGroup("Usage"), LabelText("Unaffected By Silence")]
    public bool unaffectedBySilence;

    [PropertySpace(SpaceBefore = 10, SpaceAfter = 0), HideLabel]
    [ListDrawerSettings(ShowIndexLabels = false), LabelText("Skill Attributes")]
    public AttributeInfo[] skillAttributes;

    //Set Values
    private float manaCost;
    private float cooldown;
    private float[] attributes;

    [FoldoutGroup("Hidden Values")]
    public float runningCooldown, appliedCooldown, cdModifier;

    public void SetSkillAttributes(int skillLevel)
    {
        //calculate skill level
        if (skillLevel == 10)
        {
            manaCost = baseManaCost; //manaCostAtMaxLevel;
            cooldown = baseCooldown; //cooldownAtMaxLevel;

            attributes = new float[skillAttributes.Length];
            for (int i = 0; i < skillAttributes.Length; i++)
                attributes[i] = skillAttributes[i].valueAtMaxLevel;
        }
        else
        {
            int mod = skillLevel - 1;
            manaCost = baseManaCost; //+ (mod * manaCostIncreasePerLevel);
            cooldown = baseCooldown; //+ (mod * cooldownIncreasePerLevel);

            attributes = new float[skillAttributes.Length];
            for (int i = 0; i < skillAttributes.Length; i++)
                attributes[i] = skillAttributes[i].baseValue + (mod * skillAttributes[i].valueIncreasePerLevel);
        }

        appliedCooldown = cooldown;
    }

    public string SkillName()
    {
        return I2.Loc.LocalizationManager.GetTranslation("skill/" + id + "/name");
    }
    public string Description()
    {
        return I2.Loc.LocalizationManager.GetTranslation("skill/" + id + "/desc");
    }
    public string AttributeDescription(int index)
    {
        return I2.Loc.LocalizationManager.GetTranslation("skill/" + id + "/atb_" + index);
    }

    public float ManaCost(float manaRedution)
    {
        float totalManaCost = manaCost + custom_extra_manacost;

        float reducedManaCost = manaRedution / 100 * totalManaCost;
        return totalManaCost - reducedManaCost;
    }
    public float ApplyCooldown(float cdReduction)
    {
        appliedCooldown = (1 - (cdReduction / 100)) * cooldown;
        return appliedCooldown;
    }

    public float GetFinalCooldown(float cdReduction)
    {
        var cd = (1 - (cdReduction / 100)) * cooldown;
        return cd;
    }
    public float Attribute(int index)
    {
        try
        {
            return attributes[index];
        }
        catch
        {
            Debug.Log("Attribute out of index");
            return 0f;
        }
    }

    public List<SkillExAttribute> ex_rune;
    public string EX_Skill(int index, float value, bool active)
    {
        float finalModifiedVal = ex_rune[0].additive ? ex_rune[0].modifier + value : ex_rune[0].modifier * value;
        string val = string.Format("[{0}]{1}%[-]", active ? "FF8CFF" : "828282", (finalModifiedVal).ToString());
        return string.Format(I2.Loc.LocalizationManager.GetTranslation("skill/" + id + "/ex_" + index), val);
    }
    public float custom_extra_manacost;
}

[System.Serializable]
public class SkillExAttribute
{
    public SocketType rune;
    public float modifier;
    public bool additive;
}