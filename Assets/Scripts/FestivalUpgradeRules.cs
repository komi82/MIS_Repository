using System;

// Pure calculations shared by gameplay and automated checks.
public static class FestivalUpgradeRules
{
    public static double Positive(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 0d : Math.Max(0d, value);

    public static int Reward(int original, double bonus, double penalty = 0d)
    {
        // Serialized float rates such as 0.2 must not turn an exact 160G into 159G.
        double factor = Math.Max(0d, 1d + Math.Round(Positive(bonus), 6) - Math.Round(Positive(penalty), 6));
        double result = Math.Max(0, original) * factor;
        return result >= int.MaxValue ? int.MaxValue : (int)Math.Floor(result + 1e-8d);
    }

    public static double CappedBonus(double units, double rate, double cap) =>
        Math.Min(Positive(cap), Positive(units) * Positive(rate));

    public static bool Wins(double roll, double chance) =>
        !double.IsNaN(roll) && roll >= 0d && roll < 1d && roll < Math.Min(1d, Positive(chance));

    public static int WorkCategory(RequestType type)
    {
        if (type == RequestType.DeliverItem) return 0;
        if (type == RequestType.PurifyWeapon) return 1;
        if (type == RequestType.CraftWeapon || type == RequestType.RepairWeapon) return 2;
        return 3; // Attribute work is one category.
    }
}

[Serializable]
public sealed class DeliveryStreakState
{
    private int category = -1;
    private int count;
    public int PreviousMatching(RequestType type) => category == FestivalUpgradeRules.WorkCategory(type) ? count : 0;
    public void Complete(RequestType type)
    {
        int next = FestivalUpgradeRules.WorkCategory(type);
        count = category == next ? Math.Min(count, int.MaxValue - 1) + 1 : 1;
        category = next;
    }
    public void Reset() { category = -1; count = 0; }
}

[Serializable]
public sealed class ItemInstanceState
{
    public bool oreReuseSpent;
    public float tableSeconds;
    public bool TrySpendOreReuse()
    {
        if (oreReuseSpent) return false;
        oreReuseSpent = true;
        return true;
    }
    public ItemInstanceState Copy() => new ItemInstanceState { oreReuseSpent = oreReuseSpent, tableSeconds = tableSeconds };
}
