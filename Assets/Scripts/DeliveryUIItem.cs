using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeliveryUIItem : MonoBehaviour
{
    [Header("UI要素")]
    public TextMeshProUGUI itemNameText;
    public Image itemIcon;
    public TextMeshProUGUI rewardText;
    public Button deliverButton;

    [Header("依頼タイプ別の色設定")]
    public Color deliverItemColor = Color.white;
    public Color purifyWeaponColor = Color.cyan;
    public Color addAttributeFireColor = Color.red;
    public Color addAttributeFrozenColor = Color.blue;
    public Color addAttributeWindColor = Color.green;
    public Color addAttributeBrightColor = Color.yellow;
    public Color addAttributeDarknessColor = Color.magenta;
    public Color craftWeaponColor = Color.white;
    public Color repairWeaponColor = Color.gray;

    // リンクされた依頼とマネージャー
    private Request linkedRequest;
    private RequestManager requestManager;
    private DeliveryUIList parentList; // リスト全体の管理クラス

    /// <summary>
    /// UI要素の初期化
    /// </summary>
    public void Setup(Request request, RequestManager manager, DeliveryUIList list)
    {
        linkedRequest = request;
        requestManager = manager;
        parentList = list;

        itemNameText.text = request.requiredItem.itemName;
        itemIcon.sprite = request.requiredItem.icon;
        
        // 画像の比率を保持するように設定
        if (itemIcon != null)
        {
            itemIcon.preserveAspect = true;
            itemIcon.type = Image.Type.Simple;
        }
        
        rewardText.text = $"{request.rewardAmount} G";

        // 依頼タイプに応じて文字色を変更
        UpdateTextColor(request.requestType);

        // アイテム所持チェック
        bool hasItem = InventoryManager.Instance.HasItem(request.requiredItem);

        // ボタン状態設定
        deliverButton.interactable = hasItem;

        // ボタン色変更（ColorBlockを使う場合）
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
    /// 依頼タイプに応じてテキストの色を更新
    /// </summary>
    private void UpdateTextColor(RequestType requestType)
    {
        Color targetColor = Color.white; // デフォルト色

        switch (requestType)
        {
            case RequestType.DeliverItem:
                targetColor = deliverItemColor;
                break;
            case RequestType.PurifyWeapon:
                targetColor = purifyWeaponColor;
                break;
            case RequestType.AddAttribute_Fire:
                targetColor = addAttributeFireColor;
                break;
            case RequestType.AddAttribute_Frozen:
                targetColor = addAttributeFrozenColor;
                break;
            case RequestType.AddAttribute_Wind:
                targetColor = addAttributeWindColor;
                break;
            case RequestType.AddAttribute_Bright:
                targetColor = addAttributeBrightColor;
                break;
            case RequestType.AddAttribute_Darkness:
                targetColor = addAttributeDarknessColor;
                break;
            case RequestType.CraftWeapon:
                targetColor = craftWeaponColor;
                break;
            case RequestType.RepairWeapon:
                targetColor = repairWeaponColor;
                break;
        }

        // テキストの色を適用
        if (itemNameText != null)
        {
            itemNameText.color = targetColor;
        }
        
        // 報酬テキストにも色を適用
        if (rewardText != null)
        {
            rewardText.color = targetColor;
        }
    }

    /// <summary>
    /// デリバーボタンクリック時の処理
    /// </summary>
    public void OnDeliverClicked()
    {
        RequestBoard.playRequestSound = false; // サウンド抑制
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
                SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.deliverySound);
                InventoryManager.Instance.RemoveItem(slot);
                Debug.Log($"インベントリから '{linkedRequest.requiredItem.itemName}' を削除しました");
            }
            else
            {
                Debug.LogWarning($"デリバー対象アイテム '{linkedRequest.requiredItem.itemName}' がインベントリに見つかりませんでした");
            }

            // UI要素削除
            Destroy(gameObject);

            // リスト全体を更新
            parentList?.RefreshList();
        }
        else
        {
//            Debug.Log($"デリバー失敗: {linkedRequest.requiredItem.itemName}");
        }
    }
}

