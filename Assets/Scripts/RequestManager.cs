using UnityEngine;
using System.Collections.Generic;

public class RequestManager : MonoBehaviour
{
    [Header("依頼生成設定")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private float minInterval = 10f;
    [SerializeField] private float maxInterval = 30f;
    [SerializeField] private int maxRequests = 4;
    [SerializeField] private List<RequestType> requestTypesPool;

    [Header("依頼管理")]
    [SerializeField] private List<Request> activeRequests = new List<Request>();
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private RequestBoard requestBoard;

    private float nextRequestTime;

    void Start()
    {
        ScheduleNextRequest();
    }

    void Update()
    {
        if (SceneTimer.Instance == null) return;

        float elapsed = SceneTimer.Instance.GetElapsedTime();
        if (elapsed >= nextRequestTime)
        {
            GenerateRequest();
            ScheduleNextRequest();
        }
    }

    void ScheduleNextRequest()
    {
        float interval = Random.Range(minInterval, maxInterval);
        nextRequestTime = SceneTimer.Instance.GetElapsedTime() + interval;
    }

    void GenerateRequest()
    {
        if (activeRequests.Count >= maxRequests) return;

        RequestType type = requestTypesPool[Random.Range(0, requestTypesPool.Count)];
        Request newRequest = ScriptableObject.CreateInstance<Request>();
        newRequest.requestType = type;
        newRequest.isCompleted = false;
        newRequest.rewardAmount = Random.Range(100, 300);

        switch (type)
        {
            case RequestType.DeliverItem:
                var item = itemDatabase.GetRandomItemByType("薬品");
                if (item == null) return;
                newRequest.requestName = $"納品依頼: {item.itemName}";
                newRequest.requiredItem = item;
                break;

            case RequestType.PurifyWeapon:
                var cursed = itemDatabase.GetRandomItemByType("穢れた武器");
                if (cursed == null) return;
                var purified = itemDatabase.GetPurifiedVersion(cursed);
                newRequest.requestName = $"浄化依頼: {cursed.itemName}";
                newRequest.providedItem = cursed;
                newRequest.requiredItem = purified;
                break;

            case RequestType.AddAttribute:
                var baseWeapon = itemDatabase.GetRandomItemByType("武器");
                if (baseWeapon == null) return;
                var enhanced = itemDatabase.GetEnhancedVersion(baseWeapon);
                newRequest.requestName = $"属性付与依頼: {baseWeapon.itemName}";
                newRequest.providedItem = baseWeapon;
                newRequest.requiredItem = enhanced;
                break;

            case RequestType.CraftWeapon:
                var crafted = itemDatabase.GetRandomItemByType("武器");
                if (crafted == null) return;
                newRequest.requestName = $"武器作成依頼: {crafted.itemName}";
                newRequest.requiredItem = crafted;
                break;

            case RequestType.RepairWeapon:
                var broken = itemDatabase.GetRandomItemByType("壊れた武器");
                if (broken == null) return;
                var repaired = itemDatabase.GetRepairedVersion(broken);
                newRequest.requestName = $"修理依頼: {broken.itemName}";
                newRequest.providedItem = broken;
                newRequest.requiredItem = repaired;
                break;
        }

        activeRequests.Add(newRequest);
        requestBoard.DisplayRequests();
    }

    public bool TryDeliverByRequest(Request request)
    {
        if (request == null) return false;

        if (!request.isCompleted)
        {
            request.isCompleted = true;
            moneyManager.AddMoney(request.rewardAmount);
            activeRequests.Remove(request);
            requestBoard.DisplayRequests();
            Debug.Log($"納品成功: {request.requestName} 報酬 {request.rewardAmount} 円");
            return true;
        }
        return false;
    }

    public List<Request> GetActiveRequests()
    {
        return activeRequests;
    }
}