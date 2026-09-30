#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor専用。ゲーム本体には含まれない、手動比較用の補助メニュー。
public static class PurificationComparisonMenu
{
    private const string MenuPath = "MIS/テスト/浄化師のお守りを1個追加";
    private const string AssetPath = "Assets/Prefab/BaffItem/PurificationWindowExpansion.asset";

    [MenuItem(MenuPath)]
    private static void AddOne()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != SceneNames.Arcade)
        {
            Debug.LogWarning("[浄化計測] arcadeを再生中に実行してください。");
            return;
        }

        BaffItemData item = AssetDatabase.LoadAssetAtPath<BaffItemData>(AssetPath);
        if (item == null || item.effecttype != BaffEffectType.purificationWindowExpansion)
        {
            Debug.LogError("[浄化計測] 浄化師のお守りのデータを確認できません。");
            return;
        }

        OwnedProgressManager.AddBaffItem(item.B_itemID);
        Debug.Log($"[浄化計測] テスト用にお守りを1個追加。現在 {OwnedProgressManager.GetBaffOwned(item.B_itemID)}個。所持金は変更していません。");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanAddOne()
    {
        return EditorApplication.isPlaying && SceneManager.GetActiveScene().name == SceneNames.Arcade;
    }
}
#endif