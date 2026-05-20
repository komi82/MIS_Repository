using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;


public class backtitle : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI xScoreText;
    [SerializeField] private TextMeshProUGUI yScoreText;

    void Start()
    {
        // resultシーンではカーソルを表示状態にする
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public void change_button()
    {
        FadeManager.Instance.LoadSceneWithFade("title");
        xScoreText.text = null;
        yScoreText.text = null;
        MoneyManager.currentMoney = 0;
        RequestManager.RequestCompleted=0;
    }
}
