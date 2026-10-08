using System;
using System.Collections.Generic;

/// <summary>既存の依頼プールの重複数を維持し、対象種別の抽選の重みを加算する。</summary>
public static class RequestTypeLottery
{
    public static double GetWeight(RequestType type, IList<RequestType> targets, float increasePerItem, int ownedCount)
    {
        if (!IsTarget(type, targets) || ownedCount <= 0 || increasePerItem <= 0f ||
            float.IsNaN(increasePerItem) || float.IsInfinity(increasePerItem)) return 1d;
        return 1d + (double)increasePerItem * ownedCount;
    }

    public static bool IsTarget(RequestType type, IList<RequestType> targets)
    {
        if (targets == null) return false;
        for (int i = 0; i < targets.Count; i++)
            if (targets[i] == type) return true;
        return false;
    }

    public static RequestType Pick(IList<RequestType> pool, IList<RequestType> targets,
        float increasePerItem, int ownedCount, double roll)
    {
        if (pool == null || pool.Count == 0) throw new ArgumentException("依頼の抽選候補がありません。", nameof(pool));
        if (double.IsNaN(roll) || roll < 0d || roll > 1d) throw new ArgumentOutOfRangeException(nameof(roll));

        double total = 0d;
        for (int i = 0; i < pool.Count; i++)
            total += GetWeight(pool[i], targets, increasePerItem, ownedCount);

        double remaining = roll * total;
        for (int i = 0; i < pool.Count; i++)
        {
            remaining -= GetWeight(pool[i], targets, increasePerItem, ownedCount);
            if (remaining < 0d) return pool[i];
        }
        // UnityEngine.Random.value は1も返すため、端点では最後の候補を返す。
        return pool[pool.Count - 1];
    }

    public static double GetTargetProbability(IList<RequestType> pool, IList<RequestType> targets,
        float increasePerItem, int ownedCount)
    {
        if (pool == null || pool.Count == 0) return 0d;
        double total = 0d;
        double targetWeight = 0d;
        for (int i = 0; i < pool.Count; i++)
        {
            double weight = GetWeight(pool[i], targets, increasePerItem, ownedCount);
            total += weight;
            if (IsTarget(pool[i], targets)) targetWeight += weight;
        }
        return targetWeight / total;
    }
}
