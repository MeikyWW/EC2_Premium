using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class SkinnedCostume
{
    public Item costume;
    [ListDrawerSettings(Expanded = true, AlwaysAddDefaultValue = true)]
    public List<GameObject> objectToActive;
    [HorizontalGroup("")]
    public bool disableObj;
    [ShowIf("disableObj")]
    [ListDrawerSettings(Expanded = true)]
    public List<GameObject> objectToDeactive;
}

public class CostumeChanger : MonoBehaviour
{
    public bool isViewer;
    public bool disableChange;

    [Title("Reference")]
    public Hero hero;

    public bool weaponGloballySpawned;


    [FoldoutGroup("Special"), ShowIf("@weaponGloballySpawned")]
    public Transform weaponPos;

    [FoldoutGroup("Special"), ShowIf("@weaponGloballySpawned")]
    public GameObject spawnedWeapon;

    [FoldoutGroup("Special"), ShowIf("@weaponGloballySpawned")]
    public bool disableWeaponOnSafeArea;

    [HideIf("@weaponGloballySpawned")]
    public List<Transform> weaponPosition;
    public List<Transform> weaponPositionSecondary;
    public List<Transform> auraPosition;
    //public Transform wingPosition;
    //public Transform maskPosition;
    public SkinnedMeshRenderer bodyRenderer;
    public SkinnedMeshRenderer hairRenderer;
    public List<DefaultCostume> defaultCostumes; //will be disabled if costume applied
    public List<SkinnedCostume> skinnedCostumes; //costume active - deactive

    [HideInEditorMode] public List<WeaponAura> weaponAuras = new List<WeaponAura>();
    public Dictionary<CostumeSlot, EC2Costume> costumes;

    private void Awake()
    {
        weaponAuras = new List<WeaponAura>();
    }

    public void AddToList(WeaponAura weaponAura)
    {
        weaponAuras.Add(weaponAura);
    }

    public void RemoveFromList(WeaponAura weaponAura)
    {
        weaponAuras.Remove(weaponAura);
    }

    private void OnEnable()
    {
        if (!isViewer) return;
        if (spawnedWeapon)
        {
            spawnedWeapon.SetActive(true);
        }
    }
    private void OnDisable()
    {
        if (spawnedWeapon)
        {
            spawnedWeapon.SetActive(false);
        }
    }

    public void ApplyAura(int level)
    {
        if (disableChange) return;

        foreach (var item in weaponAuras)
            item.SetEnhancementAura(level);
    }

    public GameObject ChangeCostume(EC2Costume costume, int index)
    {
        if (disableChange) return null;

        GameObject selectedCostume = null;

        switch (costume.costumePart)
        {
            case CostumeSlot.Weapon:
                weaponAuras.Clear();

                CheckDefaultOnAppliedCostume(costume);
                if (!costume.isSkinnedMesh)
                {
                    if(costume.isGloballySpawned)
                    {
                        selectedCostume = SpawnGlobalWeapon(costume, index);
                    }
                    else
                    {
                        foreach (Transform t in weaponPosition)
                            CheckWeapon(t, costume, costume.prefab, index, true);

                        foreach (Transform t in weaponPositionSecondary)
                            CheckWeapon(t, costume, costume.secondaryPrefab, index, false);
                    }
                }
                else
                {
                    CheckSkinnedWeapon(costume, index);
                }
                break;

            case CostumeSlot.Accessory:
                //CheckFrontAccPart(costume);
                CheckDefaultOnAppliedCostume(costume);
                CheckSkinned(costume, index);
                break;

            case CostumeSlot.Suit:
                CheckDefaultOnAppliedCostume(costume);
                if (bodyRenderer)
                {
                    bodyRenderer.sharedMesh = costume.mesh;
                    bodyRenderer.material = costume.material;
                    GetMainTexture(costume, a => { bodyRenderer.material.SetTexture("_BaseColorRGBOutlineWidthA", a); });

                    //bodyRenderer.material = costume.MATERIAL;
                    //SetMaterial(costume.GetMainMaterials(index), bodyRenderer);
                }
                CheckSkinned(costume, index);
                break;

            case CostumeSlot.Hair:
                CheckDefaultOnAppliedCostume(costume);
                if (hairRenderer)
                {
                    hairRenderer.sharedMesh = costume.mesh;
                    //GetMainTexture(costume, a => { hairRenderer.material.SetTexture("_BaseColorRGBOutlineWidthA", a); });

                    hairRenderer.material = costume.material;
                    //SetMaterial(costume.GetMainMaterials(index), hairRenderer);
                }
                CheckSkinned(costume, index);
                break;

            default:
                break;
        }

        return selectedCostume;
    }

