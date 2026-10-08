#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MixingFrequencyComparisonMenu
{
    private const string AddMenu = "MIS/テスト/調合師の看板を1個追加";
    private const string InspectMenu = "MIS/テスト/現在の調合依頼の抽選割合を確認";
    private const string ValidateMenu = "MIS/テスト/調合依頼の出現率を検証";
    private const int SampleCount = 24000;

    [MenuItem(AddMenu)]
    private static void AddOne()
    {
        if (!CanPlayTest()) return;
        var effect = LoadMixingEffect();
        OwnedProgressManager.AddBaffItem(effect.B_itemID);
        Debug.Log($"[調合出現率] 看板を1個追加。現在{OwnedProgressManager.GetBaffOwned(effect.B_itemID)}個。次の依頼抽選から反映されます。");
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
            Debug.LogWarning("[調合出現率] 浄化のみのテスト設定が有効です。自然な出現を確認するときは解除してください。");
        var manager = UnityEngine.Object.FindFirstObjectByType<RequestManager>();
        Check(manager != null, "依頼管理が見つかりません");
        var mixing = LoadMixingEffect();
        var blacksmith = LoadEffect("BlacksmithFrequency", BaffEffectType.blacksmithFrequency);
        var purification = LoadEffect("PurificationFrequency", BaffEffectType.purificationFrequency);
        CheckManagerReferences(manager, mixing, blacksmith, purification);
        int[] selected = new int[3];
        for (int i = 0; i < SampleCount; i++)
            CountType(manager.SelectRequestTypeForRoll((i + 0.5d) / SampleCount), selected);
        Debug.Log($"[調合出現率] 看板の所持：調合{OwnedProgressManager.GetBaffOwned(mixing.B_itemID)}個・鍛冶{OwnedProgressManager.GetBaffOwned(blacksmith.B_itemID)}個・浄化{OwnedProgressManager.GetBaffOwned(purification.B_itemID)}個。現在の抽選割合：調合 {100d * selected[0] / SampleCount:F2}%、武器作成・修理 {100d * selected[1] / SampleCount:F2}%、浄化 {100d * selected[2] / SampleCount:F2}%（24,000点比較）。依頼や所持金は変更していません。");
    }

    [MenuItem(InspectMenu, true)]
    private static bool CanInspectRuntime() => CanPlayTest();

    [MenuItem(ValidateMenu)]
    public static void ValidateConfiguration()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("再生を止めて検証してください。");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("先にシーンの変更を保存してください。");

        var mixing = LoadMixingEffect();
        var blacksmith = LoadEffect("BlacksmithFrequency", BaffEffectType.blacksmithFrequency);
        var purification = LoadEffect("PurificationFrequency", BaffEffectType.purificationFrequency);
        Check(mixing.startprice == 150, "仮の購入価格150G");
        Check(Mathf.Approximately(mixing.requestWeightIncreasePerItem, 0.5f), "仮の重み増加50%");
        Check(mixing.requestWeightTargetTypes != null && mixing.requestWeightTargetTypes.Length == 1 &&
            mixing.requestWeightTargetTypes[0] == RequestType.DeliverItem, "対象は調合のみ");
        Check(blacksmith.requestWeightTargetTypes != null && blacksmith.requestWeightTargetTypes.Length == 2 &&
            blacksmith.requestWeightTargetTypes.Contains(RequestType.CraftWeapon) &&
            blacksmith.requestWeightTargetTypes.Contains(RequestType.RepairWeapon), "鍛冶の対象は武器作成・修理");
        Check(purification.requestWeightTargetTypes != null && purification.requestWeightTargetTypes.Length == 1 &&
            purification.requestWeightTargetTypes[0] == RequestType.PurifyWeapon, "浄化の対象");
        Check(mixing.prefab != null && mixing.prefab.GetComponent<UnityEngine.UI.Button>() != null, "購入ボタン");
        var database = AssetDatabase.LoadAssetAtPath<BaffItemDatabase>("Assets/Scripts/BaffItemDatabase.asset");
        Check(database != null && database.allBaffItems.Contains(mixing), "所持効果一覧への登録");
        Check(database.allBaffItems.Count(item => item != null && item.B_itemID == mixing.B_itemID) == 1, "アイテムIDが一意");

        var setup = EditorSceneManager.GetSceneManagerSetup();
        double[] percentages = new double[3];
        try
        {
            var arcade = EditorSceneManager.OpenScene("Assets/Scenes/arcade.unity", OpenSceneMode.Single);
            var manager = arcade.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RequestManager>(true)).Single();
            CheckManagerReferences(manager, mixing, blacksmith, purification);
            var poolProperty = new SerializedObject(manager).FindProperty("requestTypesPool");
            var pool = new RequestType[poolProperty.arraySize];
            for (int i = 0; i < pool.Length; i++) pool[i] = (RequestType)poolProperty.GetArrayElementAtIndex(i).intValue;
            int[] tickets = new int[3];
            foreach (var type in pool) CountType(type, tickets);
            Check(tickets.All(count => count > 0) && tickets.Sum() < pool.Length, "調合・鍛冶・浄化・対象外の候補が存在");

            for (int mixingCount = 0; mixingCount <= 2; mixingCount++)
            for (int blacksmithCount = 0; blacksmithCount <= 2; blacksmithCount++)
            for (int purificationCount = 0; purificationCount <= 2; purificationCount++)
            {
                var modifiers = new[]
                {
                    new RequestTypeLottery.WeightModifier(mixing.requestWeightTargetTypes, mixing.requestWeightIncreasePerItem, mixingCount),
                    new RequestTypeLottery.WeightModifier(blacksmith.requestWeightTargetTypes, blacksmith.requestWeightIncreasePerItem, blacksmithCount),
                    new RequestTypeLottery.WeightModifier(purification.requestWeightTargetTypes, purification.requestWeightIncreasePerItem, purificationCount)
                };
                // 各種類の元の票数から期待値を求め、3つの効果を併用した抽選結果と比較する。
                double[] weights =
                {
                    tickets[0] * (1d + (double)mixing.requestWeightIncreasePerItem * mixingCount),
                    tickets[1] * (1d + (double)blacksmith.requestWeightIncreasePerItem * blacksmithCount),
                    tickets[2] * (1d + (double)purification.requestWeightIncreasePerItem * purificationCount)
                };
                double total = pool.Length - tickets.Sum() + weights.Sum();
                int[] selected = new int[3];
                for (int i = 0; i < SampleCount; i++)
                    CountType(RequestTypeLottery.Pick(pool, modifiers, (i + 0.5d) / SampleCount), selected);
                for (int category = 0; category < selected.Length; category++)
                    Check(Math.Abs((double)selected[category] / SampleCount - weights[category] / total) <= (double)pool.Length / SampleCount,
                        $"調合{mixingCount}個・鍛冶{blacksmithCount}個・浄化{purificationCount}個の抽選（種類{category}）");
                for (int type = (int)RequestType.AddAttribute_Fire; type <= (int)RequestType.AddAttribute_Darkness; type++)
                    Check(RequestTypeLottery.GetWeight((RequestType)type, modifiers) == 1d, "対象外の重み");
                if (blacksmithCount == 0 && purificationCount == 0) percentages[mixingCount] = 100d * weights[0] / total;
            }
            Check(percentages[0] < percentages[1] && percentages[1] < percentages[2], "所持数に応じて出現率が上昇");
            var shop = EditorSceneManager.OpenScene("Assets/Scenes/Shop.unity", OpenSceneMode.Single);
            Check(shop.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShopManager>(true))
                .Any(shopManager => Array.IndexOf(shopManager.baffitemDatas, mixing) >= 0), "ショップの商品候補への登録");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        Debug.Log($"[MixingFrequencyValidation] PASS: 調合のみ所持 0個 {percentages[0]:F2}% → 1個 {percentages[1]:F2}% → 2個 {percentages[2]:F2}%。鍛冶・浄化との併用27通りを各24,000点で比較し、対象外とショップ・所持一覧・arcadeへの登録を確認。");
    }

    [MenuItem(ValidateMenu, true)]
    private static bool CanValidate() => !EditorApplication.isPlaying;

    private static void CountType(RequestType type, int[] counts)
    {
        if (type == RequestType.DeliverItem) counts[0]++;
        else if (type == RequestType.CraftWeapon || type == RequestType.RepairWeapon) counts[1]++;
        else if (type == RequestType.PurifyWeapon) counts[2]++;
    }

    private static void CheckManagerReferences(RequestManager manager, BaffItemData mixing, BaffItemData blacksmith, BaffItemData purification)
    {
        var serialized = new SerializedObject(manager);
        Check(serialized.FindProperty("mixingFrequencyItem").objectReferenceValue == mixing, "調合の依頼管理への登録");
        Check(serialized.FindProperty("blacksmithFrequencyItem").objectReferenceValue == blacksmith, "鍛冶との併用の登録");
        Check(serialized.FindProperty("purificationFrequencyItem").objectReferenceValue == purification, "浄化との併用の登録");
    }

    private static BaffItemData LoadMixingEffect()
    {
        var effect = LoadEffect("MixingFrequency", BaffEffectType.mixingFrequency);
        Check(effect.B_itemID == 13, "調合の看板のID");
        return effect;
    }

    private static BaffItemData LoadEffect(string name, BaffEffectType type)
    {
        var effect = AssetDatabase.LoadAssetAtPath<BaffItemData>($"Assets/Prefab/BaffItem/{name}.asset");
        Check(effect != null && effect.effecttype == type, name + "のデータ");
        return effect;
    }

    private static void Check(bool passed, string label)
    {
        if (!passed) throw new InvalidOperationException("調合依頼の出現率の検証失敗: " + label);
    }
}
#endif
