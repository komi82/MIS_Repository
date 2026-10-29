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
            case "tutorial":
                instruction = "マウスを動かす：見渡す\nW / A / S / D：移動する\n目印の場所まで歩いてみよう";
                break;
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
        if (label == null) return;
        TutorialPresentation.ConfigureGuide(window, label);
        label.text = instruction;
    }
}
