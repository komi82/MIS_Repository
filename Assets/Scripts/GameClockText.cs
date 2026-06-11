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
    [SerializeField] private int border = 3;
    [SerializeField] public int borderbaff = 0;
    [SerializeField] public int borderdown = 0;
    [SerializeField] private int completeMoneyThreshold = 10000; // Complete分岐の所持金しきい値
    [SerializeField] private DayAdvanceButton dayAdvanceButton;
    private static bool s_hasCompleteMoneyThreshold;
    private static int s_completeMoneyThreshold;
    public List<BaffItemData> items;

    private bool transitionStarted = false;
    private bool isCompleteTransition = false; // Completeシーンへ遷移するかどうか
    [SerializeField] private DeliveryStation deliveryStation;
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
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }


    void Start()
    {
        borderbaff = GetTotal(BaffEffectType.limitup);
        borderdown = GetTotal(BaffEffectType.borderdown);
        border += borderbaff;//ボーダー増加アイテムの所持数分ボーダーを増加

        transitionPanel.SetActive(false); // UI非表示
        if (completePanel != null)
        {
            completePanel.SetActive(false);
        }
        UpdateClockDisplay();
        UpdateCompleteThresholdDisplay();
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

            // 目標所持金以上ならComplete導線、それ以外は通常導線に分岐
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
            if (deliveryStation != null) deliveryStation.CursorActive = true;
            Cursor.lockState = CursorLockMode.Confined; // マウスカーソル表示
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
        // ここで Day を進めて次回の閾値計算に反映させる。
        if (isCompleteTransition && dayAdvanceButton != null)
        {
            dayAdvanceButton.OnClickAdvanceDay();
        }

        string nextScene = isCompleteTransition ? "Complete" : "result";
        FadeManager.Instance.LoadSceneWithFade(nextScene);
    }

    private void DeadlineCountDown()
    {
        border--;
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
    /// 仕様: completeMoneyThreshold = currentMoney * 0.8 * day
    /// </summary>
    public void UpdateCompleteThresholdByDay(int day)
    {
        if (day < 1) day = 1;
        float scaled = (MoneyManager.currentMoney-borderdown*100) * 0.8f * day;//所持金からボーダーダウンアイテムの所持数×100を減らして計算する
        SetCompleteMoneyThreshold(Mathf.Max(0, Mathf.RoundToInt(scaled)));
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