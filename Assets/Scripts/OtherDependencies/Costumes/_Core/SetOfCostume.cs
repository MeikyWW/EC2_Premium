using CodeStage.AntiCheat.ObscuredTypes;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "New Set of Costume", menuName = "EC2/Set of Costume", order = 4)]
public class SetOfCostume : ScriptableObject
{
    #region EDITOR DISPLAY
    [BoxGroup("General Info"), ShowInInspector, PropertyOrder(-1), DisplayAsString, HideLabel, GUIColor(0, 1, 0)]
    private string Editor_Set_Name
    {
        get { return "[" + I2.Loc.LocalizationManager.GetTranslation("item/" + id + "/name") + "]"; }
        set { }
    }
    public string SetName()
    {
        return I2.Loc.LocalizationManager.GetTranslation("item/" + id + "/name");
    }
    #endregion

    public HeroReference heroReference;

    [LabelText("ID"), PropertyOrder(-2)]
    public string id;
    public ObscuredInt price;
    public Hero hero;

    [Header("Costumes")]
    public List<Item> costumes;

    [Header("Custom PP")]
    public AssetReference ppDiamond;
    public AssetReference ppMenu;
}
