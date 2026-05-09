using UnityEngine;
using TMPro;

/// <summary>
/// ゲーム進行用のカウンタ表示と、規定件数到達時のシーン遷移を担当する。
/// `RequestManager.RequestCompleted` を参照し、表示値（border - 完了件数）を更新する。
/// </summary>
public class GameClockText : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI completeThresholdText; // completeMoneyThreshold表示用
    [SerializeField] private GameObject transitionPanel; // 遷移用UI
    [SerializeField] private GameObject completePanel; // 目標達成時の遷移用UI
    [SerializeField] private int border = 30;
    [SerializeField] private int completeMoneyThreshold = 10000; // Complete分岐の所持金しきい値

    private bool transitionStarted = false;
    private bool isCompleteTransition = false; // Completeシーンへ遷移するかどうか
    [SerializeField] private DeliveryStation deliveryStation;
    int remainingCount;


    void Start()
    {
        remainingCount = border;
        transitionPanel.SetActive(false); // UI非表示
        if (completePanel != null)
        {
            completePanel.SetActive(false);
        }
        UpdateClockDisplay();
        UpdateCompleteThresholdDisplay();
    }

    void Update()
    {
        if (remainingCount <= 0 && !transitionStarted)
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
    }

    void UpdateClockDisplay()
    {
        remainingCount = border - RequestManager.RequestCompleted;
        clockText.text = $"DeadLine: {remainingCount:N0}";
    }

    void TransitionToNextScene()
    {
        string nextScene = isCompleteTransition ? "Complete" : "result";
        FadeManager.Instance.LoadSceneWithFade(nextScene);
    }

    /// <summary>
    /// Day値に応じて CompleteMoneyThreshold を更新する。
    /// 仕様: completeMoneyThreshold = currentMoney * 0.8 * day
    /// </summary>
    public void UpdateCompleteThresholdByDay(int day)
    {
        if (day < 1) day = 1;
        float scaled = MoneyManager.currentMoney * 0.8f * day;
        completeMoneyThreshold = Mathf.Max(0, Mathf.RoundToInt(scaled));
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