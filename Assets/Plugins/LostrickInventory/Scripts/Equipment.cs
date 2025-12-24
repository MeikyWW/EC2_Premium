using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.AddressableAssets;

[System.Serializable]
public class Equipment
{
    [Space]
    public EquipmentCostumeType equipmentType;
    [ShowIf("@this.equipmentType == EquipmentCostumeType.OnlyEquipment " +
        "|| this.equipmentType == EquipmentCostumeType.EquipmentCostume")]
    public EquipSlot equipSlot;
    [ShowIf("@this.equipmentType == EquipmentCostumeType.OnlyEquipment " +
        "|| this.equipmentType == EquipmentCostumeType.EquipmentCostume")]
    public EquipCategory type;
    [ShowIf("@this.equipmentType == EquipmentCostumeType.OnlyEquipment " +
        "|| this.equipmentType == EquipmentCostumeType.EquipmentCostume")]
    public int levelRequirement;
    [ShowIf("@this.equipmentType == EquipmentCostumeType.OnlyEquipment " +
        "|| this.equipmentType == EquipmentCostumeType.EquipmentCostume")]
    public Hero characterRequirement;

    [Title("Base Stat"), HideLabel]
    public EquipStats baseStats;
    [Tooltip("1 = min socket val. 5 = max socket val.")]
    [Range(0,10)]
    public float secondaryStatLevel = 5;

    [HideInInspector]
    public bool followPrevious;

    public EquipSet setCategory;

    [Space]
    [ShowIf("@this.equipmentType == EquipmentCostumeType.EquipmentCostume " +
        "|| this.equipmentType == EquipmentCostumeType.Costume"), HideLabel]
    public EC2Costume costume;

}

[System.Serializable]
public class EC2Costume
{
    [Header("Costume Properties")]
    public Hero hero;
    public CostumeSlot costumePart;
    public CostumeSet costumeType;
    public bool isGloballySpawned;

    [ShowIf("@this.costumePart == CostumeSlot.Weapon && !this.isGloballySpawned")]
    public Vector3 offset;

    [ShowIf("@this.costumePart == CostumeSlot.Weapon && !this.isGloballySpawned")]
    public bool isSkinnedMesh;
    [ShowIf("@this.costumePart == CostumeSlot.Weapon && !this.isSkinnedMesh")]
    public GameObject prefab;
    [ShowIf("@this.costumePart == CostumeSlot.Weapon && !this.isSkinnedMesh")]
    public GameObject secondaryPrefab;

    [ShowIf("@this.costumePart == CostumeSlot.Hair || " +
        "this.costumePart == CostumeSlot.Suit || (this.costumePart == CostumeSlot.Weapon && this.isSkinnedMesh)")]
    public Material material;
    [ShowIf("@this.costumePart == CostumeSlot.Hair || " +
        "this.costumePart == CostumeSlot.Suit || (this.costumePart == CostumeSlot.Weapon && this.isSkinnedMesh)")]
    //public AssetReference materialAddress;
    public AssetReference textureAddress;
    [ShowIf("@this.costumePart == CostumeSlot.Hair || " +
        "this.costumePart == CostumeSlot.Suit || (this.costumePart == CostumeSlot.Weapon && this.isSkinnedMesh)")]
    public Mesh mesh;

    [ShowIf("@this.costumePart == CostumeSlot.Weapon && this.isSkinnedMesh")]
    public GameObject spawnedAura;

    [Title("Textures Available")]
    public List<Material> otherMainMaterials;

    [Title("Textures Available")]
    public List<Material> otherSecondaryMaterials;

    [Title("Unlock Stat"), HideLabel]
    public EquipStats unlockStat;

    public bool HasOtherMaterials
    {
        get
        {
            if(prefab && secondaryPrefab && costumePart == CostumeSlot.Weapon)
            {
                return HasOtherMainTexture && HasOtherSecondaryTexture;
            }
            else
            {
                return HasOtherMainTexture;
            }
        }
    }

    public int CountOtherMaterials
    {
        get
        {
            if (prefab && secondaryPrefab && costumePart == CostumeSlot.Weapon)
            {
                return Mathf.Max(otherMainMaterials.Count, otherSecondaryMaterials.Count);
            }
            else
            {
                return otherMainMaterials.Count;
            }
        }
    }

