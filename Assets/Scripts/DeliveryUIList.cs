using UnityEngine;

/// <summary>
/// Delivery UI のリスト全体を生成・更新するコンテナ管理クラス。
/// `RequestManager` の未完了依頼から `DeliveryUIItem` を都度作り直す。
/// </summary>
public class DeliveryUIList : MonoBehaviour
{
    [Header("UI設定")]
    public Transform contentParent;              // DeliveryUIItem を配置する親
    public GameObject deliveryItemPrefab;        // DeliveryUIItem プレハブ
    public RequestManager requestManager;        // 依頼管理

    /// <summary>
    /// リストを再描画
    /// </summary>
    public void RefreshList()
    {
        // 既存子を削除
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 依頼を表示
        foreach (var request in requestManager.GetActiveRequests()) // ActiveRequests プロパティでOK
        {
            if (!request.isCompleted)
            {
                var itemGO = Instantiate(deliveryItemPrefab, contentParent);
                var uiItem = itemGO.GetComponent<DeliveryUIItem>();
                uiItem.Setup(request, requestManager, this);
            }
        }

        // レイアウト再計算（ZigZagLayoutGroup用）
        var layout = contentParent.GetComponent<ZigZagLayoutGroup>();
        if (layout != null)
        {
            layout.SetLayoutHorizontal();
            layout.SetLayoutVertical();
        }
    }
}

