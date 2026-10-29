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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit integration checks: production timers, actual scene transitions and result button callbacks.
[InitializeOnLoad]
public static class RunResultValidation
{
    const string Armed = "MIS.RunResultValidation";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Stack<IEnumerator> steps = new Stack<IEnumerator>();
    static Report report;
    static double deadline;
    static int lastFrame;
    static bool previousBackground;
    [Serializable] class Report
    {
        public string status = "RUNNING", phase, failure;
        public List<string> passed = new List<string>();
        public List<string> errors = new List<string>();
        public string evidence = "Unity Play Mode: eight tutorial scenes with forced expired timer / reached goal; day 6, 7, 8, 14 success and timeout transitions; real result/shop/title button callbacks; result inventory and frozen counts. Whole-run gameplay and native Tab key are checked separately.";
    }
    static RunResultValidation() => EditorApplication.playModeStateChanged += State;
    [MenuItem("MIS/テスト/チュートリアル時間制限と7日・14日リザルトを検証")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save scene changes first.");
        SessionState.SetBool(Armed, true);
        SessionState.SetString(Armed + ".Scene", SceneManager.GetActiveScene().path);
        EditorSceneManager.OpenScene("Assets/Scenes/tutorial/tutorial.unity");
        EditorApplication.isPlaying = true;
    }
    public static void RunBatch() => Run();
    static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Armed, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            report = new Report();
            Directory.CreateDirectory("Logs");
            Application.logMessageReceived += Log;
            previousBackground = Application.runInBackground;
            Application.runInBackground = true;
            steps.Clear(); steps.Push(Suite());
            lastFrame = -1; deadline = EditorApplication.timeSinceStartup + 600;
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
        Application.runInBackground = previousBackground;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        report.status = error == null ? "PASS" : "FAIL"; report.failure = error?.ToString();
        Save();
        Debug.Log("[RunResultValidation] " + report.status + " / " + report.passed.Count + " checks");
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
    static void Save() => File.WriteAllText("Logs/run-result-validation.json", JsonUtility.ToJson(report, true));
    static void Phase(string value) { report.phase = value; Save(); }
    static void Check(bool ok, string label) { if (!ok) throw new Exception(report.phase + ": " + label); report.passed.Add(label); }
    static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Flags).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }
    static IEnumerator Until(Func<bool> condition, string label, double seconds = 90)
    {
        double stop = EditorApplication.timeSinceStartup + seconds;
        while (!condition() && EditorApplication.timeSinceStartup < stop) yield return null;
        Check(condition(), label);
    }
    static IEnumerator Ready(string scene)
    {
        yield return Until(() => SceneManager.GetActiveScene().name == scene, "Reached " + scene);
        yield return Frames(8);
        yield return Until(() => FadeManager.Instance == null || !FadeManager.Instance.IsFading(), "Fade finished");
        if (scene == SceneNames.Arcade)
            yield return Until(() => GameClockText.Instance != null && Get<bool>(GameClockText.Instance, "dailyIncomeInitialized"), "Day clock and daily income initialized");
    }
    static IEnumerator Load(string scene)
    {
        var operation = SceneManager.LoadSceneAsync(scene);
        yield return Until(() => operation.isDone, "Loaded " + scene);
        yield return Ready(scene);
    }
    static Button Button(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(x => x.name == name);
    static TextMeshProUGUI Label(string name) => Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(x => x.name == name);
    static void StopRequests() { var manager = Find<RequestManager>(); if (manager != null) manager.enabled = false; }
    static IEnumerator Capture(string filename)
    {
        Canvas.ForceUpdateCanvases();
        yield return Frames(3);
        ScreenCapture.CaptureScreenshot("Logs/result-" + filename + ".png");
        yield return Frames(4);
    }

    static IEnumerator Suite()
    {
        yield return Frames(8);
        Phase("Tutorials never end from arcade time or money");
        for (int day = 1; day <= 8; day++)
        {
            string scene = day == 1 ? "tutorial" : "tutorial" + day;
            if (SceneManager.GetActiveScene().name != scene) yield return Load(scene);
            var clock = Find<GameClockText>();
            if (day > 1) Check(clock != null, scene + " production clock exists");
            if (clock != null)
            {
                Check(clock.IsTutorial, scene + " recognized as practice");
                Set(clock, "remainingTime", 0f);
                Set(clock, "dailyIncomeInitialized", true);
                MoneyManager.currentMoney = int.MaxValue;
                yield return Frames(5);
                Check(!clock.RoundEnded, scene + " reached money goal cannot end practice");
                MoneyManager.currentMoney = 0;
                yield return Frames(5);
                Check(!clock.RoundEnded && SceneManager.GetActiveScene().name == scene, scene + " expired timer cannot send to result");
                var timer = Get<TextMeshProUGUI>(clock, "clockText");
                Check(timer == null || !timer.isActiveAndEnabled, scene + " no misleading time limit displayed");
                var complete = Get<GameObject>(clock, "completePanel");
                var timeout = Get<GameObject>(clock, "transitionPanel");
                Check((complete == null || !complete.activeSelf) && (timeout == null || !timeout.activeSelf), scene + " no round-end overlays");
            }
        }

        Phase("Day 6 remains a normal shop transition");
        OwnedProgressManager.ResetAll(); GameClockText.ResetPersistentState();
        DayAdvanceButton.SetDayStatic(6); MoneyManager.currentMoney = 0;
        yield return Load(SceneNames.Arcade);
        StopRequests();
        var clock6 = GameClockText.Instance;
        clock6.RecordSuccessfulDelivery();
        MoneyManager.currentMoney = clock6.GetCompleteMoneyThreshold() + 120;
        yield return Ready(SceneNames.Shop);
        Check(DayAdvanceButton.GetDayStatic() == 7 && !RunResultState.HasResult, "Day 6 success reserves day 7 without result");
        MoneyManager.currentMoney = 0;
        var exit = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i =>
                b.onClick.GetPersistentTarget(i) is LoadTutorialScene loader && Get<string>(loader, "sceneName") == SceneNames.Arcade));
        exit.onClick.Invoke();
        yield return Ready(SceneNames.Arcade);
        StopRequests();

        Phase("Day 7 goal goes to result and holds completed day");
        var baff = AssetDatabase.LoadAssetAtPath<BaffItemDatabase>("Assets/Scripts/BaffItemDatabase.asset");
        var artifacts = AssetDatabase.LoadAssetAtPath<ArtifactDatabase>("Assets/Scripts/ArtifactDatabase.asset");
        foreach (var item in baff.allBaffItems.Where(x => x != null)) OwnedProgressManager.AddBaffItem(item.B_itemID, 2);
        foreach (var item in artifacts.allArtifacts.Where(x => x != null)) OwnedProgressManager.AddArtifact(item.A_itemID);
        var carried = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Scripts/ItemDatabase.asset").allItems.First(x => x != null && x.prefab != null);
        InventoryManager.Instance.ClearAllSlots(); InventoryManager.Instance.AddItem(carried);
        var clock7 = GameClockText.Instance;
        clock7.RecordSuccessfulDelivery();
        int expectedMoney = clock7.GetCompleteMoneyThreshold() + 123;
        MoneyManager.currentMoney = expectedMoney;
        RequestManager.RequestCompleted = 23;
        yield return Ready(SceneNames.Result);
        Check(RunResultState.Cleared && RunResultState.Day == 7 && RunResultState.CanContinue, "7-day clear snapshot permits optional continuation");
        Check(DayAdvanceButton.GetDayStatic() == 7, "Result says day 7, not day 8");
        Check(RunResultState.Money == expectedMoney && RunResultState.Requests == 23, "Final money and requests captured");
        Check(Label("ResultHeading").text == "7日間 クリア！", "Week-one clear wording");
        Check(Button("ContinueToDay8").gameObject.activeInHierarchy, "Continue action visible only for week one");
        yield return Capture("day7");

        Phase("Result uses shared inventory view and frozen ownership");
        Button("ViewResultItems").onClick.Invoke(); yield return Frames(5);
        var hud = Find<OwnedItemsHUD>();
        Check(hud.IsOpen, "Item button opens shared HUD");
        var descriptions = Get<TextMeshProUGUI>(hud, "benefitsText");
        var ownedBaff = baff.allBaffItems.First(x => x != null && x.B_itemID != DailyIncomeState.ItemId);
        int oldCount = RunResultState.GetBaffCount(ownedBaff.B_itemID);
        Check(descriptions.text.Contains(ownedBaff.itemName) && descriptions.text.Contains("x" + oldCount), "Owned name/count/description present");
        Check(descriptions.text.Contains(artifacts.allArtifacts.First(x => x != null).itemName), "Artifacts present alongside buffs");
        OwnedProgressManager.AddBaffItem(ownedBaff.B_itemID);
        Check(RunResultState.GetBaffCount(ownedBaff.B_itemID) == oldCount, "Result snapshot independent of later live inventory changes");
        Check(Get<int>(hud, "pageCount") > 1, "Large inventory descriptions are paginated");
        Button("NextOwnedPage").onClick.Invoke(); yield return Frames(3);
        Check(Get<int>(hud, "currentPage") == 2, "Next page button works");
        Button("PreviousOwnedPage").onClick.Invoke(); yield return Frames(3);
        Check(Get<int>(hud, "currentPage") == 1, "Previous page button works");
        yield return Capture("owned-items");
        Button("CloseOwnedItems").onClick.Invoke(); yield return Frames(3);
        Check(!hud.IsOpen, "Close button returns to scores");

        Phase("Continue retains run state and enters day 8 through shop");
        Button("ContinueToDay8").onClick.Invoke();
        Check(!ChangeScene.ContinueAfterWeekOne(), "Repeated continue cannot increment another day or duplicate transition");
        yield return Ready(SceneNames.Shop);
        Check(DayAdvanceButton.GetDayStatic() == 8 && MoneyManager.currentMoney == expectedMoney, "Shop opens with day 8 reserved and same money");
        Check(OwnedProgressManager.GetBaffOwned(ownedBaff.B_itemID) == oldCount + 1, "Buff inventory retained across result");
        Check(OwnedProgressManager.GetArtifactOwned(100) > 0, "New artifact inventory retained across result");
        exit = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i =>
                b.onClick.GetPersistentTarget(i) is LoadTutorialScene loader && Get<string>(loader, "sceneName") == SceneNames.Arcade));
        MoneyManager.currentMoney = 0;
        exit.onClick.Invoke(); yield return Ready(SceneNames.Arcade);
        StopRequests();
        Check(DayAdvanceButton.GetDayStatic() == 8, "Arcade resumes day 8 without resetting or skipping");
        Check(InventoryManager.Instance.GetSlot(0).CurrentItem == carried, "Carried material restored after result and shop");
        Check(RequestManager.RequestCompleted == 23, "Completed requests retained");

        Phase("Day 14 success is final clear");
        DayAdvanceButton.SetDayStatic(14);
        var clock14 = GameClockText.Instance;
        clock14.UpdateCompleteThresholdByDay(14); clock14.RecordSuccessfulDelivery();
        MoneyManager.currentMoney = clock14.GetCompleteMoneyThreshold();
        yield return Ready(SceneNames.Result);
        Check(RunResultState.Cleared && RunResultState.Day == 14 && !RunResultState.CanContinue, "14-day success is terminal clear");
        Check(DayAdvanceButton.GetDayStatic() == 14 && Label("ResultHeading").text == "14日間 完全クリア！", "Final clear displays day 14");
        Check(!Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(x => x.name == "ContinueToDay8"), "No continue action on final clear");
        Check(!ChangeScene.ContinueAfterWeekOne(), "Final clear cannot continue programmatically");
        yield return Capture("day14");

        Phase("Title and new run reset result / day / buffs");
        Find<backtitle>().GetComponent<Button>().onClick.Invoke(); yield return Ready(SceneNames.Title);
        ChangeScene.Instance.change_button(); yield return Ready(SceneNames.Arcade); StopRequests();
        Check(DayAdvanceButton.GetDayStatic() == 1 && !RunResultState.HasResult, "New game starts day 1 with no stale clear result");
        Check(OwnedProgressManager.GetArtifactOwned(100) == 0 && OwnedProgressManager.GetBaffOwned(ownedBaff.B_itemID) == 0, "New run clears previous owned effects");

        Phase("Empty inventory and timeouts never become milestone clears");
        foreach (int failedDay in new[] { 1, 7, 14 })
        {
            if (SceneManager.GetActiveScene().name != SceneNames.Arcade)
            {
                MoneyManager.currentMoney = 0;
                yield return Load(SceneNames.Arcade);
            }
            StopRequests();
            DayAdvanceButton.SetDayStatic(failedDay);
            var clock = GameClockText.Instance;
            clock.UpdateCompleteThresholdByDay(failedDay);
            MoneyManager.currentMoney = 0;
            Set(clock, "remainingTime", .01f);
            yield return Ready(SceneNames.Result);
            Check(!RunResultState.Cleared && !RunResultState.CanContinue && RunResultState.Day == failedDay, "Timeout on day " + failedDay + " remains a failure");
            Check(Label("ResultHeading").text == "営業終了", "Timeout heading is distinct from clear");
            Button("ViewResultItems").onClick.Invoke(); yield return Frames(4);
            Check(Get<TextMeshProUGUI>(Find<OwnedItemsHUD>(), "benefitsText").text == "所持している効果はありません", "Empty inventory remains readable");
            Button("CloseOwnedItems").onClick.Invoke(); yield return Frames(2);
        }
        yield return Capture("timeout");
    }
}
#endif
