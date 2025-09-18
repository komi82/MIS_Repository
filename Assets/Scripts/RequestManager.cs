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
    public static int RequestCompleted = 0;

    void Start()
    {
        RequestCompleted = 0;
        GenerateRequest();
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

            case RequestType.AddAttribute_Fire:
                var baseWeapon_fire = itemDatabase.GetRandomItemByType("無属性武器");
                if (baseWeapon_fire == null) return;
                var enhancedfire = itemDatabase.GetEnhancedFireVersion(baseWeapon_fire);
                newRequest.requestName = $"属性付与依頼: {baseWeapon_fire.itemName}";
                newRequest.providedItem = baseWeapon_fire;
                newRequest.requiredItem = enhancedfire;
                break;

            case RequestType.AddAttribute_Frozen:
                var baseWeapon_frozen = itemDatabase.GetRandomItemByType("無属性武器");
                if (baseWeapon_frozen == null) return;
                var enhancedfrozen = itemDatabase.GetEnhancedFrozenVersion(baseWeapon_frozen);
                newRequest.requestName = $"属性付与依頼: {baseWeapon_frozen.itemName}";
                newRequest.providedItem = baseWeapon_frozen;
                newRequest.requiredItem = enhancedfrozen;
                break;

            case RequestType.AddAttribute_Wind:
                var baseWeapon_wind = itemDatabase.GetRandomItemByType("無属性武器");
                if (baseWeapon_wind == null) return;
                var enhancedwind = itemDatabase.GetEnhancedWindVersion(baseWeapon_wind);
                newRequest.requestName = $"属性付与依頼: {baseWeapon_wind.itemName}";
                newRequest.providedItem = baseWeapon_wind;
                newRequest.requiredItem = enhancedwind;
                break;

            case RequestType.AddAttribute_Bright:
                var baseWeapon_bright = itemDatabase.GetRandomItemByType("無属性武器");
                if (baseWeapon_bright == null) return;
                var enhancedbright = itemDatabase.GetEnhancedBrightVersion(baseWeapon_bright);
                newRequest.requestName = $"属性付与依頼: {baseWeapon_bright.itemName}";
                newRequest.providedItem = baseWeapon_bright;
                newRequest.requiredItem = enhancedbright;
                break;

            case RequestType.AddAttribute_Darkness:
                var baseWeapon_darkness = itemDatabase.GetRandomItemByType("無属性武器");
                if (baseWeapon_darkness == null) return;
                var enhanceddarkness = itemDatabase.GetEnhancedDarknessVersion(baseWeapon_darkness);
                newRequest.requestName = $"属性付与依頼: {baseWeapon_darkness.itemName}";
                newRequest.providedItem = baseWeapon_darkness;
                newRequest.requiredItem = enhanceddarkness;
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
            RequestCompleted++;
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