    public Material GetMainMaterials(int index)
    {
        try
        {
            if (index < otherMainMaterials.Count)
                return otherMainMaterials[index];
        }
        catch { }
        return null;
    }
    public Material GetSecondaryMaterials(int index)
    {
        try
        {
            if (index < otherSecondaryMaterials.Count)
                return otherSecondaryMaterials[index];
        }
        catch { }
        return null;
    }
    private bool HasOtherMainTexture
    {
        get
        {
            if (otherMainMaterials == null) return false;
            if (otherMainMaterials.Count > 1) return true;
            return false;
        }
    }

    private bool HasOtherSecondaryTexture
    {
        get
        {
            if (otherSecondaryMaterials == null) return false;
            if (otherSecondaryMaterials.Count > 1) return true;
            return false;
        }
    }

    public int NextTexture(int currentIndex)
    {
        if (!HasOtherMaterials) return -1;

        currentIndex++;
        if(currentIndex >= CountOtherMaterials)
        {
            currentIndex = 0;
        }

        return currentIndex;
    }

    [FoldoutGroup("Helper")]
    public ShaderMapper helper;
    /*
    [FoldoutGroup("Helper"), Button]
    public void ResetOtherTexture()
    {
#if UNITY_EDITOR
        otherMainMaterials = new List<Material>(); //
        otherSecondaryMaterials = new List<Material>(); //
#endif
    }
    [FoldoutGroup("Helper"), Button]
    public void ExtractDefaultTexture()
    {
#if UNITY_EDITOR
        switch (costumePart)
        {
            case CostumeSlot.Weapon:
                if(!isSkinnedMesh)
                {
                    if(prefab)
                    {
                        var renderer = prefab.GetComponent<Renderer>();
                        if(renderer)
                        {
                            var material = renderer.sharedMaterial;
                            otherMainMaterials.Add(material);
                        }
                    }

                    if (secondaryPrefab)
                    {
                        var renderer = secondaryPrefab.GetComponent<Renderer>();
                        if (renderer)
                        {
                            var material = renderer.sharedMaterial;
                            otherSecondaryMaterials.Add(material);
                        }
                    }
                }
                else
                {
                    if (material)
                    {
                        otherMainMaterials.Add(material);
                    }
                }
                break;

            case CostumeSlot.Hair:
            case CostumeSlot.Suit:
                if (material)
                {
                    otherMainMaterials.Add(material);
                }
                break;
        }
#endif
    }
    */
    [FoldoutGroup("Helper"), Button]
    public void SetMaterialAddress()
    {
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material.GetTexture("_BaseColorRGBOutlineWidthA"), out string guid, out long _);
        textureAddress = new AssetReference(guid);
#endif
    }
}

[System.Serializable]
public class MaterialTexturesPair
{
    public Material material;
    public List<Texture> availableTextures;
}

[System.Serializable]
public class EquipStats
{
    [HorizontalGroup("a"), HideLabel]
    public EC2.Stats stats;
    [HorizontalGroup("a"), HideLabel]
    public float value;
    public EquipStats()
    {
        stats = EC2.Stats.None;
        value = 0;
    }
    public EquipStats(EC2.Stats _stats, float _value)
    {
        stats = _stats;
        value = _value;
    }
}

[System.Serializable]
public class EquipSocket
{
    public string socketID;
    public int currentLevel;
    public float currentEXP;

    public EquipSocket()
    {
        socketID = "";
        currentLevel = 0;
        currentEXP = 0;
    }

    public EquipSocket(string socketID, int level, float exp)
    {
        this.socketID = socketID;
        this.currentLevel = level;
        currentEXP = exp;
    }
}
public enum AccessoriesBackPosition
{
    None, Wing
}
public enum AccessoriesFrontPosition
{
    None, Mask
}

public enum EquipmentCostumeType
{
    OnlyEquipment, 
    EquipmentCostume,
    Costume
}

public enum CostumeSet
{
    Default,
    Croven,
    School,
    Maid,
    OG_Claris,
    Casual,
    Santa,
    Eclipse,
    Summer,
    Midnight,
    Halloween,
    OtherNoEffect,
    TimeSkip,

}