using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// アイテム定義の検索・変換を提供するデータベース。
/// `RequestManager` やクラフト系処理から、タイプ別ランダム取得や派生品取得に利用される。
/// </summary>
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
        if (items.Count == 0)
        {
            Debug.LogWarning($"GetRandomItemByType: '{type}' タイプのアイテムが見つかりません");
            return null;
        }
        ItemData selected = items[Random.Range(0, items.Count)];
        Debug.Log($"GetRandomItemByType: '{type}' から '{selected.itemName}' を選択");
        return selected;
    }

    public ItemData GetPurifiedVersion(ItemData cursed)
    {
        if (cursed == null)
        {
            Debug.LogWarning("GetPurifiedVersion: cursed is null");
            return null;
        }
        // 穢れたを浄化されたに置換
        string targetName = cursed.itemName.Replace("穢れた", "浄化された");
        ItemData result = allItems.Find(i => i.itemName == targetName);
        
        if (result == null)
        {
            Debug.LogWarning($"GetPurifiedVersion: '{cursed.itemName}' に対応する '{targetName}' が見つかりません");
        }
        else
        {
            Debug.Log($"GetPurifiedVersion: '{cursed.itemName}' → '{targetName}' を発見");
        }
        
        return result;
    }

    public ItemData GetRepairedVersion(ItemData broken)
    {
        return allItems.Find(i => i.itemName == broken.itemName.Replace("壊れた", "修復した"));
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
