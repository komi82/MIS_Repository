using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Always visible purchase instructions, independent of hover text and stock.</summary>
public static class ShopQuickGuide
{
    public static void Create(TextMeshProUGUI template)
    {
        if (template == null || template.canvas == null) return;
        var root = template.canvas.rootCanvas.transform;
        if (root.Find("ShopQuickGuide") != null) return;
        var panel = new GameObject("ShopQuickGuide", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = new Vector2(.64f, .035f);
        rect.anchorMax = new Vector2(.975f, .29f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = panel.GetComponent<Image>();
        image.color = new Color(.11f, .07f, .045f, .94f);
        image.raycastTarget = false;
        var edge = panel.AddComponent<Outline>();
        edge.effectColor = new Color(.78f, .60f, .34f);
        edge.effectDistance = new Vector2(2, -2);
        var label = new GameObject("ShopQuickGuideText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(rect, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(24, 18);
        label.rectTransform.offsetMax = new Vector2(-24, -18);
        label.font = template.font;
        label.fontSize = label.fontSizeMax = 30f;
        label.fontSizeMin = 24f;
        label.enableAutoSizing = true;
        label.color = new Color(1f, .97f, .88f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        label.text = "<color=#FFD78C>ショップの使い方</color>\n商品にマウスを重ねる：効果・値段\n左クリック：所持金を使って購入\n買った商品は、その入店中は売り切れ\n右上の EXIT：次の日へ\n<size=90%>何も買わずに進んでも大丈夫</size>";
    }
}
