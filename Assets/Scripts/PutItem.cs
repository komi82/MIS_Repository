using UnityEngine;
using UnityEngine.UI;

public class PutItem : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private SlotSelector slotselector;
    [SerializeField] private ItemPickup itempickup;
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
                    slotselector.SelectSlot(slotselector.selectedIndex);
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