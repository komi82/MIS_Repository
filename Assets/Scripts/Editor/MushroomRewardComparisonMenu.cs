#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MushroomRewardComparisonMenu
{
    private const string AssetPath = "Assets/Prefab/BaffItem/MushroomReward.asset";
    private const string MenuPath = "MIS/テスト/キノコ採りの籠を1個追加";

    [MenuItem(MenuPath)]
    private static void AddOne()
    {
        if (!CanAddOne()) return;
        var effect = AssetDatabase.LoadAssetAtPath<BaffItemData>(AssetPath);
        if (effect == null) throw new InvalidOperationException("籠のデータがありません。");
        OwnedProgressManager.AddBaffItem(effect.B_itemID);
        Debug.Log($"[キノコ報酬] 籠を1個追加。現在{OwnedProgressManager.GetBaffOwned(effect.B_itemID)}個。次に生成する対象依頼から加算されます。");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanAddOne() => EditorApplication.isPlaying &&
        SceneManager.GetActiveScene().name == SceneNames.Arcade;

    [MenuItem("MIS/テスト/睡眠薬の依頼を出す")]
    private static void GenerateSleepingPotionRequest()
    {
        if (!CanAddOne()) return;
        var manager = UnityEngine.Object.FindFirstObjectByType<RequestManager>();
        var item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Prefab/Item/睡眠薬.asset");
        if (manager == null || item == null) throw new InvalidOperationException("依頼管理か睡眠薬のデータがありません。");
        manager.GenerateDeliveryForEditorTest(item);
    }

    [MenuItem("MIS/テスト/睡眠薬の依頼を出す", true)]
    private static bool CanGenerateRequest() => CanAddOne();

    // バッチ実行にも使う。実データとシーンの接続を検証する。
    [MenuItem("MIS/テスト/キノコ報酬の設定を検証")]
    public static void ValidateConfiguration()
    {
        var effect = AssetDatabase.LoadAssetAtPath<BaffItemData>(AssetPath);
        Check(effect != null && effect.B_itemID == 10 && effect.effecttype == BaffEffectType.mushroomReward, "効果データ");
        Check(effect.startprice == 150 && effect.rewardBonusPerOwnedItem == 100, "初期価格と加算額");
        Check(effect.prefab != null && effect.prefab.GetComponent<UnityEngine.UI.Button>() != null, "購入用プレハブ");
        var database = AssetDatabase.LoadAssetAtPath<BaffItemDatabase>("Assets/Scripts/BaffItemDatabase.asset");
        Check(database.allBaffItems.Contains(effect), "所持一覧への登録");
        foreach (var other in database.allBaffItems)
            Check(other == effect || other.B_itemID != effect.B_itemID, "IDの重複なし");

        string[] targets = { "爆薬", "劇毒薬", "睡眠薬" };
        foreach (string name in targets)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/Prefab/Item/{name}.asset");
            Check(RequestManager.CalculateMushroomRewardBonus(effect, RequestType.DeliverItem, item, 0) == 0, name + " 未所持");
            Check(RequestManager.CalculateMushroomRewardBonus(effect, RequestType.DeliverItem, item, 1) == 100, name + " 1個");
            Check(RequestManager.CalculateMushroomRewardBonus(effect, RequestType.DeliverItem, item, 2) == 200, name + " 2個");
            Check(RequestManager.CalculateMushroomRewardBonus(effect, RequestType.CraftWeapon, item, 1) == 0, "依頼種類の限定");
        }
        foreach (string name in new[] { "回復薬", "魔力薬", "浄化液" })
            Check(RequestManager.CalculateMushroomRewardBonus(effect, RequestType.DeliverItem,
                AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/Prefab/Item/{name}.asset"), 2) == 0, name + " 対象外");
        Check(RequestManager.CalculateMushroomRewardBonus(effect, RequestType.DeliverItem, null, 1) == 0, "対象未設定");

        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (!EditorApplication.isPlaying)
        {
            // 編集中の変更を守り、保存していないシーンでは計算のみ検証する。
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("先にシーンを保存してください。");
            try
            {
                var arcade = EditorSceneManager.OpenScene("Assets/Scenes/arcade.unity", OpenSceneMode.Single);
                bool connected = false;
                foreach (var root in arcade.GetRootGameObjects())
                    foreach (var manager in root.GetComponentsInChildren<RequestManager>(true))
                        connected |= new SerializedObject(manager).FindProperty("mushroomRewardItem").objectReferenceValue == effect;
                Check(connected, "arcadeの報酬設定");
                var shop = EditorSceneManager.OpenScene("Assets/Scenes/Shop.unity", OpenSceneMode.Single);
                bool stocked = false;
                foreach (var root in shop.GetRootGameObjects())
                    foreach (var manager in root.GetComponentsInChildren<ShopManager>(true))
                        stocked |= Array.IndexOf(manager.baffitemDatas, effect) >= 0;
                Check(stocked, "ショップへの登録");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        Debug.Log("[MushroomRewardValidation] PASS: 対象3種類、対象外、未所持、複数所持、ショップ・arcade・所持一覧の接続を確認。");
    }

    private static void Check(bool passed, string label)
    {
        if (!passed) throw new InvalidOperationException("キノコ報酬の検証失敗: " + label);
    }
}
#endif
