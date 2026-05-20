using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;


/// <summary>
/// 視線先アイテムの検出と取得処理を担当する。
/// 取得時は `InventoryManager` へ追加し、必要に応じて `PlacementSlots` の占有状態を解放する。
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("アイテム取得設定")]
    [SerializeField] private float pickupRange = 10f;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SlotSelector slotSelector; // スロット再選択用


    [Header("UI設定")]
    [SerializeField] private GameObject pickupPromptUI; // 表示用UI（例：Text付きのPanel）
    [SerializeField] private TextMeshProUGUI pickupPromptText;     // アイテム名表示用Text

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
                pickupPromptText.text = $"<sprite name=F> 拾う：{item.ItemData.itemName}";
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
            if (SceneManager.GetActiveScene().name == "tutorial2")
            {
                ConditionalSceneTransition.TriggerTransitionStatic();
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.itemPickupSound);
            }
            // Infinity タグ以外のときのみ、スロット解放＆プレハブ破壊
            if (!currentTargetItem.CompareTag("Infinity"))
			{
				// スロット占有解除：スロットの子から拾った場合は親の PlacementSlots に通知
				var parentSlots = currentTargetItem.transform.GetComponentInParent<PlacementSlots>();
				if (parentSlots != null)
				{
					parentSlots.ClearSlotByTransform(currentTargetItem.transform.parent);
				}
				Destroy(currentTargetItem.gameObject);
			}
            pickupPromptUI.SetActive(false);
            
            // 現在選択中のスロットを再選択して、InventoryManagerの選択状態を更新
            if (slotSelector != null)
            {
                slotSelector.SelectSlot(slotSelector.selectedIndex);
                Debug.Log($"スロット {slotSelector.selectedIndex} を再選択しました");
            }

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
    [Header("�E�A�E�C�E�e�E��E��E�擾�E�ݒ�")]
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
                    Debug.Log($"�E�A�E�C�E�e�E��E� '{item.ItemData.itemName}' �E��E��E�擾�E��E��E�܂��E��E�");
                    Destroy(item.gameObject); // �E��E��E��E��E��E��E�̂ݍ폜
                }
                else
                {
                    Debug.LogWarning("�E�C�E��E��E�x�E��E��E�g�E��E��E��E��E��E��E�t�E�ł��E�B�E�A�E�C�E�e�E��E��E�͎c�E��E�܂�");
                    // �E��E��E��E��E�ł͉��E��E��E��E��E��E��E�A�E�A�E�C�E�e�E��E��E�͂��E�̂܂܎c�E��E�
                }
            }
        }
    }
}
*/
