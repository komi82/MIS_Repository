using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// オプション画面のUIパネルを管理
/// BGM/SE音量スライダーとキー操作ガイド表示切り替えを制御
/// </summary>
public class OptionsUIPanel : MonoBehaviour
{
    [Header("音量調整UI")]
    [SerializeField]
    private Slider bgmVolumeSlider;
    
    [SerializeField]
    private Slider sfxVolumeSlider;
    
    [SerializeField]
    private TextMeshProUGUI bgmVolumeText;
    
    [SerializeField]
    private TextMeshProUGUI sfxVolumeText;
    
    [Header("キー操作ガイド")]
    [SerializeField]
    private Toggle showControlGuideToggle;
    
    [SerializeField]
    private TextMeshProUGUI showControlGuideLabel;
    
    [Header("ボタン")]
    [SerializeField]
    private Button resetButton;
    
    [SerializeField]
    private Button closeButton;
    
    [Header("オプションパネル")]
    [SerializeField]
    private CanvasGroup panelCanvasGroup;
    
    [SerializeField]
    private float fadeAnimationTime = 0.3f;
    
    private CanvasGroup canvasGroup;
    
    void Start()
    {
        if (!ValidateUI())
        {
            Debug.LogError("OptionsUIPanel: 必要なUIコンポーネントが割り当てられていません");
            enabled = false;
            return;
        }
        
        SetupUI();
        SetupListeners();
        UpdateUIFromSettings();
    }
    
    /// <summary>
    /// UI要素が正しく割り当てられているか確認
    /// </summary>
    private bool ValidateUI()
    {
        bool isValid = true;
        
        if (bgmVolumeSlider == null)
        {
            Debug.LogError("OptionsUIPanel: BGM音量スライダーが割り当てられていません");
            isValid = false;
        }
        
        if (sfxVolumeSlider == null)
        {
            Debug.LogError("OptionsUIPanel: SE音量スライダーが割り当てられていません");
            isValid = false;
        }
        
        if (showControlGuideToggle == null)
        {
            Debug.LogError("OptionsUIPanel: キー操作ガイド表示トグルが割り当てられていません");
            isValid = false;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// UIの初期設定
    /// </summary>
    private void SetupUI()
    {
        // スライダーの範囲設定
        bgmVolumeSlider.minValue = 0f;
        bgmVolumeSlider.maxValue = 1f;
        
        sfxVolumeSlider.minValue = 0f;
        sfxVolumeSlider.maxValue = 1f;
        
        // ボタンのリスナー設定
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
        
        // CanvasGroupの取得
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    /// <summary>
    /// UIイベントリスナー設定
    /// </summary>
    private void SetupListeners()
    {
        // スライダーリスナー
        bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        
        // トグルリスナー
        showControlGuideToggle.onValueChanged.AddListener(OnShowControlGuideToggled);
        
        // OptionsManagerのコールバック登録
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.OnBGMVolumeChanged(() => UpdateBGMVolumeUI());
            OptionsManager.Instance.OnSFXVolumeChanged(() => UpdateSFXVolumeUI());
            OptionsManager.Instance.OnShowControlGuideChanged(() => UpdateControlGuideUI());
        }
    }
    
    /// <summary>
    /// 設定からUIを更新
    /// </summary>
    private void UpdateUIFromSettings()
    {
        if (OptionsManager.Instance != null)
        {
            bgmVolumeSlider.value = OptionsManager.Instance.GetBGMVolume();
            sfxVolumeSlider.value = OptionsManager.Instance.GetSFXVolume();
            showControlGuideToggle.isOn = OptionsManager.Instance.GetShowControlGuide();
        }
    }
    
    private void OnBGMVolumeChanged(float value)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.SetBGMVolume(value);
        }
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.SetSFXVolume(value);
        }
    }
    
    private void OnShowControlGuideToggled(bool isOn)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.SetShowControlGuide(isOn);
        }
    }
    
    private void UpdateBGMVolumeUI()
    {
        if (bgmVolumeText != null && OptionsManager.Instance != null)
        {
            bgmVolumeText.text = $"{Mathf.RoundToInt(OptionsManager.Instance.GetBGMVolume() * 100)}%";
        }
    }
    
    private void UpdateSFXVolumeUI()
    {
        if (sfxVolumeText != null && OptionsManager.Instance != null)
        {
            sfxVolumeText.text = $"{Mathf.RoundToInt(OptionsManager.Instance.GetSFXVolume() * 100)}%";
        }
    }
    
    private void UpdateControlGuideUI()
    {
        if (showControlGuideToggle != null && OptionsManager.Instance != null)
        {
            showControlGuideToggle.isOn = OptionsManager.Instance.GetShowControlGuide();
        }
        
        if (showControlGuideLabel != null)
        {
            showControlGuideLabel.text = OptionsManager.Instance.GetShowControlGuide() ? "表示する" : "表示しない";
        }
    }
    
    private void OnResetClicked()
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.ResetToDefault();
            UpdateUIFromSettings();
        }
    }
    
    private void OnCloseClicked()
    {
        Close();
    }
    
    /// <summary>
    /// オプションパネルを開く
    /// </summary>
    public void Open()
    {
        gameObject.SetActive(true);
        UpdateUIFromSettings();
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(FadeCanvasGroup(0f, 1f, fadeAnimationTime));
        }
    }
    
    /// <summary>
    /// オプションパネルを閉じる
    /// </summary>
    public void Close()
    {
        if (canvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroupAndDeactivate(1f, 0f, fadeAnimationTime));
        }
        else
        {
            gameObject.SetActive(false);
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
        gameObject.SetActive(false);
    }
    
    void OnDestroy()
    {
        // リスナー削除
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }
        
        if (showControlGuideToggle != null)
        {
            showControlGuideToggle.onValueChanged.RemoveListener(OnShowControlGuideToggled);
        }
    }
}
