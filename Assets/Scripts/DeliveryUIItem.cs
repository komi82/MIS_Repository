using UnityEngine;
using UnityEngine.UI;

public class DeliveryUIItem : MonoBehaviour
{
    [Header("UI参照")]
    public Text itemNameText;
    public Image itemIcon;
    public Text rewardText;
    public Button deliverButton;

    // 紐づく依頼とマネージャー
    private Request linkedRequest;
    private RequestManager requestManager;
    private DeliveryUIList parentList; // リスト全体の管理クラス

    /// <summary>
    /// UIセルの初期化
    /// </summary>
    public void Setup(Request request, RequestManager manager, DeliveryUIList list)
    {
        linkedRequest = request;
        requestManager = manager;
        parentList = list;

        itemNameText.text = request.requiredItem.itemName;
        itemIcon.sprite = request.requiredItem.icon;
        rewardText.text = $"{request.rewardAmount} G";

        // 所持判定
        bool hasItem = InventoryManager.Instance.HasItem(request.requiredItem);

        // ボタン状態制御
        deliverButton.interactable = hasItem;

        // 半透明化（ColorBlockを使う場合）
        var colors = deliverButton.colors;
        colors.normalColor = hasItem ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        deliverButton.colors = colors;

        // イベント登録
        deliverButton.onClick.RemoveAllListeners();
        if (hasItem)
        {
            deliverButton.onClick.AddListener(OnDeliverClicked);
        }
    }

    /// <summary>
    /// 納品ボタン押下時の処理
    /// </summary>
    public void OnDeliverClicked()
    {
        if (linkedRequest == null || requestManager == null)
        {
            Debug.LogWarning("DeliveryUIItem: Request または Manager が未設定です。");
            return;
        }

        if (requestManager.TryDeliverByRequest(linkedRequest))
        {
            // インベントリからアイテムを削除
            var slot = InventoryManager.Instance.FindSlotByItem(linkedRequest.requiredItem);
            if (slot != null)
            {
                InventoryManager.Instance.RemoveItem(slot);
                Debug.Log($"インベントリから '{linkedRequest.requiredItem.itemName}' を削除しました");
            }
            else
            {
                Debug.LogWarning($"納品対象アイテム '{linkedRequest.requiredItem.itemName}' がインベントリに見つかりませんでした");
            }

            // UIセル削除
            Destroy(gameObject);

            // リスト全体を更新
            parentList?.RefreshList();
        }
        else
        {
//            Debug.Log($"納品失敗: {linkedRequest.requiredItem.itemName}");
        }
    }
}