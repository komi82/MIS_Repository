using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("管理対象のスロット（4つ）")]
    [SerializeField] private InventorySlotUI[] slotUIs;

    /// <summary>
    /// アイテムを最初の空スロットに追加する
    /// </summary>
    public bool AddItem(ItemData item)
    {
        foreach (var slot in slotUIs)
        {
            if (!slot.IsOccupied)
            {
                slot.AssignItem(item);
                return true;
            }
        }

        Debug.LogWarning("インベントリが満杯です");
        return false;
    }

    /// <summary>
    /// 指定スロットにアイテムを追加（インデックス指定）
    /// </summary>
    public bool AddItemToSlot(int index, ItemData item)
    {
        if (index < 0 || index >= slotUIs.Length)
        {
            Debug.LogWarning($"スロット番号 {index} は範囲外です");
            return false;
        }

        if (slotUIs[index].IsOccupied)
        {
            Debug.LogWarning($"スロット {index} はすでに使用されています");
            return false;
        }

        slotUIs[index].AssignItem(item);
        return true;
    }

    /// <summary>
    /// 全スロットを初期化（空にする）
    /// </summary>
    public void ClearAllSlots()
    {
        foreach (var slot in slotUIs)
        {
            slot.ClearSlot();
        }

        Debug.Log("インベントリを初期化しました");
    }

    /// <summary>
    /// スロットの状態を取得
    /// </summary>
    public InventorySlotUI GetSlot(int index)
    {
        if (index < 0 || index >= slotUIs.Length) return null;
        return slotUIs[index];
    }

    /// <summary>
    /// インベントリが満杯かどうか
    /// </summary>
    public bool IsFull()
    {
        foreach (var slot in slotUIs)
        {
            if (!slot.IsOccupied) return false;
        }
        return true;
    }
}




/*using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private int maxSlots = 4;
    private List<ItemData> inventory = new List<ItemData>();

    public enum ItemStatus
    {
        one, two, three, four, max
    }

    public void ClearAllSlots()
    {
        foreach (var slot in slotUIs)
        {
            slot.ClearSlot();
        }
    }


    public void AddItem(ItemData item)
    {
        if (inventory.Count >= maxSlots)
        {
            Debug.Log("インベントリが満杯です");
            return;
        }

        inventory.Add(item);
        int slotIndex = inventory.Count;
        Debug.Log($"アイテム '{item.itemName}' をインベントリの {slotIndex} 番目のスロットに追加しました");
    }

    public IReadOnlyList<ItemData> GetInventory() => inventory;
} */