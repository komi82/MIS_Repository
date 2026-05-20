using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 特定ボタン押下時に指定UIを非表示化する軽量ハンドラ。
/// 主に単発演出のトリガー用途で利用する。
/// </summary>
public class button_click : MonoBehaviour
{
    public GameObject image;

    public void DeactivateButton()
    {
        image.SetActive(false);
    }
}