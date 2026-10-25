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
using Object = UnityEngine.Object;

// Runs only when explicitly requested. Uses real arcade components with virtual mouse input.
[InitializeOnLoad]
public static class KouaContentValidation
{
    const string Armed = "MIS.KouaContent.Validation";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static IEnumerator routine;
    static Mouse mouse;
    static Mouse previousMouse;
    static Keyboard keyboard;
    static Keyboard previousKeyboard;
    static int lastFrame;
    static double deadline;
    static bool previousMode;
    static Report report;
    static string ReportPath => Path.GetFullPath("Logs/koua-content-validation.json");
    [Serializable] class Report
    {
        public string status;
        public string evidence = "Unity Play Mode; real catalog, recipes, shop, requests, production effect hooks and QTE widgets. Human comfort/balance and full playthrough are not automated.";
        public List<string> passed = new List<string>();
        public List<string> engineErrors = new List<string>();
        public string failure;
        public List<string> screenshots = new List<string>();
    }
    static KouaContentValidation() => EditorApplication.playModeStateChanged += OnState;
    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch mode required.");
        Run();
    }
    [MenuItem("MIS/テスト/新アイテムと日替わり苦難を検証")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("再生を停止してください。");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("シーンを保存してから実行してください。");
        SessionState.SetBool(Armed + ".Previous", SessionState.GetBool(InteractionControlTrial.PreferenceKey, true));
        SessionState.SetBool(InteractionControlTrial.PreferenceKey, true);
        SessionState.SetBool(Armed, true);
        EditorSceneManager.OpenScene("Assets/Scenes/arcade.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
    static void OnState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Armed, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            previousMode = SessionState.GetBool(Armed + ".Previous", true);
            report = new Report { status = "RUNNING" };
            previousMouse = Mouse.current;
            mouse = InputSystem.AddDevice<Mouse>("MouseControlTrialTest");
            mouse.MakeCurrent();
            previousKeyboard = Keyboard.current;
            keyboard = InputSystem.AddDevice<Keyboard>("MouseControlTrialKeyboardTest");
            keyboard.MakeCurrent();
            Application.runInBackground = true;
            Application.logMessageReceived += OnLog;
            lastFrame = -1;
            deadline = EditorApplication.timeSinceStartup + 360;
            routine = Suite();
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && report != null && report.status == "RUNNING")
            Finish(new InvalidOperationException("Test interrupted."));
    }
    static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            report.engineErrors.Add(message + "\n" + trace);
    }
    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Mouse trial test timed out.");
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            if (!routine.MoveNext())
            {
                Check(report.engineErrors.Count == 0, "no Unity errors during validation");
                Finish(null);
            }
        }
        catch (Exception ex) { Finish(ex); }
    }
    static void Finish(Exception ex)
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        SessionState.SetBool(Armed, false);
        SessionState.SetBool(InteractionControlTrial.PreferenceKey, previousMode);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        report.status = ex == null ? "PASS" : "FAIL";
        report.failure = ex?.ToString();
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        Debug.Log("[操作比較検証] " + report.status + " / " + report.passed.Count + " checks / " + ReportPath);
        if (Application.isBatchMode) EditorApplication.Exit(ex == null ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
    static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, Flags).GetValue(obj);
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    static void Call(object obj, string name) => obj.GetType().GetMethod(name, Flags).Invoke(obj, null);
    static void Check(bool result, string label)
    {
        if (!result) throw new InvalidOperationException(label);
        report.passed.Add(label);
    }
    static void Press(MouseButton button)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.Update();
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(button));
        InputSystem.Update();
    }
    static void PressKey(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        InputSystem.Update();
    }
    static void Capture(string name)
    {
        string path = Path.GetFullPath("Logs/koua-content-" + name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path);
        report.screenshots.Add(path);
    }
    static void ValidateLabel(TMPro.TextMeshProUGUI label, string name)
    {
        Canvas.ForceUpdateCanvases();
        label.ForceMeshUpdate(true, true);
        float canvasScale = label.canvas.rootCanvas.scaleFactor;
        float referenceSize = label.fontSize * Mathf.Abs(label.transform.lossyScale.y) / canvasScale;
        Check(referenceSize >= 40f && label.color.r > 0.9f && label.color.a > 0.9f
            && label.margin == Vector4.zero && !label.isTextOverflowing,
            name + " is readable (at least 40 reference pixels, white, no clipping)");
    }
    static void Release()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.Update();
    }
    static void GameplayCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    static void PickupUpdate(ItemPickup pickup)
    {
        pickup.enabled = true;
        Call(pickup, "Update");
        pickup.enabled = false;
    }
    const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static void Challenge(DailyChallenge value) => typeof(DailyChallengeRuntime).GetField("selected", StaticFlags).SetValue(null, value);
    static object Invoke(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Flags).Invoke(obj, args);
    static void Own(params int[] ids)
    {
        OwnedProgressManager.ResetAll();
        foreach (int id in ids) OwnedProgressManager.AddArtifact(id);
    }
    static IEnumerator Suite()
    {
        for (int i = 0; i < 10; i++) yield return null;
        var clock = GameClockText.Instance; clock.StopAllCoroutines(); clock.enabled = false;
        var runtime = Find<DailyChallengeRuntime>(); runtime.enabled = false;
        var requests = Find<RequestManager>(); requests.enabled = false;
        var put = Find<PutItem>(); put.enabled = false;
        var pickup = Find<ItemPickup>(); pickup.enabled = false;
        var player = Find<FirstPersonController>(); player.enabled = false;
        var delivery = Find<DeliveryStation>(); delivery.enabled = false;
        var recipeStation = Find<RecipeStation>(); recipeStation.enabled = false;
        Own();
        var settings = Object.Instantiate(DailyChallengeRuntime.Settings);
        typeof(DailyChallengeRuntime).GetField("settings", StaticFlags).SetValue(null, settings);
        var catalog = KouaArtifactRuntime.Catalog;
        Check(catalog != null && catalog.artifacts.Length == 10 && catalog.artifacts.All(a => a != null), "10 new artifact assets load");
        Check(catalog.artifacts.Select(a => a.A_itemID).Distinct().Count() == 10, "new IDs are unique");
        Check(settings.enabledChallenges.Distinct().Count() == 20, "all 20 daily challenges enabled");
        foreach (var artifact in catalog.artifacts)
        {
            Check(artifact.uniquePurchase && artifact.startprice > 0 && !string.IsNullOrEmpty(artifact.description), artifact.itemName + ": purchase data");
            var img = artifact.prefab.GetComponent<UnityEngine.UI.Image>();
            Check(img != null && img.sprite != null && artifact.prefab.GetComponent<UnityEngine.UI.Button>() != null, artifact.itemName + ": real sprite/button asset");
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(img.sprite.texture));
            Check(importer.textureType == TextureImporterType.Sprite && importer.alphaIsTransparency, artifact.itemName + ": alpha sprite import");
        }

        DailyChallengeRuntime.BeginDay(1);
        Check(DailyChallengeRuntime.Current == DailyChallenge.None, "day 1 has no hardship");
        foreach (var item in catalog.artifacts) OwnedProgressManager.AddArtifact(item.A_itemID);
        var seen = new HashSet<DailyChallenge>(); var previous = DailyChallenge.None;
        for (int day = 2; day <= 600; day++)
        {
            DailyChallengeRuntime.BeginDay(day); var chosen = DailyChallengeRuntime.Current;
            Check(chosen != DailyChallenge.None && chosen != previous, "day " + day + ": changes from previous day");
            DailyChallengeRuntime.BeginDay(day);
            Check(DailyChallengeRuntime.Current == chosen, "same-day selection remains stable");
            seen.Add(chosen); previous = chosen;
        }
        Check(seen.Count == 20, "all 20 can be selected over successive days");
        settings.enabledChallenges = new[] { DailyChallenge.DisabledArtifacts };
        DailyChallengeRuntime.BeginDay(601);
        Check(catalog.artifacts.Count(a => !KouaArtifactRuntime.Has(a.A_itemID)) == 3, "three different owned artifacts disabled");
        Check(catalog.artifacts.All(a => OwnedProgressManager.GetArtifactOwned(a.A_itemID) == 1), "disabled artifacts remain owned");
        settings.enabledChallenges = new[] { DailyChallenge.HigherGoal };
        DailyChallengeRuntime.BeginDay(602);
        Check(catalog.artifacts.All(a => KouaArtifactRuntime.Has(a.A_itemID)), "disabled artifacts restored next day");
        Own();
        Challenge(DailyChallenge.HigherGoal); Check(DailyChallengeRuntime.Goal(1000) == 1250, "higher goal +25%");
        Challenge(DailyChallenge.ShorterDay); Check(DailyChallengeRuntime.Duration(120) == 100, "shorter day -20 seconds");
        foreach (var row in new[] { (DailyChallenge.FewerMixing, RequestType.DeliverItem), (DailyChallenge.FewerPurification, RequestType.PurifyWeapon), (DailyChallenge.FewerBlacksmith, RequestType.RepairWeapon) })
        { Challenge(row.Item1); Check(Mathf.Approximately(DailyChallengeRuntime.Weight(row.Item2), .35f), row.Item1 + " reduced request weight"); }
        Challenge(DailyChallenge.HarderQte); Check(DailyChallengeRuntime.QteDifficulty == 1.5f, "harder QTE multiplier");
        Challenge(DailyChallenge.LostOutput);
        Check(DailyChallengeRuntime.Output(1, 0) == 0 && DailyChallengeRuntime.Output(2, 0) == 1 && DailyChallengeRuntime.Output(1, .5f) == 1, "lost output handles zero and double output");
        Challenge(DailyChallenge.Turbo); Check(DailyChallengeRuntime.Speed == 2f, "turbo multiplier");
        Challenge(DailyChallenge.RotatingWorkBan);
        for (int i = 0; i < 3; i++)
        { Set(runtime, "elapsed", i * 20f); Check(DailyChallengeRuntime.WorkBlocked(new[] { "craft", "wash", "blacksmith" }[i]), "rotating work ban " + i); }
        Challenge(DailyChallenge.RedLightGreenLight); Set(runtime, "elapsed", 10.5f); Set(clock, "remainingTime", 120f);
        DailyChallengeRuntime.ObserveMovement(new Vector3(.2f, 0, 0));
        DailyChallengeRuntime.ObserveMovement(new Vector3(.2f, 0, 0));
        Check(Get<float>(clock, "remainingTime") == 115, "red light costs five seconds once per stop interval");
        Set(runtime, "elapsed", 13f); DailyChallengeRuntime.ObserveMovement(new Vector3(.2f, 0, 0));
        Check(Get<float>(clock, "remainingTime") == 115, "green light permits movement");

        Own(); delivery.CursorActive = false; GameplayCursor();
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false; player.transform.position = new Vector3(1000, 1000, 1000); controller.enabled = true;
        Set(player, "yRotation", 0f); Challenge(DailyChallenge.NoRightTurn);
        InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(10, 0) }); InputSystem.Update(); Call(player, "Update");
        Check(Get<float>(player, "yRotation") == 0, "right-turn hardship blocks rightward mouse input");
        InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(-10, 0) }); InputSystem.Update(); Call(player, "Update");
        Check(Get<float>(player, "yRotation") < 0, "right-turn hardship still permits left turns");
        Own(100); Set(player, "yRotation", 0f);
        InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(10, 0) }); InputSystem.Update(); Call(player, "Update");
        Check(Get<float>(player, "yRotation") < 0, "another round reverses mouse view");
        Release(); PressKey(Key.W); Set(player, "yRotation", 0f);
        Vector3 movementStart = player.transform.position; Call(player, "Update");
        Check(player.transform.position.z < movementStart.z, "another round reverses W movement");
        Own(); Set(player, "yRotation", 0f); Release(); PressKey(Key.W);
        movementStart = player.transform.position; Call(player, "Update"); float normalDistance = player.transform.position.z - movementStart.z;
        Challenge(DailyChallenge.Turbo); movementStart = player.transform.position; Call(player, "Update");
        Check(normalDistance > 0 && Mathf.Abs(player.transform.position.z - movementStart.z - 2 * normalDistance) < .002f, "turbo doubles actual forward distance");
        MoneyManager.currentMoney = 0; Set(clock, "remainingTime", 120f); Call(clock, "Update");
        Check(Mathf.Abs(Get<float>(clock, "remainingTime") - (120f - 2 * Time.deltaTime)) < .001f, "turbo doubles real clock consumption");
        Release(); Challenge(DailyChallenge.SlipperyMovement); Set(player, "slipperyVelocity", Vector3.forward);
        movementStart = player.transform.position; Call(player, "Update");
        Check(player.transform.position.z > movementStart.z, "sliding continues after releasing movement keys");
        player.ResetToStartState(); player.enabled = false; Release();

        var request = ScriptableObject.CreateInstance<Request>(); request.requestType = RequestType.RepairWeapon;
        foreach (var row in new[] { (100, 500), (102, 130), (103, 160), (105, 300) })
        { Own(row.Item1); Check(KouaArtifactRuntime.Reward(100, request) == row.Item2, "reward effect " + row.Item1); }
        Own(105); Check(DailyChallengeRuntime.Duration(120) == 90, "time is money removes 30 seconds");
        Own(108); Check(KouaArtifactRuntime.RollPayment(100, .1f) == 150 && KouaArtifactRuntime.RollPayment(100, .9f) == 50, "wealth coin toss both outcomes");
        Own(107); request.requestType = RequestType.DeliverItem; Check(KouaArtifactRuntime.Reward(100, request) == 130, "mixing reward +30%");
        foreach (var row in new[] { (DailyChallenge.OrePenalty, catalog.ores, 106), (DailyChallenge.PlantPenalty, catalog.plants, 101), (DailyChallenge.MushroomPenalty, catalog.mushrooms, -1) })
        {
            Own(); request.providedItem = null;
            var result = catalog.recipes.SelectMany(db => db.allRecipes).First(r => r != null && r.requiredItems.Any(row.Item2.Contains));
            request.requiredItem = result.resultItem;
            Check(KouaArtifactRuntime.UsesMaterial(request, row.Item2), row.Item1 + " traverses real recipe ingredients");
            Challenge(row.Item1); Check(DailyChallengeRuntime.Reward(100, request) == 70, row.Item1 + " reward -30%");
            if (row.Item3 >= 0) { Own(row.Item3); Check(KouaArtifactRuntime.Reward(100, request) == 130, "material artifact " + row.Item3 + " +30%"); }
        }

        Own(109); GameplayCursor(); delivery.CursorActive = false; Release(); yield return null;
        PressKey(Key.M); Call(recipeStation, "Update");
        Check(recipeStation.IsUIOpen && Cursor.visible, "memory book opens real recipe UI with M");
        Release(); yield return null; PressKey(Key.F); Call(recipeStation, "Update");
        Check(!recipeStation.IsUIOpen && !Cursor.visible, "F closes memory recipe UI");
        Release(); yield return null; Challenge(DailyChallenge.NoRecipes); PressKey(Key.M); Call(recipeStation, "Update");
        Check(!recipeStation.IsUIOpen, "no recipes hardship blocks memory book too");
        Release(); yield return null; player.enabled = false;

        Own(); Challenge(DailyChallenge.DeliveryCooldown); Set(runtime, "elapsed", 0f);
        InventoryManager.Instance.ClearAllSlots(); requests.GetActiveRequests().Clear();
        var medicine = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Scripts/ItemDatabase.asset").allItems.First(i => i != null && i.itemType == ItemTypes.Medicine);
        var first = ScriptableObject.CreateInstance<Request>(); first.requiredItem = medicine; first.requestType = RequestType.DeliverItem; first.rewardAmount = 100;
        var second = Object.Instantiate(first); requests.GetActiveRequests().Add(first); requests.GetActiveRequests().Add(second);
        InventoryManager.Instance.AddItem(medicine); Check(requests.TryDeliverByRequest(first), "first delivery succeeds");
        InventoryManager.Instance.AddItem(medicine); Check(!requests.TryDeliverByRequest(second), "immediate second delivery blocked");
        Check(InventoryManager.Instance.HasItem(medicine), "blocked delivery retains item");
        Set(runtime, "elapsed", 5.1f); Check(requests.TryDeliverByRequest(second), "delivery resumes after cooldown");
        Challenge(DailyChallenge.ExpiringRequests); requests.GetActiveRequests().Clear();
        var expired = ScriptableObject.CreateInstance<Request>(); requests.GetActiveRequests().Add(expired);
        Get<Dictionary<Request,float>>(requests, "expiryTimes")[expired] = 0;
        var spawnSlot = new GameObject("ExpiryTestSlot").transform;
        var oldTarget = new GameObject("oldTarget"); var newTarget = new GameObject("newTarget"); newTarget.transform.SetParent(spawnSlot);
        Get<Dictionary<Request,Transform>>(requests, "requestToSlot")[expired] = spawnSlot;
        Get<Dictionary<Request,GameObject>>(requests, "requestTargets")[expired] = oldTarget;
        Call(requests, "ExpireRequests"); yield return null;
        Check(!requests.GetActiveRequests().Contains(expired) && newTarget != null && oldTarget != null, "expiry retains moved target and unrelated replacement");
        Object.Destroy(oldTarget); Object.Destroy(spawnSlot.gameObject);

        Own(104); var db = Get<RecipeDatabase>(put, "recipeDatabase");
        var mixing = db.allRecipes.First(r => r != null && r.requiredItems.Length == 2 && r.resultItem != null);
        var pair = Object.FindObjectsByType<PlacementSlots>(FindObjectsSortMode.None).Where(s => s.CompareTag("craft")).Take(2).ToArray();
        Check(pair.Length == 2, "two real mixing stations exist");
        foreach (var station in pair) station.ClearAllAndDestroyChildren(); yield return null;
        foreach (var station in pair) foreach (var ingredient in mixing.requiredItems)
        { Check(station.TryPlace(ingredient, out var anchor), "paired ingredient registered"); WorldItemPlacement.Spawn(ingredient, anchor.position, anchor.rotation, anchor, station.gameObject); }
        Set(put, "isCraftingInProgress", true);
        put.StartCoroutine((IEnumerator)Invoke(put, "ProcessCrafting", pair[0], db, pair[0].gameObject, new RaycastHit(), pair[0].gameObject));
        for (int i = 0; i < 3; i++) yield return null;
        Set(put, "isPowerGageCompleted", true);
        for (int i = 0; i < 60 && put.IsCraftingInProgress; i++) yield return null;
        Check(!put.IsCraftingInProgress && pair.All(s => s.GetItemInSlot(0) == mixing.resultItem), "one QTE completes both actual stations");
        // This case injects QTE success; hide its fixture UI before the unrelated layout screenshots.
        Get<UnityEngine.UI.Slider>(put, "powerGageSlider").gameObject.SetActive(false);
        player.enabled = false;
        foreach (var station in pair) station.ClearAllAndDestroyChildren(); yield return null;

        Own(); Challenge(DailyChallenge.LostOutput); settings.lostOutputChance = 1;
        foreach (var ingredient in mixing.requiredItems)
        { pair[0].TryPlace(ingredient, out var anchor); WorldItemPlacement.Spawn(ingredient, anchor.position, anchor.rotation, anchor, pair[0].gameObject); }
        Invoke(put, "CompleteRecipe", pair[0], mixing, true); yield return null;
        Check(pair[0].GetItemInSlot(0) == null && pair[0].GetItemInSlot(1) == null && pair[0].GetComponentsInChildren<ItemBehaviour>().Length == 0, "lost output consumes materials and creates no phantom result");
        settings.lostOutputChance = .1f;

        Own(103); var weaponDb = Get<RecipeDatabase>(put, "weaponRecipeDatabase");
        var repair = weaponDb.allRecipes.First(KouaArtifactRuntime.IsRepair);
        int normalTargets = Get<UnityEngine.UI.Image[]>(put, "blacksmithImagesB").Count(x => x != null);
        var qte = (IEnumerator)Invoke(put, "WaitForBlacksmithCompletion", repair); qte.MoveNext();
        var targets = Get<UnityEngine.UI.Image[]>(put, "blacksmithImagesB");
        Check(targets.Count(x => x != null && x.gameObject.activeSelf) == normalTargets + 3, "strange hammer adds three repair targets");
        foreach (var target in targets) if (target != null)
        {
            var button = target.GetComponent<UnityEngine.UI.Button>();
            Check(button != null, "repair target has clickable button");
            button.onClick.Invoke();
            Check(!target.gameObject.activeSelf, "click deactivates the correct original or cloned target");
        }
        while (qte.MoveNext()) yield return null;
        Check(Get<UnityEngine.UI.Image[]>(put, "blacksmithImagesB").Length == normalTargets, "temporary QTE targets cleaned up");
        player.enabled = false; yield return null;

        Own(); Challenge(DailyChallenge.ShuffledStations);
        var stations = Object.FindObjectsByType<PlacementSlots>(FindObjectsSortMode.None).Where(s => s.CompareTag("craft") || s.CompareTag("wash") || s.CompareTag("blacksmith")).ToArray();
        var before = stations.Select(s => s.GetSlotTransform(0).position).ToArray();
        int moved = DailyChallengeLayout.Apply(15231);
        Check(moved > stations.Length && stations.Where((s,i) => Vector3.Distance(before[i], s.GetSlotTransform(0).position) > .1f).Count() == stations.Length, "material sources and all station anchors moved");
        Check(stations.All(s => s.GetSlotTransform(0).IsChildOf(s.transform) && s.GetSlotTransform(1).IsChildOf(s.transform) && s.GetResultAnchor().IsChildOf(s.transform)), "station visuals, slots and results travel together");
        foreach (var station in stations)
        {
            var surface = station.GetComponent<Collider>();
            Check(surface != null && surface.bounds.size.sqrMagnitude > 0 && Vector3.Distance(surface.bounds.center, station.GetResultAnchor().position) < 4, "shuffled station has a nearby collider and result anchor");
        }
        Call(runtime, "Update"); Capture("shuffled-layout");
        for (int i = 0; i < 5; i++) yield return null;

        Own(); var shopLoad = SceneManager.LoadSceneAsync(SceneNames.Shop); while (!shopLoad.isDone) yield return null;
        for (int i = 0; i < 10; i++) yield return null;
        Check(DailyChallengeRuntime.Current == DailyChallenge.None && !KouaArtifactRuntime.Has(100), "effects inactive outside arcade");
        var shop = Find<ArtifactManager>();
        Check(catalog.artifacts.All(a => shop.ArtifactDatas.Contains(a)), "all 10 registered in real shop");
        foreach (var artifact in catalog.artifacts)
        {
            foreach (var slot in shop.slots) foreach (Transform child in slot) Object.Destroy(child.gameObject);
            yield return null;
            Own(); MoneyManager.currentMoney = 100000; shop.ArtifactDatas = new[] { artifact };
            Call(shop, "InitializeArtifact"); Call(shop, "SpawnItems");
            var button = shop.slots.SelectMany(s => s.GetComponentsInChildren<UnityEngine.UI.Button>()).FirstOrDefault();
            Check(button != null, artifact.itemName + ": shop offer displayed");
            Check(button.GetComponent<UnityEngine.UI.Image>().type == UnityEngine.UI.Image.Type.Simple && button.GetComponent<UnityEngine.UI.Image>().preserveAspect, "artwork keeps original aspect ratio");
            for (int i = 0; i < 10; i++) yield return null;
            Capture("shop-" + artifact.A_itemID);
            for (int i = 0; i < 3; i++) yield return null;
            int balance = MoneyManager.currentMoney; int price = artifact.price;
            button.onClick.Invoke();
            Check(OwnedProgressManager.GetArtifactOwned(artifact.A_itemID) == 1 && MoneyManager.currentMoney == balance - price, artifact.itemName + ": buys for shown price");
            button.onClick.Invoke(); Check(MoneyManager.currentMoney == balance - price, "sold item cannot charge twice");
            Call(shop, "SpawnItems");
            Check(!shop.slots.SelectMany(s => s.GetComponentsInChildren<UnityEngine.UI.Button>()).Any(), "unique owned item excluded from future offers");
        }
        Own(); Object.Destroy(settings); typeof(DailyChallengeRuntime).GetField("settings", StaticFlags).SetValue(null, null);
        DayAdvanceButton.ResetPersistentState(); GameClockText.ResetPersistentState();
    }

}
#endif
