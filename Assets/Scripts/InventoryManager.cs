using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("管理対象のスロット（4つ）")]
    [SerializeField] private InventorySlotUI[] slotUIs;
    public ItemData selectedItem;
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

        Debug.LogWarning("Inventory full");
        return false;
    }

    /// <summary>
    /// 指定スロットにアイテムを追加（インデックス指定）
    /// </summary>
    public bool AddItemToSlot(int index, ItemData item)
    {
        if (index < 0 || index >= slotUIs.Length)
        {
            return false;
        }

        if (slotUIs[index].IsOccupied)
        {
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

        Debug.Log("Inventory Cleared.");
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
    public void SelectItem(int index)
    {
        // インデックスの範囲チェック
        if (index < 0 || index >= slotUIs.Length)
        {
            Debug.LogWarning($"SelectItem: index {index} が範囲外です");
            selectedItem = null;
            return;
        }

        // 対象スロットの取得
        InventorySlotUI slot = slotUIs[index];
        if (slot == null)
        {
            Debug.LogWarning($"SelectItem: index {index} に対応するスロットが null です");
            selectedItem = null;
            return;
        }

        // スロットから ItemData を取得
        ItemData item = slot.CurrentItem;

        // 選択状態の更新
        selectedItem = item;

        // ログ出力（nullチェック込み）
        if (item != null)
        {
            Debug.Log($"SelectItem: index {index} に '{item.itemName}' を選択しました");
        }
        else
        {
            Debug.Log($"SelectItem: index {index} は空スロットです");
        }
    }
    public void RemoveItem(InventorySlotUI slot)
    {
        
        slot.ClearSlot();
    }

    public InventorySlotUI FindSlotByItem(ItemData item)//selecteditemに対応するスロットを検索
    {
        foreach (var slot in slotUIs)
        {
            if (slot.CurrentItem == item)
            {
                return slot;
            }
        }
        return null;
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
            Debug.Log("�C���x���g�������t�ł�");
            return;
        }

        inventory.Add(item);
        int slotIndex = inventory.Count;
        Debug.Log($"�A�C�e�� '{item.itemName}' ���C���x���g���� {slotIndex} �Ԗڂ̃X���b�g�ɒǉ����܂���");
    }

    public IReadOnlyList<ItemData> GetInventory() => inventory;
} */