using System;

/// <summary>Prices follow cleared days and persistent ownership, rather than resetting each visit.</summary>
public static class ShopPricing
{
    // The outgoing round keeps its goal until the next arcade scene starts.
    // Follow the authored daily goals, including later days that do not double.
    public static int ForCurrentVisit(int basePrice, int ownedCount) =>
        Calculate(basePrice, GameClockText.CurrentDayBaseGoal, ownedCount);

    public static int Calculate(int basePrice, int dailyGoal, int ownedCount)
    {
        if (basePrice <= 0) return 0;
        // Goals 100/500/1000: x1/x2/x4. Repeated purchases: +25% per existing copy.
        // Linear ownership scaling prevents repeatedly buying an upgrade becoming punitive.
        double price = basePrice * Math.Max(1d, dailyGoal / 250d)
            * (1 + 0.25d * Math.Max(0, ownedCount));
        return (int)(Math.Ceiling(Math.Min(1000000000d, price) / 10d) * 10d);
    }
}
