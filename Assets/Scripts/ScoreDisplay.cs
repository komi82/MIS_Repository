using UnityEngine;
using UnityEngine.UI;

public class ScoreDisplay : MonoBehaviour
{
    [Header("UIQÆ")]
    [SerializeField] private Text xScoreText;
    [SerializeField] private Text yScoreText;
    [SerializeField] private Text aScoreText;

    float score=0;


    void Start()
    {
        xScoreText.text = $"—˜‰v: {MoneyManager.currentMoney}";
        yScoreText.text = $"ˆË—ŠŒ”: {RequestManager.RequestCompleted}";
        float re = RequestManager.RequestCompleted;
        score = MoneyManager.currentMoney * (re * re * 0.1f); //ŒvZ®‚Í‚±‚ê
        int truncated = Mathf.FloorToInt(score);
        aScoreText.text = truncated.ToString("N0");
    }
}