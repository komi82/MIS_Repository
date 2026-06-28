using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// ゲーム進行用のカウンタ表示と、規定件数到達時のシーン遷移を担当する。
/// `RequestManager.RequestCompleted` を参照し、表示値（border - 完了件数）を更新する。
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
    public int border = 3;
    private static int s_border;
    private static bool s_hasBorder;

    [SerializeField] public int borderbaff = 0;
    [SerializeField] public int borderdown = 0;
    [SerializeField] private int completeMoneyThreshold = 10000; // Complete分岐の所持金しきい値
    [SerializeField] private DayAdvanceButton dayAdvanceButton;
    private static bool s_hasCompleteMoneyThreshold;
    private static int s_completeMoneyThreshold;

    [Header("目標金額設定 (日ごと)")]
    [SerializeField] private int[] dailyThresholds = new int[7] { 1000, 2000, 4000, 6000, 8000, 10000, 15000 };

    public List<BaffItemData> items;

    private bool transitionStarted = false;
    private bool isCompleteTransition = false; // Completeシーンへ遷移するかどうか
    [SerializeField] private DeliveryStation deliveryStation;
    [SerializeField] private FirstPersonController playerController;
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
        s_hasBorder = false;
        s_border = 0;
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

        borderbaff = GetTotal(BaffEffectType.limitup);
        borderdown = GetTotal(BaffEffectType.borderdown);

        // シーンを跨いで border を保持
        // ただし、s_border が 0 以下の場合は「期限切れ」状態なので、
        // ショップから戻ってきた際などは初期値（Inspectorのborder + バフ）にリセットする必要がある
        if (!s_hasBorder || s_border <= 0)
        {
            // 初期値にリセット（Inspectorの値をベースにする）
            // border は Inspector で設定された初期値が入っている状態
            border += borderbaff;
            s_border = border;
            s_hasBorder = true;
        }
        else
        {
            // 途中経過（セーブデータ復元など）がある場合はそちらを採用
            border = s_border;
        }

        transitionPanel.SetActive(false); // UI非表示
        if (completePanel != null)
        {
            completePanel.SetActive(false);
        }
        int currentDay = DayAdvanceButton.Instance != null ? DayAdvanceButton.Instance.GetDay() : 1;
        UpdateCompleteThresholdByDay(currentDay);
        UpdateClockDisplay();
    }

    private void OnEnable()
    {
        RequestManager.RequestComp += DeadlineCountDown;
    }

    private void OnDisable()
    {
        RequestManager.RequestComp -= DeadlineCountDown;
    }

    void Update()
    {
        if (border <= 0 && !transitionStarted)
        {
            transitionStarted = true;

            // 目標所持金以上ならShop導線、それ以外は通常導線に分岐

            isCompleteTransition = MoneyManager.currentMoney >= completeMoneyThreshold;
            if (isCompleteTransition)
            {
                if (completePanel != null) completePanel.SetActive(true);
                if (transitionPanel != null) transitionPanel.SetActive(false);
            }
            else
            {
                if (transitionPanel != null) transitionPanel.SetActive(true);
                if (completePanel != null) completePanel.SetActive(false);
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.timeupSound);
            }
            BlockGameplayInput();

            Invoke(nameof(TransitionToNextScene), 1f); //1秒後にシーン遷移
        }

        UpdateClockDisplay();
        UpdateCompleteThresholdDisplay();
        DayAdvanceButton.Instance.Updateday();
    }

    void UpdateClockDisplay()
    {
        clockText.text = $"DeadLine: {border:N0}";
    }

    void TransitionToNextScene()
    {
        // arcadeシーン内で DayAdvanceButton を参照できる想定。
        // 所持金しきい値達成時は Day を進めて次回の閾値計算に反映させる。

        if (isCompleteTransition && dayAdvanceButton != null)
        {
            dayAdvanceButton.OnClickAdvanceDay();
        }

        int currentDay = dayAdvanceButton != null
            ? dayAdvanceButton.GetDay()
            : 1;

        string nextScene;
        if (currentDay >= DayAdvanceButton.ResultDayThreshold)
        {
            nextScene = SceneNames.Result;
        }
        else if (isCompleteTransition)
        {
            nextScene = SceneNames.Shop;
        }
        else
        {
            nextScene = SceneNames.Result;
        }

        if (IsMenuScene(nextScene))
        {
            ActivateCursorForMenuScene();
        }

        FadeManager.Instance.LoadSceneWithFade(nextScene);
    }

    static bool IsMenuScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        string lower = sceneName.ToLowerInvariant();
        return lower == SceneNames.Shop.ToLowerInvariant() || lower == SceneNames.Result;
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

    private void DeadlineCountDown()
    {
        border--;
        s_border = border;
    }

    public void ResetBorderToDefault()
    {
        transitionStarted = false;
        UpdateClockDisplay();
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
    /// 仕様: インスペクターの固定値 dailyThresholds[day - 1] からボーダーダウンバフを引いて計算
    /// </summary>
    public void UpdateCompleteThresholdByDay(int day)
    {
        if (day < 1) day = 1;

        int baseThreshold = 0;
        if (dailyThresholds != null && dailyThresholds.Length > 0)
        {
            int index = Mathf.Clamp(day - 1, 0, dailyThresholds.Length - 1);
            baseThreshold = dailyThresholds[index];
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
        completeThresholdText.text = $"Border: {completeMoneyThreshold:N0}G";

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
}