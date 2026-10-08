#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FestivalUpgradesValidation
{
    private const string Menu = "MIS/テスト/残り10種類の追加効果を検証";
    private static int checks;
    private static void Check(bool result, string label)
    {
        if (!result) throw new InvalidOperationException("追加効果の検証失敗: " + label);
        checks++;
    }

    [MenuItem(Menu)]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("再生を止めて検証してください。");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("シーンの変更を先に保存してください。");
        checks = 0;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var ownership = (Dictionary<int, int>)typeof(OwnedProgressManager).GetField("baffOwnedById", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        var savedOwnership = new Dictionary<int, int>(ownership);
        var database = AssetDatabase.LoadAssetAtPath<BaffItemDatabase>("Assets/Scripts/BaffItemDatabase.asset");
        GameObject temporary = null;
        try
        {
            var upgrades = database.allBaffItems.Where(item => item != null && item.B_itemID >= 14 && item.B_itemID <= 23).ToArray();
            Check(upgrades.Length == 10 && upgrades.Select(item => item.B_itemID).Distinct().Count() == 10, "10種類の一意なID");
            foreach (var item in upgrades)
            {
                Check((int)item.effecttype == item.B_itemID + 2, item.name + "の効果種類");
                Check(item.startprice == 150 && item.prefab != null && item.prefab.GetComponent<UnityEngine.UI.Button>() != null, item.name + "の価格と購入ボタン");
            }
            var shop = EditorSceneManager.OpenScene("Assets/Scenes/Shop.unity", OpenSceneMode.Single);
            var shopManager = shop.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShopManager>(true)).Single();
            Check(upgrades.All(item => shopManager.baffitemDatas.Contains(item)), "全10種類がショップ候補に登録");
            var arcade = EditorSceneManager.OpenScene("Assets/Scenes/arcade.unity", OpenSceneMode.Single);
            // Opening another scene can unload the previously loaded asset instance.
            // Reacquire it so the checks use the same live data as the arcade scene.
            database = AssetDatabase.LoadAssetAtPath<BaffItemDatabase>("Assets/Scripts/BaffItemDatabase.asset");
            Check(database != null, "arcadeを開いた後の効果データの読み込み");
            upgrades = database.allBaffItems.Where(item => item != null && item.B_itemID >= 14 && item.B_itemID <= 23).ToArray();
            var manager = arcade.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RequestManager>(true)).Single();
            var registeredDatabase = new SerializedObject(manager).FindProperty("upgradeDatabase").objectReferenceValue;
            Check(registeredDatabase != null && registeredDatabase == database, "arcadeへの登録");
            Check(GameObject.FindGameObjectsWithTag("Recipe").Any(item => item.GetComponent<Collider>() != null), "中央レシピ台の配置面");
            FestivalUpgradeRuntime.Configure(database);
            ownership.Clear();
            var streak = new DeliveryStreakState();
            var liquid = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Prefab/Item/浄化液.asset");
            Check(FestivalUpgradeRuntime.GenerationReward(200, RequestType.DeliverItem) == 200, "未所持の報酬を維持");
            Check(FestivalUpgradeRuntime.CraftOutputCount(liquid, true) == 1, "未所持なら1本");
            OwnedProgressManager.AddBaffItem(15);
            Check(FestivalUpgradeRuntime.CraftOutputCount(liquid, true) == 2, "調合の浄化液は2本");
            Check(FestivalUpgradeRuntime.CraftOutputCount(liquid, false) == 1, "調合以外は対象外");
            var herb = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Prefab/Item/薬草.asset");
            Check(FestivalUpgradeRuntime.CraftOutputCount(herb, true) == 1, "浄化液以外は対象外");
            ownership.Clear(); OwnedProgressManager.AddBaffItem(16);
            var ore = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Prefab/Item/魔道鉱石.asset");
            var instance = new ItemInstanceState();
            Check(FestivalUpgradeRuntime.RetainOre(ore, instance), "鉱石の初回返却");
            Check(!FestivalUpgradeRuntime.RetainOre(ore, instance), "同じ鉱石は2回目に消費");
            Check(!FestivalUpgradeRuntime.RetainOre(herb, new ItemInstanceState()), "対象外素材を返却しない");
            temporary = new GameObject("FestivalValidationTemporary");
            var slot = temporary.AddComponent<InventorySlotUI>();
            instance.tableSeconds = 20;
            slot.AssignItem(ore, instance);
            var saved = slot.InstanceState.Copy();
            slot.ClearSlot(); slot.AssignItem(ore, saved);
            Check(slot.InstanceState.oreReuseSpent && slot.InstanceState.tableSeconds == 20, "持ち物復元時の状態を保持");
            ownership.Clear(); OwnedProgressManager.AddBaffItem(14);
            Check(FestivalUpgradeRuntime.DeliveryReward(200, RequestType.DeliverItem, streak, null) == 200, "連続納品の1回目");
            streak.Complete(RequestType.DeliverItem);
            Check(FestivalUpgradeRuntime.DeliveryReward(200, RequestType.DeliverItem, streak, null) == 220, "連続納品の2回目");
            streak.Complete(RequestType.PurifyWeapon);
            Check(FestivalUpgradeRuntime.DeliveryReward(200, RequestType.DeliverItem, streak, null) == 200, "種類変更でリセット");
            ownership.Clear(); OwnedProgressManager.AddBaffItem(17);
            Check(FestivalUpgradeRuntime.LuckyReward(200, .1) == 300 && FestivalUpgradeRuntime.LuckyReward(200, .21) == 200, "20%納品抽選の当たりと外れ");
            ownership.Clear(); OwnedProgressManager.AddBaffItem(19);
            Check(FestivalUpgradeRuntime.DeliveryReward(200, RequestType.DeliverItem, streak, new ItemInstanceState {tableSeconds=10}) == 220, "台に10秒置くと+10%");
            Check(FestivalUpgradeRuntime.DeliveryReward(200, RequestType.DeliverItem, streak, new ItemInstanceState {tableSeconds=1000}) == 300, "台の上限+50%");
            ownership.Clear(); OwnedProgressManager.AddBaffItem(20);
            Check(FestivalUpgradeRuntime.GenerationReward(200, RequestType.DeliverItem) == 260 && Mathf.Approximately(FestivalUpgradeRuntime.SpeedMultiplier(), .8f), "低速と報酬の交換");
            foreach (var pair in new[] {(id:21, type:RequestType.CraftWeapon), (id:22, type:RequestType.DeliverItem), (id:23, type:RequestType.PurifyWeapon)})
            {
                ownership.Clear(); OwnedProgressManager.AddBaffItem(pair.id);
                Check(FestivalUpgradeRuntime.GenerationReward(200, pair.type) == 300, "専業の対象+50% " + pair.id);
                Check(FestivalUpgradeRuntime.GenerationReward(200, RequestType.AddAttribute_Fire) == 160, "専業の対象外-20% " + pair.id);
            }
            ownership.Clear(); foreach (int id in new[]{21,22,23}) OwnedProgressManager.AddBaffItem(id);
            Check(FestivalUpgradeRuntime.GenerationReward(200, RequestType.DeliverItem) == 220, "専業3種併用時の丸め");
            Check(upgrades.Where(item => item.uniquePurchase).Select(item => item.B_itemID).OrderBy(id=>id).SequenceEqual(new[]{15,16,18}), "重複購入なしの対象");
        }
        finally
        {
            if (temporary != null) UnityEngine.Object.DestroyImmediate(temporary);
            ownership.Clear(); foreach (var entry in savedOwnership) ownership.Add(entry.Key, entry.Value);
            FestivalUpgradeRuntime.Configure(null);
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        Debug.Log($"[FestivalUpgradesValidation] PASS: {checks}項目。10種類の登録・報酬・完成数・鉱石返却・持ち物状態を確認。見た目と操作感は再生して確認してください。");
    }

    [MenuItem(Menu, true)] private static bool CanRun() => !EditorApplication.isPlaying;

    private const string StreakMenu = "MIS/テスト/連続納品テストを準備（睡眠薬3個）";

    [MenuItem(StreakMenu, true)]
    private static bool CanPrepareStreak() => EditorApplication.isPlaying && FestivalUpgradeRuntime.InArcade;

    [MenuItem(StreakMenu)]
    private static void PrepareStreak()
    {
        if (!PrepareDeliveryFixture(14, "連続納品テスト", out _, out _)) return;
        Debug.Log("[連続納品テスト] 準備完了：睡眠薬3個と依頼3件、連続手帳1個、目標10000G。Pで再開し、納品所で睡眠薬を3件続けて納品してください。期待する入金は200G→220G→240G、所持金は200G→420G→660Gです。途中で別の依頼を納品しないでください。再生を止めればこのテスト準備は終了します。");
    }

    private static bool PrepareDeliveryFixture(int effectId, string label,
        out RequestManager manager, out List<Request> requests)
    {
        manager = null;
        requests = null;
        if (!CanPrepareStreak()) return false;
        if (Time.timeScale != 0f)
        {
            Debug.LogWarning($"[{label}] Game画面でPを押して一時停止してから実行してください。");
            return false;
        }
        manager = UnityEngine.Object.FindFirstObjectByType<RequestManager>(FindObjectsInactive.Include);
        var inventory = InventoryManager.Instance;
        var clock = GameClockText.Instance;
        var database = AssetDatabase.LoadAssetAtPath<BaffItemDatabase>("Assets/Scripts/BaffItemDatabase.asset");
        var potion = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Prefab/Item/睡眠薬.asset");
        if (manager == null || inventory == null || clock == null || database == null || potion == null)
        {
            Debug.LogWarning($"[{label}] arcadeの初期化を待ってから実行してください。");
            return false;
        }
        // Require a fresh run, rather than discarding inventory or changing other owned effects.
        bool hasOtherEffects = database.allBaffItems.Any(item => item != null &&
            OwnedProgressManager.GetBaffOwned(item.B_itemID) > (item.B_itemID == effectId ? 1 : 0));
        if (MoneyManager.currentMoney != 0 || RequestManager.RequestCompleted != 0 || hasOtherEffects ||
            Enumerable.Range(0, 4).Any(i => inventory.GetSlot(i) == null || inventory.GetSlot(i).IsOccupied))
        {
            Debug.LogWarning($"[{label}] 再生し直し、すぐPで一時停止してください。10種類まとめて取得は使わず、この準備メニューだけを実行します。");
            return false;
        }
        var active = manager.GetActiveRequests();
        var potions = active.Where(request => request != null && !request.isCompleted &&
            request.requestType == RequestType.DeliverItem && request.requiredItem == potion).ToList();
        var settings = new SerializedObject(manager);
        var nextRequestTime = typeof(RequestManager).GetField("nextRequestTime", BindingFlags.Instance | BindingFlags.NonPublic);
        int capacity = settings.FindProperty("maxRequests").intValue;
        int needed = 3 - potions.Count;
        if (active.Count == 0 || needed < 0 || active.Count + needed > capacity)
        {
            Debug.LogWarning($"[{label}] 依頼の空きが足りません。再生し直し、Game画面ですぐPを押してから準備してください。");
            return false;
        }
        if (settings.FindProperty("requestBoard").objectReferenceValue == null ||
            settings.FindProperty("moneyManager").objectReferenceValue == null ||
            settings.FindProperty("requestTypesPool").arraySize == 0 || nextRequestTime == null ||
            potions.Any(request => EditorUtility.IsPersistent(request)))
        {
            Debug.LogWarning($"[{label}] 依頼管理の設定を確認してください。準備は行っていません。");
            return false;
        }
        var added = new List<Request>();
        try
        {
            for (int i = 0; i < needed; i++)
            {
                int countBefore = active.Count;
                manager.GenerateDeliveryForEditorTest(potion);
                if (active.Count != countBefore + 1) throw new InvalidOperationException("睡眠薬の依頼を生成できませんでした。");
                added.Add(active[active.Count - 1]);
            }
        }
        catch
        {
            foreach (var request in added) { active.Remove(request); UnityEngine.Object.Destroy(request); }
            var board = (RequestBoard)settings.FindProperty("requestBoard").objectReferenceValue;
            board.DisplayRequests();
            throw;
        }
        potions.AddRange(added);
        // These are runtime requests only. Fixed base rewards make each real delivery easy to compare.
        foreach (var request in potions) request.rewardAmount = 200;
        for (int i = 0; i < 3; i++) inventory.AddItemToSlot(i, potion, new ItemInstanceState());
        if (OwnedProgressManager.GetBaffOwned(effectId) == 0) OwnedProgressManager.AddBaffItem(effectId);
        clock.SetCompleteMoneyThreshold(10000);
        // Keep random new requests from mixing with the three fixed test deliveries.
        nextRequestTime.SetValue(manager, float.MaxValue);
        requests = potions;
        return true;
    }

    private const string LuckyMenu = "MIS/テスト/納品ボーナスの当たり外れを検証";

    [MenuItem(LuckyMenu, true)]
    private static bool CanRunLuckyDelivery() => CanPrepareStreak();

    [MenuItem(LuckyMenu)]
    private static void RunLuckyDelivery()
    {
        if (!PrepareDeliveryFixture(17, "納品ボーナス検証", out var manager, out var requests)) return;
        int passed = 0;
        Action<bool, string> assert = (result, label) =>
        {
            if (!result) throw new InvalidOperationException("納品ボーナス検証失敗: " + label);
            passed++;
        };
        var originalRandom = UnityEngine.Random.state;
        try
        {
            var effect = FestivalUpgradeRuntime.Owned(BaffEffectType.luckyDelivery);
            assert(effect != null && Mathf.Approximately(effect.upgradeChance, .2f) &&
                Mathf.Approximately(effect.upgradeBonusRate, .5f), "20%で報酬+50%の設定");
            var inventory = InventoryManager.Instance;
            Func<int> occupied = () => Enumerable.Range(0, 4).Count(i => inventory.GetSlot(i).IsOccupied);
            assert(occupied() == 3, "検証用の睡眠薬3個");
            var beforePreview = UnityEngine.Random.state;
            for (int i = 0; i < 3; i++)
                assert(manager.GetDeliveryReward(requests[i]) == 200, "納品前の表示200G");
            assert(beforePreview.Equals(UnityEngine.Random.state) && MoneyManager.currentMoney == 0 &&
                RequestManager.RequestCompleted == 0, "表示確認では抽選・入金しない");

            // Supply known random states only around this synchronous test call.
            // The production delivery code still draws its own Random.value and pays normally.
            Action<Request, bool, int, int> deliver = (request, win, expectedMoney, itemsLeft) =>
            {
                UnityEngine.Random.state = FindDeliveryRandomState(win);
                int moneyBefore = MoneyManager.currentMoney;
                assert(manager.TryDeliverByRequest(request), "通常の納品処理が成功");
                assert(MoneyManager.currentMoney - moneyBefore == expectedMoney,
                    "入金額 " + expectedMoney + "G");
                assert(request.isCompleted && !manager.GetActiveRequests().Contains(request) &&
                    occupied() == itemsLeft, "依頼完了と持ち物の消費");
            };
            deliver(requests[0], false, 200, 2);
            deliver(requests[1], true, 300, 1);

            int balance = MoneyManager.currentMoney;
            int completed = RequestManager.RequestCompleted;
            var beforeRepeat = UnityEngine.Random.state;
            assert(!manager.TryDeliverByRequest(requests[1]), "同じ依頼は二重納品できない");
            assert(MoneyManager.currentMoney == balance && RequestManager.RequestCompleted == completed &&
                beforeRepeat.Equals(UnityEngine.Random.state) && occupied() == 1,
                "二重納品で入金・抽選・消費しない");

            // Remove only the remaining fixture item, then restore it after checking rejection.
            var slot = inventory.FindSlotByItem(requests[2].requiredItem);
            var state = slot.InstanceState.Copy();
            slot.ClearSlot();
            try
            {
                var beforeMissing = UnityEngine.Random.state;
                Debug.Log("[納品ボーナス検証] 次の『デリバー失敗: 必要アイテムを所持していません』という警告は、持ち物がない場合の確認で意図的に出します。");
                assert(!manager.TryDeliverByRequest(requests[2]), "持ち物なしの納品を拒否");
                assert(MoneyManager.currentMoney == balance && RequestManager.RequestCompleted == completed &&
                    !requests[2].isCompleted && manager.GetActiveRequests().Contains(requests[2]) &&
                    beforeMissing.Equals(UnityEngine.Random.state), "失敗時に依頼・入金・抽選を進めない");
            }
            finally { slot.AssignItem(requests[2].requiredItem, state); }

            OwnedProgressManager.AddBaffItem(17);
            assert(OwnedProgressManager.GetBaffOwned(17) == 2 && Mathf.Approximately(effect.upgradeChance, .2f),
                "2個所持でも確率は20%のまま");
            deliver(requests[2], true, 400, 0);
            assert(MoneyManager.currentMoney == 900 && RequestManager.RequestCompleted == 3,
                "3件の実入金合計900G");
        }
        finally { UnityEngine.Random.state = originalRandom; }
        Debug.Log($"[LuckyDeliveryValidation] PASS: {passed}項目。外れ200G・当たり300G・2個所持の当たり400G、実入金合計900G。表示中の抽選なし、二重納品と持ち物なしの入金なし。抽選値を検証用に固定した結果です。自然な抽選の出現頻度の実測ではありません。");
    }

    private static UnityEngine.Random.State FindDeliveryRandomState(bool win)
    {
        var previous = UnityEngine.Random.state;
        try
        {
            for (int seed = 0; seed < 1024; seed++)
            {
                UnityEngine.Random.InitState(seed);
                var candidate = UnityEngine.Random.state;
                float roll = UnityEngine.Random.value;
                if (win ? roll < .1f : roll > .8f && roll < 1f) return candidate;
            }
            throw new InvalidOperationException("検証用の抽選状態を用意できませんでした。");
        }
        finally { UnityEngine.Random.state = previous; }
    }


    private const string TradeoffMenu = "MIS/テスト/速度と専業3種類の報酬を検証";
    private const string SlowToggleMenu = "MIS/テスト/重たい金の靴を切替（0個・1個）";

    [MenuItem(TradeoffMenu, true)]
    private static bool CanRunTradeoffs() => CanPrepareStreak();

    [MenuItem(TradeoffMenu)]
    private static void RunTradeoffs()
    {
        if (!CanRunTradeoffs()) return;
        if (Time.timeScale != 0f)
        {
            Debug.LogWarning("[速度・専業検証] Game画面でPを押して一時停止してから実行してください。");
            return;
        }
        var manager = UnityEngine.Object.FindFirstObjectByType<RequestManager>(FindObjectsInactive.Include);
        var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Include);
        var ownership = (Dictionary<int, int>)typeof(OwnedProgressManager)
            .GetField("baffOwnedById", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        if (manager == null || player == null || MoneyManager.currentMoney != 0 ||
            RequestManager.RequestCompleted != 0 || ownership.Any(entry => entry.Value > 0))
        {
            Debug.LogWarning("[速度・専業検証] 新しく再生してPで一時停止し、ほかの効果を取得せずに実行してください。");
            return;
        }
        var build = typeof(RequestManager).GetMethod("BuildRequest", BindingFlags.NonPublic | BindingFlags.Instance);
        if (build == null) throw new InvalidOperationException("依頼生成処理が見つかりません。");
        int passed = 0;
        Action<bool, string> assert = (result, label) =>
        {
            if (!result) throw new InvalidOperationException("速度・専業検証失敗: " + label);
            passed++;
        };
        var types = new[] {
            RequestType.DeliverItem, RequestType.PurifyWeapon, RequestType.CraftWeapon, RequestType.RepairWeapon,
            RequestType.AddAttribute_Fire, RequestType.AddAttribute_Frozen, RequestType.AddAttribute_Wind,
            RequestType.AddAttribute_Bright, RequestType.AddAttribute_Darkness
        };
        // Independent expected percentages, ordered by the request types above.
        // Owned counts are ordered: slow boots, smith, mixing, purification.
        var scenarios = new[] {
            (label:"靴1個", counts:new[]{1,0,0,0}, speed:.8f, percent:new[]{130,130,130,130,130,130,130,130,130}),
            (label:"靴2個", counts:new[]{2,0,0,0}, speed:.6f, percent:new[]{160,160,160,160,160,160,160,160,160}),
            (label:"靴4個・速度下限", counts:new[]{4,0,0,0}, speed:.4f, percent:new[]{220,220,220,220,220,220,220,220,220}),
            (label:"鍛冶専業", counts:new[]{0,1,0,0}, speed:1f, percent:new[]{80,80,150,150,80,80,80,80,80}),
            (label:"調合専業", counts:new[]{0,0,1,0}, speed:1f, percent:new[]{150,80,80,80,80,80,80,80,80}),
            (label:"浄化専業", counts:new[]{0,0,0,1}, speed:1f, percent:new[]{80,150,80,80,80,80,80,80,80}),
            (label:"調合専業2個", counts:new[]{0,0,2,0}, speed:1f, percent:new[]{200,60,60,60,60,60,60,60,60}),
            (label:"浄化専業2個", counts:new[]{0,0,0,2}, speed:1f, percent:new[]{60,200,60,60,60,60,60,60,60}),
            (label:"専業3種併用", counts:new[]{0,1,1,1}, speed:1f, percent:new[]{110,110,110,110,40,40,40,40,40}),
            (label:"靴と専業3種併用", counts:new[]{1,1,1,1}, speed:.8f, percent:new[]{140,140,140,140,70,70,70,70,70}),
            (label:"鍛冶6個・報酬下限", counts:new[]{0,6,0,0}, speed:1f, percent:new[]{0,0,400,400,0,0,0,0,0})
        };
        var savedOwnership = new Dictionary<int, int>(ownership);
        var savedRandom = UnityEngine.Random.state;
        var existing = manager.GetActiveRequests().ToArray();
        var existingRewards = existing.Select(request => request.rewardAmount).ToArray();
        var position = player.transform.position;
        var rotation = player.transform.rotation;
        var temporary = new List<Request>();
        try
        {
            Func<int, Request> generate = index =>
            {
                // Reuse the same draw for each type so only the owned effect changes.
                UnityEngine.Random.InitState(27100 + index);
                var request = (Request)build.Invoke(manager, new object[] {types[index]});
                if (request != null) temporary.Add(request);
                assert(request != null && request.requiredItem != null && request.rewardAmount >= 0,
                    types[index] + "の実依頼生成");
                return request;
            };
            ownership.Clear();
            assert(Mathf.Approximately(FestivalUpgradeRuntime.SpeedMultiplier(), 1f), "未所持の速度倍率100%");
            var baseline = Enumerable.Range(0, types.Length).Select(generate).ToArray();
            foreach (var scenario in scenarios)
            {
                ownership.Clear();
                for (int i = 0; i < 4; i++) OwnedProgressManager.AddBaffItem(20 + i, scenario.counts[i]);
                assert(Mathf.Approximately(FestivalUpgradeRuntime.SpeedMultiplier(), scenario.speed),
                    scenario.label + "の速度倍率");
                for (int i = 0; i < types.Length; i++)
                {
                    var request = generate(i);
                    assert(request.requiredItem == baseline[i].requiredItem &&
                        request.providedItem == baseline[i].providedItem && request.requestType == types[i],
                        scenario.label + ": 比較する依頼内容が同じ " + types[i]);
                    int expected = (int)((long)baseline[i].rewardAmount * scenario.percent[i] / 100);
                    assert(request.rewardAmount == expected && manager.GetDeliveryReward(request) == expected,
                        scenario.label + ": " + types[i] + " " + baseline[i].rewardAmount + "G → " +
                        expected + "G（実際 " + request.rewardAmount + "G）");
                }
            }
            assert(manager.GetActiveRequests().SequenceEqual(existing) &&
                existing.Select(request => request.rewardAmount).SequenceEqual(existingRewards),
                "生成済みの依頼と報酬は変化しない");
            assert(MoneyManager.currentMoney == 0 && RequestManager.RequestCompleted == 0,
                "検証で実納品や入金を行わない");
            assert(player.transform.position == position && player.transform.rotation == rotation,
                "プレイヤーの位置と向きは変化しない");
        }
        finally
        {
            ownership.Clear();
            foreach (var entry in savedOwnership) ownership.Add(entry.Key, entry.Value);
            UnityEngine.Random.state = savedRandom;
            foreach (var request in temporary) if (request != null) UnityEngine.Object.Destroy(request);
        }
        assert(ownership.Count == savedOwnership.Count && savedOwnership.All(entry =>
            ownership.TryGetValue(entry.Key, out int value) && value == entry.Value) &&
            savedRandom.Equals(UnityEngine.Random.state), "所持効果と乱数状態を復元");
        Debug.Log($"[RewardTradeoffValidation] PASS: {passed}項目。9種類の実依頼生成で靴・専業3種・複数所持・併用・報酬下限を比較。速度倍率100%→80%→60%、下限40%。所持効果と乱数を復元しました。歩行の操作感は別途確認してください。");
    }

    [MenuItem(SlowToggleMenu, true)]
    private static bool CanToggleSlowBoots() => CanPrepareStreak();

    [MenuItem(SlowToggleMenu)]
    private static void ToggleSlowBoots()
    {
        if (!CanToggleSlowBoots()) return;
        if (Time.timeScale != 0f)
        {
            Debug.LogWarning("[靴の比較] Pで一時停止してから切り替えてください。");
            return;
        }
        var ownership = (Dictionary<int, int>)typeof(OwnedProgressManager)
            .GetField("baffOwnedById", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        if (ownership.Any(entry => entry.Value > (entry.Key == 20 ? 1 : 0)))
        {
            Debug.LogWarning("[靴の比較] 再生し直し、ほかの効果を取得せずに使ってください。");
            return;
        }
        bool owned = OwnedProgressManager.GetBaffOwned(20) > 0;
        if (owned) ownership.Remove(20); else OwnedProgressManager.AddBaffItem(20);
        Debug.Log(owned
            ? "[靴の比較] 靴0個：移動速度100%。Pで再開して歩いてください。"
            : "[靴の比較] 靴1個：移動速度80%、新しく生成する依頼の報酬+30%。Pで再開して歩いてください。");
    }


    [MenuItem("MIS/テスト/追加効果の10種類を1個ずつ取得")]
    private static void AddAll()
    {
        if (!EditorApplication.isPlaying || !FestivalUpgradeRuntime.InArcade) return;
        for (int id=14;id<=23;id++) if(OwnedProgressManager.GetBaffOwned(id)==0) OwnedProgressManager.AddBaffItem(id);
        Debug.Log("[追加効果テスト] 10種類を取得しました。Tabで説明を確認できます。専業3種は同時に有効です。");
    }
}
#endif
