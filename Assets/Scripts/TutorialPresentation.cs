using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shared, provisional tutorial layout. Dialogue remains editable in each scene's TutorialSequence.
public sealed class TutorialPresentation : MonoBehaviour
{
    TextMeshProUGUI pageHint;
    public static TutorialPresentation Create(GameObject dialogue, TextMeshProUGUI body, GameObject guide)
    {
        if (dialogue == null || body == null) return null;
        // A dedicated reference canvas avoids inheriting the differing scales of the eight scenes.
        var root = new GameObject("TutorialPresentation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        var presentation = root.AddComponent<TutorialPresentation>();

        Image portrait = null;
        foreach (var graphic in dialogue.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic is Image picture && picture.sprite != null && picture.transform != body.transform.parent)
                portrait = picture;
            if (graphic != body) graphic.enabled = false;
        }
        var panel = (RectTransform)dialogue.transform;
        panel.SetParent(root.transform, false);
        Place(panel, new Vector2(.07f, .13f), new Vector2(.93f, .87f));
        var background = dialogue.GetComponent<Image>() ?? dialogue.AddComponent<Image>();
        background.enabled = true;
        background.sprite = null;
        background.color = new Color(.085f, .06f, .045f, .97f);
        background.raycastTarget = true;
        var edge = dialogue.GetComponent<Outline>() ?? dialogue.AddComponent<Outline>();
        edge.effectColor = new Color(.72f, .53f, .28f);
        edge.effectDistance = new Vector2(2, -2);

        if (portrait != null)
        {
            var rotation = portrait.transform.localRotation;
            portrait.transform.SetParent(panel, false);
            Place(portrait.rectTransform, new Vector2(.035f, .15f), new Vector2(.265f, .82f));
            portrait.transform.localRotation = rotation;
            portrait.enabled = true;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
        }
        body.transform.SetParent(panel, false);
        Place(body.rectTransform, new Vector2(portrait != null ? .31f : .06f, .18f), new Vector2(.95f, .80f));
        Style(body, 44, TextAlignmentOptions.MidlineLeft);
        body.fontSizeMin = 34;
        body.fontSizeMax = 44;
        body.enableAutoSizing = true;
        body.lineSpacing = 8;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;
        body.enabled = true;
        body.gameObject.SetActive(true);

        var heading = Label(panel, body, "TutorialHeading", new Vector2(.04f, .86f), new Vector2(.96f, .96f));
        Style(heading, 34, TextAlignmentOptions.MidlineLeft);
        heading.color = new Color(1f, .83f, .53f);
        heading.text = LessonTitle();
        presentation.pageHint = Label(panel, body, "TutorialPageHint", new Vector2(.04f, .04f), new Vector2(.96f, .13f));
        Style(presentation.pageHint, 28, TextAlignmentOptions.MidlineRight);
        presentation.pageHint.color = new Color(.87f, .83f, .75f);
        if (guide != null && InteractionControlTrial.UseMouse) guide.transform.SetParent(root.transform, false);
        return presentation;
    }

    public void SetPage(int index, int count)
    {
        if (pageHint == null) return;
        string next = index + 1 < count ? "左クリックで次へ" :
            SceneManager.GetActiveScene().name == "tutorial8" ? "左クリックでタイトルへ" : "左クリックで練習へ";
        pageHint.text = $"{index + 1} / {count}    {next}";
    }

    public static void ConfigureGuide(GameObject window, TextMeshProUGUI label)
    {
        if (window == null || label == null) return;
        var panel = window.transform as RectTransform;
        if (panel == null) return;
        foreach (var graphic in window.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.transform != panel && graphic != label) graphic.enabled = false;
            graphic.raycastTarget = false;
        }
        Place(panel, new Vector2(.02f, .71f), new Vector2(.43f, .97f));
        var background = window.GetComponent<Image>() ?? window.AddComponent<Image>();
        background.sprite = null;
        background.type = Image.Type.Simple;
        background.color = new Color(.085f, .06f, .045f, .92f);
        background.raycastTarget = false;
        label.transform.SetParent(panel, false);
        Place(label.rectTransform, Vector2.zero, Vector2.one);
        label.rectTransform.offsetMin = new Vector2(24, 18);
        label.rectTransform.offsetMax = new Vector2(-24, -18);
        Style(label, 36, TextAlignmentOptions.MidlineLeft);
        label.fontSizeMin = 28;
        label.fontSizeMax = 36;
        label.enableAutoSizing = true;
        label.enabled = true;
        label.gameObject.SetActive(true);
    }

    static string LessonTitle()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "tutorial": return "チュートリアル  1 / 8  ―  見渡す・移動する";
            case "tutorial2": return "チュートリアル  2 / 8  ―  素材を拾う";
            case "tutorial3": return "チュートリアル  3 / 8  ―  調合する";
            case "tutorial4": return "チュートリアル  4 / 8  ―  納品する";
            case "tutorial5": return "チュートリアル  5 / 8  ―  鍛冶をする";
            case "tutorial6": return "チュートリアル  6 / 8  ―  浄化する";
            case "tutorial7": return "チュートリアル  7 / 8  ―  レシピを見る";
            default: return "チュートリアル  8 / 8  ―  ショップと苦難";
        }
    }
    static TextMeshProUGUI Label(Transform parent, TextMeshProUGUI source, string name, Vector2 min, Vector2 max)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent, false);
        label.font = source.font;
        Place(label.rectTransform, min, max);
        return label;
    }
    static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = min; rect.anchorMax = max;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    static void Style(TextMeshProUGUI label, float size, TextAlignmentOptions alignment)
    {
        label.fontSize = size;
        label.enableAutoSizing = false;
        label.fontStyle = FontStyles.Normal;
        label.color = new Color(1f, .97f, .9f);
        label.enableVertexGradient = false;
        label.margin = Vector4.zero;
        label.characterSpacing = label.wordSpacing = 0;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
    }
}
