using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopCostumeUIButton : MonoBehaviour
{
    int index;
    public UILabel costumeName;
    public GameObject ownStatus;

    public void SetInfo(int i, bool isOwned, string name)
    {
        index = i;
        costumeName.text = name;
        ownStatus.SetActive(isOwned);
    }

    public static System.Action<int> OnListClick;
    void OnClick()
    {
        OnListClick?.Invoke(index);
    }
}
