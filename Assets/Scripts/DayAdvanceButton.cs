using UnityEngine;

/// <summary>
/// アタッチ先ボタン押下で Day を進め、GameClockText の
/// CompleteMoneyThreshold を再計算する。
/// </summary>
public class DayAdvanceButton : MonoBehaviour
{
    [Header("日数管理")]
    [SerializeField] private int day = 1;

    [Header("連携先")]
    [SerializeField] private GameClockText gameClockText;

    /// <summary>
    /// ボタンの OnClick から呼び出す。
    /// Dayを1加算した後、そのDay値で閾値を更新する。
    /// </summary>
    public void OnClickAdvanceDay()
    {
        day += 1;

        if (gameClockText != null)
        {
            gameClockText.UpdateCompleteThresholdByDay(day);
        }
        else
        {
            Debug.LogWarning("DayAdvanceButton: GameClockText が未設定です。");
        }
    }
}
