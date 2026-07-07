using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// オプション設定を管理するシステム
/// PlayerPrefsで設定を永続化
/// </summary>
public class OptionsManager : MonoBehaviour
{
    public static OptionsManager Instance { get; private set; }
    
    private const string BGM_VOLUME_KEY = "BGM_Volume";
    private const string SFX_VOLUME_KEY = "SFX_Volume";
    private const string SHOW_CONTROL_GUIDE_KEY = "Show_Control_Guide";
    
    // デフォルト値
    private const float DEFAULT_BGM_VOLUME = 1f;
    private const float DEFAULT_SFX_VOLUME = 1f;
    private const bool DEFAULT_SHOW_CONTROL_GUIDE = true;
    
    // 現在の設定
    [SerializeField]
    private float bgmVolume = DEFAULT_BGM_VOLUME;
    [SerializeField]
    private float sfxVolume = DEFAULT_SFX_VOLUME;
    [SerializeField]
    private bool showControlGuide = DEFAULT_SHOW_CONTROL_GUIDE;
    
    // オプション変更時のコールバック
    private List<System.Action> onBGMVolumeChangedCallbacks = new List<System.Action>();
    private List<System.Action> onSFXVolumeChangedCallbacks = new List<System.Action>();
    private List<System.Action> onShowControlGuideChangedCallbacks = new List<System.Action>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadOptions();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 保存されたオプション設定を読み込む
    /// </summary>
    public void LoadOptions()
    {
        // BGM: PlayerPrefsに保存済みがあればそれを使い、なければSoundDataの設定（あれば）を優先する
        if (PlayerPrefs.HasKey(BGM_VOLUME_KEY))
        {
            bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY);
        }
        else if (SoundManager.Instance != null && SoundManager.Instance.soundData != null)
        {
            bgmVolume = SoundManager.Instance.soundData.bgmVolume;
        }
        else
        {
            bgmVolume = DEFAULT_BGM_VOLUME;
        }

        // SFX: 同様の優先順
        if (PlayerPrefs.HasKey(SFX_VOLUME_KEY))
        {
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY);
        }
        else if (SoundManager.Instance != null && SoundManager.Instance.soundData != null)
        {
            sfxVolume = SoundManager.Instance.soundData.sfxVolume;
        }
        else
        {
            sfxVolume = DEFAULT_SFX_VOLUME;
        }

        // キー操作ガイドはPlayerPrefsがあればそれを使い、なければデフォルト
        if (PlayerPrefs.HasKey(SHOW_CONTROL_GUIDE_KEY))
        {
            showControlGuide = PlayerPrefs.GetInt(SHOW_CONTROL_GUIDE_KEY) == 1;
        }
        else
        {
            showControlGuide = DEFAULT_SHOW_CONTROL_GUIDE;
        }

        ApplyOptions();
    }
    
    /// <summary>
    /// オプション設定を適用
    /// </summary>
    private void ApplyOptions()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(bgmVolume);
            SoundManager.Instance.SetSFXVolume(sfxVolume);
        }
    }
    
    // BGM音量
    public float GetBGMVolume()
    {
        return bgmVolume;
    }
    
    public void SetBGMVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (Mathf.Approximately(bgmVolume, volume))
        {
            return;
        }
        
        bgmVolume = volume;
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        PlayerPrefs.Save();
        
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(bgmVolume);
        }
        
        NotifyBGMVolumeChanged();
    }
    
    // SE音量
    public float GetSFXVolume()
    {
        return sfxVolume;
    }
    
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (Mathf.Approximately(sfxVolume, volume))
        {
            return;
        }
        
        sfxVolume = volume;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.Save();
        
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(sfxVolume);
        }
        
        NotifySFXVolumeChanged();
    }
    
    // キー操作ガイド表示
    public bool GetShowControlGuide()
    {
        return showControlGuide;
    }
    
    public void SetShowControlGuide(bool show)
    {
        if (showControlGuide == show)
        {
            return;
        }
        
        showControlGuide = show;
        PlayerPrefs.SetInt(SHOW_CONTROL_GUIDE_KEY, showControlGuide ? 1 : 0);
        PlayerPrefs.Save();
        
        NotifyShowControlGuideChanged();
    }
    
    // コールバック登録
    public void OnBGMVolumeChanged(System.Action callback)
    {
        if (!onBGMVolumeChangedCallbacks.Contains(callback))
        {
            onBGMVolumeChangedCallbacks.Add(callback);
        }
    }
    
    public void OnSFXVolumeChanged(System.Action callback)
    {
        if (!onSFXVolumeChangedCallbacks.Contains(callback))
        {
            onSFXVolumeChangedCallbacks.Add(callback);
        }
    }
    
    public void OnShowControlGuideChanged(System.Action callback)
    {
        if (!onShowControlGuideChangedCallbacks.Contains(callback))
        {
            onShowControlGuideChangedCallbacks.Add(callback);
        }
    }
    
    private void NotifyBGMVolumeChanged()
    {
        foreach (var callback in onBGMVolumeChangedCallbacks)
        {
            callback?.Invoke();
        }
    }
    
    private void NotifySFXVolumeChanged()
    {
        foreach (var callback in onSFXVolumeChangedCallbacks)
        {
            callback?.Invoke();
        }
    }
    
    private void NotifyShowControlGuideChanged()
    {
        foreach (var callback in onShowControlGuideChangedCallbacks)
        {
            callback?.Invoke();
        }
    }
    
    /// <summary>
    /// すべてのオプションをデフォルト値にリセット
    /// </summary>
    public void ResetToDefault()
    {
        SetBGMVolume(DEFAULT_BGM_VOLUME);
        SetSFXVolume(DEFAULT_SFX_VOLUME);
        SetShowControlGuide(DEFAULT_SHOW_CONTROL_GUIDE);
    }
}
