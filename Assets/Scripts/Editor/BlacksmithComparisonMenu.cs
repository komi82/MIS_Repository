#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor専用。ゲーム本体には含まれない、手動比較用の補助メニュー。
public static class BlacksmithComparisonMenu
{
    private const string MenuPath = "MIS/テスト/鍛冶師の槌を1個追加";
    private const string AssetPath = "Assets/Prefab/BaffItem/BlacksmithClickReduction.asset";

    [MenuItem(MenuPath)]
    private static void AddOne()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != SceneNames.Arcade)
        {
            Debug.LogWarning("[鍛冶計測] arcadeを再生中に実行してください。");
            return;
        }

        BaffItemData item = AssetDatabase.LoadAssetAtPath<BaffItemData>(AssetPath);
        if (item == null || item.effecttype != BaffEffectType.blacksmithClickReduction)
        {
            Debug.LogError("[鍛冶計測] 鍛冶師の槌のデータを確認できません。");
            return;
        }

        OwnedProgressManager.AddBaffItem(item.B_itemID);
        Debug.Log($"[鍛冶計測] テスト用に槌を1個追加。現在 {OwnedProgressManager.GetBaffOwned(item.B_itemID)}個。所持金は変更していません。");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanAddOne()
    {
        return EditorApplication.isPlaying && SceneManager.GetActiveScene().name == SceneNames.Arcade;
    }
}
#endif