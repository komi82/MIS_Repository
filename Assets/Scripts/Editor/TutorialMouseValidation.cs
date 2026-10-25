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
public static class TutorialMouseValidation
{
    const string Armed = "MIS.TutorialMouse.Validation";
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
    static string ReportPath => Path.GetFullPath("Logs/tutorial-mouse-validation.json");
    [Serializable] class Report
    {
        public string status;
        public string evidence = "Unity Play Mode; all eight tutorial scenes; virtual mouse/keyboard input into production components; actual guide layout, pickup, placement, crafting start, delivery and recipe panels. Human playthrough and QTE completion are not automated.";
        public List<string> passed = new List<string>();
        public List<string> engineErrors = new List<string>();
        public string failure;
        public List<string> screenshots = new List<string>();
    }
    static TutorialMouseValidation() => EditorApplication.playModeStateChanged += OnState;
    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch mode required.");
        Run();
    }
    [MenuItem("MIS/テスト/チュートリアルのマウス操作を検証")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("再生を停止してください。");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("シーンを保存してから実行してください。");
        SessionState.SetBool(Armed + ".Previous", SessionState.GetBool(InteractionControlTrial.PreferenceKey, true));
        SessionState.SetBool(InteractionControlTrial.PreferenceKey, true);
        SessionState.SetBool(Armed, true);
        EditorSceneManager.OpenScene("Assets/Scenes/tutorial/tutorial.unity", OpenSceneMode.Single);
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
        string path = Path.GetFullPath("Logs/tutorial-mouse-" + name + ".png");
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
    static void ValidateDialogue(TutorialSequence sequence, string scene)
    {
        var body = Get<TMPro.TextMeshProUGUI>(sequence, "tutorialText");
        var lines = Get<string[]>(sequence, "tutorialLines");
        var canvas = body.canvas.rootCanvas.transform as RectTransform;
        var corners = new Vector3[4];
        for (int i = 0; i < lines.Length; i++)
        {
            Set(sequence, "currentLineIndex", i); Call(sequence, "ApplyLineText");
            Canvas.ForceUpdateCanvases(); body.ForceMeshUpdate(true, true);
            Check(!body.isTextOverflowing && body.fontSize >= 34f,
                scene + ": dialogue " + (i + 1) + " fits its text area at a readable size");
        }
        foreach (var label in body.canvas.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
        {
            label.rectTransform.GetWorldCorners(corners);
            Check(corners.All(c => canvas.rect.Contains(canvas.InverseTransformPoint(c))),
                scene + ": " + label.name + " stays on screen");
        }
        if (scene == "tutorial8")
        {
            Check(lines.Any(x => x.Contains("所持金")) && lines.Any(x => x.Contains("EXIT")) &&
                lines.Any(x => x.Contains("2日目")) && lines.Any(x => x.Contains("画面上部")),
                "final lesson explains buying, exiting, daily challenges and where to read them");
        }
        Set(sequence, "currentLineIndex", 0); Call(sequence, "ApplyLineText");
    }

    static IEnumerator Suite()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:BaffItemData"))
        {
            var item = AssetDatabase.LoadAssetAtPath<BaffItemData>(AssetDatabase.GUIDToAssetPath(guid));
            var icon = item.prefab != null ? item.prefab.GetComponent<UnityEngine.UI.Image>() : null;
            Check(icon != null && icon.sprite != null, item.itemName + ": prefab resolves a real sprite");
        }
        foreach (string scene in new[] { "tutorial", "tutorial2", "tutorial3", "tutorial4", "tutorial5", "tutorial6", "tutorial7", "tutorial8" })
        {
            Release();
            if (SceneManager.GetActiveScene().name != scene)
            {
                var load = SceneManager.LoadSceneAsync(scene);
                while (!load.isDone) yield return null;
            }
            for (int i = 0; i < 10; i++) yield return null;
            var sequence = Find<TutorialSequence>();
            var gate = Find<InteractionControlTrial>();
            Check(sequence != null && TutorialSequence.IsActive && gate != null && InteractionControlTrial.UseMouse,
                scene + ": explanation starts with mouse controls");
            sequence.enabled = false;
            ValidateDialogue(sequence, scene);
            Capture(scene + "-dialogue");
            yield return null;
            if (scene == "tutorial8")
            {
                Set(sequence, "currentLineIndex", 2); Call(sequence, "ApplyLineText"); Capture("shop-help");
                yield return null;
                Set(sequence, "currentLineIndex", 6); Call(sequence, "ApplyLineText"); Capture("hardship-help");
                yield return null;
                Set(sequence, "currentLineIndex", 0); Call(sequence, "ApplyLineText");
            }
            // Deliberately reset the cursor: the explanation must block gameplay independently.
            GameplayCursor();
            Press(MouseButton.Left);
            Check(!InteractionControlTrial.InteractPressed && !InteractionControlTrial.PlacePressed,
                scene + ": explanation blocks world input independently of cursor");
            Call(sequence, "Update");
            Check(Get<int>(sequence, "currentLineIndex") == 1, scene + ": left click advances the explanation");
            Release(); yield return null;
            Press(MouseButton.Right); Call(sequence, "Update");
            Check(Get<int>(sequence, "currentLineIndex") == 1, scene + ": right click does not skip explanation");
            Release(); yield return null;
            int count = Get<string[]>(sequence, "tutorialLines").Length;
            Set(sequence, "currentLineIndex", count - 1);
            Press(MouseButton.Left); Call(sequence, "Update");
            Check(!TutorialSequence.IsActive && InteractionControlTrial.WorldInputBlocked,
                scene + ": dismissing click is blocked for the rest of the frame");
            Check(!Cursor.visible && Cursor.lockState == CursorLockMode.Locked,
                scene + ": gameplay cursor restored");
            // TutorialSequence enables these components on dismissal; their first Start runs next frame.
            Release(); yield return null;
            var guide = Get<GameObject>(sequence, "postTutorialWindow");
            if (scene != "tutorial")
            {
                var label = guide.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                Canvas.ForceUpdateCanvases(); label.ForceMeshUpdate(true, true);
                Check(label.text.Contains("クリック") && !label.text.Contains("sprite") && !label.isTextOverflowing,
                    scene + ": guide shows mouse controls without text overflow");
                Check(guide.GetComponentsInChildren<UnityEngine.UI.Image>(true).All(x => x.transform == guide.transform || !x.enabled),
                    scene + ": obsolete key illustrations are hidden");
                var canvas = label.canvas.rootCanvas.transform as RectTransform;
                var corners = new Vector3[4]; (guide.transform as RectTransform).GetWorldCorners(corners);
                Check(corners.All(c => canvas.rect.Contains(canvas.InverseTransformPoint(c))),
                    scene + ": guide stays inside the screen");
                Capture(scene);
            }
            var pickup = Find<ItemPickup>(); var put = Find<PutItem>();
            var delivery = Find<DeliveryStation>(); var recipe = Find<RecipeStation>();
            foreach (var player in Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None)) player.enabled = false;
            foreach (var b in new Behaviour[] { pickup, put, delivery, recipe, Find<SlotSelector>() }) if (b != null) b.enabled = false;
            if (GameClockText.Instance != null) { GameClockText.Instance.StopAllCoroutines(); GameClockText.Instance.enabled = false; }
            if (ConditionalSceneTransition.Instance != null) Set(ConditionalSceneTransition.Instance, "delayBeforeLoad", 9999f);
            Release(); yield return null;
            GameplayCursor(); if (delivery != null) delivery.CursorActive = false;
            PressKey(Key.R);
            Check(InteractionControlTrial.CraftPressed, scene + ": R remains the craft shortcut");
            Release(); yield return null;
            var inventory = InventoryManager.Instance;
            if (scene == "tutorial2" || scene == "tutorial3")
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Scripts/ItemDatabase.asset").allItems.First(x => x != null && x.prefab != null);
                var fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fixture.transform.position = new Vector3(1000, 1000, 1000);
                var cam = Get<Camera>(pickup, "mainCamera");
                cam.transform.SetPositionAndRotation(fixture.transform.position - Vector3.forward * 3f, Quaternion.identity);
                inventory.ClearAllSlots(); Physics.SyncTransforms();
                if (scene == "tutorial2")
                {
                    fixture.tag = "Infinity";
                    Set(fixture.AddComponent<ItemBehaviour>(), "itemData", item);
                    Press(MouseButton.Right); PickupUpdate(pickup);
                    Check(!inventory.GetSlot(0).IsOccupied, "tutorial2: right click does not pick up");
                    Release(); yield return null; GameplayCursor();
                    Press(MouseButton.Left); PickupUpdate(pickup);
                    Check(inventory.GetSlot(0).CurrentItem == item, "tutorial2: left click picks up a material");
                    Check(Get<bool>(ConditionalSceneTransition.Instance, "hasTriggered"), "tutorial2: successful pickup advances the lesson");
                }
                else
                {
                    fixture.tag = "put";
                    var slots = fixture.AddComponent<PlacementSlots>();
                    for (int i = 0; i < 2; i++)
                    {
                        var anchor = new GameObject("slot" + i).transform; anchor.SetParent(fixture.transform);
                        anchor.localPosition = new Vector3(2 + i, 0, 0); Set(slots, "slot" + (i + 1), anchor);
                    }
                    inventory.AddItem(item); Find<SlotSelector>().SelectSlot(0);
                    Press(MouseButton.Left); Call(put, "Update");
                    Check(inventory.GetSlot(0).IsOccupied && slots.GetItemInSlot(0) == null, "tutorial3: left click does not place");
                    Release(); yield return null; GameplayCursor();
                    Press(MouseButton.Right); Call(put, "Update");
                    Check(slots.GetItemInSlot(0) == item && !inventory.GetSlot(0).IsOccupied, "tutorial3: right click places selected material");
                }
                Object.Destroy(fixture); Release(); yield return null;
            }
            if (scene == "tutorial4" || scene == "tutorial7")
            {
                Component station = scene == "tutorial4" ? (Component)delivery : recipe;
                var cam = Get<Camera>(station, "mainCamera"); var collider = station.GetComponent<Collider>();
                cam.transform.SetPositionAndRotation(collider.bounds.center - Vector3.forward * 2f, Quaternion.identity);
                Physics.SyncTransforms(); GameplayCursor(); delivery.CursorActive = false;
                Release(); yield return null;
                Press(MouseButton.Left); Call(station, "Update");
                var panel = Get<GameObject>(station, scene == "tutorial4" ? "deliveryUI" : "recipeUI");
                Check(panel.activeSelf && Cursor.visible, scene + ": left click opens the station panel");
                Check(panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Any(x => x.text == "F / Esc で閉じる"), scene + ": panel close hint matches the shortcut");
                Release(); yield return null;
                PressKey(Key.F); Call(station, "Update");
                Check(!panel.activeSelf && InteractionControlTrial.WorldInputBlocked, scene + ": F closes panel without leaking world input");
                if (scene == "tutorial7") Check(Get<bool>(ConditionalSceneTransition.Instance, "hasTriggered"), "tutorial7: closing the recipe finishes the lesson");
            }
            Release();
            for (int i = 0; i < 3; i++) yield return null;
        }
        float finishDeadline = Time.realtimeSinceStartup + 8f;
        while (SceneManager.GetActiveScene().name != "title" && Time.realtimeSinceStartup < finishDeadline) yield return null;
        Check(SceneManager.GetActiveScene().name == "title", "last explanation returns to the title through its real scene transition");
        Check(!TutorialSequence.IsActive && Find<TutorialPresentation>() == null, "tutorial overlay and input block are cleared on leaving the last lesson");
    }
}
#endif
