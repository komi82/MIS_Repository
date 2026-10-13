using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>アーケードの操作比較用。製品ビルドとチュートリアルは従来操作を維持する。</summary>
[DefaultExecutionOrder(-10000)]
public sealed class InteractionControlTrial : MonoBehaviour
{
    public const string PreferenceKey = "MIS.InteractionControlTrial.Mouse";
    private static int blockedFrame = -1;
    private static int consumedFrame = -1;
    private static int uiEscapeFrame = -1;
    private DeliveryStation deliveryStation;
    private RecipeStation recipeStation;

    public static bool EscapeBelongsToUI => UseMouse && uiEscapeFrame == Time.frameCount;

    public static bool UseMouse
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.SessionState.GetBool(PreferenceKey, true)
                && SceneManager.GetActiveScene().name == SceneNames.Arcade;
#else
            return false;
#endif
        }
    }

    public static string PickupLabel => UseMouse ? "左クリック" : "<sprite name=F>";
    public static string PlaceLabel => UseMouse ? "右クリック" : "<sprite name=E>";
    public static bool WorldInputBlocked => UseMouse &&
        (blockedFrame == Time.frameCount || Time.timeScale <= 0f || Cursor.visible
            || Cursor.lockState != CursorLockMode.Locked);

    public static bool InteractPressed => UseMouse
        ? MousePressed(false) : Input.GetKeyDown(KeyCode.F);
    public static bool PlacePressed => UseMouse
        ? MousePressed(true) : Input.GetKeyDown(KeyCode.E);
    public static bool CraftPressed => UseMouse
        ? !WorldInputBlocked && consumedFrame != Time.frameCount && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame
        : Input.GetKeyDown(KeyCode.R);

    // Closing a modal is independent of the world-input gate and the current gaze target.
    // Keep F as the advertised close shortcut in the mouse trial; Esc also works.
    public static bool ClosePanelPressed => UseMouse
        ? Keyboard.current != null && (Keyboard.current.fKey.wasPressedThisFrame
            || Keyboard.current.escapeKey.wasPressedThisFrame)
        : Input.GetKeyDown(KeyCode.Escape);

    private static bool MousePressed(bool right)
    {
        if (WorldInputBlocked || consumedFrame == Time.frameCount || Mouse.current == null) return false;
        return right ? Mouse.current.rightButton.wasPressedThisFrame : Mouse.current.leftButton.wasPressedThisFrame;
    }

    // Successful target selection consumes the whole click, including a simultaneous second button.
    public static void Consume()
    {
        if (UseMouse) consumedFrame = Time.frameCount;
    }

    public static void BlockCurrentFrame()
    {
        if (UseMouse) blockedFrame = Time.frameCount;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        blockedFrame = consumedFrame = uiEscapeFrame = -1;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (UseMouse && Object.FindFirstObjectByType<InteractionControlTrial>() == null)
        {
            var host = new GameObject("InteractionControlTrial");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<InteractionControlTrial>();
        }
    }

    private void Awake()
    {
        deliveryStation = Object.FindFirstObjectByType<DeliveryStation>();
        recipeStation = Object.FindFirstObjectByType<RecipeStation>();
    }

    private void Update()
    {
        // Remember panel focus before either station closes it, regardless of their Update order.
        if (Time.timeScale > 0f && ((deliveryStation != null && deliveryStation.IsUIOpen)
            || (recipeStation != null && recipeStation.IsUIOpen)))
            uiEscapeFrame = Time.frameCount;
        // Run before UI callbacks: a click that closes a panel cannot reach the world this frame.
        if (WorldInputBlocked || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            BlockCurrentFrame();
    }

    public static void ConfigurePrompt(GameObject prompt, string action)
    {
        if (!UseMouse || prompt == null) return;
        var label = prompt.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null) return;
        var panel = prompt.GetComponent<RectTransform>();
        ConfigureLabelPanel(panel, label, new Vector2(720f, 144f), 48f);
        label.text = (action.EndsWith("始める") ? "R キー" : "左クリック") + "\n" + action;
    }

    public static void ConfigureCloseHint(GameObject ui)
    {
        if (!UseMouse || ui == null) return;
        foreach (var label in ui.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label.text.Trim() != "で閉じる") continue;
            var panel = label.transform.parent as RectTransform;
            if (panel == null || panel.GetComponent<Image>() == null) continue;
            panel.SetParent(ui.transform, false);
            ConfigureLabelPanel(panel, label, new Vector2(580f, 100f), 48f);
            // Both modal hierarchies use different nested scales; position in root-canvas units.
            var canvas = label.canvas.rootCanvas.transform as RectTransform;
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
            Vector3 parentScale = panel.parent.lossyScale;
            panel.localScale = new Vector3(canvas.lossyScale.x / parentScale.x,
                canvas.lossyScale.y / parentScale.y, 1f);
            panel.position = canvas.TransformPoint(canvas.rect.min + new Vector2(28f, 20f));
            label.text = "F / Esc で閉じる";
        }
    }

    private static void ConfigureLabelPanel(RectTransform panel, TextMeshProUGUI label,
        Vector2 size, float fontSize)
    {
        // The scene's legacy labels have 0.2 scale and negative margins for large key icons.
        // Reset both transforms before using ordinary font sizes.
        panel.localScale = Vector3.one;
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        foreach (var icon in panel.GetComponentsInChildren<Image>(true))
            if (icon.transform != panel) icon.enabled = false;
        label.rectTransform.localScale = Vector3.one;
        label.rectTransform.localRotation = Quaternion.identity;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(20f, 8f);
        label.rectTransform.offsetMax = new Vector2(-20f, -8f);
        label.margin = Vector4.zero;
        label.enableAutoSizing = false;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.enableVertexGradient = false;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }
}
