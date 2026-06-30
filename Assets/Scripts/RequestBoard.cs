using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RequestBoard : MonoBehaviour
{
    [SerializeField] private RequestManager requestManager;
    [SerializeField] private Transform requestListParent;
    [SerializeField] private GameObject requestUIPrefab;

    private Dictionary<Request, GameObject> requestToUI = new Dictionary<Request, GameObject>();

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

    public static bool playRequestSound = true;

    private void Awake()
    {
        // 最初から置いてある見本UIを消す
        foreach (Transform child in requestListParent)
        {
            Destroy(child.gameObject);
        }

        requestToUI.Clear();
    }

    private void Start()
    {
        DisplayRequests();
    }

    public void DisplayRequests()
    {
        var activeRequests = requestManager.GetActiveRequests();

        // 完了して消えた依頼のUIだけ削除
        foreach (var pair in new List<KeyValuePair<Request, GameObject>>(requestToUI))
        {
            if (!activeRequests.Contains(pair.Key))
            {
                Destroy(pair.Value);
                requestToUI.Remove(pair.Key);
            }
        }

        // 新しく増えた依頼だけ作る
        foreach (var request in activeRequests)
        {
            if (requestToUI.ContainsKey(request))
            {
                continue;
            }

            var ui = Instantiate(requestUIPrefab, requestListParent);
            requestToUI.Add(request, ui);

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
                case RequestType.AddAttribute_Frozen:
                case RequestType.AddAttribute_Wind:
                case RequestType.AddAttribute_Bright:
                case RequestType.AddAttribute_Darkness:
                    description = $"属性付与:{request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;

                case RequestType.CraftWeapon:
                    description = $"武器作成:{request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;

                case RequestType.RepairWeapon:
                    description = $"修理:{request.requiredItem.itemName}\n報酬: {request.rewardAmount}G";
                    break;
            }

            if (text != null)
            {
                text.text = description;
                text.color = GetColorForRequestType(request.requestType);
            }

            Animator animator = ui.GetComponent<Animator>();
            if (animator != null)
            {
                // Rebind to reset animator state, sample once, then play the named state from the start
                animator.Rebind();
                animator.Update(0f);
                animator.Play("TaskSlideIn", 0, 0f);
            }

            if (playRequestSound && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.RequestSound);
                playRequestSound = false;
            }
        }
    }

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