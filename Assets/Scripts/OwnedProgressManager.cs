using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// ショップで購入したバフアイテム・アーティファクトの所持数をシーンをまたいで保持する。
/// </summary>
public static class OwnedProgressManager
{
    private static readonly Dictionary<int, int> baffOwnedById = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> artifactOwnedById = new Dictionary<int, int>();

    public static void ResetAll()
    {
        baffOwnedById.Clear();
        artifactOwnedById.Clear();
    }

    public static void AddBaffItem(int itemId, int amount = 1)
    {
        if (amount <= 0) return;
        baffOwnedById.TryGetValue(itemId, out int current);
        baffOwnedById[itemId] = current + amount;
    }

    public static void AddArtifact(int itemId, int amount = 1)
    {
        if (amount <= 0) return;
        artifactOwnedById.TryGetValue(itemId, out int current);
        artifactOwnedById[itemId] = current + amount;
    }

    public static int GetBaffOwned(int itemId)
    {
        return baffOwnedById.TryGetValue(itemId, out int count) ? count : 0;
    }

    public static int GetArtifactOwned(int itemId)
    {
        return artifactOwnedById.TryGetValue(itemId, out int count) ? count : 0;
    }

    public static void LogOwnedInventory(BaffItemDatabase baffDatabase, ArtifactDatabase artifactDatabase)
    {
        var log = new StringBuilder();
        log.AppendLine("[OwnedItems] === 所持一覧 (arcade入場) ===");

        bool hasAny = false;

        if (baffDatabase != null && baffDatabase.allBaffItems != null)
        {
            foreach (BaffItemData item in baffDatabase.allBaffItems)
            {
                if (item == null) continue;
                int count = GetBaffOwned(item.B_itemID);
                if (count <= 0) continue;
                hasAny = true;
                log.AppendLine($"  Baff: {item.B_itemName} ({item.effecttype}) x{count}");
            }
        }

        if (artifactDatabase != null && artifactDatabase.allArtifacts != null)
        {
            foreach (ArtifactData item in artifactDatabase.allArtifacts)
            {
                if (item == null) continue;
                int count = GetArtifactOwned(item.A_itemID);
                if (count <= 0) continue;
                hasAny = true;
                log.AppendLine($"  Artifact: {item.A_itemName} ({item.A_itemType}) x{count}");
            }
        }

        if (!hasAny)
        {
            log.AppendLine("  (所持なし)");
        }

        Debug.Log(log.ToString());
    }
}
