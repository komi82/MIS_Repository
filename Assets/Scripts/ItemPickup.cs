using UnityEngine;
using UnityEngine.InputSystem;

public class ItemPickup : MonoBehaviour
{
    [Header("アイテム取得設定")]
    [SerializeField] private float pickupRange = 10f;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Camera mainCamera;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryPickupItem();
        }
    }

    void TryPickupItem()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            ItemBehaviour item = hit.collider.GetComponent<ItemBehaviour>();
            if (item != null)
            {
                bool success = inventoryManager.AddItem(item.ItemData);

                if (success)
                {
                    Debug.Log($"アイテム '{item.ItemData.itemName}' を取得しました");
                    Destroy(item.gameObject); // 成功時のみ削除
                }
                else
                {
                    Debug.LogWarning("インベントリが満杯です。アイテムは残ります");
                    // ここでは何もせず、アイテムはそのまま残す
                }
            }
        }
    }
}
