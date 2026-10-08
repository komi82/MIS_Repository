using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FestivalUpgradeRuntime
{
    private static BaffItemDatabase database;
    public static bool InArcade => SceneManager.GetActiveScene().name == SceneNames.Arcade;
    public static void Configure(BaffItemDatabase source) { database = source; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { database = null; }

    public static BaffItemData Owned(BaffEffectType type)
    {
        if (!InArcade || database == null || database.allBaffItems == null) return null;
        return database.allBaffItems.Find(item => item != null && item.effecttype == type && Count(item) > 0);
    }

    public static int Count(BaffItemData item) => item == null ? 0 : OwnedProgressManager.GetBaffOwned(item.B_itemID);

    public static int GenerationReward(int original, RequestType type)
    {
        double bonus = 0d, penalty = 0d;
        var slow = Owned(BaffEffectType.slowReward);
        if (slow != null) bonus += slow.upgradeBonusRate * Count(slow);
        var specialists = new[] { BaffEffectType.blacksmithSpecialist, BaffEffectType.mixingSpecialist, BaffEffectType.purificationSpecialist };
        int category = FestivalUpgradeRules.WorkCategory(type);
        int[] categories = {2, 0, 1};
        for (int i = 0; i < specialists.Length; i++)
        {
            var item = Owned(specialists[i]);
            if (item == null) continue;
            if (category == categories[i]) bonus += item.upgradeBonusRate * Count(item);
            else penalty += item.upgradePenaltyRate * Count(item);
        }
        return FestivalUpgradeRules.Reward(original, bonus, penalty);
    }

    public static int DeliveryReward(int original, RequestType type, DeliveryStreakState streak, ItemInstanceState instance)
    {
        double bonus = 0d;
        var chain = Owned(BaffEffectType.deliveryStreak);
        if (chain != null) bonus += FestivalUpgradeRules.CappedBonus(streak.PreviousMatching(type),
            chain.upgradeBonusRate * Count(chain), chain.upgradeMaxBonus);
        var aging = Owned(BaffEffectType.tableAging);
        if (aging != null && instance != null) bonus += FestivalUpgradeRules.CappedBonus(instance.tableSeconds,
            aging.upgradeBonusRate * Count(aging), aging.upgradeMaxBonus);
        return FestivalUpgradeRules.Reward(original, bonus);
    }

    public static int LuckyReward(int original, double roll)
    {
        var item = Owned(BaffEffectType.luckyDelivery);
        return item != null && FestivalUpgradeRules.Wins(roll, item.upgradeChance)
            ? FestivalUpgradeRules.Reward(original, item.upgradeBonusRate * Count(item)) : original;
    }

    public static float SpeedMultiplier()
    {
        var item = Owned(BaffEffectType.slowReward);
        return item == null ? 1f : Mathf.Max(0.4f, 1f - item.upgradePenaltyRate * Count(item));
    }

    public static bool Matches(BaffEffectType type, ItemData target)
    {
        var effect = Owned(type);
        return effect != null && target != null && effect.upgradeTargetItems != null && Array.IndexOf(effect.upgradeTargetItems, target) >= 0;
    }

    public static bool RetainOre(ItemData item, ItemInstanceState state) =>
        Matches(BaffEffectType.oreRetention, item) && state != null && state.TrySpendOreReuse();

    public static int CraftOutputCount(ItemData item, bool mixing) =>
        mixing && Matches(BaffEffectType.doublePurificationLiquid, item) ? 2 : 1;
}
