using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


/// <summary>
/// 依頼の生成・保持・完了処理を統括する管理クラス。
/// `SceneTimer` の経過時間を使って次の依頼生成タイミングを決め、
/// 完了件数を `RequestCompleted` で全体共有する。
/// </summary>
public class RequestManager : MonoBehaviour
{
    [Header("依頼生成設定")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private float minInterval = 10f;
    [SerializeField] private float maxInterval = 30f;
    [SerializeField] private int maxRequests = 4;
    [SerializeField] private List<RequestType> requestTypesPool;

	[Header("依頼対象スポーン設定")]
	[SerializeField] private Transform[] requestSpawnSlots = new Transform[4]; // 4スロットに配置
	[SerializeField] private Transform requestSpawnParent; // スロット未設定時の親（任意）

	// テストとスロットの対応を保持
	private Dictionary<Request, Transform> requestToSlot = new Dictionary<Request, Transform>();

    [Header("依頼管理")]
    [SerializeField] private List<Request> activeRequests = new List<Request>();
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private RequestBoard requestBoard;

    public List<BaffItemData> items;

    public int potionReward = 0;
    public int weaponReward = 0;
    public int cursedReward = 0;

    public static event Action RequestComp;
    private float nextRequestTime;
    public static int RequestCompleted = 0;

    public int GetTotal(BaffEffectType type)
    {
        int total = 0;

        foreach (BaffItemData item in items)
        {
            if (item.effecttype == type)
            {
                total += item.ownedCount;
            }
        }

        return total;
    }

    void Start()
    {
        // シーン遷移で初期化しない仕様
        GenerateRequest();
        ScheduleNextRequest();


    }

    void Update()
    {
        //各種バフアイテムの所持数を合計する
        potionReward = GetTotal(BaffEffectType.potionup);
        weaponReward = GetTotal(BaffEffectType.weaponup);
        cursedReward = GetTotal(BaffEffectType.cursedup);

        if (SceneTimer.Instance == null) return;

        // 依頼がなくなった場合は強制的に1つ生成してタイマーをリセット
        if (activeRequests.Count == 0)
        {
            if (SceneManager.GetActiveScene().name != "tutorial4")
            {       GenerateRequest();
                    ScheduleNextRequest();
                    return;
            }

        }

        float elapsed = SceneTimer.Instance.GetElapsedTime();
        if (elapsed >= nextRequestTime)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.RequestSound);
            }
            GenerateRequest();
            ScheduleNextRequest();
        }
    }

    void ScheduleNextRequest()
    {
        float interval = UnityEngine.Random.Range(minInterval, maxInterval);
        nextRequestTime = SceneTimer.Instance.GetElapsedTime() + interval;
    }

    void GenerateRequest()
    {
        if (activeRequests.Count >= maxRequests) return;

		RequestType type = requestTypesPool[UnityEngine.Random.Range(0, requestTypesPool.Count)];

		// スロット必要タイプ（Deliver/Craft以外）で空きスロットがない場合は生成を停止
		if (type != RequestType.DeliverItem && type != RequestType.CraftWeapon)
		{
			Transform free = FindFreeSpawnSlot();
			if (free == null)
			{
				Debug.Log("Request 生成停止: Request Spawn Slots に空きがありません");
				return;
			}
		}

		Request newRequest = ScriptableObject.CreateInstance<Request>();
        newRequest.requestType = type;
        newRequest.isCompleted = false;
        // rewardAmount計算式: Random(150,200) * 1.1^(n+1) 最小値150以下は切り上げ
        int baseReward = UnityEngine.Random.Range(150, 201);
        float multiplier = Mathf.Pow(1.1f, RequestCompleted + 1);
        newRequest.rewardAmount = Mathf.FloorToInt(baseReward * multiplier);

        switch (type)
        {
            case RequestType.DeliverItem:
                var item = itemDatabase.GetRandomItemByType("薬品");
                if (item == null) return;
                newRequest.requestName = $"デリバー依頼: {item.itemName}";
                newRequest.requiredItem = item;
                newRequest.rewardAmount += Mathf.FloorToInt(potionReward * 50);
                break;

            case RequestType.PurifyWeapon:
                var cursed = itemDatabase.GetRandomItemByType("穢れた武器");
                if (cursed == null) return;
                var purified = itemDatabase.GetPurifiedVersion(cursed);
                if (purified == null)
                {
                    Debug.LogWarning($"浄化依頼: {cursed.itemName} に対応する浄化されたアイテムが見つかりません");
                    return;
                }
                newRequest.requestName = $"浄化依頼: {cursed.itemName} → 浄化された{purified.itemName}";
                newRequest.providedItem = cursed;
                newRequest.requiredItem = purified;
                // 浄化依頼のみ報酬を2倍
                newRequest.rewardAmount += Mathf.FloorToInt(cursedReward * 100);
                newRequest.rewardAmount *= 2;
                break;

            case RequestType.AddAttribute_Fire:
                var baseWeapon_fire = itemDatabase.GetRandomItemByType("基本武器");
                if (baseWeapon_fire == null) return;
                var enhancedfire = itemDatabase.GetEnhancedFireVersion(baseWeapon_fire);
                if (enhancedfire == null)
                {
                    Debug.LogWarning($"炎属性依頼: {baseWeapon_fire.itemName} に対応する炎属性アイテムが見つかりません");
                    return;
                }
                newRequest.requestName = $"炎属性依頼: {baseWeapon_fire.itemName} → 炎の{enhancedfire.itemName}";
                newRequest.providedItem = baseWeapon_fire;
                newRequest.requiredItem = enhancedfire;
                newRequest.rewardAmount += Mathf.FloorToInt(weaponReward * 75);
                break;

            case RequestType.AddAttribute_Frozen:
                var baseWeapon_frozen = itemDatabase.GetRandomItemByType("基本武器");
                if (baseWeapon_frozen == null) return;
                var enhancedfrozen = itemDatabase.GetEnhancedFrozenVersion(baseWeapon_frozen);
                if (enhancedfrozen == null)
                {
                    Debug.LogWarning($"氷属性依頼: {baseWeapon_frozen.itemName} に対応する氷属性アイテムが見つかりません");
                    return;
                }
                newRequest.requestName = $"氷属性依頼: {baseWeapon_frozen.itemName} → 氷の{enhancedfrozen.itemName}";
                newRequest.providedItem = baseWeapon_frozen;
                newRequest.requiredItem = enhancedfrozen;
                newRequest.rewardAmount += Mathf.FloorToInt(weaponReward * 75);
                break;

            case RequestType.AddAttribute_Wind:
                var baseWeapon_wind = itemDatabase.GetRandomItemByType("基本武器");
                if (baseWeapon_wind == null) return;
                var enhancedwind = itemDatabase.GetEnhancedWindVersion(baseWeapon_wind);
                if (enhancedwind == null)
                {
                    Debug.LogWarning($"風属性依頼: {baseWeapon_wind.itemName} に対応する風属性アイテムが見つかりません");
                    return;
                }
                newRequest.requestName = $"風属性依頼: {baseWeapon_wind.itemName} → 風の{enhancedwind.itemName}";
                newRequest.providedItem = baseWeapon_wind;
                newRequest.requiredItem = enhancedwind;
                newRequest.rewardAmount += Mathf.FloorToInt(weaponReward * 75);
                break;

            case RequestType.AddAttribute_Bright:
                var baseWeapon_bright = itemDatabase.GetRandomItemByType("基本武器");
                if (baseWeapon_bright == null) return;
                var enhancedbright = itemDatabase.GetEnhancedBrightVersion(baseWeapon_bright);
                if (enhancedbright == null)
                {
                    Debug.LogWarning($"光属性依頼: {baseWeapon_bright.itemName} に対応する光属性アイテムが見つかりません");
                    return;
                }
                newRequest.requestName = $"光属性依頼: {baseWeapon_bright.itemName} → 光の{enhancedbright.itemName}";
                newRequest.providedItem = baseWeapon_bright;
                newRequest.requiredItem = enhancedbright;
                newRequest.rewardAmount += Mathf.FloorToInt(weaponReward * 75);
                break;

            case RequestType.AddAttribute_Darkness:
                var baseWeapon_darkness = itemDatabase.GetRandomItemByType("基本武器");
                if (baseWeapon_darkness == null) return;
                var enhanceddarkness = itemDatabase.GetEnhancedDarknessVersion(baseWeapon_darkness);
                if (enhanceddarkness == null)
                {
                    Debug.LogWarning($"闇属性依頼: {baseWeapon_darkness.itemName} に対応する闇属性アイテムが見つかりません");
                    return;
                }
                newRequest.requestName = $"闇属性依頼: {baseWeapon_darkness.itemName} → 闇の{enhanceddarkness.itemName}";
                newRequest.providedItem = baseWeapon_darkness;
                newRequest.requiredItem = enhanceddarkness;
                newRequest.rewardAmount += Mathf.FloorToInt(weaponReward * 75);
                break;

            case RequestType.CraftWeapon:
                var crafted = itemDatabase.GetRandomItemByType("武器");
                if (crafted == null) return;
                newRequest.requestName = $"武器作成依頼: {crafted.itemName}";
                newRequest.requiredItem = crafted;
                newRequest.rewardAmount += Mathf.FloorToInt(weaponReward * 75);
                break;

            case RequestType.RepairWeapon:
                var broken = itemDatabase.GetRandomItemByType("壊れた武器");
                if (broken == null) return;
                var repaired = itemDatabase.GetRepairedVersion(broken);
                if (repaired == null)
                {
                    Debug.LogWarning($"修理依頼: {broken.itemName} に対応する修復されたアイテムが見つかりません");
                    return;
                }
                newRequest.requestName = $"修理依頼: {broken.itemName} → 修復した{repaired.itemName}";
                newRequest.providedItem = broken;
                newRequest.requiredItem = repaired;
                newRequest.rewardAmount += Mathf.FloorToInt(weaponReward * 75);
                break;
        }

        activeRequests.Add(newRequest);

		// DeliverItem / CraftWeapon 以外は、作業対象となるアイテムのプレハブをスポーン
		if (type != RequestType.DeliverItem && type != RequestType.CraftWeapon)
		{
			SpawnRequestTarget(newRequest);
		}
        requestBoard.DisplayRequests();
    }

	private void SpawnRequestTarget(Request request)
	{
		if (request == null) return;
		// 提供する材料（providedItem）のみを使用。それ以外はスポーンしない
		ItemData targetItem = request.providedItem;
		if (targetItem == null || targetItem.prefab == null)
		{
			Debug.LogWarning($"providedItem が未設定、またはプレハブが未設定のためスポーンしません: {request.requestName}");
			return;
		}

		// 空きスロット検索（0から3）
		Transform slot = FindFreeSpawnSlot();
		Vector3 spawnPos = slot != null ? slot.position : Vector3.zero;
		Quaternion spawnRot = slot != null ? slot.rotation : Quaternion.identity;
		Transform parent = slot != null ? slot : requestSpawnParent;

		GameObject spawned = Instantiate(targetItem.prefab, spawnPos, spawnRot, parent);
		spawned.name = $"RequestTarget_{targetItem.itemName}";

		if (slot != null)
		{
			requestToSlot[request] = slot;
		}
		Debug.Log($"依頼対象をスポーン: {targetItem.itemName} ({request.requestType})");
	}

	private Transform FindFreeSpawnSlot()
	{
		if (requestSpawnSlots == null || requestSpawnSlots.Length == 0) return null;
		for (int i = 0; i < requestSpawnSlots.Length; i++)
		{
			Transform s = requestSpawnSlots[i];
			if (s == null) continue;
			if (s.childCount == 0) return s; // 子がない場合は空きとみなす
		}
		return null;
	}

    public bool TryDeliverByRequest(Request request)
    {
        if (request == null) return false;

        if (!request.isCompleted)
        {
            request.isCompleted = true;
            moneyManager.AddMoney(request.rewardAmount);
			// デリバー系ではrequestspawnslotsのプレハブは削除しない
			// （デリバーは依頼の完了であり、作業対象の削除ではない）
			// ReleaseSpawnSlot(request); // コメントアウト
            activeRequests.Remove(request);
            requestBoard.DisplayRequests();
            RequestCompleted++;
            RequestComp?.Invoke();
            Debug.Log($"デリバー完了: {request.requestName} 報酬 {request.rewardAmount} 円");
            return true;
        }
        return false;
    }

	private void ReleaseSpawnSlot(Request request)
	{
		if (requestToSlot == null) return;
		if (!requestToSlot.TryGetValue(request, out Transform slot) || slot == null) return;
		for (int i = slot.childCount - 1; i >= 0; i--)
		{
			var child = slot.GetChild(i);
			Destroy(child.gameObject);
		}
		requestToSlot.Remove(request);
	}

    public List<Request> GetActiveRequests()
    {
        return activeRequests;
    }
}
