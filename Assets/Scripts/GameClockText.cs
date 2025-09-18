using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameClockText : MonoBehaviour
{
    [Header("リアル時間とゲーム時間のスケール")]
    [SerializeField] private float realSecondsPerStep = 1f;
    [SerializeField] private float gameMinutesPerStep = 1f;

    [Header("UI参照")]
    [SerializeField] private Text clockText;
    [SerializeField] private GameObject transitionPanel; // 表示するUI

    private float timer = 0f;
    private int gameHour = 9;
    private int gameMinute = 0;
    private bool transitionStarted = false;
    [SerializeField] private DeliveryStation deliveryStation;


    void Start()
    {
        transitionPanel.SetActive(false); // UI表示

    }

    void Update()
    {

        if (timer >= realSecondsPerStep)
        {
            timer -= realSecondsPerStep;
            AdvanceGameTime();
        }

        if (gameHour == 21 && !transitionStarted)
        {
            transitionStarted = true;
            transitionPanel.SetActive(true); // UI表示
            deliveryStation.CursorActive = true;
            Cursor.lockState = CursorLockMode.Confined; // ゲームウィンドウ内に制限
            Invoke(nameof(TransitionToNextScene), 3f); // 3秒後にシーン遷移
        }
        else
        {
            timer += Time.deltaTime;

        }

        UpdateClockDisplay();
    }

    void AdvanceGameTime()
    {
        gameMinute += Mathf.RoundToInt(gameMinutesPerStep);
        if (gameMinute >= 60)
        {
            gameHour += gameMinute / 60;
            gameMinute %= 60;
        }
        if (gameHour >= 24) gameHour %= 24;
    }

    void UpdateClockDisplay()
    {
        clockText.text = $"{gameHour:00}:{gameMinute:00}";
    }

    void TransitionToNextScene()
    {
        SceneManager.LoadScene("result");
    }
}