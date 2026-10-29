using TMPro;
using UnityEngine;

/// <summary>Keep full amounts inside a screen-relative area, even with ten-digit balances.</summary>
public static class NumericTextLayout
{
    public static void Fit(TextMeshProUGUI label, float maximum = 46f)
    {
        if (label == null) return;
        label.enableAutoSizing = true;
        label.fontSizeMin = 24f;
        label.fontSizeMax = maximum;
        label.fontSize = maximum;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.characterSpacing = label.wordSpacing = 0;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
    }

    // Rows are separate from day/challenge UI in the centre of the screen.
    public static void Hud(TextMeshProUGUI label, int row)
    {
        if (label == null || label.canvas == null) return;
        var rect = label.rectTransform;
        rect.SetParent(label.canvas.rootCanvas.transform, false);
        rect.anchorMin = new Vector2(.75f, .91f - .065f * row);
        rect.anchorMax = new Vector2(.98f, .97f - .065f * row);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        label.alignment = TextAlignmentOptions.MidlineRight;
        Fit(label);
    }
}
