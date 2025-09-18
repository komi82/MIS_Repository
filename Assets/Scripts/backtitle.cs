using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class backtitle : MonoBehaviour
{
    [SerializeField] private Text xScoreText;
    [SerializeField] private Text yScoreText;

    public void change_button()
    {
        SceneManager.LoadScene("title");
        xScoreText.text = null;
        yScoreText.text = null;
        MoneyManager.currentMoney = 0;
        RequestManager.RequestCompleted=0;
    }
}
