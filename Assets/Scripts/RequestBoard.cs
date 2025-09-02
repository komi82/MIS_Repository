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
        foreach (Transform child in requestListParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var request in requestManager.GetActiveRequests())
        {
            var ui = Instantiate(requestUIPrefab, requestListParent);
            var text = ui.GetComponentInChildren<Text>();
            text.text = $"{request.requestName} - î[ïi: {request.requiredItem.itemName} - ïÒèV: {request.rewardAmount}â~";
        }
    }
}