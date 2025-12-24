using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CostumeUIButton : MonoBehaviour
{
    private EC2Costume costume;
    public int index;
    public UILabel label;
    public GameObject ownedInfo;
    public UISprite icon;

    public void SetInfo(Item item, int index, bool owned)
    {
        this.index = index;
        icon.spriteName = EC2Utils.GetIcon(item);
        label.text = item.ItemName();
        costume = item.equipment.costume;
        ownedInfo.SetActive(owned);

        if (owned)
            label.color = Color.white;
        else label.color = Color.gray;
    }

    public static event System.Action<int> OnSelectCostume;
    void OnClick()
    {
        OnSelectCostume?.Invoke(index);
    }
}
