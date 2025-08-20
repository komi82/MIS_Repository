using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("�Ǘ��Ώۂ̃X���b�g�i4�j")]
    [SerializeField] private InventorySlotUI[] slotUIs;
    public ItemData selectedItem;
    /// <summary>
    /// �A�C�e�����ŏ��̋�X���b�g�ɒǉ�����
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
    /// �w��X���b�g�ɃA�C�e����ǉ��i�C���f�b�N�X�w��j
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
    /// �S�X���b�g���������i��ɂ���j
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
    /// �X���b�g�̏�Ԃ��擾
    /// </summary>
    public InventorySlotUI GetSlot(int index)
    {
        if (index < 0 || index >= slotUIs.Length) return null;
        return slotUIs[index];
    }

    /// <summary>
    /// �C���x���g�������t���ǂ���
    /// </summary>
    public bool IsFull()
    {
        foreach (var slot in slotUIs)
        {
            if (!slot.IsOccupied) return false;
        }
        return true;
    }
    public void SelectItem(ItemData item)
    {
        selectedItem = item;
        Debug.Log("Selected: " + item.itemName);
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