using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveContainer : MonoBehaviour
{
    public List<PindahanContainer> pindahanList;

    public List<Item> toRemove;

    public void RemoveAll()
    {
        foreach (var item in toRemove)
        {
            var find = GameManager.instance.inventory.CharacterInventory().GetItemData(item.id);
            if(find != null)
            {
                if (!string.IsNullOrEmpty(find.id))
                {
                    if(find.quantity > 0)
                    {
                        GameManager.instance.inventory.CharacterInventory().RemoveItem(find.id, find.quantity);
                    }
                }
            }
        }
    }

    public void Migrate()
    {
        foreach (var pindahan in pindahanList)
        {
            foreach (var item in pindahan.toMoves)
            {
                var find = GameManager.instance.inventory.CharacterInventory().FindItem(pindahan.from, item.id);
                if (find != null)
                {
                    if (!string.IsNullOrEmpty(find.id))
                    {
                        Debug.Log("Removing " + find.id);
                        var copy = find.Copy();
                        GameManager.instance.inventory.CharacterInventory().RemoveItem(find.id, 999999);

                        GameManager.instance.inventory.AddItem(copy.id, copy.quantity, copy);
                    }
                }
            }
        }
    }
}

[System.Serializable]
public class PindahanContainer
{
    public ItemType from;
    public List<Item> toMoves;
}