    private void GetMainTexture(EC2Costume cos, System.Action<Texture> onLoaded)
    {
        if (cos.textureAddress.RuntimeKeyIsValid())
        {
            AddressableManager.instance.LoadFile<Texture>(cos.textureAddress, a =>
            {
                onLoaded(a);
            });
        }
    }

    private void CheckWeapon(Transform parent, EC2Costume costume, GameObject toApply, int index, bool main)
    {
        if (disableChange) return;

        DestroyParentChild(parent);
        if (toApply)
        {
            var instantiatedPart = Instantiate(toApply, parent);
            if (costume.costumePart == CostumeSlot.Weapon)
            {
                var auras = instantiatedPart.GetComponents<WeaponAura>();

                foreach (var aura in auras)
                {
                    if (aura)
                    {
                        weaponAuras.Add(aura);
                    }
                }
            }

            if (main)
            {
                SetMaterial(instantiatedPart, costume.GetMainMaterials(index));
            }
            else
            {
                SetMaterial(instantiatedPart, costume.GetSecondaryMaterials(index));
            }

            OnWeaponChanged?.Invoke(toApply);
        }
    }
    public System.Action<GameObject> OnWeaponChanged;

    public ShaderMapper shaderMapper;
    private void SetMaterial(GameObject objToSet, Material mat)
    {
        // Debug.Log("SetMaterial : " + gameObject.name);
        var renderer = objToSet.GetComponent<Renderer>();
        if (renderer)
        {
            SetMaterial(mat, renderer);
        }

        var listOfRenderer = objToSet.transform.GetComponentsInChildren<Renderer>(true).ToList();
        if(listOfRenderer.Count > 0)
        {
            foreach (var item in listOfRenderer)
            {
                if (item.GetComponent<ParticleSystem>())
                    return;

                if (item)
                    SetMaterial(mat, item);
            }
        }
    }

    private void SetMaterial(Material newMats, Renderer renderer)
    {
        if (newMats == null) return;

        renderer.material = newMats;

        /*
        var key = shaderMapper.GetMyMainTextureProp(sharedMaterial.shader.name);
        if (string.IsNullOrEmpty(key)) return;

        sharedMaterial.SetTexture(key, texture);*/
    }

    private GameObject SpawnGlobalWeapon(EC2Costume costume, int index)
    {
        if (disableChange) return null;

        GameObject selectedPart = null;
        var instantiatedPart = Instantiate(costume.prefab);

        SetMaterial(instantiatedPart, costume.GetMainMaterials(index));
        selectedPart = instantiatedPart;
        if (costume.costumePart == CostumeSlot.Weapon)
        {
            var auras = instantiatedPart.GetComponents<WeaponAura>();

            foreach (var aura in auras)
            {
                if (aura)
                {
                    weaponAuras.Add(aura);
                }
            }
        }

        return selectedPart;
    }
    private void CheckSkinnedWeapon(EC2Costume costume, int index)
    {
        if (disableChange) return;

        foreach (var weapon in weaponPosition)
        {
            if(weapon)
            {
                DisableParentChild(weapon);
                var renderer = weapon.GetComponent<SkinnedMeshRenderer>();
                if(renderer)
                {
                    if(costume.mesh)
                        renderer.sharedMesh = costume.mesh;
                    
                    if(costume.material)
                        renderer.material = costume.material;
                }

                else
                {
                    var meshRenderer = weapon.GetComponent<MeshRenderer>();
                    if(meshRenderer)
                    {
                        var meshFilter = weapon.GetComponent<MeshFilter>();
                        if (meshFilter) meshFilter.sharedMesh = costume.mesh;

                        meshRenderer.material = costume.material;
                    }
                }
            }
        }

        foreach (var parent in auraPosition)
        {
            if (parent)
            {
                DestroyParentChild(parent);
                if (costume.spawnedAura)
                {
                    var instantiatedPart = Instantiate(costume.spawnedAura, parent);
                    var auras = instantiatedPart.GetComponents<WeaponAura>();

                    foreach (var aura in auras)
                    {
                        if (aura) weaponAuras.Add(aura);
                    }
                }
            }
        }

        CheckSkinned(costume, index);
    }


    private void CheckSkinned(EC2Costume costume, int index)
    {
        //Debug.Log("cos : " + index + " - " + costume.material);
         

        if (disableChange) return;

        SkinnedCostume skinnedCostume = skinnedCostumes.Find(x => x.costume.equipment.costume == costume);
        if(skinnedCostume != null)
        {
            CostumeEnableObject(skinnedCostume, true);
            /*
            skinnedCostume.objectToActive.SetActive(true);
            if(skinnedCostume.objectToActive)
            {
                SetMaterial(skinnedCostume.objectToActive, costume.GetMainMaterials(index));
            }*/

            foreach (var item in skinnedCostume.objectToActive)
            {
                SetMaterial(item, costume.GetMainMaterials(index));
            }

            CostumeDisableObject(skinnedCostume, false);
        }
    }

