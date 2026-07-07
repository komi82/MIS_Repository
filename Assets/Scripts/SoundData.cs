using UnityEngine;

/// <summary>
/// 音声データを管理するScriptableObject
/// インスペクターで音源と音量設定を管理
/// </summary>
[CreateAssetMenu(fileName = "SoundData", menuName = "Audio/Sound Data")]
public class SoundData : ScriptableObject
{
    [Header("効果音")]
    [Tooltip("ボタンクリック音")]
    public AudioClip buttonClickSound;
    
    [Tooltip("アイテム取得音")]
    public AudioClip itemPickupSound;
    
    [Tooltip("レシピ完成音")]
    public AudioClip recipeCompleteSound;
    
    [Tooltip("納品完了音")]
    public AudioClip deliverySound;
    
    [Tooltip("依頼受け取り音")]
    public AudioClip RequestSound;
    
    [Tooltip("設置音")]
    public AudioClip putSound;

    [Tooltip("時間切れ音")]
    public AudioClip timeupSound;

    [Tooltip("スコア算出音")]
    public AudioClip rouletteSound;

    [Tooltip("スコア確定音")]
    public AudioClip displaySound;


    [Header("BGM")]
    [Tooltip("ゲームプレイBGM")]
    public AudioClip gameplayBGM;
    

    
    [Header("音量設定")]
    [Range(0f, 1f)]
    [Tooltip("BGM音量")]
    public float bgmVolume = 1f;
    
    [Range(0f, 1f)]
    [Tooltip("効果音音量")]
    public float sfxVolume = 1f;
    
    [Range(0f, 1f)]
    [Tooltip("UI効果音音量")]
    public float uiVolume = 0.9f;
    
    [Header("音声設定")]
    [Tooltip("BGMフェード時間（秒）")]
    public float bgmFadeTime = 2f;
    
    [Tooltip("最大同時再生SE数")]
    public int maxConcurrentSFX = 5;
    
    /// <summary>
    /// 効果音の音量を取得（UI音量を考慮）
    /// </summary>
    public float GetSFXVolume(bool isUI = false)
    {
        return isUI ? sfxVolume * uiVolume : sfxVolume;
    }
    
    /// <summary>
    /// 指定された効果音が存在するかチェック
    /// </summary>
    public bool HasSFX(AudioClip clip)
    {
        return clip != null;
    }
    
    /// <summary>
    /// 指定されたBGMが存在するかチェック
    /// </summary>
    public bool HasBGM(AudioClip clip)
    {
        return clip != null;
    }
}

