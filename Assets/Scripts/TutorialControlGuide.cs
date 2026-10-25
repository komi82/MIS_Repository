using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>各レッスンの操作案内を実際のマウス操作に合わせる。</summary>
public static class TutorialControlGuide
{
    public static void Configure(GameObject window)
    {
        if (window == null || !InteractionControlTrial.UseMouse) return;
        string instruction;
        switch (SceneManager.GetActiveScene().name)
        {
            case SceneNames.Tutorial2:
                instruction = "左クリック：素材を拾う\n右クリック：テーブルに置く\n1～4：持ち物を選ぶ";
                break;
            case SceneNames.Tutorial3:
                instruction = "右クリック：釜に素材を置く\nR キー：調合を始める\nSpace キー：長押しで力を込める\n1～4：持ち物を選ぶ";
                break;
            case SceneNames.Tutorial4:
                instruction = "カラスを見て左クリック：納品画面\n左クリック：納品する品を選ぶ\nF / Esc：画面を閉じる";
                break;
            case SceneNames.Tutorial5:
                instruction = "右クリック：金床に素材を置く\nR キー：鍛冶を始める\n左クリック：光る点を叩く\n1～4：持ち物を選ぶ";
                break;
            case SceneNames.Tutorial6:
                instruction = "右クリック：浄化台に素材を置く\nR キー：浄化を始める\nSpace キー：タイミングよく押す\n1～4：持ち物を選ぶ";
                break;
            case SceneNames.Tutorial7:
            case "tutorial8":
                instruction = "魔導書を見て左クリック：レシピ\nF / Esc：画面を閉じる";
                break;
            default:
                return; // The first lesson's mouse-look and WASD illustrations remain useful.
        }

        var label = window.GetComponentInChildren<TextMeshProUGUI>(true);
        var panel = window.transform as RectTransform;
        if (label == null || panel == null || label.canvas == null) return;
        // Replace the legacy key illustrations inside this guide, preserving its background.
        foreach (var graphic in window.GetComponentsInChildren<Graphic>(true))
            if (graphic.transform != panel && graphic != label) graphic.enabled = false;
        label.enabled = true;
        label.gameObject.SetActive(true);
        var canvas = label.canvas.rootCanvas.transform as RectTransform;
        var background = window.GetComponent<Image>();
        if (background != null)
        {
            background.sprite = null;
            background.type = Image.Type.Simple;
            background.color = new Color(.035f, .03f, .025f, .9f);
        }
        var parentScale = panel.parent.lossyScale;
        panel.localScale = new Vector3(canvas.lossyScale.x / parentScale.x,
            canvas.lossyScale.y / parentScale.y, 1f);
        panel.pivot = new Vector2(0f, 1f);
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 760f);
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 280f);
        panel.position = canvas.TransformPoint(new Vector2(canvas.rect.xMin + 28f, canvas.rect.yMax - 28f));
        label.rectTransform.localScale = Vector3.one;
        label.rectTransform.localRotation = Quaternion.identity;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(28f, 20f);
        label.rectTransform.offsetMax = new Vector2(-28f, -20f);
        label.margin = Vector4.zero;
        label.enableAutoSizing = false;
        label.fontSize = 36f;
        label.color = Color.white;
        label.enableVertexGradient = false;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        label.text = instruction;
        foreach (var graphic in window.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
    }
}
