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

        // 表示更新
        itemNameText.text = request.requiredItem.itemName;
        itemIcon.sprite = request.requiredItem.icon; // ItemData に icon(Sprite) がある前提
        rewardText.text = $"{request.rewardAmount} 円";

        // ボタンイベント登録
        deliverButton.onClick.RemoveAllListeners();
        deliverButton.onClick.AddListener(OnDeliverClicked);
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
            // 納品成功 → UIセル削除
            Destroy(gameObject);

            // リスト全体を更新
            if (parentList != null)
            {
                parentList.RefreshList();
            }
        }
        else
        {
            Debug.Log($"納品失敗: {linkedRequest.requiredItem.itemName}");
        }
    }
}