using UnityEngine;

public class ItemslotManager : MonoBehaviour
{
    [Header("管理対象のスロット（2つ）")]
    [SerializeField] private Itemslot[] slots;
    public ItemData selectedItem;

    /// <summary>
    /// アイテムを最初の空スロットに追加する
    /// </summary>
    public bool AddItem(ItemData item)
    {
        foreach (var slot in slots)
        {
            if (!slot.IsOccupied)
            {
                slot.AssignItem(item);
                return true;
            }
        }

        Debug.LogWarning("Slot full");
        return false;
    }
    /// <summary>
    /// 指定スロットにアイテムを追加（インデックス指定）
    /// </summary>
    public bool AddItemToSlot(int index, ItemData item)
    {
        if (index < 0 || index >= slots.Length)
        {
            return false;
        }

        if (slots[index].IsOccupied)
        {
            return false;
        }

        slots[index].AssignItem(item);
        return true;
    }

    /// <summary>
    /// 全スロットを初期化（空にする）
    /// </summary>
    public void ClearAllSlots()
    {
        foreach (var slot in slots)
        {
            slot.ClearSlot();
        }

        Debug.Log("Inventory Cleared.");
    }

    /// <summary>
    /// スロットの状態を取得
    /// </summary>
    public Itemslot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    /// <summary>
    /// インベントリが満杯かどうか
    /// </summary>
    public bool IsFull()
    {
        foreach (var slot in slots)
        {
            if (!slot.IsOccupied) return false;
        }
        return true;
    }
    public void SelectItem(int index)
    {
        // インデックスの範囲チェック
        if (index < 0 || index >= slots.Length)
        {
            Debug.LogWarning($"SelectItem: index {index} が範囲外です");
            selectedItem = null;
            return;
        }

        // 対象スロットの取得
        Itemslot slot = slots[index];
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
    public void RemoveItem(Itemslot slot)
    {
        
        slot.ClearSlot();
    }

}
