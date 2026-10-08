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
        
        // シーン切り替え前にポーズを解除（PauseController がポーズUIを残さないようにする）
        var pauseControllers = UnityEngine.Object.FindObjectsOfType<PauseController>();
        if (pauseControllers != null && pauseControllers.Length > 0)
        {
            foreach (var pc in pauseControllers)
            {
                if (pc != null)
                {
                    pc.Resume();
                }
            }
        }

        // シーン切り替え
        if (sceneName == SceneNames.Shop)
            yield return LoadShopAsync(sceneName);
        else
            SceneManager.LoadScene(sceneName);

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
    
    // Large shop art must not block the main thread behind an opaque fade.
    IEnumerator LoadShopAsync(string sceneName)
    {
        GameObject loading = null;
        RectTransform progress = null;
        TMPro.TextMeshProUGUI label = null;
        if (fadeImage != null)
        {
            loading = new GameObject("ShopLoading", typeof(RectTransform));
            var root = loading.GetComponent<RectTransform>();
            root.SetParent(fadeImage.transform, false);
            root.sizeDelta = new Vector2(320f, 90f);

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(root, false);
            label = textObject.AddComponent<TMPro.TextMeshProUGUI>();
            label.text = "Loading...";
            label.fontSize = 28f;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(320f, 50f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 10f);

            var track = new GameObject("Track", typeof(RectTransform));
            track.transform.SetParent(root, false);
            var trackImage = track.AddComponent<Image>();
            trackImage.color = new Color(1f, 1f, 1f, 0.2f);
            trackImage.raycastTarget = false;
            trackImage.rectTransform.sizeDelta = new Vector2(300f, 6f);
            trackImage.rectTransform.anchoredPosition = new Vector2(0f, -30f);

            var bar = new GameObject("Progress", typeof(RectTransform));
            bar.transform.SetParent(track.transform, false);
            var barImage = bar.AddComponent<Image>();
            barImage.color = new Color(1f, 0.75f, 0.25f, 1f);
            barImage.raycastTarget = false;
            progress = barImage.rectTransform;
            progress.anchorMin = progress.anchorMax = progress.pivot = new Vector2(0f, 0.5f);
            progress.anchoredPosition = Vector2.zero;
            progress.sizeDelta = new Vector2(0f, 6f);
        }

        try
        {
            // Present the loading indicator before requesting assets.
            yield return null;
            var operation = SceneManager.LoadSceneAsync(sceneName);
            while (!operation.isDone)
            {
                if (progress != null)
                    progress.sizeDelta = new Vector2(300f * Mathf.Clamp01(operation.progress / 0.9f), 6f);
                if (label != null)
                    label.text = "Loading" + new string('.', 1 + (int)(Time.realtimeSinceStartup * 2f) % 3);
                yield return null;
            }
        }
        finally
        {
            if (loading != null) Destroy(loading);
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
            // タイムスケールの影響を受けない
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(elapsedTime / fadeTime));
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null; // 次フレームまで待機（フレーム自体は進む）
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
            // タイムスケールの影響を受けない
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsedTime / fadeTime));
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


