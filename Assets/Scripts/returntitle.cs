using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// pauseからタイトルへ戻るボタン処理を担当する。
/// </summary>
public class retuentitle : MonoBehaviour
{

    public void change_button()
    {
        FadeManager.Instance.LoadSceneWithFade(SceneNames.Title);

    }
}
