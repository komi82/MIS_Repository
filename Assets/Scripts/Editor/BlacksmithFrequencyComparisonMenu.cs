#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BlacksmithFrequencyComparisonMenu
{
    private const string AssetPath = "Assets/Prefab/BaffItem/BlacksmithFrequency.asset";
    private const string AddMenu = "MIS/テスト/鍛冶屋の看板を1個追加";
    private const int SampleCount = 24000;

    [MenuItem(AddMenu)]
    private static void AddOne()
    {
        if (!CanPlayTest()) return;
        var effect = LoadEffect();
        OwnedProgressManager.AddBaffItem(effect.B_itemID);
        Debug.Log($"[鍛冶出現率] 看板を1個追加。現在{OwnedProgressManager.GetBaffOwned(effect.B_itemID)}個。次の依頼抽選から反映されます。");
        InspectRuntimeLottery();
    }

    [MenuItem(AddMenu, true)]
    private static bool CanPlayTest() => EditorApplication.isPlaying &&
        SceneManager.GetActiveScene().name == SceneNames.Arcade;

    [MenuItem("MIS/テスト/現在の鍛冶依頼の抽選割合を確認")]
    private static void InspectRuntimeLottery()
    {
        if (!CanPlayTest()) return;
        var manager = UnityEngine.Object.FindFirstObjectByType<RequestManager>();
        Check(manager != null, "依頼管理が見つかりません");
        var effect = LoadEffect();
        Check(new SerializedObject(manager).FindProperty("blacksmithFrequencyItem").objectReferenceValue == effect, "依頼管理への登録");
        int selected = 0;
        for (int i = 0; i < SampleCount; i++)
            if (RequestTypeLottery.IsTarget(manager.SelectRequestTypeForRoll((i + 0.5d) / SampleCount), effect.requestWeightTargetTypes)) selected++;
        Debug.Log($"[鍛冶出現率] 所持{OwnedProgressManager.GetBaffOwned(effect.B_itemID)}個：現在の抽選処理で武器作成・修理が選ばれる割合 {100d * selected / SampleCount:F2}%（24,000点比較）。依頼や所持金は変更していません。");
    }

    [MenuItem("MIS/テスト/現在の鍛冶依頼の抽選割合を確認", true)]
    private static bool CanInspectRuntime() => CanPlayTest();

    [MenuItem("MIS/テスト/鍛冶依頼の出現率を検証")]
    public static void ValidateConfiguration()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("再生を止めて検証してください。");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("先にシーンの変更を保存してください。");

        var effect = LoadEffect();
        Check(effect.startprice == 150, "仮の購入価格150G");
        Check(Mathf.Approximately(effect.requestWeightIncreasePerItem, 0.5f), "仮の重み増加50%");
        Check(effect.requestWeightTargetTypes != null && effect.requestWeightTargetTypes.Length == 2 &&
            effect.requestWeightTargetTypes.Contains(RequestType.CraftWeapon) &&
            effect.requestWeightTargetTypes.Contains(RequestType.RepairWeapon), "対象は武器作成・修理");
        Check(effect.prefab != null && effect.prefab.GetComponent<UnityEngine.UI.Button>() != null, "購入ボタン");
        var database = AssetDatabase.LoadAssetAtPath<BaffItemDatabase>("Assets/Scripts/BaffItemDatabase.asset");
        Check(database != null && database.allBaffItems.Contains(effect), "所持効果一覧への登録");
        Check(database.allBaffItems.Count(item => item != null && item.B_itemID == effect.B_itemID) == 1, "アイテムIDが一意");

        var setup = EditorSceneManager.GetSceneManagerSetup();
        double[] percentages = new double[3];
        try
        {
            var arcade = EditorSceneManager.OpenScene("Assets/Scenes/arcade.unity", OpenSceneMode.Single);
            var manager = arcade.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RequestManager>(true)).Single();
            var serialized = new SerializedObject(manager);
            Check(serialized.FindProperty("blacksmithFrequencyItem").objectReferenceValue == effect, "arcadeへの登録");
            var poolProperty = serialized.FindProperty("requestTypesPool");
            var pool = new RequestType[poolProperty.arraySize];
            for (int i = 0; i < pool.Length; i++) pool[i] = (RequestType)poolProperty.GetArrayElementAtIndex(i).intValue;
            Check(pool.Length > 0, "抽選候補が存在");
            int originalTargets = pool.Count(type => RequestTypeLottery.IsTarget(type, effect.requestWeightTargetTypes));
            Check(originalTargets > 0 && originalTargets < pool.Length, "対象・対象外の候補が存在");
            for (int count = 0; count <= 2; count++)
            {
                // 設定の元の票数から求めた期待値と、実際の選択処理の結果を比較する。
                double targetWeight = originalTargets * (1d + effect.requestWeightIncreasePerItem * count);
                double expected = targetWeight / (pool.Length - originalTargets + targetWeight);
                int selected = 0;
                for (int i = 0; i < SampleCount; i++)
                    if (RequestTypeLottery.IsTarget(RequestTypeLottery.Pick(pool, effect.requestWeightTargetTypes,
                        effect.requestWeightIncreasePerItem, count, (i + 0.5d) / SampleCount), effect.requestWeightTargetTypes)) selected++;
                double actual = (double)selected / SampleCount;
                Check(Math.Abs(actual - expected) <= (double)pool.Length / SampleCount, count + "個所持の抽選結果");
                percentages[count] = 100d * expected;
            }
            Check(percentages[0] < percentages[1] && percentages[1] < percentages[2], "所持数に応じて出現率が上昇");
            foreach (RequestType type in Enum.GetValues(typeof(RequestType)))
                if (!RequestTypeLottery.IsTarget(type, effect.requestWeightTargetTypes))
                    Check(RequestTypeLottery.GetWeight(type, effect.requestWeightTargetTypes, 0.5f, 2) == 1d, "対象外の重み");

            var shop = EditorSceneManager.OpenScene("Assets/Scenes/Shop.unity", OpenSceneMode.Single);
            Check(shop.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShopManager>(true))
                .Any(shopManager => Array.IndexOf(shopManager.baffitemDatas, effect) >= 0), "ショップの商品候補への登録");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        Debug.Log($"[BlacksmithFrequencyValidation] PASS: 0個 {percentages[0]:F2}% → 1個 {percentages[1]:F2}% → 2個 {percentages[2]:F2}%。24,000点で抽選処理、対象外、ショップ・所持一覧・arcadeへの登録を確認。");
    }

    [MenuItem("MIS/テスト/鍛冶依頼の出現率を検証", true)]
    private static bool CanValidate() => !EditorApplication.isPlaying;

    private static BaffItemData LoadEffect()
    {
        var effect = AssetDatabase.LoadAssetAtPath<BaffItemData>(AssetPath);
        Check(effect != null && effect.B_itemID == 11 && effect.effecttype == BaffEffectType.blacksmithFrequency, "看板のデータ");
        return effect;
    }

    private static void Check(bool passed, string label)
    {
        if (!passed) throw new InvalidOperationException("鍛冶依頼の出現率の検証失敗: " + label);
    }
}
#endif
