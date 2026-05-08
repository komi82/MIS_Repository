using UnityEngine;
using TMPro;

public class GameClockText : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private GameObject transitionPanel; // 遷移用UI
    [SerializeField] private int border = 30;

    private bool transitionStarted = false;
    [SerializeField] private DeliveryStation deliveryStation;
    int remainingCount;


    void Start()
    {
        remainingCount = border;
        transitionPanel.SetActive(false); // UI非表示
        UpdateClockDisplay();
    }

    void Update()
    {
        if (remainingCount == 0 && !transitionStarted)
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

        UpdateClockDisplay();
    }

    void UpdateClockDisplay()
    {
        remainingCount = border - RequestManager.RequestCompleted;
        clockText.text = remainingCount.ToString();
    }

    void TransitionToNextScene()
    {
        FadeManager.Instance.LoadSceneWithFade("result");
    }
}