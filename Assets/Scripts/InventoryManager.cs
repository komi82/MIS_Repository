using UnityEngine;

/// <summary>
/// プレイヤーインベントリの内容と選択状態を管理する。
/// `ItemPickup`/`PutItem`/`SlotSelector` から参照され、追加・削除・選択を一元化する。
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("管理対象のスロット（4つ）")]
    [SerializeField] private InventorySlotUI[] slotUIs;
    public ItemData selectedItem;
    [SerializeField] public InventorySlotUI selectedSlot;


    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 重複防止
            return;
        }
        Instance = this;
    }

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

    public bool HasItem(ItemData item)
    {
        if (item == null) return false;
        
        foreach (var slot in slotUIs)
        {
            if (slot.CurrentItem != null && slot.CurrentItem.itemName == item.itemName)
            {
                return true;
            }
        }
        return false;
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
        if (item == null) return null;
        
        foreach (var slot in slotUIs)
        {
            if (slot.CurrentItem != null && slot.CurrentItem.itemName == item.itemName)
            {
                return slot;
            }
        }
        return null;
    }

}
