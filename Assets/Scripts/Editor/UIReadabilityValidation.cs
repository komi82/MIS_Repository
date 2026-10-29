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
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit Play Mode layout regression checks, including amounts above a normal run.
[InitializeOnLoad]
public static class UIReadabilityValidation
{
    const string Armed = "MIS.UIReadabilityValidation";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static IEnumerator routine;
    static int frame;
    static double deadline;
    static bool background;
    [Serializable] class Report
    {
        public string status = "RUNNING", failure;
        public int width, height;
        public List<string> checks = new List<string>(), issues = new List<string>(), errors = new List<string>(), measurements = new List<string>();
    }
    static Report report;
    static UIReadabilityValidation() => EditorApplication.playModeStateChanged += State;
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
            deadline = EditorApplication.timeSinceStartup + 240;
            routine = Suite(); EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && report?.status == "RUNNING") Finish(new Exception("Interrupted"));
    }
    static void Log(string msg, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(msg + "\n" + trace); }
    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException();
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            if (!routine.MoveNext()) Finish(null);
        }
        catch (Exception ex) { Finish(ex); }
    }
    static void Finish(Exception ex)
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
        SessionState.SetBool(Armed, false); Application.runInBackground = background;
        Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        report.status = ex == null && report.issues.Count == 0 && report.errors.Count == 0 ? "PASS" : "FAIL";
        report.failure = ex?.ToString();
        File.WriteAllText("Logs/ui-readability-validation.json", JsonUtility.ToJson(report, true));
        Debug.Log("[UIReadabilityValidation] " + report.status + " / " + report.checks.Count + " checks");
        EditorApplication.isPlaying = false;
    }
    static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, Flags).GetValue(obj);
    static void Call(object obj, string name) => obj.GetType().GetMethod(name, Flags).Invoke(obj, null);
    static void Check(bool ok, string message) { (ok ? report.checks : report.issues).Add(message); }
    static void Label(TextMeshProUGUI label, string context)
    {
        if (label == null) { Check(false, context + " exists"); return; }
        Canvas.ForceUpdateCanvases(); label.ForceMeshUpdate(true, true);
        var corners = new Vector3[4]; label.rectTransform.GetWorldCorners(corners);
        var root = (RectTransform)label.canvas.rootCanvas.transform;
        bool onScreen = corners.All(c => root.rect.Contains((Vector2)root.InverseTransformPoint(c)));
        Check(!label.isTextOverflowing && !label.isTextTruncated && onScreen, context + ": all digits and text fit");
        report.measurements.Add(context + " | " + label.text.Replace("\n", " / ") + " | size=" + label.fontSize + " rect=" + label.rectTransform.rect + " scale=" + label.transform.lossyScale + " anchored=" + label.rectTransform.anchoredPosition + " overflow=" + label.isTextOverflowing + " onScreen=" + onScreen);
    }
    static void Capture(string name) => ScreenCapture.CaptureScreenshot("Logs/ui-review-" + name + ".png");
    static IEnumerator Suite()
    {
        for (int i=0;i<20;i++) yield return null;
        report.width = Screen.width; report.height = Screen.height;
        GameClockText.Instance.enabled = false;
        var requests = Find<RequestManager>(); if (requests != null) requests.enabled = false;
        var managers = Object.FindObjectsByType<MoneyManager>(FindObjectsSortMode.None).Where(x => x.isActiveAndEnabled).ToArray();
        foreach (int value in new[] { 0, 9999, 999999, 999999999, int.MaxValue })
        {
            MoneyManager.currentMoney = value;
            foreach (var manager in managers)
            {
                Call(manager, "UpdateUI");
                var label = Get<TextMeshProUGUI>(manager, "moneyText");
                if (label != null && label.gameObject.activeInHierarchy) Label(label, "arcade money " + value);
            }
            GameClockText.Instance.SetCompleteMoneyThreshold(value);
            Call(GameClockText.Instance, "UpdateCompleteThresholdDisplay");
            Label(Get<TextMeshProUGUI>(GameClockText.Instance, "completeThresholdText"), "arcade goal " + value);
            yield return null;
        }
        managers[0].AddMoney(0);
        var gain = Get<TextMeshProUGUI>(managers[0], "gainMoneyText");
        if (gain != null) { gain.text = "+2,147,483,647G"; gain.gameObject.SetActive(true); Label(gain, "maximum gain"); }
        Capture("arcade-high"); for(int i=0;i<4;i++) yield return null;
        DayAdvanceButton.SetDayStatic(14); RequestManager.RequestCompleted = int.MaxValue; RunResultState.Capture(true);
        var load = SceneManager.LoadSceneAsync(SceneNames.Result); while(!load.isDone) yield return null;
        for(int i=0;i<20;i++) yield return null;
        var score = Find<ScoreDisplay>();
        foreach(var name in new[]{"aScoreText","xScoreText","yScoreText"}) Label(Get<TextMeshProUGUI>(score,name), "result " + name);
        Capture("result-high"); for(int i=0;i<4;i++) yield return null;
        load = SceneManager.LoadSceneAsync(SceneNames.Shop); while(!load.isDone) yield return null;
        float shopReady = Time.realtimeSinceStartup + 4f;
        while (Time.realtimeSinceStartup < shopReady) yield return null;
        foreach (var manager in Object.FindObjectsByType<MoneyManager>(FindObjectsSortMode.None)) Label(Get<TextMeshProUGUI>(manager,"moneyText"), "shop money");
        var detail = Get<TextMeshProUGUI>(Find<ShopManager>(), "itemDetailText");
        var help = GameObject.Find("ShopQuickGuide");
        Check(help != null, "shop instructions are visible on entry");
        if (help != null) foreach(var label in help.GetComponentsInChildren<TextMeshProUGUI>()) Label(label, "shop quick guide");
        Capture("shop-entry"); for(int i=0;i<4;i++) yield return null;
        var offer = Find<ShopItemDescriptionTrigger>();
        offer.OnPointerEnter(new PointerEventData(EventSystem.current)); yield return null;
        Label(detail, "shop item description"); Capture("shop-hover"); for(int i=0;i<4;i++) yield return null;
        foreach (var guid in AssetDatabase.FindAssets("t:BaffItemData"))
        {
            var item = AssetDatabase.LoadAssetAtPath<BaffItemData>(AssetDatabase.GUIDToAssetPath(guid));
            detail.text = item.itemName + "\n" + item.description + "\n必要な料金: 1,000,000,000G";
            Label(detail, "buff description " + item.B_itemID);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:ArtifactData"))
        {
            var item = AssetDatabase.LoadAssetAtPath<ArtifactData>(AssetDatabase.GUIDToAssetPath(guid));
            detail.text = item.itemName + "\n" + item.description + "\n必要な料金: 1,000,000,000G";
            Label(detail, "artifact description " + item.A_itemID);
        }
        offer.OnPointerExit(new PointerEventData(EventSystem.current)); yield return null;
        Check(help != null && help.activeInHierarchy, "shop help survives hover exit");
        var buy = offer.GetComponent<Button>();
        int before = MoneyManager.currentMoney, price = Get<int>(offer,"price");
        buy.onClick.Invoke(); buy.onClick.Invoke(); yield return null;
        Check(MoneyManager.currentMoney == before-price && !offer.gameObject.activeSelf, "purchase charges once and removes offer");
        Check(help != null && help.activeInHierarchy, "shop help survives purchase");
        foreach(var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        { var text=button.GetComponentInChildren<TextMeshProUGUI>(); if(text!=null && text.text.Trim()=="EXIT") Label(text,"shop EXIT"); }
    }
}
#endif
