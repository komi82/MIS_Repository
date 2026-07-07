using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// キー操作ガイドの表示制御
/// OptionsManagerの設定に基づいて表示/非表示を切り替える
/// </summary>
public class ControlGuideVisibilityController : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> controlGuideObjects = new List<GameObject>();

    [Header("Tag-based auto-find")]
    [Tooltip("タグ検索で自動的にガイドオブジェクトを見つける場合は true にする")]
    public bool useTagSearch = true;
    [Tooltip("タグ検索用のタグ名。該当タグを持つオブジェクトをシーン内から探します（非アクティブ含む）")]
    public string guideTagName = "ControlGuide";

    [SerializeField]
    private CanvasGroup canvasGroup;
    
    [SerializeField]
    private float fadeAnimationTime = 0.3f;
    
    private bool isCurrentlyVisible = true;
    
    void Start()
    {
        // CanvasGroupの取得
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // タグ検索を使う場合は事前に該当オブジェクトを探索してキャッシュ
        if (useTagSearch && !string.IsNullOrEmpty(guideTagName))
        {
            FindGuidesByTagInScene(guideTagName);
        }

        // OptionsManagerのコールバック登録
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.OnShowControlGuideChanged(OnControlGuideSettingChanged);
            UpdateVisibility(OptionsManager.Instance.GetShowControlGuide());
        }
        else
        {
            Debug.LogWarning("ControlGuideVisibilityController: OptionsManagerが見つかりません");
        }
    }
    
    /// <summary>
    /// キー操作ガイド設定変更時のコールバック
    /// </summary>
    private void OnControlGuideSettingChanged()
    {
        if (OptionsManager.Instance != null)
        {
            UpdateVisibility(OptionsManager.Instance.GetShowControlGuide());
        }
    }
    
    /// <summary>
    /// 表示/非表示を更新
    /// </summary>
    private void UpdateVisibility(bool shouldShow)
    {
        if (isCurrentlyVisible == shouldShow)
        {
            return;
        }
        
        isCurrentlyVisible = shouldShow;
        
        if (shouldShow)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
    
    /// <summary>
    /// ガイドを表示
    /// </summary>
    private void Show()
    {
        // オブジェクトを有効化
        foreach (var obj in controlGuideObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        
        if (gameObject != null)
        {
            gameObject.SetActive(true);
        }
        
        // フェードイン
        if (canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(0f, 1f, fadeAnimationTime));
        }
    }
    
    /// <summary>
    /// ガイドを非表示
    /// </summary>
    private void Hide()
    {
        if (canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroupAndDeactivate(1f, 0f, fadeAnimationTime));
        }
        else
        {
            // CanvasGroupがない場合は即座に非表示
            foreach (var obj in controlGuideObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
            
            if (gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// CanvasGroupのフェードアニメーション
    /// </summary>
    private System.Collections.IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
    }
    
    /// <summary>
    /// CanvasGroupをフェードアウトしてから非表示にする
    /// </summary>
    private System.Collections.IEnumerator FadeCanvasGroupAndDeactivate(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
        
        // オブジェクトを非表示
        foreach (var obj in controlGuideObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        
        if (gameObject != null)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// シーン全体から指定タグのオブジェクトを検索し、controlGuideObjects を更新する
    /// 非アクティブも含めて検索します。
    /// </summary>
    private void FindGuidesByTagInScene(string tag)
    {
        try
        {
            var allGos = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
            var list = new List<GameObject>();
            foreach (var go in allGos)
            {
                if (go == null) continue;
                if (go.CompareTag(tag)) list.Add(go);
            }
            controlGuideObjects = list;
            Debug.Log($"ControlGuideVisibilityController: Found {list.Count} guide objects with tag '{tag}'");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"ControlGuideVisibilityController: FindGuidesByTagInScene failed: {ex.Message}");
        }
    }
}
