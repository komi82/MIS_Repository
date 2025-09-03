using UnityEngine;

public class DeliveryUIList : MonoBehaviour
{
    [Header("UI設定")]
    public Transform contentParent;              // DeliveryUIItem を並べる親
    public GameObject deliveryItemPrefab;        // DeliveryUIItem プレハブ
    public RequestManager requestManager;        // 依頼管理

    /// <summary>
    /// リストを再描画
    /// </summary>
    public void RefreshList()
    {
        // 既存セル削除
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 依頼を並べる
        foreach (var request in requestManager.GetActiveRequests()) // ActiveRequests プロパティでもOK
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