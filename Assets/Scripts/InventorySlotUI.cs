using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("このスロットのアイコン画像")]
    [SerializeField] private Image iconImage;
    [SerializeField] private ItemData currentItem;
    [SerializeField] private Sprite emptySlotSprite;
    public ItemData CurrentItem => currentItem;

    // このスロットがアイテムで埋まっているかどうか
    public bool IsOccupied => currentItem != null;

    // スロットの初期化（空にする）
    void Awake()
    {
        ClearSlot();
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
        ClearSlot(); // ���������ɃX���b�g����ɂ���
    }
    public void AssignItem(ItemData item)
    {
        currentItem = item;
        if (iconImage != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
        }

        Debug.Log($"�X���b�g '{gameObject.name}' �� '{item.itemName}' ���i�[���܂���");
    }

    public void ClearSlot()
    {
        currentItem = null;
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        Debug.Log($"�X���b�g '{gameObject.name}' ����ɂ��܂���");
    }

    public ItemData GetItem() => currentItem;
}*/