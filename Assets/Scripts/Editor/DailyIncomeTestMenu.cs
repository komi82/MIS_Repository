#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DailyIncomeTestMenu
{
    private const string Path = "MIS/テスト/貯金箱/ショップに貯金箱を並べる";

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
