using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Costume Database", menuName = "EC2/Database/Costume", order = 1)]
public class CostumeDatabase : ScriptableObject
{
    public List<Item> costumes;
}
