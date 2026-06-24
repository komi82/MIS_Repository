using TMPro;
using UnityEngine;

/// <summary>
/// プロンプト用 TMP の文字列長に合わせて UI 横幅を調整する。
/// </summary>
public static class PromptUIUtility
{
	private const float DefaultHorizontalPadding = 20f;
	private const float MeasureLayoutWidth = 10000f;

	public static void SetTextAndResizeWidth(TextMeshProUGUI text, RectTransform container, string value, float horizontalPadding = DefaultHorizontalPadding)
	{
		if (text == null) return;

		bool textChanged = text.text != value;
		if (textChanged)
		{
			text.text = value;
			ResizeWidth(text, container, horizontalPadding);
		}
	}

	public static void ResizeWidth(TextMeshProUGUI text, RectTransform container, float horizontalPadding = DefaultHorizontalPadding)
	{
		if (text == null || container == null) return;

		float textWidth = MeasureTextWidthInContainerSpace(text, container);
		container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth + horizontalPadding * 2f);
	}

	private static float MeasureTextWidthInContainerSpace(TextMeshProUGUI text, RectTransform container)
	{
		TextWrappingModes previousWrap = text.textWrappingMode;
		text.textWrappingMode = TextWrappingModes.NoWrap;

		RectTransform textRect = text.rectTransform;
		float previousWidth = textRect.rect.width;
		textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MeasureLayoutWidth);

		text.ForceMeshUpdate(true);

		float contentWidth = text.textBounds.size.x + text.margin.x + text.margin.z;
		float scaleInContainer = textRect.lossyScale.x / Mathf.Max(container.lossyScale.x, 0.001f);
		float widthInContainerSpace = contentWidth * scaleInContainer;

		textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, previousWidth);
		text.textWrappingMode = previousWrap;

		return widthInContainerSpace;
	}
}
