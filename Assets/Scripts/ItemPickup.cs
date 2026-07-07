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
    [SerializeField] private DeliveryStation deliveryStation;
    [SerializeField] private PutItem putItem; // 作業中判定用


    [Header("UI設定")]
    [SerializeField] private GameObject pickupPromptUI; // 表示用UI（例：Text付きのPanel）
    [SerializeField] private TextMeshProUGUI pickupPromptText;     // アイテム名表示用Text

    private ItemBehaviour currentTargetItem;

    void Update()
    {
        // 作業中はレイキャストを完全にオフにする
        if (putItem != null && putItem.IsCraftingInProgress)
        {
            Debug.Log($"[ItemPickup] 作業中により、レイキャストをオフ。isCraftingInProgress = {putItem.IsCraftingInProgress}");
            currentTargetItem = null;
            if (pickupPromptUI != null)
            {
                pickupPromptUI.SetActive(false);
            }
            return;
        }

        if (putItem == null)
        {
            Debug.LogWarning("[ItemPickup] putItem が設定されていません。Inspector で PutItem を割り当ててください");
        }

        // QTE中や入力が無効化されている場合はレイキャストをオフにする
        if (!this.enabled)
        {
            currentTargetItem = null;
            if (pickupPromptUI != null)
            {
                pickupPromptUI.SetActive(false);
            }
            return;
        }

        if (deliveryStation != null && deliveryStation.CursorActive)
        {
            currentTargetItem = null;
            if (pickupPromptUI != null)
            {
                pickupPromptUI.SetActive(false);
            }
            return;
        }

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
                PromptUIUtility.SetTextAndResizeWidth(
                    pickupPromptText,
                    pickupPromptUI.GetComponent<RectTransform>(),
                    $"<sprite name=F> 拾う：{item.ItemData.itemName}");
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
