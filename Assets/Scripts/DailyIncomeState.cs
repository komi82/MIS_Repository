// Run-local state, independent of scene lifetime. Reset with OwnedProgressManager.
public static class DailyIncomeState
{
    public const int ItemId = 9;
    public const int Reward = 50;
    private static int lastEnteredDay = -1;
    private static int lastRewardDay = -1;

    public static void Reset()
    {
        lastEnteredDay = -1;
        lastRewardDay = -1;
    }

    public static bool TryClaim(int day, int ownedCount)
    {
        // Mark even an unowned day: buying/adding mid-day cannot pay retroactively.
        if (day <= lastEnteredDay) return false;
        lastEnteredDay = day;
        if (day <= 1 || ownedCount <= 0) return false;
        lastRewardDay = day;
        return true;
    }

    public static bool RequiresDelivery(int day, int deliveriesToday)
    {
        return day == lastRewardDay && deliveriesToday < 1;
    }
}