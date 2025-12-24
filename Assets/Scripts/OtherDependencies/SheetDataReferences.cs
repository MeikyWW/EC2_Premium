using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleSheetsToUnity;

public class SheetDataReferences : MonoBehaviour
{
    private const string spreadSheetId = "1R4fTRDYE3q-50TOtIbCykRE5Um4NLiujLeEs2WlA_Qk";

    [System.Serializable]
    public struct StatCapData
    {
        public string statName;
        public float baseValue;
        public float softCap;
        public float hardCap;
        public float minSocketValue;
        public float maxSocketValue;
    }
    public StatCapData[] statData;// = new StatCapData[14];
    void BuildStatusData(GstuSpreadSheet ss)
    {
        statData = new StatCapData[21];
        int count = ss.rows.primaryDictionary.Count - 1;

        for (int i = 0; i < count; i++)
        {
            statData[i].statName = ss[i.ToString(), "Status"].value;
            if (!float.TryParse(ss[i.ToString(), "BaseVal"].value, out statData[i].baseValue))
                statData[i].baseValue = -1;
            if (!float.TryParse(ss[i.ToString(), "SoftCap"].value, out statData[i].softCap))
                statData[i].softCap = -1;
            if (!float.TryParse(ss[i.ToString(), "HardCap"].value, out statData[i].hardCap))
                statData[i].hardCap = -1;
            if (!float.TryParse(ss[i.ToString(), "MinSocket"].value, out statData[i].minSocketValue))
                statData[i].minSocketValue = -1;
            if (!float.TryParse(ss[i.ToString(), "MaxSocket"].value, out statData[i].maxSocketValue))
                statData[i].maxSocketValue = -1;
        }
    }

    [System.Serializable]
    public struct AttributeData
    {
        public string attributeName;
        public string statA;
        public int statValA;
        public string statB;
        public int statValB;
    }
    public AttributeData[] attrData = new AttributeData[5];
    void BuildAttributeData(GstuSpreadSheet ss)
    {
        int count = ss.rows.primaryDictionary.Count - 1;

        for (int i = 0; i < count; i++)
        {
            attrData[i].attributeName = ss[i.ToString(), "Name"].value;
            attrData[i].statA = ss[i.ToString(), "StatA"].value;
            attrData[i].statValA = int.Parse(ss[i.ToString(), "ValA"].value);
            attrData[i].statB = ss[i.ToString(), "StatB"].value;
            attrData[i].statValB = int.Parse(ss[i.ToString(), "ValB"].value);
        }
    }

    public float[] expTable, enemyExpTable, enemyHpTable, enemyDamageTable, enemyGoldTable, enemyArmorTable;
    void BuildExpTable(GstuSpreadSheet ss)
    {
        int count = ss.rows.primaryDictionary.Count - 1;

        expTable = new float[count];
        enemyExpTable = new float[count];
        enemyHpTable = new float[count];
        enemyDamageTable = new float[count];
        enemyGoldTable = new float[count];
        enemyArmorTable = new float[count];

        for (int i = 0; i < count; i++)
        {
            expTable[i] = float.Parse(ss[(i + 1).ToString(), "Exp"].value);
            enemyExpTable[i] = float.Parse(ss[(i + 1).ToString(), "EnemyExp"].value);
            enemyHpTable[i] = float.Parse(ss[(i + 1).ToString(), "EnemyHp"].value);
            enemyDamageTable[i] = float.Parse(ss[(i + 1).ToString(), "EnemyDamage"].value);
            enemyGoldTable[i] = float.Parse(ss[(i + 1).ToString(), "EnemyGold"].value);
            enemyArmorTable[i] = float.Parse(ss[(i + 1).ToString(), "EnemyArmor"].value);
        }
    }

    public void FetchSheetData()
    {
        SpreadsheetManager.ReadPublicSpreadsheet(new GSTU_Search(spreadSheetId, "StatusCap"), BuildStatusData);
        //SpreadsheetManager.ReadPublicSpreadsheet(new GSTU_Search(spreadSheetId, "AttributeAlloc"), BuildAttributeData);
        SpreadsheetManager.ReadPublicSpreadsheet(new GSTU_Search(spreadSheetId, "ExpTable"), BuildExpTable);
    }

