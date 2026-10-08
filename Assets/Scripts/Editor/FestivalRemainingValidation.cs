#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicitly started integration tests. Never run just because Unity opens or scripts reload.
[InitializeOnLoad]
public static class FestivalRemainingValidation
{
    const string Armed = "MIS.FestivalRemaining.Armed";
    const string ReportPath = "MIS.FestivalRemaining.Report";
    const string ShopOnly = "MIS.FestivalRemaining.ShopOnly";
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Stack<IEnumerator> steps = new Stack<IEnumerator>();
    static Report report;
    static double deadline;
    static int lastFrame = -1;
    static Keyboard testKeyboard;
    static Mouse testMouse;
    static RequestManager manager;
    static BaffItemDatabase database;
    static ItemDatabase items;

    [Serializable] public sealed class Report
    {
        public string status = "RUNNING";
        public string phase;
        public string unityVersion;
        public string scope;
        public string evidence = "Unity Play Mode, real scenes/assets and gameplay methods; virtual game input only. Crafting QTE completion is supplied by the test because QTE behavior was checked separately.";
        public List<string> passed = new List<string>();
        public List<string> measurements = new List<string>();
        public List<string> engineErrors = new List<string>();
        public string failure;
    }

    static FestivalRemainingValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
    }

    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("RunBatch requires batch mode.");
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-festivalReport");
        if (index < 0 || index + 1 >= args.Length) throw new InvalidOperationException("Missing -festivalReport path.");
        Start(args[index + 1]);
    }

    [MenuItem("MIS/テスト/追加効果の残りをまとめて検証")]
    public static void RunFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("再生停止後に実行してください。");
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/festival-remaining-validation.json"));
        Start(path);
    }

    [MenuItem("MIS/テスト/ショップ購入と持ち物引継ぎを検証")]
    public static void RunShopFromMenu()
    {
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/festival-shop-validation.json"));
        Start(path, true);
    }

    static void Start(string path, bool shopOnly = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene changes before testing.");
        SessionState.SetString(ReportPath, Path.GetFullPath(path));
        SessionState.SetBool(ShopOnly, shopOnly);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, "{\"status\":\"STARTING\"}");
        SessionState.SetBool(Armed, true);
        SessionState.EraseBool("MIS.PurificationOnlyTest");
        EditorSceneManager.OpenScene("Assets/Scenes/arcade.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    static void OnPlayState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Armed, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            bool shopOnly = SessionState.GetBool(ShopOnly, false);
            report = new Report { unityVersion = Application.unityVersion, scope = shopOnly ? "shop only" : "all remaining effects" };
            deadline = EditorApplication.timeSinceStartup + 900;
            Application.runInBackground = true;
            Application.logMessageReceived += OnLog;
            if (!shopOnly)
            {
                testKeyboard = InputSystem.AddDevice<Keyboard>("FestivalTestKeyboard");
                testMouse = InputSystem.AddDevice<Mouse>("FestivalTestMouse");
                testKeyboard.MakeCurrent();
                testMouse.MakeCurrent();
            }
            steps.Clear();
            steps.Push(Suite(shopOnly));
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && report != null && report.status == "RUNNING")
            Finish(new InvalidOperationException("Play Mode ended before the test completed."));
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            report.engineErrors.Add(report.phase + ": " + message + "\n" + stack);
    }

    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Integration test timeout at " + report.phase);
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            while (steps.Count > 0)
            {
                var step = steps.Peek();
                if (!step.MoveNext()) { steps.Pop(); continue; }
                if (step.Current is IEnumerator nested) { steps.Push(nested); continue; }
                return;
            }
            if (report.engineErrors.Count > 0) throw new InvalidOperationException("Unity reported errors; see engineErrors.");
            Finish(null);
        }
        catch (Exception error) { Finish(error); }
    }

    static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        SessionState.SetBool(Armed, false);
        if (testKeyboard != null && testKeyboard.added) InputSystem.RemoveDevice(testKeyboard);
        if (testMouse != null && testMouse.added) InputSystem.RemoveDevice(testMouse);
        Time.captureDeltaTime = 0;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        report.status = error == null ? "PASS" : "FAIL";
        report.failure = error?.ToString();
        SaveReport();
        Debug.Log("[FestivalRemainingValidation] " + report.status + ": " + report.passed.Count + " checks. " + SessionState.GetString(ReportPath, ""));
        if (error != null) Debug.LogError(error);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }

    static void SaveReport() => File.WriteAllText(SessionState.GetString(ReportPath, ""), JsonUtility.ToJson(report, true));
    static void Phase(string name) { report.phase = name; SaveReport(); Debug.Log("[FestivalRemainingValidation] " + name); }
    static void Check(bool result, string label)
    {
        if (!result) throw new InvalidOperationException(report.phase + ": " + label);
        report.passed.Add(report.phase + ": " + label);
    }
    static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Fields).Invoke(target, args);
    static Dictionary<int, int> Ownership => (Dictionary<int, int>)typeof(OwnedProgressManager)
        .GetField("baffOwnedById", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    static void Own(params int[] ids)
    {
        Ownership.Clear();
        foreach (int id in ids) OwnedProgressManager.AddBaffItem(id);
    }
    static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }
    static IEnumerator Until(Func<bool> condition, string label, int frames = 1200)
    {
        while (!condition() && frames-- > 0) yield return null;
        Check(condition(), label);
    }
    static void FreezeArcade()
    {
        manager = Find<RequestManager>();
        Check(manager != null && InventoryManager.Instance != null, "arcade managers initialized");
        manager.enabled = false;
        GameClockText.Instance.enabled = false;
        GameClockText.Instance.StopAllCoroutines();
        GameClockText.Instance.SetCompleteMoneyThreshold(int.MaxValue);
        foreach (var player in Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None)) player.enabled = false;
        database = AssetDatabase.LoadAssetAtPath<BaffItemDatabase>("Assets/Scripts/BaffItemDatabase.asset");
        items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Scripts/ItemDatabase.asset");
        FestivalUpgradeRuntime.Configure(database);
        Time.timeScale = 1;
        Time.captureDeltaTime = 1f / 60f;
        QualitySettings.vSyncCount = 0;
    }
    static Request Build(RequestType type, int seed)
    {
        UnityEngine.Random.InitState(seed);
        var request = (Request)Call(manager, "BuildRequest", type);
        Check(request != null && request.requiredItem != null, "generated " + type);
        return request;
    }

    // Uses the actual delivery-button callback, including item removal and money update.
    static void Deliver(Request request, int expected, ItemInstanceState state = null)
    {
        InventoryManager.Instance.ClearAllSlots();
        Check(InventoryManager.Instance.AddItem(request.requiredItem, state), "delivery item available");
        manager.GetActiveRequests().Add(request);
        var list = Find<DeliveryUIList>();
        var go = Object.Instantiate(list.deliveryItemPrefab, list.contentParent);
        var row = go.GetComponent<DeliveryUIItem>();
        row.Setup(request, manager, list);
        Check(row.deliverButton.interactable && manager.GetDeliveryReward(request) == expected, "delivery quote " + expected + "G");
        int before = MoneyManager.currentMoney;
        int completed = RequestManager.RequestCompleted;
        row.deliverButton.onClick.Invoke();
        Check(request.isCompleted && !manager.GetActiveRequests().Contains(request), "button completes request");
        Check(MoneyManager.currentMoney - before == expected && RequestManager.RequestCompleted == completed + 1, "actual payment " + expected + "G");
        Check(!InventoryManager.Instance.HasItem(request.requiredItem), "delivered item consumed");
        Object.Destroy(go);
        Object.Destroy(request);
    }

    static IEnumerator Suite(bool shopOnly)
    {
        Phase("initialization");
        yield return Frames(8);
        FreezeArcade();
        OwnedProgressManager.ResetAll();
        MoneyManager.currentMoney = 0;
        RequestManager.RequestCompleted = 0;
        manager.GetActiveRequests().Clear();
        if (!shopOnly)
        {
            yield return SpecialistPayments();
            yield return Streak();
            yield return Forecast();
            yield return Ores();
            yield return Aging();
            yield return Walking();
        }
        yield return ShopRoundTrip();
        Phase("complete");
    }

    static IEnumerator SpecialistPayments()
    {
        Phase("specialists and slow boots: delivery UI payments");
        var types = (RequestType[])Enum.GetValues(typeof(RequestType));
        foreach (int id in new[] {20,21,22,23})
        foreach (var type in types)
        {
            Own();
            RequestManager.RequestCompleted = 0;
            var baseline = Build(type, 4010 + (int)type);
            int original = baseline.rewardAmount;
            var item = baseline.requiredItem;
            Own(id);
            var request = Build(type, 4010 + (int)type);
            int percent = id == 20 ? 130 :
                id == 21 && (type == RequestType.CraftWeapon || type == RequestType.RepairWeapon) ||
                id == 22 && type == RequestType.DeliverItem || id == 23 && type == RequestType.PurifyWeapon ? 150 : 80;
            Check(request.requiredItem == item, "same baseline item");
            Deliver(request, (int)((long)original * percent / 100));
            Object.Destroy(baseline);
            yield return null;
        }
    }

    static IEnumerator Streak()
    {
        Phase("streak reset and cap through actual deliveries");
        Own(14);
        DayAdvanceButton.Instance.OnClickAdvanceDay();
        for (int i = 0; i < 13; i++)
        {
            var request = Build(RequestType.DeliverItem, 701);
            request.rewardAmount = 200; // Isolate delivery bonuses from progressive base rewards.
            Deliver(request, 200 + 20 * Math.Min(i, 10));
            yield return null;
        }
        foreach (var sample in new[] {
            (RequestType.CraftWeapon,200), (RequestType.RepairWeapon,220),
            (RequestType.PurifyWeapon,200), (RequestType.DeliverItem,200), (RequestType.DeliverItem,220)})
        {
            var request = Build(sample.Item1, 706);
            request.rewardAmount = 200;
            Deliver(request, sample.Item2);
            yield return null;
        }
        DayAdvanceButton.Instance.OnClickAdvanceDay();
        var nextDay = Build(RequestType.DeliverItem, 701);
        nextDay.rewardAmount = 200;
        Deliver(nextDay, 200);
    }

    static IEnumerator Forecast()
    {
        Phase("forecast while full and after a slot opens");
        Own(18);
        manager.GetActiveRequests().Clear();
        int capacity = Get<int>(manager, "maxRequests");
        for (int i = 0; i < capacity; i++) manager.GetActiveRequests().Add(Build(RequestType.DeliverItem, 870+i));
        var preview = manager.GetUpcomingRequest();
        Check(preview != null, "preview exists at capacity");
        int reward = preview.rewardAmount;
        var item = preview.requiredItem;
        for (int i = 0; i < 8; i++)
        {
            Call(manager, "GenerateRequest");
            Check(manager.GetActiveRequests().Count == capacity && manager.GetUpcomingRequest() == preview,
                "full list does not reroll preview");
        }
        var first = manager.GetActiveRequests()[0];
        manager.GetActiveRequests().Remove(first);
        Deliver(first, manager.GetDeliveryReward(first));
        // Clear only old fixture spawn objects, so non-delivery predictions can spawn their target.
        foreach (var slot in Get<Transform[]>(manager, "requestSpawnSlots"))
            if (slot != null) foreach (Transform child in slot) Object.Destroy(child.gameObject);
        yield return Frames(2);
        Call(manager, "GenerateRequest");
        Check(manager.GetActiveRequests().Count == capacity && manager.GetActiveRequests().Contains(preview), "exact preview becomes active");
        Check(preview.requiredItem == item && preview.rewardAmount == reward, "preview item and price preserved");
        Check(manager.GetUpcomingRequest() != preview, "new forecast advances after arrival");
        manager.GetActiveRequests().Clear();
    }

    static void PutMaterial(PlacementSlots slots, ItemData item, ItemInstanceState state)
    {
        Check(slots.TryPlace(item, out var slot), "material slot available");
        var placed = Object.Instantiate(item.prefab, slot.position, slot.rotation, slot);
        placed.GetComponent<ItemBehaviour>().SetInstanceState(state);
    }
    static IEnumerator Craft(PutItem put, PlacementSlots slots, RecipeDatabase recipes)
    {
        Set(put, "isCraftingInProgress", true);
        var routine = (IEnumerator)Call(put, "ProcessCrafting", slots, recipes, slots.gameObject, new RaycastHit(), slots.gameObject);
        put.StartCoroutine(routine);
        yield return Frames(2);
        // Only supply QTE success. The real post-QTE material consumption/output code runs unchanged.
        Set(put, "isPowerGageCompleted", true);
        Set(put, "isWashCompleted", true);
        var targets = Get<Image[]>(put, "blacksmithImagesB");
        if (targets != null) foreach (var target in targets) if (target != null) target.gameObject.SetActive(false);
        yield return Until(() => !put.IsCraftingInProgress, "crafting finishes", 240);
        Find<FirstPersonController>().enabled = false;
        yield return Frames(2);
    }

    static IEnumerator Ores()
    {
        Phase("all ores: once-only reuse and full inventory fallback");
        Own(16);
        var put = Find<PutItem>();
        var effect = database.allBaffItems.Single(x => x != null && x.B_itemID == 16);
        var available = new[] {
            (tag:"craft", db:Get<RecipeDatabase>(put,"recipeDatabase")),
            (tag:"blacksmith", db:Get<RecipeDatabase>(put,"weaponRecipeDatabase")),
            (tag:"wash", db:Get<RecipeDatabase>(put,"washRecipeDatabase"))};
        foreach (var ore in effect.upgradeTargetItems)
        foreach (bool full in new[] {false, true})
        {
            var choice = available.First(x => x.db.allRecipes.Any(r => r != null && r.requiredItems.Length == 2 && r.requiredItems.Contains(ore)));
            var recipe = choice.db.allRecipes.First(r => r != null && r.requiredItems.Length == 2 && r.requiredItems.Contains(ore));
            var slots = Object.FindObjectsByType<PlacementSlots>(FindObjectsSortMode.None).First(s => s.CompareTag(choice.tag));
            var state = new ItemInstanceState { tableSeconds = 73 };
            for (int use = 0; use < 2; use++)
            {
                slots.ClearAllAndDestroyChildren();
                InventoryManager.Instance.ClearAllSlots();
                yield return Frames(2);
                var filler = items.allItems.First(x => x != null && x.itemName == "睡眠薬");
                if (full) for (int i = 0; i < 4; i++) InventoryManager.Instance.AddItem(filler);
                foreach (var material in recipe.requiredItems) PutMaterial(slots, material, material == ore ? state : new ItemInstanceState());
                yield return Craft(put, slots, choice.db);
                Check(state.oreReuseSpent, ore.itemName + " marks physical item used");
                var returnedSlot = InventoryManager.Instance.FindSlotByItem(ore);
                var returnedWorld = Object.FindObjectsByType<ItemBehaviour>(FindObjectsSortMode.None)
                    .Where(x => x.ItemData == ore && ReferenceEquals(x.InstanceState, state)).ToArray();
                if (use == 0 && !full) Check(returnedSlot != null && ReferenceEquals(returnedSlot.InstanceState,state), ore.itemName + " first use returns to inventory");
                if (use == 0 && full)
                {
                    Check(returnedWorld.Length == 1 && returnedSlot == null && InventoryManager.Instance.IsFull(), ore.itemName + " full inventory leaves one world item");
                    Check(Vector3.Distance(returnedWorld[0].transform.position,slots.GetResultAnchor().position)<1.2f, "returned ore stays beside the station");
                    var pickup = Find<ItemPickup>();
                    Set(pickup,"currentTargetItem",returnedWorld[0]);
                    Call(pickup,"TryPickupItem");
                    Check(returnedWorld[0] != null, "full pickup does not delete returned ore");
                    InventoryManager.Instance.GetSlot(0).ClearSlot();
                    Set(pickup,"currentTargetItem",returnedWorld[0]);
                    Call(pickup,"TryPickupItem");
                    Check(InventoryManager.Instance.FindSlotByItem(ore)?.InstanceState.oreReuseSpent == true, "pickup retains spent flag");
                    yield return Frames(2);
                }
                if (use == 1) Check(returnedSlot == null && returnedWorld.Length == 0, ore.itemName + " second use consumes it");
                Check(slots.GetItemInSlot(0) == recipe.resultItem, "real recipe output produced");
                Check(slots.GetSlotTransform(0).GetComponentInChildren<ItemBehaviour>().InstanceState.tableSeconds == 0,
                    "new crafted output does not inherit material aging");
            }
            slots.ClearAllAndDestroyChildren();
        }
        InventoryManager.Instance.ClearAllSlots();
        yield return Frames(2);
    }

    static IEnumerator Aging()
    {
        Phase("table elapsed time, reward cap and pickup persistence");
        Own(19);
        var potion = items.allItems.First(x => x != null && x.itemName == "睡眠薬");
        var table = GameObject.FindGameObjectsWithTag("Recipe").First(x => x.GetComponent<Collider>() != null);
        var world = Object.Instantiate(potion.prefab, table.GetComponent<Collider>().bounds.center + Vector3.up, Quaternion.identity);
        var behaviour = world.GetComponent<ItemBehaviour>();
        behaviour.SetInstanceState(new ItemInstanceState(), true);
        float start = Time.time;
        yield return Until(() => Time.time - start >= 55f, "55 simulated seconds elapsed", 4000);
        var state = behaviour.InstanceState;
        Check(Mathf.Abs(state.tableSeconds - (Time.time-start)) < .15f, "actual Update accumulates elapsed time");
        var request = Build(RequestType.DeliverItem, 760);
        request.requiredItem = potion;
        request.rewardAmount = 200;
        InventoryManager.Instance.AddItem(potion, state);
        Check(manager.GetDeliveryReward(request) == 300, "reward capped at +50% after 55s");
        InventoryManager.Instance.ClearAllSlots();
        var pickup = Find<ItemPickup>();
        Set(pickup,"currentTargetItem",behaviour);
        Call(pickup,"TryPickupItem");
        float atPickup = InventoryManager.Instance.FindSlotByItem(potion).InstanceState.tableSeconds;
        yield return Frames(120);
        Check(Mathf.Approximately(InventoryManager.Instance.FindSlotByItem(potion).InstanceState.tableSeconds, atPickup), "inventory does not add table time");
        Deliver(request, 300, state);
        var fresh = Object.Instantiate(potion.prefab).GetComponent<ItemBehaviour>();
        Check(fresh.InstanceState.tableSeconds == 0, "fresh output starts unaged");
        Object.Destroy(fresh.gameObject);
        report.measurements.Add("Table elapsed=" + (Time.time-start).ToString("F3") + "s, at pickup=" + atPickup.ToString("F3") + "s, capped payout=300G for base200G.");
    }

    static IEnumerator Walking()
    {
        Phase("measured CharacterController walking distance");
        var player = Find<FirstPersonController>();
        var controller = player.GetComponent<CharacterController>();
        player.enabled = false;
        var delivery = Get<DeliveryStation>(player,"deliveryStation");
        delivery.CursorActive = false;
        player.gravity = 0;
        Set(player,"verticalVelocity",0f);
        var distances = new List<float>();
        foreach (int count in new[] {0,1,2,4})
        {
            Own();
            OwnedProgressManager.AddBaffItem(20,count);
            controller.enabled = false;
            player.transform.position = new Vector3(0,1000,0); // Clear of level collisions; same controller and movement method.
            player.playerBody.rotation = Quaternion.identity;
            Set(player,"yRotation",0f);
            controller.enabled = true;
            Physics.SyncTransforms();
            var start = player.transform.position;
            float seconds = 0;
            for (int i = 0; i < 120; i++)
            {
                InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.W));
                InputSystem.Update();
                Call(player,"Update");
                seconds += Time.deltaTime;
                yield return null;
            }
            float distance = Vector3.Distance(start,player.transform.position);
            distances.Add(distance/seconds);
            report.measurements.Add("Boots=" + count + ", duration=" + seconds.ToString("F3") + "s, distance=" + distance.ToString("F3") + ", speed=" + (distance/seconds).ToString("F3"));
        }
        Check(distances[0] > 0, "baseline physically moves");
        Check(Mathf.Abs(distances[1]/distances[0]-.8f)<.01f, "one boot actual speed 80%");
        Check(Mathf.Abs(distances[2]/distances[0]-.6f)<.01f, "two boots actual speed 60%");
        Check(Mathf.Abs(distances[3]/distances[0]-.4f)<.01f, "four boots actual speed floor 40%");
        InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());
        InputSystem.Update();
        player.ResetToStartState();
    }

    static IEnumerator ShopRoundTrip()
    {
        Phase("shop purchase, uniqueness and inventory round trip");
        Own();
        InventoryManager.Instance.ClearAllSlots();
        var potion = items.allItems.First(x => x != null && x.itemName == "睡眠薬");
        var ore = database.allBaffItems.Single(x => x != null && x.B_itemID == 16).upgradeTargetItems[0];
        string potionName = potion.itemName;
        string oreName = ore.itemName;
        InventoryManager.Instance.AddItemToSlot(0,potion,new ItemInstanceState {tableSeconds=37});
        InventoryManager.Instance.AddItemToSlot(2,ore,new ItemInstanceState {oreReuseSpent=true});
        MoneyManager.currentMoney = 5000;
        int day = DayAdvanceButton.GetDayStatic();
        int completed = RequestManager.RequestCompleted;
        Check(ChangeScene.Instance != null, "persistent scene state manager exists");
        ChangeScene.Instance.GoToShop();
        yield return Until(() => SceneManager.GetActiveScene().name == SceneNames.Shop, "entered real Shop scene");
        yield return Frames(8);
        var shop = Find<ShopManager>();
        Check(shop != null && MoneyManager.currentMoney == 5000, "shop initialized without losing money");
        var all = shop.baffitemDatas.ToArray();
        int spent = 0;
        for (int id = 14; id <= 23; id++)
        {
            foreach (var slot in shop.slots) foreach (Transform child in slot) Object.Destroy(child.gameObject);
            yield return Frames(2);
            var item = all.Single(x => x != null && x.B_itemID == id);
            shop.baffitemDatas = new[] {item};
            shop.spawnCount = 1;
            Call(shop,"SpawnItems");
            // Slot roots also have Button components. Only spawned children receive
            // ShopManager's purchase callback, so inspect those roots exactly as it does.
            var offers = shop.slots.SelectMany(s => s.Cast<Transform>())
                .Select(child => child.GetComponent<Button>()).Where(button => button != null).ToArray();
            Check(offers.Length == 1, "exactly one spawned purchase button for " + id + " (found " + offers.Length + ")");
            var button = offers[0];
            int price = item.price;
            Check(price == 150, "initial price150 for " + id);
            MoneyManager.currentMoney = price-1;
            button.onClick.Invoke();
            Check(OwnedProgressManager.GetBaffOwned(id)==0 && MoneyManager.currentMoney==price-1 && button.gameObject.activeSelf, "insufficient money does not purchase " + id);
            MoneyManager.currentMoney = 5000-spent;
            button.onClick.Invoke();
            spent += price;
            Check(OwnedProgressManager.GetBaffOwned(id)==1 && MoneyManager.currentMoney==5000-spent && !button.gameObject.activeSelf, "real purchase and charge " + id);
            if (item.uniquePurchase)
            {
                button.onClick.Invoke();
                Check(OwnedProgressManager.GetBaffOwned(id)==1 && MoneyManager.currentMoney==5000-spent, "unique item refuses repeat callback " + id);
                Object.Destroy(button.gameObject);
                yield return Frames(2);
                Call(shop,"SpawnItems");
                Check(shop.slots.All(s => s.childCount==0), "owned unique item removed from offers " + id);
            }
        }
        var back = Get<Button>(shop,"backToArcadeButton");
        if (back == null)
            back = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(button =>
                Enumerable.Range(0,button.onClick.GetPersistentEventCount()).Any(index =>
                    button.onClick.GetPersistentTarget(index) is LoadTutorialScene loader &&
                    Get<string>(loader,"sceneName") == SceneNames.Arcade &&
                    button.onClick.GetPersistentMethodName(index) == "LoadScene"));
        Check(back != null, "shop return button is wired");
        yield return Until(() => FadeManager.Instance == null || !FadeManager.Instance.IsFading(), "shop fade complete");
        back.onClick.Invoke();
        yield return Until(() => SceneManager.GetActiveScene().name == SceneNames.Arcade, "return button loads arcade");
        yield return Frames(8);
        FreezeArcade();
        Check(MoneyManager.currentMoney==5000-spent, "shop spending retained on return");
        Check(Enumerable.Range(14,10).All(id => OwnedProgressManager.GetBaffOwned(id)==1), "all ten purchased effects retained");
        var inv = InventoryManager.Instance;
        Check(inv.GetSlot(0).CurrentItem?.itemName == potionName && Mathf.Approximately(inv.GetSlot(0).InstanceState.tableSeconds,37), "aged inventory item restored with time");
        Check(inv.GetSlot(2).CurrentItem?.itemName == oreName && inv.GetSlot(2).InstanceState.oreReuseSpent, "ore item restored with spent flag");
        Check(!inv.GetSlot(1).IsOccupied && !inv.GetSlot(3).IsOccupied, "empty slot positions preserved");
        Check(DayAdvanceButton.GetDayStatic()==day && RequestManager.RequestCompleted==completed, "day and delivery count preserved");
        Check(FestivalUpgradeRuntime.Owned(BaffEffectType.oreRetention)!=null && Mathf.Approximately(FestivalUpgradeRuntime.SpeedMultiplier(),.8f), "purchased effects active in arcade");
        report.measurements.Add("Shop: 10 purchases x150G; 5000G -> " + MoneyManager.currentMoney + "G; inventory slots/state restored.");
    }
}
#endif
