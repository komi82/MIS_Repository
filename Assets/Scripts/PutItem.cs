using UnityEngine;
using UnityEngine.UI;

public class PutItem : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private float pickupRange = 10f;
    [SerializeField] private Camera mainCamera;
    [Header("アイテム設置")]
    [SerializeField] private float placementOffset = 0.5f; // 任意の高さ

    [Header("UI設定")]
    [SerializeField] private GameObject pickupPromptUI; // 表示用UI（例：Text付きのPanel）
    [SerializeField] private Text pickupPromptText;     // アイテム名表示用Text
    void Update()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            GameObject targetObject = hit.collider.gameObject;

            if (targetObject.CompareTag("craft"))
            {
                  if (Input.GetKeyDown(KeyCode.E))
                   {
                        PutSelectedItem();
                   }
            }
        }

    }


    void PutSelectedItem()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            GameObject targetObject = hit.collider.gameObject;

            if (targetObject.CompareTag("craft"))
            {
                ItemData itemToPlace = inventoryManager.selectedItem;
                if (itemToPlace != null)
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

                    if (itemToPlace.prefab != null)
                    {
                        // "craft" オブジェクトのローカル上方向に offset だけずらして配置
                        Vector3 spawnPosition = hit.point + targetObject.transform.up * placementOffset;

                        Instantiate(itemToPlace.prefab, spawnPosition, Quaternion.identity);

                        Debug.Log($"アイテム '{itemToPlace.itemName}' を 'craft' オブジェクト上に配置しました");
                        inventoryManager.RemoveItem(slot);

                        inventoryManager.selectedItem = null; // 選択状態も解除
                    }
                    else
                    {
                        Debug.LogWarning("プレハブが設定されていません。配置できませんでした");
                    }
                }
                else
                {
                    Debug.LogWarning("選択中のアイテムがありません");
                }
            }
        }
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
