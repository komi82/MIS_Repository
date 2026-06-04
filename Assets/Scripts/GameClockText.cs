using UnityEngine;
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
    [SerializeField] private int completeMoneyThreshold = 10000; // Complete分岐の所持金しきい値
    [SerializeField] private DayAdvanceButton dayAdvanceButton;
    private static bool s_hasCompleteMoneyThreshold;
    private static int s_completeMoneyThreshold;

    private bool transitionStarted = false;
    private bool isCompleteTransition = false; // 所持金しきい値達成時（Shop遷移）かどうか
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
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }


    void Start()
    {
        border = 3;
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
            nextScene = "result";
        }
        else if (isCompleteTransition)
        {
            nextScene = "Shop";
        }
        else
        {
            nextScene = "result";
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
        return lower == "shop" || lower == "result";
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
            deliveryStation.enabled = false;
        }

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        DisableInputBehaviour(FindFirstObjectByType<ItemPickup>());
        DisableInputBehaviour(FindFirstObjectByType<SlotSelector>());
        DisableInputBehaviour(FindFirstObjectByType<PutItem>());
        DisableInputBehaviour(FindFirstObjectByType<RecipeStation>());

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    static void DisableInputBehaviour(Behaviour behaviour)
    {
        if (behaviour != null)
        {
            behaviour.enabled = false;
        }
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
        float scaled = completeMoneyThreshold * day * 0.7f;
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
}