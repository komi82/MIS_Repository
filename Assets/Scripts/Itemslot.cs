using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Itemslot : MonoBehaviour
{
    [SerializeField] InventoryManager inventoryManager;
    [SerializeField] private ItemData currentItem;
    public ItemData CurrentItem => currentItem;

    // このスロットがアイテムで埋まっているかどうか
    public bool IsOccupied => currentItem != null;

    // アイテムをこのスロットに格納する
    public void AssignItem(ItemData item)
    {
        currentItem = item;
        Debug.Log($"[{gameObject.name}] にアイテム '{item.itemName}'を格納しました");
    }

    public void ClearSlot()
    {
        currentItem = null;
        Debug.Log($"[{gameObject.name}] スロットを空にしました");
    }
        // 現在格納されているアイテムを取得
    public ItemData GetItem() => currentItem;
}

