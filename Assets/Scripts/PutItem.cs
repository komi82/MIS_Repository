using UnityEngine;

public class PutItem : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            RemoveSelectedItem();
        }
    }

    void RemoveSelectedItem()
    {
        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManagerが未設定です");
            return;
        }
        if (inventoryManager.selectedItem == null)
        {
            Debug.LogWarning("selectedItem が未設定です");
            return;
        }

        InventorySlotUI slot = inventoryManager.FindSlotByItem(inventoryManager.selectedItem);
        if (slot == null)
        {
            Debug.LogWarning("選択されたアイテムに対応するスロットが見つかりません");
            return;
        }

        inventoryManager.RemoveItem(slot);
        Debug.Log($"アイテム '{inventoryManager.selectedItem.itemName}' を削除しました");

        inventoryManager.selectedItem = null; // 選択状態も解除
    }
}


/*using UnityEngine;

public class PutItem : MonoBehaviour
{
    [Header("アイテム情報")]
    [SerializeField] private float dropDistance = 2f;
    [SerializeField] private ItemData ItemData;
    [SerializeField] private InventoryManager InventoryManager;
    [SerializeField] private Camera mainCamera;
    
    private InventorySlotUI InventorySlotUI;


    // Update is called once per frame
    void Update()
    {
     if (Input.GetKeyDown(KeyCode.E))
        {
        //    PutItems();
        }
        
    }*/

/*  void PutItems()
  {
       ItemData prefab = InventoryManager.selectedItem;
      if (ItemData == null || InventoryManager == null) return;

      Vector3 dropPosition = mainCamera.transform.position + mainCamera.transform.forward * dropDistance;
      Instantiate(ItemData.prefab, transform.position, Quaternion.identity);
      InventorySlotUI slot = InventoryManager.FindSlotByItem(InventoryManager.selectedItem);
      if (slot != null)
      {
          InventoryManager.RemoveItem(slot);
      }
      else
      {
          Debug.LogWarning("選択されたアイテムに対応するスロットが見つかりませんでした");
      }
  } 
} */
