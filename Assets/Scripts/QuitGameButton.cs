using UnityEngine;

/// <summary>
/// ボタン押下でゲームを終了する。
/// Button の On Click () に QuitGame を登録して使用する。
/// </summary>
public class QuitGameButton : MonoBehaviour
{
    [Header("フェード")]
    [Tooltip("終了前に FadeManager で暗転するか")]
    [SerializeField] private bool useFadeOnQuit = true;

    [Header("サウンド")]
    [Tooltip("ボタンクリック音を再生するか")]
    [SerializeField] private bool playButtonSound = true;

    /// <summary>
    /// ボタンの On Click () から呼び出す。
    /// </summary>
    public void QuitGame()
    {
        if (playButtonSound && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.buttonClickSound);
        }

        if (useFadeOnQuit && FadeManager.Instance != null)
        {
            FadeManager.Instance.QuitWithFade();
            return;
        }

        QuitImmediately();
    }

    static void QuitImmediately()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
