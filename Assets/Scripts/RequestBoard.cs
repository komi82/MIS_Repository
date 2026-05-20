using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RequestBoard : MonoBehaviour
{
    [SerializeField] private RequestManager requestManager;
    [SerializeField] private Transform requestListParent;
    [SerializeField] private GameObject requestUIPrefab;

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

    public static bool playRequestSound = true; // 鳴らしたい時だけtrue

    private void Start()
    {
        DisplayRequests();
    }

    public void DisplayRequests()
    {
        // 既存UIを削除
        foreach (Transform child in requestListParent)
        {
            Destroy(child.gameObject);
        }

        // 依頼を表示するUI作成
        foreach (var request in requestManager.GetActiveRequests())
        {
            var ui = Instantiate(requestUIPrefab, requestListParent);
            var text = ui.GetComponentInChildren<TextMeshProUGUI>();

            string description = "";

            switch (request.requestType)
            {
                case RequestType.DeliverItem:
                    description = $"調合: {request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;

                case RequestType.PurifyWeapon:
                    description = $"浄化:{request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;

                case RequestType.AddAttribute_Fire:
                    description = $"属性付与:{request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;

                case RequestType.AddAttribute_Frozen:
                    description = $"属性付与:{request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;

                case RequestType.AddAttribute_Wind:
                    description = $"属性付与:{request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;

                case RequestType.AddAttribute_Bright:
                    description = $"属性付与:{request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;

                case RequestType.AddAttribute_Darkness:
                    description = $"属性付与:{request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;

                case RequestType.CraftWeapon:
                    description = $"武器作成: {request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;

                case RequestType.RepairWeapon:
                    description = $"修理:{request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;
            }

            text.text = description;
            if (playRequestSound && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.RequestSound);
                playRequestSound = false; // 1回だけ再生
            }
            // 依頼タイプに応じて文字色を変更
            Color textColor = GetColorForRequestType(request.requestType);
            text.color = textColor;
        }
    }

    /// <summary>
    /// 依頼タイプに応じた色を取得
    /// </summary>
    private Color GetColorForRequestType(RequestType requestType)
    {
        switch (requestType)
        {
            case RequestType.DeliverItem:
                return deliverItemColor;
            case RequestType.PurifyWeapon:
                return purifyWeaponColor;
            case RequestType.AddAttribute_Fire:
                return addAttributeFireColor;
            case RequestType.AddAttribute_Frozen:
                return addAttributeFrozenColor;
            case RequestType.AddAttribute_Wind:
                return addAttributeWindColor;
            case RequestType.AddAttribute_Bright:
                return addAttributeBrightColor;
            case RequestType.AddAttribute_Darkness:
                return addAttributeDarknessColor;
            case RequestType.CraftWeapon:
                return craftWeaponColor;
            case RequestType.RepairWeapon:
                return repairWeaponColor;
            default:
                return Color.white;
        }
    }
}

