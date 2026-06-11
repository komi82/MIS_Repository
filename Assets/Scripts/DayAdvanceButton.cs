using UnityEngine;
using TMPro;

/// <summary>
/// アタッチ先ボタン押下で Day を進め、GameClockText の
/// CompleteMoneyThreshold を再計算する。
/// </summary>
public class DayAdvanceButton : MonoBehaviour
{
    public static DayAdvanceButton Instance;
    [SerializeField] private TextMeshProUGUI DayText;

    [Header("日数管理")]
    [SerializeField] private int day = 1;
    private static bool s_hasDay;
    private static int s_day;

    [Header("連携先")]
    // 以前のInspector参照が残っている場合のためのフォールバック。
    // GameClockText は別シーンにあるため、基本は GameClockText.Instance を使います。
    [SerializeField] private GameClockText gameClockText;

    private void Awake()
    {
        Instance = this;
        // 初回だけInspector値を採用し、以後は静的値を保持する
        if (!s_hasDay)
        {
            SetDay(day);
        }
        else
        {
            day = s_day;
        }
    }

    public void Updateday()
    {
        DayText.text = $"Day {day}";
    }

    public static void ResetPersistentState()
    {
        s_hasDay = false;
        s_day = 1;
    }

    public int GetDay()
    {
        return s_hasDay ? s_day : day;
    }

    public void SetDay(int value)
    {
        s_day = Mathf.Max(1, value);
        s_hasDay = true;
        day = s_day; // inspector表示も追従させる
    }

    /// <summary>
    /// ボタンの OnClick から呼び出す。
    /// Dayを1加算した後、そのDay値で閾値を更新する。
    /// </summary>
    public void OnClickAdvanceDay()
    {
        if (!s_hasDay) SetDay(day);
        SetDay(s_day + 1);

        // 参照先が別シーンにあってInspectorで設定できない場合でも、
        // 実行時に Singleton から取得して更新します。
        var clock = GameClockText.Instance != null ? GameClockText.Instance : gameClockText;
        if (clock != null)
        {
            clock.UpdateCompleteThresholdByDay(GetDay());
        }
        else
        {
            Debug.LogWarning("DayAdvanceButton: GameClockText が未設定です。");
        }
    }
}
