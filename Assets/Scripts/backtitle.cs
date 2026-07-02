using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;


/// <summary>
/// リザルト画面からタイトルへ戻るボタン処理を担当する。
/// 遷移時にスコア表示や `MoneyManager` / `RequestManager` の静的値を初期化する。
/// </summary>
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
        FadeManager.Instance.LoadSceneWithFade(SceneNames.Title);
        xScoreText.text = null;
        yScoreText.text = null;
        
        // ショップで購入したアイテム・アーティファクトの所持状況をクリア
        OwnedProgressManager.ResetAll();
    }
}
