using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 音声管理システム
/// ScriptableObjectを使用して音源と設定を管理
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    [Header("音声データ")]
    [Tooltip("音声設定データ（ScriptableObject）")]
    public SoundData soundData;
    
    [Header("デバッグ")]
    [Tooltip("デバッグログを表示するか")]
    public bool enableDebugLog = false;
    
    [Header("自動再生")]
    [Tooltip("シーン起動時にGamePlayBGMを自動再生するか")]
    public bool autoPlayGameplayBGM = true;
    
    // AudioSource管理
    private AudioSource bgmSource;
    private List<AudioSource> sfxSources;
    private int currentSFXIndex = 0;
    
    // フェード管理
    private Coroutine bgmFadeCoroutine;
    
    void Awake()
    {
        // シングルトンパターン
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            // 自動再生設定が有効なら、GameplayBGMを再生
            if (autoPlayGameplayBGM && soundData != null && soundData.gameplayBGM != null)
            {
                PlayBGM(soundData.gameplayBGM, true, true);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// AudioSourceを初期化
    /// </summary>
    void InitializeAudioSources()
    {
        if (soundData == null)
        {
            Debug.LogError("SoundManager: SoundDataが設定されていません！");
            return;
        }
        
        // BGM用AudioSource
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = soundData.bgmVolume;
        bgmSource.playOnAwake = false;
        
        // SE用AudioSource（複数）
        sfxSources = new List<AudioSource>();
        for (int i = 0; i < soundData.maxConcurrentSFX; i++)
        {
            AudioSource sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.volume = soundData.sfxVolume;
            sfxSource.playOnAwake = false;
            sfxSources.Add(sfxSource);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"SoundManager: 初期化完了 - BGM用AudioSource: 1個, SE用AudioSource: {sfxSources.Count}個");
        }
    }
    
    /// <summary>
    /// 効果音を再生
    /// </summary>
    /// <param name="clip">再生する音源</param>
    /// <param name="volume">音量（0-1）</param>
    /// <param name="isUI">UI効果音かどうか</param>
    public void PlaySFX(AudioClip clip, float volume = 1f, bool isUI = false)
    {
        if (clip == null)
        {
            if (enableDebugLog) Debug.LogWarning("SoundManager: 再生するAudioClipがnullです");
            return;
        }
        
        if (sfxSources == null || sfxSources.Count == 0)
        {
            Debug.LogError("SoundManager: SFX用AudioSourceが初期化されていません");
            return;
        }
        
        // 音量計算
        float finalVolume = volume * soundData.GetSFXVolume(isUI);
        
        // ラウンドロビン方式でAudioSourceを選択
        AudioSource currentSource = sfxSources[currentSFXIndex];
        currentSource.PlayOneShot(clip, finalVolume);
        
        // 次のAudioSourceに切り替え
        currentSFXIndex = (currentSFXIndex + 1) % sfxSources.Count;
        
        if (enableDebugLog)
        {
            Debug.Log($"SoundManager: SE再生 - {clip.name}, 音量: {finalVolume:F2}");
        }
    }
    
    /// <summary>
    /// BGMを再生（フェードイン付き）
    /// </summary>
    /// <param name="clip">再生するBGM</param>
    /// <param name="loop">ループするかどうか</param>
    /// <param name="fadeIn">フェードインするかどうか</param>
    public void PlayBGM(AudioClip clip, bool loop = true, bool fadeIn = false)
    {
        if (clip == null)
        {
            if (enableDebugLog) Debug.LogWarning("SoundManager: 再生するBGMがnullです");
            return;
        }
        
        if (bgmSource == null)
        {
            Debug.LogError("SoundManager: BGM用AudioSourceが初期化されていません");
            return;
        }
        
        // 既存のフェードを停止
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }
        
        if (fadeIn)
        {
            bgmFadeCoroutine = StartCoroutine(FadeToNewBGM(clip, loop));
        }
        else
        {
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = soundData.bgmVolume;
            bgmSource.Play();
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"SoundManager: BGM再生 - {clip.name}, ループ: {loop}");
        }
    }
    
    /// <summary>
    /// BGMを停止（フェードアウト付き）
    /// </summary>
    /// <param name="fadeOut">フェードアウトするかどうか</param>
    public void StopBGM(bool fadeOut = true)
    {
        if (bgmSource == null) return;
        
        if (fadeOut)
        {
            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
            }
            bgmFadeCoroutine = StartCoroutine(FadeOutBGM());
        }
        else
        {
            bgmSource.Stop();
        }
        
        if (enableDebugLog)
        {
            Debug.Log("SoundManager: BGM停止");
        }
    }
    
    /// <summary>
    /// 新しいBGMにフェード
    /// </summary>
    IEnumerator FadeToNewBGM(AudioClip newClip, bool loop)
    {
        // 現在のBGMをフェードアウト
        float startVolume = bgmSource.volume;
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVolume * Time.deltaTime / soundData.bgmFadeTime;
            yield return null;
        }
        
        // 新しいBGMに切り替え
        bgmSource.clip = newClip;
        bgmSource.loop = loop;
        bgmSource.Play();
        
        // フェードイン
        while (bgmSource.volume < startVolume)
        {
            bgmSource.volume += startVolume * Time.deltaTime / soundData.bgmFadeTime;
            yield return null;
        }
        bgmSource.volume = startVolume;
    }
    
    /// <summary>
    /// BGMをフェードアウト
    /// </summary>
    IEnumerator FadeOutBGM()
    {
        float startVolume = bgmSource.volume;
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVolume * Time.deltaTime / soundData.bgmFadeTime;
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.volume = startVolume;
    }
    
    /// <summary>
    /// BGM音量を設定
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(volume);
            soundData.bgmVolume = bgmSource.volume;
        }
    }
    
    /// <summary>
    /// 効果音音量を設定
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        if (sfxSources != null)
        {
            float clampedVolume = Mathf.Clamp01(volume);
            foreach (AudioSource source in sfxSources)
            {
                source.volume = clampedVolume;
            }
            soundData.sfxVolume = clampedVolume;
        }
    }
    
    /// <summary>
    /// 現在再生中のBGMを取得
    /// </summary>
    public AudioClip GetCurrentBGM()
    {
        return bgmSource != null ? bgmSource.clip : null;
    }
    
    /// <summary>
    /// BGMが再生中かどうか
    /// </summary>
    public bool IsBGMPlaying()
    {
        return bgmSource != null && bgmSource.isPlaying;
    }
    
    /// <summary>
    /// 全ての音声を停止
    /// </summary>
    public void StopAllSounds()
    {
        // BGM停止
        StopBGM(false);
        
        // 全てのSE停止
        if (sfxSources != null)
        {
            foreach (AudioSource source in sfxSources)
            {
                source.Stop();
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log("SoundManager: 全ての音声を停止");
        }
    }
    
    /// <summary>
    /// デバッグ情報を表示
    /// </summary>
    [ContextMenu("デバッグ情報を表示")]
    public void ShowDebugInfo()
    {
        Debug.Log($"SoundManager デバッグ情報:");
        Debug.Log($"- BGM再生中: {IsBGMPlaying()}");
        Debug.Log($"- 現在のBGM: {(GetCurrentBGM() != null ? GetCurrentBGM().name : "なし")}");
        Debug.Log($"- BGM音量: {soundData.bgmVolume:F2}");
        Debug.Log($"- SE音量: {soundData.sfxVolume:F2}");
        Debug.Log($"- 最大同時SE数: {soundData.maxConcurrentSFX}");
    }
}

