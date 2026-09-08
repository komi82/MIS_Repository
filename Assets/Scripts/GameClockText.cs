using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム進行用のカウンタ表示と、時間切れ時のシーン遷移を担当する。
/// </summary>
public class GameClockText : MonoBehaviour
{
    // 他シーンにある可能性がある連携先向け: Singleton参照。
    // DontDestroyOnLoad するかどうかは、必要になってから切り替えてください。
    public static GameClockText Instance { get; private set; }

    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI completeThresholdText; // completeMoneyThreshold表示用
    [SerializeField] private GameObject transitionPanel; // 遷移用UI
    [SerializeField] private GameObject completePanel; // 目標達成時の遷移用UI

    [Header("時間制限")]
    private static bool s_hasRemainingTime;
    private static float s_remainingTime;
    private static float s_nextRoundCarrySeconds;
    private static float s_rewardOverflowBonusX;

    [SerializeField] public int borderdown = 0;
    [SerializeField] public int limitup = 0;
    [SerializeField] private int completeMoneyThreshold = 10000; // Complete分岐の所持金しきい値
    [SerializeField] private DayAdvanceButton dayAdvanceButton;
    private static bool s_hasCompleteMoneyThreshold;
    private static int s_completeMoneyThreshold;

    [Header("目標金額設定 (日ごと)")]
    [SerializeField] private int[] dailyThresholds = new int[7] { 1000, 2000, 4000, 6000, 8000, 10000, 15000 };

    [Header("納品件数制限")]
    [SerializeField] private int maxDeliveriesPerDay = 3;
    private static int s_deliveriesCount;
    private static int s_deliveriesCountDay = -1;

    public List<BaffItemData> items;

    private bool transitionStarted = false;
    private bool isCompleteTransition = false; // Completeシーンへ遷移するかどうか
    [SerializeField] private DeliveryStation deliveryStation;
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private Transform arcadeResetPoint;
    private int defaultCompleteMoneyThreshold;

    private void Awake()
    {
        // もし複数生成された場合は後勝ちではなく、既存を優先して破棄する。
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        defaultCompleteMoneyThreshold = completeMoneyThreshold;

        if (deliveryStation == null)
        {
            deliveryStation = FindFirstObjectByType<DeliveryStation>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FirstPersonController>();
        }


        // シーンを跨いで保持（初回だけInspector値を採用）
        if (!s_hasCompleteMoneyThreshold)
        {
            s_completeMoneyThreshold = completeMoneyThreshold;
            s_hasCompleteMoneyThreshold = true;
        }
        else
        {
            completeMoneyThreshold = s_completeMoneyThreshold;
        }
    }

    public static void ResetPersistentState()
    {
        s_hasCompleteMoneyThreshold = false;
        s_completeMoneyThreshold = 0;
        s_hasRemainingTime = false;
        s_remainingTime = 0f;
        s_nextRoundCarrySeconds = 0f;
        s_rewardOverflowBonusX = 0f;
        s_deliveriesCount = 0;
        s_deliveriesCountDay = -1;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }


    void Start()
    {
        // OwnedProgressManager から各アイテムの所持数を同期する
        if (items != null)
        {
            foreach (var item in items)
            {
                if (item != null)
                {
                    item.ownedCount = OwnedProgressManager.GetBaffOwned(item.B_itemID);
                }
            }
        }

        limitup = GetTotal(BaffEffectType.limitup);
        borderdown = GetTotal(BaffEffectType.borderdown);

        maxDeliveriesPerDay = 3 + limitup;

        // 時間制限を削除したため、残り時間の初期化は不要
        // if (!s_hasRemainingTime || s_remainingTime <= 0f)
        // {
        //     s_remainingTime = Mathf.Max(1f, roundTimeSeconds + s_nextRoundCarrySeconds);
        //     s_nextRoundCarrySeconds = 0f;
        //     s_hasRemainingTime = true;
        // }

        if (transitionPanel != null) transitionPanel.SetActive(false); // UI非表示
        if (completePanel != null)
        {
            completePanel.SetActive(false);
        }
        int currentDay = DayAdvanceButton.Instance != null ? DayAdvanceButton.Instance.GetDay() : 1;
        UpdateCompleteThresholdByDay(currentDay);
        UpdateClockDisplay();
    }

