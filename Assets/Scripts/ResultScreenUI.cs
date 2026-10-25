using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Result actions and the same owned-effect viewer used during gameplay.</summary>
public static class ResultScreenUI
{
    public static void Build(TextMeshProUGUI day, TextMeshProUGUI money, TextMeshProUGUI requests,
        Image rank, BaffItemDatabase baffDatabase, ArtifactDatabase artifacts)
    {
        if (day == null || day.canvas == null) return;
        Transform canvas = day.canvas.rootCanvas.transform;
        // Normalize the original score layout so the new clear message and actions fit at any aspect ratio.
        LayoutText(day, new Vector2(.12f, .66f), new Vector2(.88f, .77f), 68);
        LayoutText(money, new Vector2(.43f, .46f), new Vector2(.94f, .57f), 42);
        LayoutText(requests, new Vector2(.43f, .32f), new Vector2(.94f, .43f), 42);
        if (rank != null)
        {
            Bounds(rank.rectTransform, new Vector2(.08f, .26f), new Vector2(.38f, .55f));
            rank.preserveAspect = true;
        }
        foreach (var text in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (text.text == "RANK") LayoutText(text, new Vector2(.08f, .56f), new Vector2(.38f, .63f), 32);

        var root = Rect("ResultActions", canvas);
        Bounds(root, Vector2.zero, Vector2.one);
        Label("ResultHeading", RunResultState.HasResult ? RunResultState.Heading : "リザルト", root, day,
            new Vector2(.06f, .84f), new Vector2(.94f, .96f), 62);
        Label("ResultMessage", RunResultState.HasResult ? RunResultState.Message : "今回の記録と所持アイテムを確認できます。", root, day,
            new Vector2(.06f, .77f), new Vector2(.94f, .84f), 27);

        // Reuse the original title button and its configured click sound / scene loader.
        foreach (var button in canvas.GetComponentsInChildren<Button>(true))
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null || label.text != "TITLE") continue;
            Bounds((RectTransform)button.transform, new Vector2(.06f, .08f), new Vector2(.31f, .18f));
            label.text = "タイトルへ";
            LayoutText(label, Vector2.zero, Vector2.one, 30);
        }

        var owned = OwnedItemsHUD.CreateForResult(canvas, day, baffDatabase, artifacts);
        CreateButton("ViewResultItems", "所持アイテムを見る [Tab]", root, day,
            new Vector2(.34f, .08f), new Vector2(.65f, .18f), owned.ToggleResultWindow);
        if (RunResultState.CanContinue)
        {
            Button continuation = null;
            continuation = CreateButton("ContinueToDay8", "14日クリアを目指す", root, day,
                new Vector2(.68f, .08f), new Vector2(.94f, .18f), () =>
                {
                    if (ChangeScene.ContinueAfterWeekOne()) continuation.interactable = false;
                });
        }
    }

    public static Button CreateButton(string name, string caption, Transform parent, TextMeshProUGUI template,
        Vector2 min, Vector2 max, UnityAction click)
    {
        var rect = Rect(name, parent);
        Bounds(rect, min, max);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.14f, .19f, .25f, 1);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        // Avoid keyboard selection carrying into a scene-changing action while paging through items.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(click);
        var label = Label("Label", caption, rect, template, new Vector2(.025f, .08f), new Vector2(.975f, .92f), 29);
        label.color = Color.white;
        return button;
    }

    private static TextMeshProUGUI Label(string name, string content, Transform parent, TextMeshProUGUI template,
        Vector2 min, Vector2 max, float size)
    {
        var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = template.font;
        text.fontSharedMaterial = template.fontSharedMaterial;
        text.color = template.color;
        text.text = content;
        LayoutText(text, min, max, size);
        return text;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void LayoutText(TextMeshProUGUI text, Vector2 min, Vector2 max, float size)
    {
        if (text == null) return;
        Bounds(text.rectTransform, min, max);
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMax = size;
        text.fontSizeMin = size * .65f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
        text.wordSpacing = 0;
        text.margin = Vector4.zero;
    }

    private static void Bounds(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