    public void DisableByType(CostumeSlot slot)
    {
        if (disableChange) return;

        var listOfSameType = skinnedCostumes.FindAll(x => x.costume.equipment.costume.costumePart == slot);
        foreach (var item in listOfSameType)
        {
            CostumeEnableObject(item, false);
            //item.objectToActive.SetActive(false);
            CostumeDisableObject(item, true);
        }
    }

    void CostumeEnableObject(SkinnedCostume item, bool state)
    {
        foreach (GameObject g in item.objectToActive)
        {
            if (g != null) g.SetActive(state);
        }
    }
    void CostumeDisableObject(SkinnedCostume item, bool state)
    {
        if (!item.disableObj) return;

        foreach (GameObject g in item.objectToDeactive)
        {
            if (g != null) g.SetActive(state);
        }
    }

    public void DisableAll()
    {
        if (disableChange) return;

        DisableByType(CostumeSlot.Accessory);
        DisableByType(CostumeSlot.Hair);
        DisableByType(CostumeSlot.Suit);
    }
    public void DisableAccessory(EC2Costume costume)
    {
        if (disableChange) return;

        var listOfSameType = skinnedCostumes.FindAll(
            x => x.costume.equipment.costume == costume);
        foreach (var item in listOfSameType)
        {
            //item.objectToActive.SetActive(false);
            CostumeEnableObject(item, false);
            CostumeDisableObject(item, true);
        }
    }
    private void DestroyParentChild(Transform parent)
    {
        if (disableChange) return;

        if (!parent) return;
        if (parent.childCount > 0)
        {
            for (int i = 0; i < parent.childCount; i++)
                Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void DisableParentChild(Transform parent)
    {
        if (disableChange) return;

        if (!parent) return;
        if (parent.childCount > 0)
        {
            for (int i = 0; i < parent.childCount; i++)
                parent.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void CheckDefaultOnAppliedCostume(EC2Costume costume)
    {
        if (disableChange) return;

        var allDefault = defaultCostumes.FindAll(x => x.slot == costume.costumePart);
        foreach (var item in allDefault)
            item.defaultObject.SetActive(costume.costumeType == CostumeSet.Default);
    }
    
    public void SetSpawned(GameObject g, System.Action OnSpawned)
    {
        ClearAllSpawned();
        spawnedWeapon = g;
        OnSpawned?.Invoke();
    }
    public void ClearAllSpawned()
    {
        if (spawnedWeapon)
        {
            Destroy(spawnedWeapon);
        }
    }
    public void HideSpawnedWeapon()
    {
        if (spawnedWeapon)
        {
            spawnedWeapon.SetActive(false);
        }
    }
    public void ShowSpawnedWeapon()
    {
        if (spawnedWeapon)
        {
            spawnedWeapon.SetActive(true);
        }
    }
    public void ResetPos()
    {
        if (spawnedWeapon)
        {
            if (weaponPos)
            {
                spawnedWeapon.transform.position = weaponPos.position;
            }
        }
    }

    bool onCombat;
    public void SetCostumeInCombat(bool isCombat)
    {
        onCombat = isCombat;

        if (spawnedWeapon)
        {
            if (!onCombat && disableWeaponOnSafeArea) HideSpawnedWeapon();
            else ShowSpawnedWeapon();
        }
    }

    /*
    [Title("Helper")]
    [Button]
    public void SetTextureSkinned()
    {
        foreach (var item in skinnedCostumes)
        {
            var renderer = item.objectToActive.GetComponent<Renderer>();
            if (!renderer) continue;

            var material = renderer.sharedMaterial;

            if (material)
            {
                var key = shaderMapper.GetMyMainTextureProp(material.shader.name);
                if (string.IsNullOrEmpty(key)) return;
                var chosenTexture = material.GetTexture(key);
                /*
                if (chosenTexture != null)
                {
                    if(!item.costume.equipment.costume.otherMainMaterials.Contains(chosenTexture))
                    {
                        item.costume.equipment.costume.otherMainMaterials.Add(chosenTexture);
                    }
                }
                EditorUtility.SetDirty(item.costume);
            }
        }
    }
    */
}

[System.Serializable]
public class DefaultCostume
{
    public GameObject defaultObject;
    public CostumeSlot slot;
}