    void Update()
    {
        if (!transitionStarted)
        {
            if (MoneyManager.currentMoney >= completeMoneyThreshold)
            {
                BeginRoundEnd(true);
                return;
            }
        }

        UpdateClockDisplay();
        UpdateCompleteThresholdDisplay();
        if (DayAdvanceButton.Instance != null)
        {
            DayAdvanceButton.Instance.Updateday();
        }
    }

    void UpdateClockDisplay()
    {
        if (clockText == null) return;
        int remaining = GetRemainingDeliveries();
        clockText.text = $"Deadline: {remaining}";
    }

    private void BeginRoundEnd(bool completedByThreshold)
    {
        transitionStarted = true;
        isCompleteTransition = completedByThreshold;

        if (isCompleteTransition)
        {
            ApplyCarryoverBonus();
            if (completePanel != null) completePanel.SetActive(true);
            if (transitionPanel != null) transitionPanel.SetActive(false);

            StartCoroutine(HandleCompleteDayProgression());
            return;
        }

        s_nextRoundCarrySeconds = 0f;
        if (transitionPanel != null) transitionPanel.SetActive(true);
        if (completePanel != null) completePanel.SetActive(false);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.timeupSound);
        }
        s_remainingTime = 0f;
        s_hasRemainingTime = false;
        BlockGameplayInput();
        Invoke(nameof(TransitionToNextScene), 1f); //1秒後にシーン遷移
    }

    private IEnumerator HandleCompleteDayProgression()
    {
        var disabledBehaviours = new List<Behaviour>();
        if (deliveryStation != null)
        {
            deliveryStation.ForceCloseUI();
        }
        GameplayInputUtility.DisableStandardInput(playerController, deliveryStation, disabledBehaviours);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // completePanel を表示
        if (completePanel != null) completePanel.SetActive(true);
        if (transitionPanel != null) transitionPanel.SetActive(false);

        // 1秒待機
        yield return new WaitForSeconds(1f);

        // フェードアウト
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeOutOnly();
            yield return new WaitForSeconds(FadeManager.Instance.fadeTime);
        }

        // Day 更新
        DayAdvanceButton targetDayButton = dayAdvanceButton != null ? dayAdvanceButton : DayAdvanceButton.Instance;
        if (targetDayButton != null)
        {
            targetDayButton.OnClickAdvanceDay();
        }
        
        if (deliveryStation != null)
        {
            deliveryStation.ForceCloseUI();
        }
        if (completePanel != null) completePanel.SetActive(false);
        if (transitionPanel != null) transitionPanel.SetActive(false);

        isCompleteTransition = false;
        transitionStarted = false;

        // フェードアウト状態のまま Shop へ遷移（フェード処理なし）
        TransitionToShopWithoutFade();
    }

    private void ApplyCarryoverBonus()
    {
        // 時間制限を削除したため、キャリーオーバー処理は不要
        // float carrySource = Mathf.Max(0f, s_remainingTime) * 0.5f;
        // float carryCap = Mathf.Max(0f, carryoverTimeCapSeconds);
        // float acceptedCarry = Mathf.Min(carrySource, carryCap);
        // float overflow = Mathf.Max(0f, carrySource - carryCap);
        //
        // s_nextRoundCarrySeconds = acceptedCarry;
        // s_rewardOverflowBonusX += overflow;
    }

    void TransitionToNextScene()
    {
        string nextScene = SceneNames.Result;

        if (IsMenuScene(nextScene))
        {
            ActivateCursorForMenuScene();
        }

        FadeManager.Instance.LoadSceneWithFade(nextScene);
    }

    private void TransitionToShop()
    {
        Debug.Log("[GameClockText] Shop へ遷移します");
        if (ChangeScene.Instance != null)
        {
            ChangeScene.Instance.GoToShop();
        }
        else if (FadeManager.Instance != null)
        {
            FadeManager.Instance.LoadSceneWithFade(SceneNames.Shop);
        }
    }

    private void TransitionToShopWithoutFade()
    {
        Debug.Log("[GameClockText] Shop へ遷移します（フェード状態のまま）");
        SceneManager.LoadScene(SceneNames.Shop);
    }

    static bool IsMenuScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        string lower = sceneName.ToLowerInvariant();
        return lower == SceneNames.Result;
    }

    static void ActivateCursorForMenuScene()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    /// <summary>
    /// ConditionalSceneTransition と同様に、キー・マウス操作をすべて無効化する。
    /// </summary>
    void BlockGameplayInput()
    {
        if (deliveryStation != null)
        {
            deliveryStation.CursorActive = false;
        }

        GameplayInputUtility.DisableStandardInput(playerController, deliveryStation);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public int GetCompleteMoneyThreshold()
    {
        return s_hasCompleteMoneyThreshold ? s_completeMoneyThreshold : completeMoneyThreshold;
    }

    public void SetCompleteMoneyThreshold(int value)
    {
        s_completeMoneyThreshold = Mathf.Max(0, value);
        s_hasCompleteMoneyThreshold = true;
        completeMoneyThreshold = s_completeMoneyThreshold; // inspector表示も追従
        UpdateCompleteThresholdDisplay();
    }

    public void ResetCompleteMoneyThresholdToDefault()
    {
        SetCompleteMoneyThreshold(defaultCompleteMoneyThreshold);
    }

    /// <summary>
    /// Day値に応じて CompleteMoneyThreshold を更新する。
    /// 仕様: インスペクターで指定した要素の最高点に達したら、その値の1.1倍を次の要素として自動生成。
    /// 以降も自動生成が続く場合、前の値にさらに1.1倍を乗じて計算。
    /// </summary>
    public void UpdateCompleteThresholdByDay(int day)
    {
        if (day < 1) day = 1;

        int baseThreshold = 0;
        if (dailyThresholds != null && dailyThresholds.Length > 0)
        {
            if (day <= dailyThresholds.Length)
            {
                int index = day - 1;
                baseThreshold = dailyThresholds[index];
            }
            else
            {
                // インスペクターで指定した最後の要素を基準に、1.1倍ずつ増加
                int lastThreshold = dailyThresholds[dailyThresholds.Length - 1];
                int autoGeneratedCount = day - dailyThresholds.Length;
                
                // lastThreshold × (1.1)^autoGeneratedCount で計算
                baseThreshold = Mathf.RoundToInt(lastThreshold * Mathf.Pow(1.1f, autoGeneratedCount));
            }
        }
        else
        {
            baseThreshold = defaultCompleteMoneyThreshold * day;
        }

        // ボーダーダウンアイテムの効果（所持数 × 100G 緩和）を適用
        int finalThreshold = baseThreshold - (borderdown * 100);

        SetCompleteMoneyThreshold(Mathf.Max(0, finalThreshold));
        UpdateCompleteThresholdDisplay();
    }

    /// <summary>
    /// 現在の completeMoneyThreshold をUIへ表示する。
    /// </summary>
    private void UpdateCompleteThresholdDisplay()
    {
        if (completeThresholdText == null) return;
        completeThresholdText.text = $"Goal: {completeMoneyThreshold:N0}G";

    }
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

    public static float GetRewardOverflowBonusX()
    {
        return Mathf.Max(0f, s_rewardOverflowBonusX);
    }

    /// <summary>
    /// 納品完了時に呼び出す。制限チェック付き。
    /// </summary>
    public bool TryAddDelivery()
    {
        int currentDay = DayAdvanceButton.Instance != null ? DayAdvanceButton.Instance.GetDay() : 1;
        
        // 日が変わったらカウントをリセット
        if (currentDay != s_deliveriesCountDay)
        {
            s_deliveriesCount = 0;
            s_deliveriesCountDay = currentDay;
        }

        if (s_deliveriesCount >= maxDeliveriesPerDay)
        {
            Debug.LogWarning($"[GameClockText] 本日の納品上限に達しています: {s_deliveriesCount}/{maxDeliveriesPerDay}");
            return false;
        }

        s_deliveriesCount++;
        Debug.Log($"[GameClockText] 納品完了: {s_deliveriesCount}/{maxDeliveriesPerDay}");
        return true;
    }

    /// <summary>
    /// 本日の残り納品回数を取得
    /// </summary>
    public int GetRemainingDeliveries()
    {
        int currentDay = DayAdvanceButton.Instance != null ? DayAdvanceButton.Instance.GetDay() : 1;
        
        if (currentDay != s_deliveriesCountDay)
        {
            return maxDeliveriesPerDay;
        }

        return Mathf.Max(0, maxDeliveriesPerDay - s_deliveriesCount);
    }

    /// <summary>
    /// 本日の納品回数が上限に達しているか判定
    /// </summary>
    public bool IsDeliveryLimitReached()
    {
        int currentDay = DayAdvanceButton.Instance != null ? DayAdvanceButton.Instance.GetDay() : 1;
        
        if (currentDay != s_deliveriesCountDay)
        {
            return false;
        }

        return s_deliveriesCount >= maxDeliveriesPerDay;
    }
}