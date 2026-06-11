using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 単一インベントリ枠のUI状態（アイコン/空状態）を管理する。
/// `InventoryManager` から呼ばれ、格納アイテムに応じて見た目を更新する。
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [Header("このスロットのアイコン画像")]
    [SerializeField] private Image iconImage;
    [SerializeField] private ItemData currentItem;
    [SerializeField] private Sprite emptySlotSprite;

    [Header("開始時")]
    [Tooltip("true: ゲーム開始時に currentItem を空にする / false: Inspector で設定した currentItem を維持する")]
    [SerializeField] private bool clearCurrentItemOnStart = true;

    public ItemData CurrentItem => currentItem;

    // このスロットがアイテムで埋まっているかどうか
    public bool IsOccupied => currentItem != null;

    void Awake()
    {
        if (clearCurrentItemOnStart)
        {
            ClearSlot();
        }
        else
        {
            SyncVisualToCurrentItem();
        }
    }

    // アイテムをこのスロットに格納する
    public void AssignItem(ItemData item)
    {
        currentItem = item;

        if (iconImage != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
        }

        Debug.Log($"[{gameObject.name}] にアイテム '{item.itemName}'を格納しました");
    }

    // スロットを空にする
    public void ClearSlot()
    {
        currentItem = null;

        if (iconImage != null)
        {
            iconImage.sprite = emptySlotSprite; // null → 空スロット用スプライト
            iconImage.enabled = true;           // スプライトは表示したまま
        }

        Debug.Log($"[{gameObject.name}] スロットを空にしました");
    }

    // 現在格納されているアイテムを取得
    public ItemData GetItem() => currentItem;

    void SyncVisualToCurrentItem()
    {
        if (currentItem != null)
        {
            if (iconImage != null && currentItem.icon != null)
            {
                iconImage.sprite = currentItem.icon;
                iconImage.enabled = true;
            }
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = emptySlotSprite;
            iconImage.enabled = true;
        }
    }
}


    void SyncVisualToCurrentItem()
    {
        if (currentItem != null)
        {
            if (iconImage != null && currentItem.icon != null)
            {
                iconImage.sprite = currentItem.icon;
                iconImage.enabled = true;
            }
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = emptySlotSprite;
            iconImage.enabled = true;
        }
    }
}
