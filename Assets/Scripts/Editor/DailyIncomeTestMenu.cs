#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DailyIncomeTestMenu
{
    private const string Path = "MIS/テスト/貯金箱/ショップに貯金箱を並べる";

    private const string GoalPath = "MIS/テスト/貯金箱/所持金を目標金額まで増やす";

    [MenuItem(GoalPath)]
    private static void ReachGoal()
    {
        if (!CanReachGoal()) return;
        int day = DayAdvanceButton.Instance.GetDay();
        if (!DailyIncomeState.RequiresDelivery(day, 0))
        {
            Debug.LogWarning("[貯金箱テスト] 貯金箱を購入した翌朝、50Gを受け取ってから実行してください。");
            return;
        }
        int before = MoneyManager.currentMoney;
        int goal = GameClockText.Instance.GetCompleteMoneyThreshold();
        int amount = Mathf.Max(0, goal - before);
        if (amount > 0) MoneyManager.Instance.AddMoney(amount);
        Debug.Log($"[貯金箱テスト] 所持金 {before}G → {MoneyManager.currentMoney}G（目標{goal}G）。今日まだ納品していなければ待機し、実際に1件納品するとショップへ進むことを確認してください。");
    }

    [MenuItem(GoalPath, true)]
    private static bool CanReachGoal()
    {
        return EditorApplication.isPlaying &&
            GameClockText.Instance != null && MoneyManager.Instance != null &&
            DayAdvanceButton.Instance != null &&
            SceneManager.GetActiveScene().name == "arcade" &&
            OwnedProgressManager.GetBaffOwned(DailyIncomeState.ItemId) > 0;
    }

    [MenuItem(Path)]
    private static void ShowBank()
    {
        if (!CanShowBank()) return;
        var shop = Object.FindFirstObjectByType<ShopManager>();
        var item = AssetDatabase.LoadAssetAtPath<BaffItemData>("Assets/Prefab/BaffItem/DailyIncome.asset");
        if (shop == null || item == null) return;
        if (OwnedProgressManager.GetBaffOwned(DailyIncomeState.ItemId) > 0)
        {
            Debug.Log("[貯金箱テスト] 所持上限1個のため、再販売しません。");
            return;
        }
        foreach (var slot in shop.slots)
            for (int i = slot.childCount - 1; i >= 0; i--)
                Object.Destroy(slot.GetChild(i).gameObject);
        item.ResetShopPrice();
        shop.baffitemDatas = new[] { item };
        shop.SendMessage("SpawnItems");
        Debug.Log("[貯金箱テスト] 通常商品を貯金箱だけに変更（右側のアーティファクトは残ります）。価格150G、購入は通常のボタンで行います。");
    }

    [MenuItem(Path, true)]
    private static bool CanShowBank()
    {
        return EditorApplication.isPlaying && SceneManager.GetActiveScene().name == SceneNames.Shop;
    }
}
#endif
