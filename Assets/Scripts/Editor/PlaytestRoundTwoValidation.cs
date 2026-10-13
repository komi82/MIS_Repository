#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit Play Mode integration checks for the five follow-up playtest reports.
[InitializeOnLoad]
public static class PlaytestRoundTwoValidation
{
    const string Armed = "MIS.PlaytestRoundTwo";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Stack<IEnumerator> steps = new Stack<IEnumerator>();
    static Report report;
    static double deadline;
    static int lastFrame;
    static Keyboard keyboard;
    static Mouse mouse;
    [Serializable] class Report
    {
        public string status = "RUNNING", phase, failure;
        public List<string> passed = new List<string>();
        public List<string> measurements = new List<string>();
        public List<string> errors = new List<string>();
    }
    static PlaytestRoundTwoValidation() => EditorApplication.playModeStateChanged += State;

    [MenuItem("MIS/テスト/追加の試遊5点を検証", false, 0)]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save scene changes first.");
        SessionState.SetBool(Armed, true);
        EditorSceneManager.OpenScene("Assets/Scenes/arcade.unity");
        EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Armed, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            report = new Report();
            Directory.CreateDirectory("Logs");
            Application.logMessageReceived += Log;
            Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>("RoundTwoKeyboard"); keyboard.MakeCurrent();
            mouse = InputSystem.AddDevice<Mouse>("RoundTwoMouse"); mouse.MakeCurrent();
            steps.Clear(); steps.Push(Suite());
            lastFrame = -1; deadline = EditorApplication.timeSinceStartup + 480;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && report?.status == "RUNNING")
            Finish(new Exception("Play Mode interrupted"));
    }
    static void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            report.errors.Add(report.phase + ": " + message + "\n" + trace);
    }
    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException(report.phase);
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            while (steps.Count > 0)
            {
                var step = steps.Peek();
                if (!step.MoveNext()) { steps.Pop(); continue; }
                if (step.Current is IEnumerator nested) { steps.Push(nested); continue; }
                return;
            }
            Check(report.errors.Count == 0, "No Unity exceptions or errors");
            Finish(null);
        }
        catch (Exception ex) { Finish(ex); }
    }
    static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log;
        SessionState.SetBool(Armed, false);
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        report.status = error == null ? "PASS" : "FAIL"; report.failure = error?.ToString();
        Save();
        Debug.Log("[PlaytestRoundTwo] " + report.status + " / " + report.passed.Count + " checks");
        EditorApplication.isPlaying = false;
    }
    static void Save() => File.WriteAllText("Logs/playtest-round-two.json", JsonUtility.ToJson(report, true));
    static void Phase(string value) { report.phase = value; Save(); }
    static void Check(bool ok, string label) { if (!ok) throw new Exception(report.phase + ": " + label); report.passed.Add(label); }
    static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Flags).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }
    static IEnumerator Until(Func<bool> condition, string label, double seconds = 120)
    {
        double stop = EditorApplication.timeSinceStartup + seconds;
        while (!condition() && EditorApplication.timeSinceStartup < stop) yield return null;
        Check(condition(), label);
    }
    static IEnumerator Faded() => Until(() => FadeManager.Instance == null || !FadeManager.Instance.IsFading(), "Fade finished");
    static void Freeze()
    {
        if (GameClockText.Instance != null) GameClockText.Instance.enabled = false;
        var request = Find<RequestManager>(); if (request != null) request.enabled = false;
        foreach (var player in Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None)) player.enabled = false;
        var put = Find<PutItem>(); if (put != null) put.enabled = false;
        var pickup = Find<ItemPickup>(); if (pickup != null) pickup.enabled = false;
    }
    static Button SceneButton(string scene) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .First(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i =>
            b.onClick.GetPersistentTarget(i) is LoadTutorialScene loader && Get<string>(loader, "sceneName") == scene));

    static IEnumerator Suite()
    {
        yield return Frames(8);
        Freeze();
        OwnedProgressManager.ResetAll(); MoneyManager.currentMoney = 0;
        yield return Materials();
        yield return GoalAndStock();
        yield return TutorialRecovery();
    }

    static IEnumerator Materials()
    {
        Phase("material size and surface placement");
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Scripts/ItemDatabase.asset");
        var camera = Get<Camera>(Find<PutItem>(), "mainCamera");
        var originalPosition = camera.transform.position; var originalRotation = camera.transform.rotation;
        var pot = Object.FindObjectsByType<PlacementSlots>(FindObjectsSortMode.None)
            .Where(s => s.CompareTag("craft")).OrderBy(s => Vector3.Distance(s.transform.position, camera.transform.position)).First();
        var roomCenter = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
            .Where(c => c.CompareTag("put") && c.GetComponentInParent<PlacementSlots>() == null)
            .OrderByDescending(c => c.bounds.size.x * c.bounds.size.z).First().bounds.center;
        foreach (int id in new[] { 14, 15, 16, 30 })
        {
            var item = db.allItems.Single(x => x.itemID == id);
            var raw = Object.Instantiate(item.prefab, new Vector3(1000, 1000, 1000), item.prefab.transform.rotation);
            Check(WorldItemPlacement.TryGetBounds(raw, out var original), "Original mesh exists: " + item.itemName);
            Check(pot.TryPlace(item, out var slot), "Material fits pot slot");
            var placed = WorldItemPlacement.Spawn(item, slot.position, slot.rotation, slot, pot.gameObject);
            Check(WorldItemPlacement.TryGetBounds(placed, out var bounds) && bounds.size.y >= .30f && bounds.size.y < .48f,
                "Material is readable without becoming taller than the bottle: " + item.itemName);
            Check(bounds.size.y / original.size.y >= 1.55f, "Material enlarged: " + item.itemName);
            var bottle = db.allItems.Single(x => x.itemID == 1);
            Check(pot.TryPlace(bottle, out var bottleSlot), "Bottle fits beside material");
            var bottleObject = WorldItemPlacement.Spawn(bottle, bottleSlot.position, bottleSlot.rotation, bottleSlot, pot.gameObject);
            WorldItemPlacement.TryGetBounds(bottleObject, out var bottleBounds);
            Check(!bounds.Intersects(bottleBounds), "Material and bottle do not overlap: " + item.itemName);
            Check(Mathf.Abs(bounds.min.y - bottleBounds.min.y) < .04f, "Material rests on same surface as bottle");
            report.measurements.Add(item.itemName + " before=" + original.size.ToString("F3") + " after=" + bounds.size.ToString("F3"));
            var center = (bounds.center + bottleBounds.center) * .5f;
            var approach = roomCenter - center; approach.y = 0; approach.Normalize();
            camera.transform.position = center + approach * 1.55f + Vector3.up * .7f;
            camera.transform.LookAt(center);
            yield return Frames(5);
            if (id == 14 || id == 30)
            {
                ScreenCapture.CaptureScreenshot("Logs/round2-material-" + id + ".png");
                yield return Frames(4);
            }
            pot.ClearAllAndDestroyChildren(); Object.Destroy(raw); yield return Frames(2);
        }
        camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
    }

    static IEnumerator GoalAndStock()
    {
        Phase("successful delivery keeps the outgoing Goal");
        var clock = GameClockText.Instance;
        DayAdvanceButton.Instance.SetDay(1); clock.UpdateCompleteThresholdByDay(1);
        var goalText = Get<TextMeshProUGUI>(clock, "completeThresholdText");
        var dayText = Get<TextMeshProUGUI>(DayAdvanceButton.Instance, "DayText");
        DayAdvanceButton.Instance.Updateday();
        int firstGoal = clock.GetCompleteMoneyThreshold(); string firstLabel = goalText.text;
        var manager = Find<RequestManager>(); manager.GetActiveRequests().Clear();
        var request = (Request)Call(manager, "BuildRequest", RequestType.DeliverItem);
        request.rewardAmount = firstGoal + 120;
        manager.GetActiveRequests().Add(request);
        InventoryManager.Instance.ClearAllSlots(); InventoryManager.Instance.AddItem(request.requiredItem);
        var list = Find<DeliveryUIList>();
        var rowObject = Object.Instantiate(list.deliveryItemPrefab, list.contentParent);
        var row = rowObject.GetComponent<DeliveryUIItem>(); row.Setup(request, manager, list);
        row.deliverButton.onClick.Invoke();
        Check(request.isCompleted && MoneyManager.currentMoney >= firstGoal, "Actual delivery button completes goal");
        clock.enabled = true;
        bool labelsKept = true, sawNextDayReserved = false;
        double stop = EditorApplication.timeSinceStartup + 150;
        while (SceneManager.GetActiveScene().name == SceneNames.Arcade && EditorApplication.timeSinceStartup < stop)
        {
            if (clock != null && goalText != null)
                labelsKept &= goalText.text == firstLabel && clock.GetCompleteMoneyThreshold() == firstGoal && dayText.text == "Day 1";
            if (DayAdvanceButton.GetDayStatic() == 2) sawNextDayReserved = true;
            yield return null;
        }
        Check(SceneManager.GetActiveScene().name == SceneNames.Shop, "Delivery reaches shop");
        Check(labelsKept && sawNextDayReserved, "Day 1 Goal and Day label unchanged through next-day reservation and fade");
        yield return Faded(); yield return Frames(8);
        Check(GameClockText.CurrentDayBaseGoal == 100, "First shop uses completed Day 1 goal");

        Phase("shop prices and all-stock notice");
        Check(ShopPricing.Calculate(150, 100, 0) == 150 && ShopPricing.Calculate(150, 500, 0) == 300
            && ShopPricing.Calculate(150, 1000, 0) == 600, "150G item costs 150 / 300 / 600 across first three days");
        Check(ShopPricing.Calculate(150, 1000, 1) == 750 && ShopPricing.Calculate(150, 1000, 2) == 900,
            "Repeated ownership increases next offer by 25 percent per copy");
        Check(ShopPricing.Calculate(150, 3000, 0) == 1800 && ShopPricing.Calculate(150, 5000, 0) == 3000,
            "Later prices follow actual goals instead of unchecked exponential growth");
        var stock = Find<ShopStockNotice>();
        var offers = Object.FindObjectsByType<ShopItemDescriptionTrigger>(FindObjectsSortMode.None);
        Check(stock != null && offers.Length == 4 && !stock.IsSoldOut, "Three item offers and one artifact; no premature sold-out notice");
        MoneyManager.currentMoney = 0;
        offers[0].GetComponent<Button>().onClick.Invoke();
        Check(offers[0].gameObject.activeSelf && !stock.IsSoldOut, "Insufficient funds leaves stock unchanged");
        MoneyManager.currentMoney = 10000;
        for (int i = 0; i < offers.Length; i++)
        {
            int quote = Get<int>(offers[i], "price"), before = MoneyManager.currentMoney;
            var button = offers[i].GetComponent<Button>();
            button.onClick.Invoke(); button.onClick.Invoke();
            Check(!offers[i].gameObject.activeSelf && MoneyManager.currentMoney == before - quote, "Offer charges displayed price exactly once");
            Check(stock.IsSoldOut == (i == offers.Length - 1), "Notice waits for both item and artifact stock");
        }
        yield return Frames(5);
        var notice = stock.GetComponentsInChildren<TextMeshProUGUI>().Single(x => x.name == "SoldOutText");
        notice.ForceMeshUpdate();
        Check(notice.text.Contains("本日完売") && !notice.isTextOverflowing, "Readable sold-out message fits its frame");
        var exit = SceneButton(SceneNames.Arcade);
        Check(exit.interactable && exit.gameObject.activeInHierarchy, "EXIT remains available after sellout");
        ScreenCapture.CaptureScreenshot("Logs/round2-sold-out.png"); yield return Frames(4);
        // Avoid auto-completing the next day using the test purchase budget.
        MoneyManager.currentMoney = 0;
        exit.onClick.Invoke();
        yield return Until(() => SceneManager.GetActiveScene().name == SceneNames.Arcade, "Shop EXIT returns to next day");
        yield return Frames(5); Freeze(); yield return Faded();
        clock = GameClockText.Instance;
        Check(DayAdvanceButton.GetDayStatic() == 2 && GameClockText.CurrentDayBaseGoal == 500,
            "Day 2 and its base Goal start only on arcade return");
        Check(Get<TextMeshProUGUI>(clock, "completeThresholdText").text == $"Goal: {500 - clock.borderdown * 100:N0}G",
            "Next day Goal includes purchased goal reduction");

        Phase("Day 3 offers and stock reset");
        DayAdvanceButton.Instance.SetDay(3); clock.UpdateCompleteThresholdByDay(3);
        // Model reaching Day 3's goal, then reserving the next day just as production does.
        DayAdvanceButton.Instance.AdvanceDayForNextRound();
        MoneyManager.currentMoney = 1250;
        ChangeScene.Instance.GoToShop();
        yield return Until(() => SceneManager.GetActiveScene().name == SceneNames.Shop, "Next shop visit loads");
        yield return Faded(); yield return Frames(5);
        stock = Find<ShopStockNotice>();
        offers = Object.FindObjectsByType<ShopItemDescriptionTrigger>(FindObjectsSortMode.None);
        Check(!stock.IsSoldOut && offers.Length == 4, "New visit restocks and clears sold-out state");
        int fullPrice = offers.Sum(x => Get<int>(x, "price"));
        Check(fullPrice > 1250, "Typical Day 3 balance cannot buy every offer");
        var shop = Find<ShopManager>(); var artifacts = Find<ArtifactManager>();
        foreach (var offer in offers)
        {
            string name = Get<string>(offer, "itemName");
            var item = shop.baffitemDatas.FirstOrDefault(x => x.itemName == name);
            int expected;
            if (item != null) expected = ShopPricing.Calculate(item.startprice, 1000, OwnedProgressManager.GetBaffOwned(item.B_itemID));
            else
            {
                var artifact = artifacts.ArtifactDatas.Single(x => x.itemName == name);
                expected = ShopPricing.Calculate(artifact.startprice, 1000, OwnedProgressManager.GetArtifactOwned(artifact.A_itemID));
            }
            Check(Get<int>(offer, "price") == expected, "Day 3 visible quote retains purchase history: " + name);
        }
        report.measurements.Add("Day 3 test balance: 1250G; all four offers: " + fullPrice + "G.");
        offers[0].OnPointerEnter(null); yield return Frames(5);
        ScreenCapture.CaptureScreenshot("Logs/round2-shop-pricing.png"); yield return Frames(4);
        MoneyManager.currentMoney = 0;
        SceneButton(SceneNames.Arcade).onClick.Invoke();
        yield return Until(() => SceneManager.GetActiveScene().name == SceneNames.Arcade, "Arcade ready for game-over regression");
        yield return Frames(5); Freeze(); yield return Faded();
    }

    static IEnumerator TutorialRecovery()
    {
        Phase("game over -> title -> tutorial");
        var clock = GameClockText.Instance;
        MoneyManager.currentMoney = 0;
        Set(clock, "remainingTime", 0f); clock.enabled = true;
        yield return Until(() => SceneManager.GetActiveScene().name == SceneNames.Result, "Actual timeout reaches result");
        yield return Faded(); yield return Frames(5);
        var title = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentTarget(i) is backtitle));
        title.onClick.Invoke();
        yield return Until(() => SceneManager.GetActiveScene().name == SceneNames.Title, "Result button reaches title");
        yield return Faded(); yield return Frames(5);
        SceneButton("tutorial").onClick.Invoke();
        yield return Until(() => SceneManager.GetActiveScene().name == "tutorial", "Title tutorial button opens tutorial");
        yield return Faded(); yield return Frames(20);
        yield return ReadTutorialText();
        var recipe = Find<RecipeStation>();
        Check(recipe != null && !recipe.enabled && !recipe.IsUIOpen, "Early tutorial keeps unavailable recipe station disabled");
        var player = Find<FirstPersonController>();
        Check(player != null && player.enabled && Time.timeScale == 1 && !EditorApplication.isPaused, "Tutorial is running and player controls are enabled");
        PauseController.ForceUnpauseAll();
        Check(!recipe.enabled && player.enabled, "Force-unpause preserves intentionally disabled tutorial controls");
        var pause = PauseController.Instance;
        pause.TogglePause();
        Check(!player.enabled && !recipe.enabled && Time.timeScale == 0, "Pause disables only active player input");
        pause.Resume();
        Check(player.enabled && !recipe.enabled && Time.timeScale == 1, "Resume restores player without enabling missing recipe panel");
        Vector3 start = player.transform.position;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        yield return Frames(20);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        Vector3 moved = player.transform.position - start; moved.y = 0;
        Check(moved.magnitude > .05f, "W input actually moves player after game-over/title/tutorial flow");
        ScreenCapture.CaptureScreenshot("Logs/round2-tutorial-recovered.png"); yield return Frames(4);

        // Early tutorial stations can also be inadvertently enabled by external scripts.
        recipe.enabled = true; yield return Frames(4);
        Check(!recipe.enabled && !recipe.IsUIOpen, "Missing optional recipe UI safely disables itself");
        // The authored recipe lesson is the final lesson and returns to title when closed.
        FadeManager.Instance.LoadSceneWithFade(SceneNames.Tutorial7);
        yield return Until(() => SceneManager.GetActiveScene().name == SceneNames.Tutorial7, "Recipe lesson loads");
        yield return Faded(); yield return Frames(10);
        yield return ReadTutorialText();
        recipe = Find<RecipeStation>();
        var panel = Get<GameObject>(recipe, "recipeUI");
        Check(recipe.enabled && panel != null, "Recipe lesson retains configured panel");
        panel.SetActive(true); recipe.CursorActive = true;
        Call(recipe, "CloseRecipeUI");
        Check(!panel.activeSelf && !recipe.CursorActive, "Recipe lesson panel closes correctly");
        yield return Until(() => SceneManager.GetActiveScene().name == SceneNames.Title, "Closing final recipe lesson returns to title");
        yield return Faded(); yield return Frames(10);
        Check(Time.timeScale == 1 && !EditorApplication.isPaused && Cursor.visible, "Tutorial completion returns a usable title screen");
    }

    static IEnumerator ReadTutorialText()
    {
        var sequence = Find<TutorialSequence>();
        Check(sequence != null && TutorialSequence.IsActive, "Tutorial explanation is shown before movement unlocks");
        int lines = Get<string[]>(sequence, "tutorialLines").Length;
        // Advance through the production dialogue callback; legacy Input is not simulated.
        for (int i = 0; i < lines && TutorialSequence.IsActive; i++)
        {
            Call(sequence, "AdvanceLine");
            yield return Frames(2);
        }
        Check(!TutorialSequence.IsActive && !Get<GameObject>(sequence, "textBoxRoot").activeSelf,
            "Tutorial text finishes and releases its input block");
        yield return Frames(3);
    }
}
#endif
