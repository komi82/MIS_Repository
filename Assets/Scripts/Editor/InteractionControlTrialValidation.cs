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
public static class InteractionControlTrialValidation
{
    const string Armed = "MIS.InteractionControlTrial.Validation";
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
    static string ReportPath => Path.GetFullPath("Logs/mouse-control-validation.json");
    [Serializable] class Report
    {
        public string status;
        public string evidence = "Unity Play Mode; virtual mouse events; real arcade components and item/recipe assets. QTE completion and human comfort comparison are not automated.";
        public List<string> passed = new List<string>();
        public List<string> engineErrors = new List<string>();
        public string failure;
        public List<string> screenshots = new List<string>();
    }
    static InteractionControlTrialValidation() => EditorApplication.playModeStateChanged += OnState;
    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch mode required.");
        Run();
    }
    [MenuItem("MIS/操作比較/クリック操作の動作を検証")]
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
            deadline = EditorApplication.timeSinceStartup + 180;
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
        string path = Path.GetFullPath("Logs/mouse-trial-" + name + ".png");
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
    static IEnumerator Suite()
    {
        for (int i = 0; i < 4; i++) yield return null;
        var pickup = Find<ItemPickup>();
        var put = Find<PutItem>();
        var delivery = Find<DeliveryStation>();
        var recipeStation = Find<RecipeStation>();
        var inventory = InventoryManager.Instance;
        var selector = Find<SlotSelector>();
        var manager = Find<RequestManager>();
        var gate = Find<InteractionControlTrial>();
        Check(gate != null && InteractionControlTrial.UseMouse, "mouse trial starts in arcade");
        foreach (var player in Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None)) player.enabled = false;
        manager.enabled = false;
        GameClockText.Instance.StopAllCoroutines();
        GameClockText.Instance.enabled = false;
        foreach (var behaviour in new Behaviour[] { pickup, put, delivery, recipeStation, selector })
            if (behaviour != null) behaviour.enabled = false;
        delivery.CursorActive = false;
        // Validate the real scene transforms; the legacy children used 0.2 scale and negative margins.
        foreach (string field in new[] { "craftPromptUI", "blacksmithPromptUI", "washPromptUI", "recipePromptUI", "karasuPromptUI" })
        {
            var prompt = Get<GameObject>(put, field);
            Check(prompt != null, field + " exists");
            prompt.SetActive(true);
            ValidateLabel(prompt.GetComponentInChildren<TMPro.TextMeshProUGUI>(true), field);
            if (field == "craftPromptUI")
            {
                Capture("craft-prompt");
                for (int i = 0; i < 4; i++) yield return null;
            }
            prompt.SetActive(false);
        }
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Scripts/ItemDatabase.asset");
        var item = db.allItems.First(x => x != null && x.prefab != null);
        var fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fixture.name = "Mouse trial target";
        fixture.transform.position = new Vector3(1000f, 1000f, 1000f);
        var itemBehaviour = fixture.AddComponent<ItemBehaviour>();
        Set(itemBehaviour, "itemData", item);
        fixture.tag = "Infinity";
        var cam = Get<Camera>(pickup, "mainCamera");
        cam.transform.SetPositionAndRotation(fixture.transform.position - Vector3.forward * 3f, Quaternion.identity);
        Physics.SyncTransforms();
        inventory.ClearAllSlots();
        GameplayCursor();
        Release(); yield return null;

        Press(MouseButton.Left);
        PickupUpdate(pickup);
        Check(inventory.GetSlot(0).CurrentItem == item && !inventory.GetSlot(1).IsOccupied, "left click picks up one item");
        PickupUpdate(pickup);
        Check(!inventory.GetSlot(1).IsOccupied, "same click cannot pick up twice");
        Check(Get<TMPro.TextMeshProUGUI>(pickup, "pickupPromptText").text.Contains("左クリック"), "pickup hint matches mouse input");
        Release(); yield return null;
        Press(MouseButton.Right); PickupUpdate(pickup);
        Check(!inventory.GetSlot(1).IsOccupied, "right click does not pick up");
        Release(); yield return null;

        while (!inventory.IsFull()) inventory.AddItem(item);
        Press(MouseButton.Left); PickupUpdate(pickup);
        Check(inventory.IsFull() && fixture != null, "full inventory retains world item");
        Release(); yield return null;
        inventory.ClearAllSlots(); inventory.AddItem(item); selector.SelectSlot(0);
        Object.DestroyImmediate(itemBehaviour);
        fixture.tag = "put";
        var slots = fixture.AddComponent<PlacementSlots>();
        for (int i = 0; i < 2; i++)
        {
            var anchor = new GameObject("slot" + i).transform;
            anchor.SetParent(fixture.transform);
            anchor.localPosition = new Vector3(2f + i, 0f, 0f);
            Set(slots, "slot" + (i + 1), anchor);
        }
        Physics.SyncTransforms();
        Press(MouseButton.Right); Call(put, "Update");
        Check(slots.GetItemInSlot(0) == item && !inventory.GetSlot(0).IsOccupied, "right click places selected item and removes it from inventory");
        Release(); yield return null;
        inventory.AddItem(item); selector.SelectSlot(0);
        Press(MouseButton.Left); Call(put, "Update");
        Check(inventory.GetSlot(0).IsOccupied && slots.GetItemInSlot(1) == null, "left click does not place item");
        Release(); yield return null;
        Press(MouseButton.Right); Call(put, "Update");
        Check(slots.GetItemInSlot(1) == item, "right click fills second slot");
        Release(); yield return null;
        inventory.AddItem(item); selector.SelectSlot(0);
        Press(MouseButton.Right); Call(put, "Update");
        Check(inventory.GetSlot(0).IsOccupied, "full station does not consume inventory item");
        Release(); yield return null;

        // A station material must win over its parent station regardless of Update order.
        fixture.tag = "craft";
        itemBehaviour = fixture.AddComponent<ItemBehaviour>(); Set(itemBehaviour, "itemData", item);
        inventory.ClearAllSlots();
        Press(MouseButton.Left); Call(put, "Update"); PickupUpdate(pickup);
        Check(!put.IsCraftingInProgress && inventory.GetSlot(0).CurrentItem == item, "material pickup wins over crafting (station updates first)");
        // Pickup destroys non-infinite fixtures at end of frame; restore Infinity for subsequent checks using a fresh item.
        Release(); yield return null;

        fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fixture.transform.position = cam.transform.position + Vector3.forward * 3f;
        fixture.tag = "Infinity";
        itemBehaviour = fixture.AddComponent<ItemBehaviour>(); Set(itemBehaviour, "itemData", item);
        Physics.SyncTransforms();
        inventory.ClearAllSlots();
        Cursor.visible = true; Cursor.lockState = CursorLockMode.Confined;
        Release(); yield return null;
        Press(MouseButton.Left); PickupUpdate(pickup); Call(put, "Update");
        Check(!inventory.GetSlot(0).IsOccupied, "UI cursor blocks world pickup and placement");
        Call(gate, "Update"); GameplayCursor();
        PickupUpdate(pickup);
        Check(!inventory.GetSlot(0).IsOccupied, "closing UI cannot leak its click into world in the same frame");
        Release(); yield return null;
        Time.timeScale = 0f; Press(MouseButton.Left); PickupUpdate(pickup);
        Check(!inventory.GetSlot(0).IsOccupied, "pause blocks world input");
        Time.timeScale = 1f; Release(); yield return null;

        // The production station Update receives a virtual click, followed by another click while its UI is open.
        Object.DestroyImmediate(fixture);
        var deliveryPanel = Get<GameObject>(delivery, "deliveryUI");
        if (manager.GetActiveRequests().Count == 0) Call(manager, "Update");
        Check(manager.GetActiveRequests().Count > 0, "delivery request exists");
        Set(delivery, "mainCamera", cam);
        var stationCollider = delivery.GetComponent<Collider>();
        Check(stationCollider != null, "delivery station collider exists");
        cam.transform.SetPositionAndRotation(stationCollider.bounds.center - Vector3.forward * 2f, Quaternion.identity);
        Physics.SyncTransforms(); GameplayCursor();
        Release(); yield return null;
        Press(MouseButton.Left); Call(delivery, "Update");
        Check(deliveryPanel.activeSelf && delivery.CursorActive, "left click opens delivery panel");
        Release(); yield return null;
        Press(MouseButton.Left); Call(delivery, "Update");
        Check(deliveryPanel.activeSelf && delivery.CursorActive, "UI click does not toggle delivery panel closed");
        var closeHint = deliveryPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)
            .Single(x => x.text == "F / Esc で閉じる");
        ValidateLabel(closeHint, "delivery close hint");
        Capture("delivery");
        for (int i = 0; i < 4; i++) yield return null;
        PressKey(Key.F); Call(delivery, "Update");
        Check(!deliveryPanel.activeSelf && !delivery.CursorActive && !Cursor.visible,
            "F closes delivery panel and restores gameplay cursor");
        Check(InteractionControlTrial.WorldInputBlocked, "delivery close blocks remaining world input this frame");
        Release(); yield return null;
        Press(MouseButton.Left); Call(delivery, "Update"); Release(); yield return null;
        Call(gate, "Update"); PressKey(Key.Escape); Call(delivery, "Update");
        Check(!deliveryPanel.activeSelf && InteractionControlTrial.EscapeBelongsToUI,
            "Esc closes delivery panel and is reserved from pause for the whole frame");
        Release(); yield return null;

        var recipePanel = Get<GameObject>(recipeStation, "recipeUI");
        var recipeCollider = recipeStation.GetComponent<Collider>();
        Set(recipeStation, "mainCamera", cam);
        cam.transform.SetPositionAndRotation(recipeCollider.bounds.center - Vector3.forward * 2f, Quaternion.identity);
        Physics.SyncTransforms(); GameplayCursor();
        Press(MouseButton.Left); Call(recipeStation, "Update");
        Check(recipePanel.activeSelf, "left click opens recipe panel");
        ValidateLabel(recipePanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)
            .Single(x => x.text == "F / Esc で閉じる"), "recipe close hint");
        Capture("recipe");
        for (int i = 0; i < 4; i++) yield return null;
        Release(); yield return null;
        PressKey(Key.F); Call(recipeStation, "Update");
        Check(!recipePanel.activeSelf && !recipeStation.CursorActive && !delivery.CursorActive,
            "F closes recipe panel and clears both UI locks");
        foreach (var player in Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None)) player.enabled = false;
        Release(); yield return null;

        // Production recipes for all three craft stations start their existing QTEs.
        foreach (var entry in new[] { new[] { "craft", "recipeDatabase" }, new[] { "blacksmith", "weaponRecipeDatabase" }, new[] { "wash", "washRecipeDatabase" } })
        {
            var recipes = Get<RecipeDatabase>(put, entry[1]);
            var recipe = recipes.allRecipes.First(r => r != null && r.resultItem != null && r.requiredItems.Length == 2);
            fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fixture.transform.position = new Vector3(1000f, 1000f, 1000f);
            fixture.tag = entry[0];
            slots = fixture.AddComponent<PlacementSlots>();
            for (int i = 0; i < 2; i++)
            {
                var anchor = new GameObject("slot" + i).transform; anchor.SetParent(fixture.transform);
                anchor.localPosition = new Vector3(2f + i, 0f, 0f); Set(slots, "slot" + (i + 1), anchor);
            }
            foreach (var material in recipe.requiredItems.Where(x => x != null)) slots.TryPlace(material, out _);
            var aimedMaterial = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            aimedMaterial.transform.position = fixture.transform.position - Vector3.forward * 0.8f;
            aimedMaterial.transform.localScale = Vector3.one * 0.4f;
            aimedMaterial.transform.SetParent(slots.GetSlotTransform(0), true);
            Set(aimedMaterial.AddComponent<ItemBehaviour>(), "itemData", recipe.requiredItems[0]);
            cam.transform.SetPositionAndRotation(fixture.transform.position - Vector3.forward * 3f, Quaternion.identity);
            Physics.SyncTransforms(); GameplayCursor(); delivery.CursorActive = false;
            Release(); yield return null;
            Check(Physics.Raycast(cam.transform.position, cam.transform.forward, out var materialHit, 4f)
                && materialHit.collider.gameObject == aimedMaterial, entry[0] + " aims directly at station material");
            Press(MouseButton.Left); Call(put, "Update");
            Check(!put.IsCraftingInProgress, entry[0] + " left click does not start crafting");
            Release(); yield return null;
            PressKey(Key.R); Call(put, "Update");
            Check(put.IsCraftingInProgress && delivery.CursorActive, entry[0] + " starts QTE with R");
            Check(InteractionControlTrial.WorldInputBlocked, entry[0] + " QTE blocks world input");
            put.StopAllCoroutines();
            Set(put, "isProcessingCoroutine", false); Set(put, "isCraftingInProgress", false);
            Call(put, "EndPlayerUiBlock");
            Object.DestroyImmediate(fixture);
            foreach (var player in Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None)) player.enabled = false;
            Release(); yield return null;
        }
        SessionState.SetBool(InteractionControlTrial.PreferenceKey, false);
        Check(!InteractionControlTrial.UseMouse && InteractionControlTrial.PickupLabel.Contains("name=F"), "legacy mode restores keyboard mapping and hint");
        SessionState.SetBool(InteractionControlTrial.PreferenceKey, true);
        var originalScene = SceneManager.GetActiveScene();
        var nonArcade = SceneManager.CreateScene("MouseTrialTutorialCheck"); SceneManager.SetActiveScene(nonArcade);
        Check(!InteractionControlTrial.UseMouse, "non-arcade scenes keep legacy controls");
        SceneManager.SetActiveScene(originalScene);
        Release();
    }
}
#endif
