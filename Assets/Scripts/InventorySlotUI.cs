using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("���̃X���b�g�̃A�C�R���摜")]
    [SerializeField] private Image iconImage;
    [SerializeField] private ItemData currentItem;
    [SerializeField] private Sprite emptySlotSprite;
    public ItemData CurrentItem => currentItem;
    /// <summary>
    /// ���̃X���b�g���A�C�e���Ŗ��܂��Ă��邩�ǂ���
    /// </summary>
    public bool IsOccupied => currentItem != null;

    /// <summary>
    /// �X���b�g�̏������i��ɂ���j
    /// </summary>
    void Awake()
    {
        ClearSlot();
    }

    /// <summary>
    /// �A�C�e�������̃X���b�g�Ɋi�[����
    /// </summary>
    public void AssignItem(ItemData item)
    {
        currentItem = item;

        if (iconImage != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
        }

        Debug.Log($"[{gameObject.name}] �ɃA�C�e�� '{item.itemName}' ���i�[���܂���");
    }

    /// <summary>
    /// �X���b�g����ɂ���
    /// </summary>
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

    /// <summary>
    /// ���݊i�[����Ă���A�C�e�����擾
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