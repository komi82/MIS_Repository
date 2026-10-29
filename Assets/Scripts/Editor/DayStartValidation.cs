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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class DayStartValidation
{
    const string Armed = "MIS.DayStartValidation";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Stack<IEnumerator> steps = new Stack<IEnumerator>();
    static Report report;
    static int frame;
    static double deadline;
    static bool background;
    [Serializable] class Report
    {
        public string status = "RUNNING", phase, failure;
        public int width, height;
        public List<string> checks = new List<string>(), errors = new List<string>();
    }
    static DayStartValidation() => EditorApplication.playModeStateChanged += State;
    [MenuItem("MIS/テスト/日付アニメーションとタイマー開始を検証")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new Exception("Save scenes first.");
        SessionState.SetBool(Armed, true);
        EditorSceneManager.OpenScene("Assets/Scenes/arcade.unity");
        EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Armed, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            report = new Report(); frame = -1;
            background = Application.runInBackground; Application.runInBackground = true;
            Application.logMessageReceived += Log;
            deadline = EditorApplication.timeSinceStartup + 300;
            steps.Clear(); steps.Push(Suite()); EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && report?.status == "RUNNING") Finish(new Exception("Interrupted"));
    }
    static void Log(string msg, string trace, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(msg + "\n" + trace); }
    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException(report.phase);
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            while (steps.Count > 0)
            {
                var step = steps.Peek();
                if (!step.MoveNext()) { steps.Pop(); continue; }
                if (step.Current is IEnumerator nested) { steps.Push(nested); continue; }
                return;
            }
            Check(report.errors.Count == 0, "No Unity errors or exceptions"); Finish(null);
        }
        catch (Exception ex) { Finish(ex); }
    }
    static void Finish(Exception ex)
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
        SessionState.SetBool(Armed, false); Application.runInBackground = background;
        report.status = ex == null ? "PASS" : "FAIL"; report.failure = ex?.ToString(); Save();
        Debug.Log("[DayStartValidation] " + report.status + " / " + report.checks.Count + " checks");
        if (Application.isBatchMode) EditorApplication.Exit(ex == null ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
    static void Save() => File.WriteAllText("Logs/day-start-validation.json", JsonUtility.ToJson(report, true));
    static void Phase(string text) { report.phase = text; Save(); }
    static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, Flags).GetValue(obj);
    static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    static void Check(bool ok, string text) { if (!ok) throw new Exception(report.phase + ": " + text); report.checks.Add(text); }
    static IEnumerator Frames(int count) { for(int i=0;i<count;i++) yield return null; }
    static IEnumerator Until(Func<bool> condition, string text)
    {
        double limit=EditorApplication.timeSinceStartup+90;
        while(!condition() && EditorApplication.timeSinceStartup<limit) yield return null;
        Check(condition(),text);
    }
    static IEnumerator Ready(string name)
    {
        yield return Until(()=>SceneManager.GetActiveScene().name==name,"Reached "+name);
        yield return Frames(5);
        yield return Until(()=>FadeManager.Instance==null || !FadeManager.Instance.IsFading(),"Fade finished");
    }
    static Button Exit() => Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .First(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>
            b.onClick.GetPersistentTarget(i) is LoadTutorialScene loader && Get<string>(loader,"sceneName")==SceneNames.Arcade));
    static IEnumerator Suite()
    {
        yield return Frames(12);
        report.width=Screen.width; report.height=Screen.height;
        Check(!DayStartSequence.IsBlocking,"Direct Arcade entry remains playable");
        OwnedProgressManager.ResetAll(); DailyIncomeState.Reset();
        var carried=AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Scripts/ItemDatabase.asset").allItems.First(x=>x!=null && x.prefab!=null);
        InventoryManager.Instance.ClearAllSlots(); InventoryManager.Instance.AddItem(carried);
        foreach(int day in new[]{2,8,14})
        {
            Phase("Shop EXIT to day "+day);
            GameClockText.Instance.enabled=false; // Model the outgoing completed round.
            DayAdvanceButton.SetDayStatic(day); MoneyManager.currentMoney=123;
            RequestManager.RequestCompleted=12;
            OwnedProgressManager.AddBaffItem(DailyIncomeState.ItemId,1);
            ChangeScene.Instance.GoToShop(); yield return Ready(SceneNames.Shop);
            MoneyManager.currentMoney=73; // Simulate money spent before exiting.
            Exit().onClick.Invoke(); Exit().onClick.Invoke();
            yield return Until(()=>SceneManager.GetActiveScene().name==SceneNames.Arcade,"Arcade loaded");
            yield return Frames(2);
            var intro=Find<DayChangeAnimator>();
            Check(intro!=null && intro.IsPlaying,"Exactly one day presentation starts on scene entry");
            Check(Object.FindObjectsByType<DayChangeAnimator>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Repeated EXIT does not duplicate presentation");
            Check(intro.dayText.text==day+"日目","Correct day label: "+day+" (actual: "+intro.dayText.text+")");
            Check(Mathf.Abs(intro.Duration-3.3f)<.001f,"Authored 3.3 second timing preserved");
            Check(FadeManager.Instance.fadeImage.gameObject.activeSelf && FadeManager.Instance.fadeImage.color.a>.99f,"Arcade remains black behind the presentation");
            var player=Find<FirstPersonController>();
            Check(!player.enabled && InteractionControlTrial.WorldInputBlocked,"Movement and world input blocked during presentation");
            PauseController.Instance?.Pause();
            Check(Time.timeScale==1f,"Pause cannot interrupt day presentation");
            var clock=GameClockText.Instance;
            float remaining=Get<float>(clock,"remainingTime"), requestTime=SceneTimer.Instance.GetElapsedTime(), challengeTime=DailyChallengeRuntime.Elapsed;
            float start=Time.realtimeSinceStartup;
            bool captured=false, timersHeld=true;
            while(intro!=null && intro.IsPlaying)
            {
                timersHeld &= Mathf.Approximately(Get<float>(clock,"remainingTime"),remaining)
                    && Mathf.Approximately(SceneTimer.Instance.GetElapsedTime(),requestTime)
                    && Mathf.Approximately(DailyChallengeRuntime.Elapsed,challengeTime);
                if(!captured && Time.realtimeSinceStartup-start>1.65f)
                {
                    Canvas.ForceUpdateCanvases(); intro.dayText.ForceMeshUpdate();
                    Check(!intro.dayText.isTextOverflowing && !intro.dayText.isTextTruncated,"Day text is fully rendered");
                    Check(intro.dayText.font.HasCharacters(day+"日目"),"Day font contains every character");
                    ScreenCapture.CaptureScreenshot("Logs/day-start-"+day+".png"); captured=true;
                }
                yield return null;
            }
            Check(timersHeld,"All three gameplay timers hold throughout animation");
            Check(MoneyManager.currentMoney==73,"Daily income waits until the day starts");
            Check(DayStartSequence.IsBlocking && FadeManager.Instance.IsFading(),"Fade-in follows presentation removal, still blocking play");
            bool fadeHeld=true;
            while(FadeManager.Instance.IsFading())
            {
                fadeHeld &= Mathf.Approximately(Get<float>(clock,"remainingTime"),remaining);
                yield return null;
            }
            // The frame releasing the gate may also run the first gameplay Update.
            Check(fadeHeld || remaining-Get<float>(clock,"remainingTime")<.1f,"Round time waits through fade-in");
            yield return Frames(4);
            Check(!DayStartSequence.IsBlocking && player.enabled,"Input restored after fade-in");
            Check(Find<DayChangeAnimator>()==null && Find<DayStartSequence>()==null,"No transition objects left in Arcade");
            Check(!FadeManager.Instance.fadeImage.gameObject.activeSelf,"Black fade removed");
            Check(MoneyManager.currentMoney==73+DailyIncomeState.Reward,"Daily income granted exactly once after intro");
            Check(InventoryManager.Instance.GetSlot(0).CurrentItem==carried && RequestManager.RequestCompleted==12,"Inventory and completed requests preserved");
            Check(DayAdvanceButton.GetDayStatic()==day,"Day count unchanged by animation");
            yield return Frames(15);
            Check(Get<float>(clock,"remainingTime")<remaining && SceneTimer.Instance.GetElapsedTime()>requestTime && DailyChallengeRuntime.Elapsed>challengeTime,"All three timers start after reveal");
        }
        Phase("Interrupted presentation cleans up");
        ChangeScene.Instance.GoToShop(); yield return Ready(SceneNames.Shop);
        Exit().onClick.Invoke(); yield return Until(()=>Find<DayChangeAnimator>()!=null,"Presentation entered");
        var load=SceneManager.LoadSceneAsync(SceneNames.Title); while(!load.isDone) yield return null;
        yield return Frames(5);
        Check(!DayStartSequence.IsBlocking && Find<DayChangeAnimator>()==null,"Unexpected scene change clears presentation and gate");
        yield return Ready(SceneNames.Title);
        ChangeScene.Instance.change_button(); yield return Ready(SceneNames.Arcade);
        Check(!DayStartSequence.IsBlocking && Find<DayChangeAnimator>()==null,"Title start is unchanged");
    }
}
#endif
