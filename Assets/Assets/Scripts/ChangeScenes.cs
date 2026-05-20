using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene: MonoBehaviour
{
    [Header("UI設定")]
    [Tooltip("表示するUIオブジェクト")]
    public GameObject targetUI;
    
    [Tooltip("UI表示時のボタンクリック音を再生するか")]
    public bool playButtonSound = true;
    
/*    [Header("点滅UI設定")]
    [Tooltip("点滅させるUIオブジェクト")]
    public GameObject blinkUI;
    
    [Tooltip("点滅の速度（秒）")]
    public float blinkSpeed = 2f;
    
    [Tooltip("点滅の最小アルファ値")]
    [Range(0f, 1f)]
    public float minAlpha = 0.2f;
    
    [Tooltip("点滅の最大アルファ値")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f; */
    
    [Header("キー設定")]
    [Tooltip("UIを非表示にするキー")]
    public KeyCode hideKey = KeyCode.Escape;
    
    private bool isUIVisible = false;
  //  private bool isBlinking = false;
    private Coroutine blinkCoroutine;
    private CanvasGroup blinkCanvasGroup;
    
    void Awake()
    {
        QualitySettings.vSyncCount = 1; // VSyncを無効にすることでtargetFrameRateが有効になる
        DontDestroyOnLoad(gameObject); // シーンをまたいでも破棄されないようにする

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.soundData.gameplayBGM);
        }
    }
    void Start()
    {
        // 点滅UIの初期化
        //InitializeBlinkUI();
    }
    
    void Update()
    {
        // EscキーでUIを非表示
        if (isUIVisible && Input.GetKeyDown(hideKey))
        {
            HideUI();
        }
        
        // 点滅の制御
    //    UpdateBlinking();
    }
 /*   
    /// <summary>
    /// 点滅UIの初期化
    /// </summary>
    void InitializeBlinkUI()
    {
        if (blinkUI != null)
        {
            // CanvasGroupを取得または追加
            blinkCanvasGroup = blinkUI.GetComponent<CanvasGroup>();
            if (blinkCanvasGroup == null)
            {
                blinkCanvasGroup = blinkUI.AddComponent<CanvasGroup>();
            }
            
            // 初期状態は非表示
            blinkUI.SetActive(false);
        }
    }
    
    /// <summary>
    /// 点滅の制御
    /// </summary>
    void UpdateBlinking()
    {
        if (blinkUI == null) return;
        
        // isUIVisibleがtrueの時は点滅、falseの時は非表示
        if (isUIVisible && !isBlinking)
        {
            StartBlinking();
        }
        else if (!isUIVisible && isBlinking)
        {
            StopBlinking();
        }
    }
    
    /// <summary>
    /// 点滅を開始
    /// </summary>
    void StartBlinking()
    {
        if (blinkUI == null || blinkCanvasGroup == null) return;
        
        isBlinking = true;
        blinkUI.SetActive(true);
        
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        
        blinkCoroutine = StartCoroutine(BlinkCoroutine());
    }
    
    /// <summary>
    /// 点滅を停止
    /// </summary>
    void StopBlinking()
    {
        isBlinking = false;
        
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        if (blinkUI != null)
        {
            blinkUI.SetActive(false);
        }
    }
    
    /// <summary>
    /// 点滅コルーチン
    /// </summary>
    IEnumerator BlinkCoroutine()
    {
        while (isBlinking)
        {
            // フェードイン
            float elapsedTime = 0f;
            while (elapsedTime < blinkSpeed / 2f && isBlinking)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, elapsedTime / (blinkSpeed / 2f));
                blinkCanvasGroup.alpha = alpha;
                yield return null;
            }
            
            if (!isBlinking) break;
            
            // フェードアウト
            elapsedTime = 0f;
            while (elapsedTime < blinkSpeed / 2f && isBlinking)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(maxAlpha, minAlpha, elapsedTime / (blinkSpeed / 2f));
                blinkCanvasGroup.alpha = alpha;
                yield return null;
            }
        }
    } */
    
    public void change_button()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.deliverySound);
        }
        FadeManager.Instance.LoadSceneWithFade("arcade");
    }
    
    /// <summary>
    /// 特定のUIを表示し、ボタンクリック音を再生
    /// </summary>
    public void ShowUI()
    {
        // ボタンクリック音を再生
        if (playButtonSound && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.buttonClickSound);
        }
        
        // UIを表示
        if (targetUI != null)
        {
            targetUI.SetActive(true);
            isUIVisible = true;
        }
        else
        {
            Debug.LogWarning("ChangeScene: 表示するUIが設定されていません");
        }
    }
    
    /// <summary>
    /// 特定のUIを非表示
    /// </summary>
    public void HideUI()
    {
        if (targetUI != null)
        {
            targetUI.SetActive(false);
            isUIVisible = false;
        }
    }
 /*   
    /// <summary>
    /// 点滅を手動で開始
    /// </summary>
    public void StartBlinkingManual()
    {
        if (isUIVisible)
        {
            StartBlinking();
        }
    }
    
    /// <summary>
    /// 点滅を手動で停止
    /// </summary>
    public void StopBlinkingManual()
    {
        StopBlinking();
    }
    
    /// <summary>
    /// 点滅の設定を変更
    /// </summary>
    public void SetBlinkSettings(float speed, float min, float max)
    {
        blinkSpeed = Mathf.Max(0.1f, speed);
        minAlpha = Mathf.Clamp01(min);
        maxAlpha = Mathf.Clamp01(max);
    }*/
    
    /// <summary>
    /// UIの表示状態を切り替え
    /// </summary>
    public void ToggleUI()
    {
        if (isUIVisible)
        {
            HideUI();
        }
        else
        {
            ShowUI();
        }
    }
    
    /// <summary>
    /// 現在UIが表示されているかどうか
    /// </summary>
    public bool IsUIVisible()
    {
        return isUIVisible;
    }
}
