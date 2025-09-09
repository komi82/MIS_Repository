using UnityEngine;
using UnityEngine.UI;

public class RequestBoard : MonoBehaviour
{
    [SerializeField] private RequestManager requestManager;
    [SerializeField] private Transform requestListParent;
    [SerializeField] private GameObject requestUIPrefab;

    private void Start()
    {
        DisplayRequests();
    }

    public void DisplayRequests()
    {
        // Šù‘¶UI‚ğƒNƒŠƒA
        foreach (Transform child in requestListParent)
        {
            Destroy(child.gameObject);
        }

        // ˆË—Š‚²‚Æ‚ÉUI¶¬
        foreach (var request in requestManager.GetActiveRequests())
        {
            var ui = Instantiate(requestUIPrefab, requestListParent);
            var text = ui.GetComponentInChildren<Text>();

            string description = "";

            switch (request.requestType)
            {
                case RequestType.DeliverItem:
                    description = $"”[•i: {request.requiredItem.itemName}\n•ñV: {request.rewardAmount}G";
                    break;

                case RequestType.PurifyWeapon:
                    description = $"ò‰»:{request.requiredItem.itemName}\n•ñV: {request.rewardAmount}G";
                    break;

                case RequestType.AddAttribute_Fire:
                    description = $"‘®«•t—^:{request.requiredItem.itemName}\n•ñV: {request.rewardAmount}G";
                    break;

                case RequestType.AddAttribute_Frozen:
                    description = $"‘®«•t—^:{request.requiredItem.itemName}\n•ñV: {request.rewardAmount}G";
                    break;

                case RequestType.AddAttribute_Wind:
                    description = $"‘®«•t—^:{request.requiredItem.itemName}\n•ñV: {request.rewardAmount}G";
                    break;

                case RequestType.AddAttribute_Bright:
                    description = $"‘®«•t—^:{request.requiredItem.itemName}\n•ñV: {request.rewardAmount}G";
                    break;

                case RequestType.AddAttribute_Darkness:
                    description = $"‘®«•t—^:{request.requiredItem.itemName}\n•ñV: {request.rewardAmount}G";
                    break;

                case RequestType.CraftWeapon:
                    description = $"•Šíì¬: {request.requiredItem.itemName}\n•ñV: {request.rewardAmount}G";
                    break;

                case RequestType.RepairWeapon:
                    description = $"C—:{request.requiredItem.itemName}\n•ñV: {request.rewardAmount}G";
                    break;
            }

            text.text = description;
        }
    }
}