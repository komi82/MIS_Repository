using UnityEngine;
using UnityEngine.UI;
using System.Collections;

using UnityEngine.SceneManagement;


/// <summary>
/// シーン移行時のフェード管理システム
/// 暗転フェードアウト・フェードインを制御
/// </summary>
public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }
    
    [Header("フェード設定")]
    [Tooltip("フェード時間（秒）")]
    public float fadeTime = 1f;
    
    [Tooltip("フェード色")]
    public Color fadeColor = Color.black;
    
    [Tooltip("フェード用UI")]
    public Image fadeImage;
    
    [Header("デバッグ")]
    [Tooltip("デバッグログを表示するか")]
    public bool enableDebugLog = false;
    
    private bool isFading = false;
    
    void Awake()
    {
        // シングルトンパターン
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFadeUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// フェード用UIを初期化
    /// </summary>
    void InitializeFadeUI()
    {
        // フェード用UIが設定されていない場合は自動作成
        if (fadeImage == null)
        {
            CreateFadeUI();
        }
        
        // 初期状態は透明
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// フェード用UIを自動作成
    /// </summary>
    void CreateFadeUI()
    {
        // Canvasを作成
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform); // FadeManagerの子として作成
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 最前面に表示
        
        // CanvasScalerを追加
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // GraphicRaycasterを追加
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // フェード用Imageを作成
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = fadeColor;
        
        // RectTransformを設定
        RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        if (enableDebugLog)
        {
            Debug.Log("FadeManager: フェード用UIを自動作成しました");
        }
    }
    
    /// <summary>
    /// シーンをフェード付きで切り替え
    /// </summary>
    /// <param name="sceneName">切り替え先のシーン名</param>
    public void LoadSceneWithFade(string sceneName)
    {
        if (isFading)
        {
            if (enableDebugLog) Debug.LogWarning("FadeManager: 既にフェード中です");
            return;
        }
        
        StartCoroutine(FadeAndLoadScene(sceneName));
    }
    
    /// <summary>
    /// フェードアウト → シーン切り替え → フェードイン
    /// </summary>
    IEnumerator FadeAndLoadScene(string sceneName)
    {
        isFading = true;
        
        if (enableDebugLog)
        {
            Debug.Log($"FadeManager: シーン切り替え開始 - {sceneName}");
        }
        
        // フェードアウト
        yield return StartCoroutine(FadeOut());
        
        // シーン切り替え
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        
        // 1フレーム待機（シーン読み込み完了を待つ）
        yield return null;
        
        // フェードイン
        yield return StartCoroutine(FadeIn());
        
        isFading = false;
        
        if (enableDebugLog)
        {
            Debug.Log("FadeManager: シーン切り替え完了");
        }
    }
    
    /// <summary>
    /// フェードアウト
    /// </summary>
    IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;
        
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        
        float elapsedTime = 0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeTime);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }
        
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
    }
    
    /// <summary>
    /// フェードイン
    /// </summary>
    IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;
        
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        
        float elapsedTime = 0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }
        
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        fadeImage.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// フェードアウトのみ（シーン切り替えなし）
    /// </summary>
    public void FadeOutOnly()
    {
        if (isFading) return;
        StartCoroutine(FadeOut());
    }

    /// <summary>
    /// フェードアウト後にゲームを終了する
    /// </summary>
    public void QuitWithFade()
    {
        if (isFading) return;
        StartCoroutine(FadeOutAndQuit());
    }

    IEnumerator FadeOutAndQuit()
    {
        isFading = true;

        if (enableDebugLog)
        {
            Debug.Log("FadeManager: 終了フェード開始");
        }

        yield return StartCoroutine(FadeOut());

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    /// <summary>
    /// フェードインのみ
    /// </summary>
    public void FadeInOnly()
    {
        if (isFading) return;
        StartCoroutine(FadeIn());
    }
    
    /// <summary>
    /// フェード時間を設定
    /// </summary>
    public void SetFadeTime(float time)
    {
        fadeTime = Mathf.Max(0.1f, time);
    }
    
    /// <summary>
    /// フェード色を設定
    /// </summary>
    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        if (fadeImage != null)
        {
            fadeImage.color = new Color(color.r, color.g, color.b, fadeImage.color.a);
        }
    }
    
    /// <summary>
    /// 現在フェード中かどうか
    /// </summary>
    public bool IsFading()
    {
        return isFading;
    }
    
    /// <summary>
    /// デバッグ情報を表示
    /// </summary>
    [ContextMenu("デバッグ情報を表示")]
    public void ShowDebugInfo()
    {
        Debug.Log($"FadeManager デバッグ情報:");
        Debug.Log($"- フェード中: {isFading}");
        Debug.Log($"- フェード時間: {fadeTime}秒");
        Debug.Log($"- フェード色: {fadeColor}");
        Debug.Log($"- フェードUI存在: {fadeImage != null}");
    }
}


