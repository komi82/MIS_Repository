#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor専用。ゲーム本体には含まれない、手動比較用の補助メニュー。
public static class CraftHoldComparisonMenu
{
    private const string MenuPath = "MIS/テスト/調合師の羽を1個追加";
    private const string AssetPath = "Assets/Prefab/BaffItem/CraftHoldShortening.asset";

    [MenuItem(MenuPath)]
    private static void AddOne()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != SceneNames.Arcade)
        {
            Debug.LogWarning("[調合計測] arcadeを再生中に実行してください。");
            return;
        }

        BaffItemData item = AssetDatabase.LoadAssetAtPath<BaffItemData>(AssetPath);
        if (item == null || item.effecttype != BaffEffectType.craftHoldShortening)
        {
            Debug.LogError("[調合計測] 調合師の羽のデータを確認できません。");
            return;
        }

        OwnedProgressManager.AddBaffItem(item.B_itemID);
        Debug.Log($"[調合計測] テスト用に羽を1個追加。現在 {OwnedProgressManager.GetBaffOwned(item.B_itemID)}個。所持金は変更していません。");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanAddOne()
    {
        return EditorApplication.isPlaying && SceneManager.GetActiveScene().name == SceneNames.Arcade;
    }
}
#endif