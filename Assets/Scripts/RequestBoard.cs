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
                    description = $"”[•iˆË—Š: {request.requiredItem.itemName}\n•ñV: {request.rewardAmount}‰~";
                    break;

                case RequestType.PurifyWeapon:
                    description = $"ò‰»ˆË—Š:{request.requiredItem.itemName}\n•ñV: {request.rewardAmount}‰~";
                    break;

                case RequestType.AddAttribute:
                    description = $"‘®«•t—^ˆË—Š:{request.requiredItem.itemName}\n•ñV: {request.rewardAmount}‰~";
                    break;

                case RequestType.CraftWeapon:
                    description = $"•Šíì¬ˆË—Š: {request.requiredItem.itemName}\n•ñV: {request.rewardAmount}‰~";
                    break;

                case RequestType.RepairWeapon:
                    description = $"C—ˆË—Š:{request.requiredItem.itemName}\n•ñV: {request.rewardAmount}‰~";
                    break;
            }

            text.text = description;
        }
    }
}