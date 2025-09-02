using UnityEngine;
using System.Collections.Generic;

public class RequestManager : MonoBehaviour
{
    [Header("依頼生成設定")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private string targetItemType = "薬品"; // 抽選対象のタイプ
    [SerializeField] private float minInterval = 10f;
    [SerializeField] private float maxInterval = 30f;

    [Header("依頼管理")]
    [SerializeField] private List<Request> activeRequests = new List<Request>();
    [SerializeField] private MoneyManager moneyManager;

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
            GenerateRequests();
            ScheduleNextRequest();
        }
    }

    void ScheduleNextRequest()
    {
        float interval = Random.Range(minInterval, maxInterval);
        nextRequestTime = SceneTimer.Instance.GetElapsedTime() + interval;
    }

    void GenerateRequests()
    {
        List<ItemData> candidates = itemDatabase.GetItemsByType(targetItemType);
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"指定タイプ '{targetItemType}' のアイテムが見つかりません");
            return;
        }

        int count = Random.Range(1, 3); // 1〜2件生成
        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            int index = Random.Range(0, candidates.Count);
            ItemData item = candidates[index];
            candidates.RemoveAt(index);

            Request newRequest = ScriptableObject.CreateInstance<Request>();
            newRequest.requestName = $"納品依頼: {item.itemName}";
            newRequest.requiredItem = item;
            newRequest.rewardAmount = Random.Range(100, 300);
            newRequest.isCompleted = false;

            activeRequests.Add(newRequest);
            Debug.Log($"依頼生成: {newRequest.requestName}");
        }
    }

    public bool TryDeliver(ItemData item)
    {
        foreach (var request in activeRequests)
        {
            if (!request.isCompleted && request.requiredItem == item)
            {
                request.isCompleted = true;
                moneyManager.AddMoney(request.rewardAmount);
                Debug.Log($"納品成功: {request.requestName} に {item.itemName} を納品。報酬 {request.rewardAmount} 円獲得。");
                return true;
            }
        }

        Debug.Log($"納品失敗: {item.itemName} は現在の依頼に該当しません。");
        return false;
    }

    public List<Request> GetActiveRequests()
    {
        return activeRequests;
    }
}