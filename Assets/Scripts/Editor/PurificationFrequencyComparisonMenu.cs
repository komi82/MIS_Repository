#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PurificationFrequencyComparisonMenu
{
    private const string AssetPath = "Assets/Prefab/BaffItem/PurificationFrequency.asset";
    private const string BlacksmithAssetPath = "Assets/Prefab/BaffItem/BlacksmithFrequency.asset";
    private const string AddMenu = "MIS/テスト/浄化師の看板を1個追加";
    private const string InspectMenu = "MIS/テスト/現在の浄化依頼の抽選割合を確認";
    private const string ValidateMenu = "MIS/テスト/浄化依頼の出現率を検証";
    private const int SampleCount = 24000;

    [MenuItem(AddMenu)]
    private static void AddOne()
    {
        if (!CanPlayTest()) return;
        var effect = LoadEffect();
        OwnedProgressManager.AddBaffItem(effect.B_itemID);
        Debug.Log($"[浄化出現率] 看板を1個追加。現在{OwnedProgressManager.GetBaffOwned(effect.B_itemID)}個。次の依頼抽選から反映されます。");
        InspectRuntimeLottery();
    }

    [MenuItem(AddMenu, true)]
    private static bool CanPlayTest() => EditorApplication.isPlaying &&
        SceneManager.GetActiveScene().name == SceneNames.Arcade;

    [MenuItem(InspectMenu)]
    private static void InspectRuntimeLottery()
    {
        if (!CanPlayTest()) return;
        if (SessionState.GetBool("MIS.PurificationOnlyTest", false))
            Debug.LogWarning("[浄化出現率] 浄化のみのテスト設定が有効です。自然な出現を確認するときは解除してください。");
        var manager = UnityEngine.Object.FindFirstObjectByType<RequestManager>();
        Check(manager != null, "依頼管理が見つかりません");
        var effect = LoadEffect();
        var blacksmith = AssetDatabase.LoadAssetAtPath<BaffItemData>(BlacksmithAssetPath);
        Check(blacksmith != null, "鍛冶の看板のデータ");
        var serialized = new SerializedObject(manager);
        Check(serialized.FindProperty("purificationFrequencyItem").objectReferenceValue == effect, "依頼管理への登録");
        Check(serialized.FindProperty("blacksmithFrequencyItem").objectReferenceValue == blacksmith, "鍛冶との併用の登録");
        int purificationSelected = 0;
        int blacksmithSelected = 0;
        for (int i = 0; i < SampleCount; i++)
        {
            var type = manager.SelectRequestTypeForRoll((i + 0.5d) / SampleCount);
            if (RequestTypeLottery.IsTarget(type, effect.requestWeightTargetTypes)) purificationSelected++;
            if (RequestTypeLottery.IsTarget(type, blacksmith.requestWeightTargetTypes)) blacksmithSelected++;
        }
        Debug.Log($"[浄化出現率] 看板の所持：浄化{OwnedProgressManager.GetBaffOwned(effect.B_itemID)}個・鍛冶{OwnedProgressManager.GetBaffOwned(blacksmith.B_itemID)}個。現在の抽選割合：浄化 {100d * purificationSelected / SampleCount:F2}%、武器作成・修理 {100d * blacksmithSelected / SampleCount:F2}%（24,000点比較）。依頼や所持金は変更していません。");
    }

    [MenuItem(InspectMenu, true)]
    private static bool CanInspectRuntime() => CanPlayTest();

    [MenuItem(ValidateMenu)]
    public static void ValidateConfiguration()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("再生を止めて検証してください。");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("先にシーンの変更を保存してください。");

        var effect = LoadEffect();
        var blacksmith = AssetDatabase.LoadAssetAtPath<BaffItemData>(BlacksmithAssetPath);
        Check(blacksmith != null && blacksmith.effecttype == BaffEffectType.blacksmithFrequency, "鍛冶の看板のデータ");
        Check(effect.startprice == 150, "仮の購入価格150G");
        Check(Mathf.Approximately(effect.requestWeightIncreasePerItem, 0.5f), "仮の重み増加50%");
        Check(effect.requestWeightTargetTypes != null && effect.requestWeightTargetTypes.Length == 1 &&
            effect.requestWeightTargetTypes[0] == RequestType.PurifyWeapon, "対象は浄化のみ");
        Check(blacksmith.requestWeightTargetTypes != null && blacksmith.requestWeightTargetTypes.Length == 2 &&
            blacksmith.requestWeightTargetTypes.Contains(RequestType.CraftWeapon) &&
            blacksmith.requestWeightTargetTypes.Contains(RequestType.RepairWeapon), "鍛冶の対象は武器作成・修理");
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
            Check(serialized.FindProperty("purificationFrequencyItem").objectReferenceValue == effect, "arcadeへの登録");
            Check(serialized.FindProperty("blacksmithFrequencyItem").objectReferenceValue == blacksmith, "鍛冶との併用の登録");
            var poolProperty = serialized.FindProperty("requestTypesPool");
            var pool = new RequestType[poolProperty.arraySize];
            for (int i = 0; i < pool.Length; i++) pool[i] = (RequestType)poolProperty.GetArrayElementAtIndex(i).intValue;
            int purificationTickets = pool.Count(type => type == RequestType.PurifyWeapon);
            int blacksmithTickets = pool.Count(type => type == RequestType.CraftWeapon || type == RequestType.RepairWeapon);
            Check(purificationTickets > 0 && blacksmithTickets > 0 && purificationTickets + blacksmithTickets < pool.Length,
                "浄化・鍛冶・対象外の候補が存在");

            for (int purificationCount = 0; purificationCount <= 2; purificationCount++)
            for (int blacksmithCount = 0; blacksmithCount <= 2; blacksmithCount++)
            {
                var modifiers = new[]
                {
                    new RequestTypeLottery.WeightModifier(effect.requestWeightTargetTypes, effect.requestWeightIncreasePerItem, purificationCount),
                    new RequestTypeLottery.WeightModifier(blacksmith.requestWeightTargetTypes, blacksmith.requestWeightIncreasePerItem, blacksmithCount)
                };
                // 元の票数から独立して期待値を計算し、実際の抽選を両方の所持数で比較する。
                double purificationWeight = purificationTickets * (1d + (double)effect.requestWeightIncreasePerItem * purificationCount);
                double blacksmithWeight = blacksmithTickets * (1d + (double)blacksmith.requestWeightIncreasePerItem * blacksmithCount);
                double total = pool.Length - purificationTickets - blacksmithTickets + purificationWeight + blacksmithWeight;
                int purificationSelected = 0;
                int blacksmithSelected = 0;
                for (int i = 0; i < SampleCount; i++)
                {
                    var type = RequestTypeLottery.Pick(pool, modifiers, (i + 0.5d) / SampleCount);
                    if (type == RequestType.PurifyWeapon) purificationSelected++;
                    if (type == RequestType.CraftWeapon || type == RequestType.RepairWeapon) blacksmithSelected++;
                }
                double tolerance = (double)pool.Length / SampleCount;
                string counts = $"浄化{purificationCount}個・鍛冶{blacksmithCount}個";
                Check(Math.Abs((double)purificationSelected / SampleCount - purificationWeight / total) <= tolerance, counts + "の浄化抽選");
                Check(Math.Abs((double)blacksmithSelected / SampleCount - blacksmithWeight / total) <= tolerance, counts + "の鍛冶抽選");
                Check(RequestTypeLottery.GetWeight(RequestType.DeliverItem, modifiers) == 1d, "対象外の重み");
                if (blacksmithCount == 0) percentages[purificationCount] = 100d * purificationWeight / total;
            }
            Check(percentages[0] < percentages[1] && percentages[1] < percentages[2], "所持数に応じて出現率が上昇");
            var shop = EditorSceneManager.OpenScene("Assets/Scenes/Shop.unity", OpenSceneMode.Single);
            Check(shop.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShopManager>(true))
                .Any(shopManager => Array.IndexOf(shopManager.baffitemDatas, effect) >= 0), "ショップの商品候補への登録");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        Debug.Log($"[PurificationFrequencyValidation] PASS: 浄化のみ所持 0個 {percentages[0]:F2}% → 1個 {percentages[1]:F2}% → 2個 {percentages[2]:F2}%。鍛冶との併用9通りを各24,000点で比較し、対象外とショップ・所持一覧・arcadeへの登録を確認。");
    }

    [MenuItem(ValidateMenu, true)]
    private static bool CanValidate() => !EditorApplication.isPlaying;

    private static BaffItemData LoadEffect()
    {
        var effect = AssetDatabase.LoadAssetAtPath<BaffItemData>(AssetPath);
        Check(effect != null && effect.B_itemID == 12 && effect.effecttype == BaffEffectType.purificationFrequency, "看板のデータ");
        return effect;
    }

    private static void Check(bool passed, string label)
    {
        if (!passed) throw new InvalidOperationException("浄化依頼の出現率の検証失敗: " + label);
    }
}
#endif
