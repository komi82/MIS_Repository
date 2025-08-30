using UnityEngine;
using UnityEngine.UI;

public class ItemPickup : MonoBehaviour
{
    [Header("アイテム取得設定")]
    [SerializeField] private float pickupRange = 10f;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Camera mainCamera;


    [Header("UI設定")]
    [SerializeField] private GameObject pickupPromptUI; // 表示用UI（例：Text付きのPanel）
    [SerializeField] private Text pickupPromptText;     // アイテム名表示用Text

    private ItemBehaviour currentTargetItem;

    void Update()
    {
        DetectItemInView(); // 毎フレームレイキャスト

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryPickupItem();
        }
    }

    void DetectItemInView()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            ItemBehaviour item = hit.collider.GetComponent<ItemBehaviour>();
            if (item != null)
            {
                currentTargetItem = item;

                // UI表示とテキスト更新
                pickupPromptUI.SetActive(true);
                pickupPromptText.text = $"[F] 拾う：{item.ItemData.itemName}";
                return;
            }
        }

        // 対象がない場合は非表示
        currentTargetItem = null;
        pickupPromptUI.SetActive(false);
    }

    void TryPickupItem()
    {
        if (currentTargetItem == null) return;

        bool success = inventoryManager.AddItem(currentTargetItem.ItemData);

        if (success)
        {
            Debug.Log($"アイテム '{currentTargetItem.ItemData.itemName}' を取得しました");
            Destroy(currentTargetItem.gameObject);
            pickupPromptUI.SetActive(false); // UIも非表示に

        }
        else
        {
            Debug.LogWarning("インベントリが満杯です。アイテムは残ります");
        }

        currentTargetItem = null;
    }



}





/*
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
*/
