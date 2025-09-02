using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> allItems;

    public List<ItemData> GetItemsByType(string type)
    {
        return allItems.Where(item => item.itemType == type).ToList();
    }
}