    public float GetSoftCap(EC2.Stats stat)
    {
        float result = 0;

        switch (stat)
        {
            case EC2.Stats.MaxHP: result = statData[0].softCap; break;
            case EC2.Stats.MaxMP: result = statData[1].softCap; break;
            case EC2.Stats.Attack: result = statData[2].softCap; break;
            case EC2.Stats.Defense: result = statData[3].softCap; break;
            case EC2.Stats.Crit: result = statData[4].softCap; break;
            case EC2.Stats.CritDamage: result = statData[5].softCap; break;
            case EC2.Stats.AttackSpeed: result = statData[6].softCap; break;
            case EC2.Stats.Speciality: result = statData[7].softCap; break;
            case EC2.Stats.Evasion: result = statData[8].softCap; break;
            case EC2.Stats.Recovery: result = statData[9].softCap; break;
            case EC2.Stats.CooldownReduce: result = statData[10].softCap; break;
            case EC2.Stats.ManaReduce: result = statData[11].softCap; break;
            case EC2.Stats.PhysicalResistance: result = statData[12].softCap; break;
            case EC2.Stats.ElementalResistance: result = statData[13].softCap; break;
            case EC2.Stats.HealthPercentage: result = statData[14].softCap; break;
            case EC2.Stats.BasicAtkDamage: result = statData[15].softCap; break;
            case EC2.Stats.SkillAtkDamage: result = statData[16].softCap; break;
            case EC2.Stats.Accuracy: result = statData[17].softCap; break;
            case EC2.Stats.ManaGain: result = statData[18].softCap; break;
            case EC2.Stats.SPRegen: result = statData[19].softCap; break;
            case EC2.Stats.ConsumablePlus: result = statData[20].softCap; break;

                //case Stats.Break: result = statData[4].hardCap; break;
                //case Stats.Pierce: result = statData[5].hardCap; break;

                //case Stats.ManaGain: result = statData[12].hardCap; break;
                //case Stats.ConsumablePlus: result = statData[16].hardCap; break;
                //case Stats.DropRatePlus: result = statData[17].hardCap; break;
        }

        return result;
    }
    public float GetHardCap(EC2.Stats stat)
    {
        float result = 0;

        switch (stat)
        {
            case EC2.Stats.MaxHP: result = statData[0].hardCap; break;
            case EC2.Stats.MaxMP: result = statData[1].hardCap; break;
            case EC2.Stats.Attack: result = statData[2].hardCap; break;
            case EC2.Stats.Defense: result = statData[3].hardCap; break;
            case EC2.Stats.Crit: result = statData[4].hardCap; break;
            case EC2.Stats.CritDamage: result = statData[5].hardCap; break;
            case EC2.Stats.AttackSpeed: result = statData[6].hardCap; break;
            case EC2.Stats.Speciality: result = statData[7].hardCap; break;
            case EC2.Stats.Evasion: result = statData[8].hardCap; break;
            case EC2.Stats.Recovery: result = statData[9].hardCap; break;
            case EC2.Stats.CooldownReduce: result = statData[10].hardCap; break;
            case EC2.Stats.ManaReduce: result = statData[11].hardCap; break;
            case EC2.Stats.PhysicalResistance: result = statData[12].hardCap; break;
            case EC2.Stats.ElementalResistance: result = statData[13].hardCap; break;
            case EC2.Stats.HealthPercentage: result = statData[14].hardCap; break;
            case EC2.Stats.BasicAtkDamage: result = statData[15].hardCap; break;
            case EC2.Stats.SkillAtkDamage: result = statData[16].hardCap; break;
            case EC2.Stats.Accuracy: result = statData[17].hardCap; break;
            case EC2.Stats.ManaGain: result = statData[18].hardCap; break;
            case EC2.Stats.SPRegen: result = statData[19].hardCap; break;
            case EC2.Stats.ConsumablePlus: result = statData[20].hardCap; break;

                //case Stats.Break: result = statData[4].hardCap; break;
                //case Stats.Pierce: result = statData[5].hardCap; break;

                //case Stats.ManaGain: result = statData[12].hardCap; break;
                //case Stats.ConsumablePlus: result = statData[16].hardCap; break;
                //case Stats.DropRatePlus: result = statData[17].hardCap; break;
        }

        return result;
    }
    public float GetMinSocketValue(EC2.Stats stat)
    {
        float result = 0;

        switch (stat)
        {
            case EC2.Stats.MaxHP: result = statData[0].minSocketValue; break;
            case EC2.Stats.MaxMP: result = statData[1].minSocketValue; break;
            case EC2.Stats.Attack: result = statData[2].minSocketValue; break;
            case EC2.Stats.Defense: result = statData[3].minSocketValue; break;
            case EC2.Stats.Crit: result = statData[4].minSocketValue; break;
            case EC2.Stats.CritDamage: result = statData[5].minSocketValue; break;
            case EC2.Stats.AttackSpeed: result = statData[6].minSocketValue; break;
            case EC2.Stats.Speciality: result = statData[7].minSocketValue; break;
            case EC2.Stats.Evasion: result = statData[8].minSocketValue; break;
            case EC2.Stats.Recovery: result = statData[9].minSocketValue; break;
            case EC2.Stats.CooldownReduce: result = statData[10].minSocketValue; break;
            case EC2.Stats.ManaReduce: result = statData[11].minSocketValue; break;
            case EC2.Stats.PhysicalResistance: result = statData[12].minSocketValue; break;
            case EC2.Stats.ElementalResistance: result = statData[13].minSocketValue; break;
            case EC2.Stats.HealthPercentage: result = statData[14].minSocketValue; break;
            case EC2.Stats.BasicAtkDamage: result = statData[15].minSocketValue; break;
            case EC2.Stats.SkillAtkDamage: result = statData[16].minSocketValue; break;
            case EC2.Stats.Accuracy: result = statData[17].minSocketValue; break;
            case EC2.Stats.ManaGain: result = statData[18].minSocketValue; break;
            case EC2.Stats.SPRegen: result = statData[19].minSocketValue; break;
            case EC2.Stats.ConsumablePlus: result = statData[20].minSocketValue; break;
        }

        return result;
    }
    public float GetMaxSocketValue(EC2.Stats stat)
    {
        float result = 0;

        switch (stat)
        {
            case EC2.Stats.MaxHP: result = statData[0].maxSocketValue; break;
            case EC2.Stats.MaxMP: result = statData[1].maxSocketValue; break;
            case EC2.Stats.Attack: result = statData[2].maxSocketValue; break;
            case EC2.Stats.Defense: result = statData[3].maxSocketValue; break;
            case EC2.Stats.Crit: result = statData[4].maxSocketValue; break;
            case EC2.Stats.CritDamage: result = statData[5].maxSocketValue; break;
            case EC2.Stats.AttackSpeed: result = statData[6].maxSocketValue; break;
            case EC2.Stats.Speciality: result = statData[7].maxSocketValue; break;
            case EC2.Stats.Evasion: result = statData[8].maxSocketValue; break;
            case EC2.Stats.Recovery: result = statData[9].maxSocketValue; break;
            case EC2.Stats.CooldownReduce: result = statData[10].maxSocketValue; break;
            case EC2.Stats.ManaReduce: result = statData[11].maxSocketValue; break;
            case EC2.Stats.PhysicalResistance: result = statData[12].maxSocketValue; break;
            case EC2.Stats.ElementalResistance: result = statData[13].maxSocketValue; break;
            case EC2.Stats.HealthPercentage: result = statData[14].maxSocketValue; break;
            case EC2.Stats.BasicAtkDamage: result = statData[15].maxSocketValue; break;
            case EC2.Stats.SkillAtkDamage: result = statData[16].maxSocketValue; break;
            case EC2.Stats.Accuracy: result = statData[17].maxSocketValue; break;
            case EC2.Stats.ManaGain: result = statData[18].maxSocketValue; break;
            case EC2.Stats.SPRegen: result = statData[19].maxSocketValue; break;
            case EC2.Stats.ConsumablePlus: result = statData[20].maxSocketValue; break;
        }

        return result;
    }

