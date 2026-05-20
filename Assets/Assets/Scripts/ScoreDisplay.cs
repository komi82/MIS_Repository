using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ScoreDisplay : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI xScoreText;
    [SerializeField] private TextMeshProUGUI yScoreText;
    [SerializeField] private TextMeshProUGUI aScoreText;
    
    [Header("ランク表示")]
    [SerializeField] private Image rankImage; // ランクを表示するImage
    [SerializeField] private int[] rankThresholds = new int[] { 0, 5, 10, 20, 50 }; // ランクの閾値
    [SerializeField] private Sprite[] rankSprites; // 各ランクに対応するスプライト

    [Header("スコア演出設定")]
    [SerializeField] private float digitRouletteDuration = 0.5f; // 各桁のルーレット時間
    [SerializeField] private float digitStopDelay = 0.1f; // 各桁の停止間隔
    [SerializeField] private bool enableRouletteEffect = true; // ルーレット演出の有効/無効
    [SerializeField] private RouletteType rouletteType = RouletteType.Sequential; // ルーレット演出の種類
    [SerializeField] private int rouletteCycles = 3; // ルーレット回転回数（SequentialRoulette用）
    
    [Header("サウンド設定")]
    [SerializeField] private SoundData soundData; // サウンドデータ
    
    public enum RouletteType
    {
        Sequential,     // 1桁ずつ順番に表示
        AllDigits,      // 全桁同時にルーレット
        Smooth          // 滑らかな数値変化
    }

    float score = 0;
    int finalScore = 0;


    void Start()
    {
        // resultシーンではカーソルを表示状態にする
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        
        xScoreText.text = $"報酬金: {MoneyManager.currentMoney}";
        yScoreText.text = $"依頼件数: {RequestManager.RequestCompleted}";
        float re = RequestManager.RequestCompleted;
        score = MoneyManager.currentMoney * (re * re * 0.12f); // 計算結果
        finalScore = Mathf.FloorToInt(score);
        
        // ランク画像を最初は非表示にする
        if (rankImage != null)
        {
            rankImage.gameObject.SetActive(false);
        }
        
        // ルーレット演出が有効な場合は演出を開始、無効な場合は即座に表示
        if (enableRouletteEffect)
        {
            // スコアが1桁の場合は強制的にSequentialに変更
            RouletteType effectiveRouletteType = rouletteType;
            if (finalScore.ToString().Length == 1)
            {
                effectiveRouletteType = RouletteType.Sequential;
                Debug.Log($"スコアが1桁({finalScore})のため、RouletteTypeをSequentialに変更しました");
            }
            
            switch (effectiveRouletteType)
            {
                case RouletteType.Sequential:
                    StartCoroutine(SequentialRoulette());
                    break;
                case RouletteType.AllDigits:
                    StartCoroutine(AllDigitsRoulette());
                    break;
                case RouletteType.Smooth:
                    StartCoroutine(SmoothRoulette());
                    break;
            }
        }
        else
        {
            aScoreText.text = finalScore.ToString("N0");
            UpdateRank(finalScore);
        }
    }
    
    /// <summary>
    /// 1桁ずつ順番に表示するルーレット演出（急停止版）
    /// 下位桁（1の桁）から上位桁に向けて表示
    /// </summary>
    private IEnumerator SequentialRoulette()
    {
        string scoreString = finalScore.ToString("N0");
        int digitCount = scoreString.Length;
        
        // 1桁の場合は特別処理
        if (digitCount == 1)
        {
            Debug.Log("1桁スコアの特別処理を実行します");
            
            // 1桁のルーレット演出（ルーレット音を再生）
            yield return StartCoroutine(RouletteDigit(0, scoreString, digitCount, true));
            
            // ルーレット音を停止し、停止音を再生
            StopRouletteSound();
            PlayDisplaySound();
            
            // 最終スコア音を再生
            PlayFinalScoreSound();
            
            // ランクを表示
            if (rankImage != null)
            {
                rankImage.gameObject.SetActive(true);
            }
            
            // ランクを更新
            UpdateRank(finalScore);
            yield break;
        }
        
        // 複数桁の場合の通常処理
        // 下位桁（1の桁）から上位桁に向けて各桁を順番に表示
        for (int i = digitCount - 1; i >= 0; i--)
        {
            // 最初の桁（最下位桁）でのみルーレット音を再生
            bool playRouletteSound = (i == digitCount - 1);
            
            // 現在の桁をルーレットさせる
            yield return StartCoroutine(RouletteDigit(i, scoreString, digitCount, playRouletteSound));
            
            // ルーレット音を停止し、停止音を再生
            StopRouletteSound();
            PlayDisplaySound();
            
            // 次の桁に移行するまでの短い待機
            yield return new WaitForSeconds(digitStopDelay);
        }
        
        // 最終スコア音を再生
        PlayFinalScoreSound();
        
        // ランクを表示
        if (rankImage != null)
        {
            rankImage.gameObject.SetActive(true);
        }
        
        // ランクを更新
        UpdateRank(finalScore);
    }
    
    /// <summary>
    /// 指定された桁をルーレットさせる
    /// 下位桁から上位桁に向けて表示、未処理桁は空白
    /// </summary>
    private IEnumerator RouletteDigit(int digitIndex, string scoreString, int digitCount, bool playRouletteSound = false)
    {
        float elapsedTime = 0f;
        float cycleDuration = digitRouletteDuration / rouletteCycles;
        bool soundPlayed = false;
        
        while (elapsedTime < digitRouletteDuration)
        {
            // 現在のサイクルを計算
            int currentCycle = Mathf.FloorToInt(elapsedTime / cycleDuration);
            
            // サイクルに応じてルーレット速度を調整（後半になるほど遅く）
            float cycleProgress = (elapsedTime % cycleDuration) / cycleDuration;
            float speedMultiplier = Mathf.Lerp(1f, 0.3f, (float)currentCycle / (rouletteCycles - 1));
            
            // 最初の桁でルーレット音を1回のみ再生
            if (playRouletteSound && !soundPlayed)
            {
                StartRouletteSound();
                soundPlayed = true;
            }
            
            // 下位桁から上位桁に向けて表示（固定長で表示）
            string displayText = "";
            
            for (int j = digitCount - 1; j >= 0; j--)
            {
                if (j > digitIndex)
                {
                    // まだ処理していない上位桁は空白（固定幅）
                    displayText = "　" + displayText; // 全角空白を使用
                }
                else if (j == digitIndex)
                {
                    // 現在の桁はランダムな数字を表示
                    displayText = Random.Range(0, 10).ToString() + displayText;
                }
                else
                {
                    // 既に確定した下位桁は実際の数字を表示
                    displayText = scoreString[j].ToString() + displayText;
                }
            }
            
            aScoreText.text = displayText;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 最終的に正しい桁の数字を表示
        string finalDisplayText = "";
        for (int j = digitCount - 1; j >= 0; j--)
        {
            if (j >= digitIndex)
            {
                // 現在の桁までを実際の数字で表示
                finalDisplayText = scoreString[j].ToString() + finalDisplayText;
            }
            else
            {
                // まだ処理していない上位桁は空白（固定幅）
                finalDisplayText = "　" + finalDisplayText; // 全角空白を使用
            }
        }
        aScoreText.text = finalDisplayText;
    }
    
    /// <summary>
    /// ルーレット音を開始
    /// </summary>
    private void StartRouletteSound()
    {
        if (SoundManager.Instance != null && soundData != null && soundData.rouletteSound != null)
        {
            // 直接PlaySFXを呼び出してルーレット音を再生
            SoundManager.Instance.PlaySFX(soundData.rouletteSound, 1f, true);
            Debug.Log("ルーレット音を再生しました");
        }
        else
        {
            Debug.LogWarning($"ルーレット音を再生できません - SoundManager: {SoundManager.Instance != null}, SoundData: {soundData != null}, RouletteSound: {soundData?.rouletteSound != null}");
        }
    }
    
    /// <summary>
    /// ルーレット音を停止（現在は何もしない、音は自然に終了する）
    /// </summary>
    private void StopRouletteSound()
    {
        // ルーレット音は自然に終了するため、特別な停止処理は不要
        Debug.Log("ルーレット音を停止しました");
    }
    
    /// <summary>
    /// 桁停止音を再生
    /// </summary>
    private void PlayDisplaySound()
    {
        if (SoundManager.Instance != null && soundData != null && soundData.displaySound != null)
        {
            SoundManager.Instance.PlaySFX(soundData.displaySound, 1f, true);
        }
    }
    
    /// <summary>
    /// 最終スコア音を再生（レシピ完成音を使用）
    /// </summary>
    private void PlayFinalScoreSound()
    {
        if (SoundManager.Instance != null && soundData != null && soundData.recipeCompleteSound != null)
        {
            SoundManager.Instance.PlaySFX(soundData.recipeCompleteSound, 1f, true);
        }
    }
    
    /// <summary>
    /// 全桁同時にルーレットする演出
    /// </summary>
    private IEnumerator AllDigitsRoulette()
    {
        string scoreString = finalScore.ToString("N0");
        int digitCount = scoreString.Length;
        float elapsedTime = 0f;
        
        // ルーレット音を開始
        StartRouletteSound();
        
        while (elapsedTime < digitRouletteDuration)
        {
            string displayText = "";
            
            for (int i = 0; i < digitCount; i++)
            {
                // 各桁をランダムに表示
                displayText += Random.Range(0, 10).ToString();
            }
            
            aScoreText.text = displayText;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // ルーレット音を停止し、停止音を再生
        StopRouletteSound();
        PlayDisplaySound();
        
        // 最終的に正しいスコアを表示
        aScoreText.text = finalScore.ToString("N0");
        
        // 最終スコア音を再生
        PlayFinalScoreSound();
        
        // ランクを表示
        if (rankImage != null)
        {
            rankImage.gameObject.SetActive(true);
        }
        
        // ランクを更新
        UpdateRank(finalScore);
    }
    
    /// <summary>
    /// 滑らかな数値変化の演出
    /// </summary>
    private IEnumerator SmoothRoulette()
    {
        float elapsedTime = 0f;
        int startValue = 0;
        
        // ルーレット音を開始
        StartRouletteSound();
        
        while (elapsedTime < digitRouletteDuration)
        {
            // イージング関数を使用して滑らかに変化
            float t = elapsedTime / digitRouletteDuration;
            t = 1f - Mathf.Pow(1f - t, 3f); // イーズアウト
            
            int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, finalScore, t));
            aScoreText.text = currentValue.ToString("N0");
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // ルーレット音を停止し、停止音を再生
        StopRouletteSound();
        PlayDisplaySound();
        
        // 最終的に正しいスコアを表示
        aScoreText.text = finalScore.ToString("N0");
        
        // 最終スコア音を再生
        PlayFinalScoreSound();
        
        // ランクを表示
        if (rankImage != null)
        {
            rankImage.gameObject.SetActive(true);
        }
        
        // ランクを更新
        UpdateRank(finalScore);
    }
    
    /// <summary>
    /// オブジェクト破棄時の処理
    /// </summary>
    void OnDestroy()
    {
        // 特別な処理は不要（音は自然に終了する）
    }
    
    /// <summary>
    /// 依頼件数に応じてランクを更新
    /// </summary>
    private void UpdateRank(int requestCount)
    {
        if (rankImage == null || rankSprites == null || rankSprites.Length == 0)
        {
            Debug.LogWarning("ランク表示用のImageまたはスプライトが設定されていません");
            return;
        }
        
        if (rankThresholds == null || rankThresholds.Length == 0)
        {
            Debug.LogWarning("ランクの閾値が設定されていません");
            return;
        }
        
        // スプライト配列と閾値配列のサイズが一致しているかチェック
        if (rankThresholds.Length != rankSprites.Length)
        {
            Debug.LogWarning($"ランクの閾値({rankThresholds.Length})とスプライト({rankSprites.Length})の数が一致していません");
            return;
        }
        
        // 現在の依頼件数に応じたランクを決定
        int currentRankIndex = 0;
        for (int i = 0; i < rankThresholds.Length; i++)
        {
            if (requestCount >= rankThresholds[i])
            {
                currentRankIndex = i;
            }
            else
            {
                break;
            }
        }
        
        // スプライトを設定
        if (currentRankIndex < rankSprites.Length && rankSprites[currentRankIndex] != null)
        {
            rankImage.sprite = rankSprites[currentRankIndex];
            rankImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"ランク{currentRankIndex}のスプライトが設定されていません");
        }
    }
}