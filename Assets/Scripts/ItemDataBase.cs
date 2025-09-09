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

    public ItemData GetEnhancedFireVersion(ItemData baseWeapon_fire)
    {
        return allItems.Find(i => i.itemName == $"炎の{baseWeapon_fire.itemName}");
    }

    public ItemData GetEnhancedFrozenVersion(ItemData baseWeapon_frozen)
    {
        return allItems.Find(i => i.itemName == $"氷の{baseWeapon_frozen.itemName}");
    }

    public ItemData GetEnhancedWindVersion(ItemData baseWeapon_wind)
    {
        return allItems.Find(i => i.itemName == $"風の{baseWeapon_wind.itemName}");
    }

    public ItemData GetEnhancedBrightVersion(ItemData baseWeapon_bright)
    {
        return allItems.Find(i => i.itemName == $"光の{baseWeapon_bright.itemName}");
    }

    public ItemData GetEnhancedDarknessVersion(ItemData baseWeapon_darkness)
    {
        return allItems.Find(i => i.itemName == $"闇の{baseWeapon_darkness.itemName}");
    }
}