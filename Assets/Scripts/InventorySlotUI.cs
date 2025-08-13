using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("このスロットのアイコン画像")]
    [SerializeField] private Image iconImage;

    private ItemData currentItem;

    /// <summary>
    /// このスロットがアイテムで埋まっているかどうか
    /// </summary>
    public bool IsOccupied => currentItem != null;

    /// <summary>
    /// スロットの初期化（空にする）
    /// </summary>
    void Awake()
    {
        ClearSlot();
    }

    /// <summary>
    /// アイテムをこのスロットに格納する
    /// </summary>
    public void AssignItem(ItemData item)
    {
        currentItem = item;

        if (iconImage != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
        }

        Debug.Log($"[{gameObject.name}] にアイテム '{item.itemName}' を格納しました");
    }

    /// <summary>
    /// スロットを空にする
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        Debug.Log($"[{gameObject.name}] を初期化しました");
    }

    /// <summary>
    /// 現在格納されているアイテムを取得
    /// </summary>
    public ItemData GetItem() => currentItem;
}

/*Pusing UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    private ItemData currentItem;

    public bool IsOccupied => currentItem != null;

    void Awake()
    {
        ClearSlot(); // 初期化時にスロットを空にする
    }
    public void AssignItem(ItemData item)
    {
        currentItem = item;
        if (iconImage != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
        }

        Debug.Log($"スロット '{gameObject.name}' に '{item.itemName}' を格納しました");
    }

    public void ClearSlot()
    {
        currentItem = null;
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        Debug.Log($"スロット '{gameObject.name}' を空にしました");
    }

    public ItemData GetItem() => currentItem;
}*/