using System.Collections.Generic;
using UnityEngine;

/// <summary>Frozen results for the ended day. Live run inventory remains available for day 8.</summary>
public static class RunResultState
{
    public static bool HasResult { get; private set; }
    public static bool Cleared { get; private set; }
    public static int Day { get; private set; }
    public static int Money { get; private set; }
    public static int Requests { get; private set; }
    private static bool continued;
    private static Dictionary<int, int> baffCounts = new Dictionary<int, int>();
    private static Dictionary<int, int> artifactCounts = new Dictionary<int, int>();

    public static bool CanContinue => HasResult && Cleared && Day == 7 && !continued;
    public static bool IsClearDay(int day) => day == 7 || day == 14;
    public static int GetBaffCount(int id) => baffCounts.TryGetValue(id, out int count) ? count : 0;
    public static int GetArtifactCount(int id) => artifactCounts.TryGetValue(id, out int count) ? count : 0;
    public static string Heading => Cleared ? (Day == 14 ? "14日間 完全クリア！" : "7日間 クリア！") : "営業終了";
    public static string Message => Cleared
        ? (Day == 14 ? "すべての日程を達成しました！ お疲れさまでした。" : "1週間を達成しました！ このまま14日間クリアに挑戦できます。")
        : "時間切れで今回の営業は終了です。";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        HasResult = Cleared = continued = false;
        Day = Money = Requests = 0;
        baffCounts.Clear();
        artifactCounts.Clear();
    }

    public static void Capture(bool cleared)
    {
        Day = DayAdvanceButton.GetDayStatic();
        Cleared = cleared && IsClearDay(Day);
        Money = MoneyManager.currentMoney;
        Requests = RequestManager.RequestCompleted;
        baffCounts = OwnedProgressManager.CopyBaffCounts();
        artifactCounts = OwnedProgressManager.CopyArtifactCounts();
        continued = false;
        HasResult = true;
    }

    public static bool TryContinue()
    {
        if (!CanContinue) return false;
        continued = true;
        return true;
    }
}
