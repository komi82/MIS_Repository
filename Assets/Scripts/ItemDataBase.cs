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

    public ItemData GetRandomItemByType(string type)
    {
        var items = allItems.FindAll(i => i.itemType == type);
        if (items.Count == 0) return null;
        return items[Random.Range(0, items.Count)];
    }

    public ItemData GetPurifiedVersion(ItemData cursed)
    {
        // 仮実装：IDや名前でマッピング
        return allItems.Find(i => i.itemName == cursed.itemName.Replace("穢れた", ""));
    }

    public ItemData GetRepairedVersion(ItemData broken)
    {
        return allItems.Find(i => i.itemName == broken.itemName.Replace("壊れた", ""));
    }

    public ItemData GetEnhancedVersion(ItemData baseWeapon)
    {
        return allItems.Find(i => i.itemName == baseWeapon.itemName + "・属性付き");
    }
}