    public int GetMaxLevel()
    {
        return expTable.Length - 1;
    }
    public int GetMaxExp(int level)
    {
        return Mathf.RoundToInt(expTable[level - 1]);
    }
    public int GetEnemyExp(int level)
    {
        return Mathf.RoundToInt(enemyExpTable[level - 1]);
    }
    public int GetEnemyHp(int level)
    {
        return Mathf.RoundToInt(enemyHpTable[level - 1]);
    }
    public int GetEnemyDmg(int level)
    {
        return Mathf.RoundToInt(enemyDamageTable[level - 1]);
    }
    public int GetEnemyGold(int level)
    {
        return Mathf.RoundToInt(enemyGoldTable[level - 1]);
    }
    public int GetEnemyArmor(int level)
    {
        return Mathf.RoundToInt(enemyArmorTable[level - 1]);
    }

    public float ConvertedValue(EC2.Stats stat, float rawValue)
    {
        float result = 0;
        float softCap = GetSoftCap(stat);
        float hardCap = GetHardCap(stat);

        if (softCap >= 0 && rawValue > softCap)
        {
            var softedValue = (rawValue - softCap) / 2; //600 - 500 / 2
            rawValue = softCap + softedValue; // 500 + 50 55
        }

        if (hardCap >= 0 && rawValue > hardCap)
            result = hardCap * 0.1f;
        else result = rawValue * 0.1f;

        return result;
    }

    public float GetRawValue(EC2.Stats stat, float convertedValue) // 550 => 600
    {
        float softCap = GetSoftCap(stat);
        
        if (softCap >= 0 && convertedValue > softCap)
        {
            var softedValue = (convertedValue - softCap) * 2; //550 - 500 * 2 = 100
            convertedValue = softCap + softedValue; // 500 + 100
        }

        return convertedValue;
    }

    public StatusCap GetStatusCap(EC2.Stats stat, float convertedValue)
    {
        StatusCap temp = StatusCap.NonCapped;

        float softCap = GetSoftCap(stat) * 0.1f;
        float hardCap = GetHardCap(stat) * 0.1f;

        if (softCap >= 0 && convertedValue >= softCap)
            temp = StatusCap.SoftCapped;

        if (hardCap >= 0 && convertedValue >= hardCap)
            temp = StatusCap.HardCapped;

        return temp;
    }
}