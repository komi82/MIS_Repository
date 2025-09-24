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
        DetectItemInView(); 

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

                pickupPromptUI.SetActive(true);
                pickupPromptText.text = $"[F] 拾う：{item.ItemData.itemName}";
                return;
            }
        }

        // �Ώۂ��Ȃ��ꍇ�͔�\��
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
            // スロット占有解除：スロットの子から拾った場合は親の PlacementSlots に通知
            var parentSlots = currentTargetItem.transform.GetComponentInParent<PlacementSlots>();
            if (parentSlots != null)
            {
                parentSlots.ClearSlotByTransform(currentTargetItem.transform.parent);
            }
            Destroy(currentTargetItem.gameObject);
            pickupPromptUI.SetActive(false);

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
    [Header("�A�C�e���擾�ݒ�")]
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
                    Debug.Log($"�A�C�e�� '{item.ItemData.itemName}' ���擾���܂���");
                    Destroy(item.gameObject); // �������̂ݍ폜
                }
                else
                {
                    Debug.LogWarning("�C���x���g�������t�ł��B�A�C�e���͎c��܂�");
                    // �����ł͉��������A�A�C�e���͂��̂܂܎c��
                }
            }
        }
    }
}
*/
