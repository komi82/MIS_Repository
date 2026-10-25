using System.Collections.Generic;
using UnityEngine;

/// <summary>鋼鴉さんの10種類。所持数は保存したまま、その日の無効化だけを効果判定に適用する。</summary>
public static class KouaArtifactRuntime
{
    public const int AnotherRound = 100, FertilePotion = 101, StrongHammer = 102, StrangeHammer = 103,
        ResonanceBook = 104, TimeIsMoney = 105, OreMirror = 106, ReactionPotion = 107,
        WhimsicalWealth = 108, MemoryBook = 109;
    static KouaArtifactCatalog catalog;
    public static KouaArtifactCatalog Catalog => catalog != null ? catalog : (catalog = Resources.Load<KouaArtifactCatalog>("KouaArtifactCatalog"));
    public static bool Has(int id) => FestivalUpgradeRuntime.InArcade && OwnedProgressManager.GetArtifactOwned(id) > 0
        && !DailyChallengeRuntime.IsArtifactDisabled(id);
    public static bool IsRepair(RecipeData recipe) => recipe != null && recipe.requiredItems != null
        && System.Array.Exists(recipe.requiredItems, x => x != null && x.itemType == ItemTypes.BrokenWeapon);

    // Follow the recipe graph so secondary ingredients (e.g. an element used on a sword) count too.
    // The supplied request target is already owned; don't count the ingredients used to make it earlier.
    public static bool UsesMaterial(Request request, ItemData[] materials)
    {
        if (request == null || materials == null) return false;
        return Uses(request.requiredItem, request.providedItem, materials, new HashSet<ItemData>());
    }
    static bool Uses(ItemData item, ItemData supplied, ItemData[] materials, HashSet<ItemData> visiting)
    {
        if (item == null || item == supplied || !visiting.Add(item)) return false;
        if (System.Array.IndexOf(materials, item) >= 0) return true;
        if (Catalog != null && Catalog.recipes != null)
            foreach (var db in Catalog.recipes)
                if (db != null && db.allRecipes != null)
                    foreach (var recipe in db.allRecipes)
                        if (recipe != null && recipe.resultItem == item && recipe.requiredItems != null)
                            foreach (var ingredient in recipe.requiredItems)
                                if (Uses(ingredient, supplied, materials, visiting)) return true;
        return false;
    }
    public static int Reward(int amount, Request request)
    {
        if (!FestivalUpgradeRuntime.InArcade || request == null) return amount;
        double bonus = 0;
        if (Has(FertilePotion) && Catalog != null && UsesMaterial(request, Catalog.plants)) bonus += .3;
        if (Has(OreMirror) && Catalog != null && UsesMaterial(request, Catalog.ores)) bonus += .3;
        if (request.requestType == RequestType.RepairWeapon)
        {
            if (Has(StrongHammer)) bonus += .3;
            if (Has(StrangeHammer)) bonus += .6;
        }
        if (request.requestType == RequestType.DeliverItem && Has(ReactionPotion)) bonus += .3;
        double multiplier = 1 + bonus;
        if (Has(AnotherRound)) multiplier *= 5;
        if (Has(TimeIsMoney)) multiplier *= 3;
        return FestivalUpgradeRules.Reward(amount, multiplier - 1);
    }
    public static int RollPayment(int amount, float roll) => Has(WhimsicalWealth)
        ? FestivalUpgradeRules.Reward(amount, roll < .5f ? .5 : 0, roll < .5f ? 0 : .5) : amount;
    public static string DisplayName(int id)
    {
        if (Catalog != null && Catalog.artifacts != null)
            foreach (var item in Catalog.artifacts) if (item != null && item.A_itemID == id) return item.itemName;
        return id == 0 ? "国王のお墨付き" : id == 1 ? "錆びた記念硬貨" : "アーティファクト";
    }
}
