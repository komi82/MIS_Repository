using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private int maxSlots = 4;
    private List<ItemData> inventory = new List<ItemData>();

    public enum ItemStatus
    {
        one, two, three, four, max
    }


    public void AddItem(ItemData item)
    {
        if (inventory.Count >= maxSlots)
        {
            Debug.Log("インベントリが満杯です");
            return;
        }

        inventory.Add(item);
        Debug.Log($"アイテム '{item.itemName}' をインベントリに追加しました");
    }

    public IReadOnlyList<ItemData> GetInventory() => inventory;
}