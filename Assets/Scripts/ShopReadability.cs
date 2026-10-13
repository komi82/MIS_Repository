using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Keeps shop descriptions readable over the artwork and inside the canvas.</summary>
public sealed class ShopReadability : MonoBehaviour
{
    TextMeshProUGUI label;
    RectTransform panel;
    string previousText;

    public static void Configure(TextMeshProUGUI detail)
    {
        if (detail == null || detail.GetComponent<ShopReadability>() != null) return;
        var root = detail.canvas.rootCanvas.transform;
        var border = new GameObject("ShopDescriptionFrame", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)border.transform;
        rect.SetParent(root, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(44f, 36f);
        rect.sizeDelta = new Vector2(1140f, 276f);
        var image = border.GetComponent<Image>();
        image.color = new Color(0.11f, 0.07f, 0.045f, 0.94f);
        image.raycastTarget = false;
        var outline = border.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.60f, 0.34f, 1f);
        outline.effectDistance = new Vector2(3, -3);
        detail.transform.SetParent(rect, false);
        detail.rectTransform.localScale = Vector3.one;
        detail.rectTransform.anchorMin = Vector2.zero;
        detail.rectTransform.anchorMax = Vector2.one;
        detail.rectTransform.offsetMin = new Vector2(28f, 20f);
        detail.rectTransform.offsetMax = new Vector2(-28f, -20f);
        detail.margin = Vector4.zero;
        detail.enableAutoSizing = false;
        detail.fontSize = 38;
        detail.color = new Color(1f, 0.97f, 0.88f, 1f);
        detail.alignment = TextAlignmentOptions.TopLeft;
        detail.textWrappingMode = TextWrappingModes.Normal;
        detail.raycastTarget = false;
        var readability = detail.gameObject.AddComponent<ShopReadability>();
        readability.label = detail;
        readability.panel = rect;
        readability.Refresh();

        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null || text.text.Trim() != "EXIT") continue;
            var buttonRect = (RectTransform)button.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = Vector2.one;
            buttonRect.anchoredPosition = new Vector2(-40f, -32f);
            buttonRect.sizeDelta = new Vector2(300f, 108f);
            buttonRect.localScale = Vector3.one;
            var background = button.GetComponent<Image>();
            background.color = new Color(0.13f, 0.08f, 0.05f, 0.96f);
            var edge = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            edge.effectColor = outline.effectColor;
            edge.effectDistance = new Vector2(3, -3);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(16f, 10f);
            text.rectTransform.offsetMax = new Vector2(-16f, -10f);
            text.rectTransform.localScale = Vector3.one;
            text.margin = Vector4.zero;
            text.enableAutoSizing = false;
            text.fontSize = 56f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }
    }

    void LateUpdate() { if (label != null && previousText != label.text) Refresh(); }

    public void Refresh()
    {
        previousText = label.text;
        // Keep the component active so a new hover can make the background visible again.
        panel.GetComponent<Image>().enabled = !string.IsNullOrWhiteSpace(label.text);
        panel.GetComponent<Outline>().enabled = !string.IsNullOrWhiteSpace(label.text);
        float preferred = label.GetPreferredValues(label.text, panel.rect.width - 56f, 1000f).y;
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(260f, preferred + 48f));
    }
}
