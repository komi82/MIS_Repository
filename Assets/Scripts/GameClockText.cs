using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameClockText : MonoBehaviour
{
    [Header("リアル時間とゲーム時間の比率")]
    [SerializeField] private float realSecondsPerStep = 1f;
    [SerializeField] private float gameMinutesPerStep = 1f;

    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private GameObject transitionPanel; // 遷移用UI

    private float timer = 0f;
    private int gameHour = 0;
    private int gameMinute = 0;
    private bool transitionStarted = false;
    
    [Header("カウントダウン設定")]
    [SerializeField] private int totalGameMinutes = 300; // 5時間 = 300分
    private int remainingMinutes;
    [SerializeField] private DeliveryStation deliveryStation;


    void Start()
    {
        transitionPanel.SetActive(false); // UI非表示
        remainingMinutes = totalGameMinutes; // 残り時間を初期化
        UpdateGameTimeFromRemaining(); // 初期時間を設定
    }

    void Update()
    {
        if (timer >= realSecondsPerStep)
        {
            timer -= realSecondsPerStep;
            CountdownGameTime(); // カウントダウン方式に変更
        }

        if (remainingMinutes <= 0 && !transitionStarted) // 残り時間が0以下の場合
        {
            transitionStarted = true;
            transitionPanel.SetActive(true); // UI表示
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.timeupSound);
            }
            deliveryStation.CursorActive = true;
            Cursor.lockState = CursorLockMode.Confined; // マウスカーソル表示
            Invoke(nameof(TransitionToNextScene), 1f); //1秒後にシーン遷移
        }
        else
        {
            timer += Time.deltaTime; // タイマーを加算方式に変更
        }

        UpdateClockDisplay();
    }

    void CountdownGameTime()
    {
        remainingMinutes -= Mathf.RoundToInt(gameMinutesPerStep);
        if (remainingMinutes < 0) remainingMinutes = 0;
        UpdateGameTimeFromRemaining();
    }
    
    void UpdateGameTimeFromRemaining()
    {
        gameHour = remainingMinutes / 60;
        gameMinute = remainingMinutes % 60;
    }

    void UpdateClockDisplay()
    {
        clockText.text = $"{gameHour:00}:{gameMinute:00}";
    }

    void TransitionToNextScene()
    {
        FadeManager.Instance.LoadSceneWithFade("result");
    }
}