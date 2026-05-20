using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ボタン押下で指定シーンへフェード付きで遷移する。
/// Inspector のドロップダウンで遷移先を選ぶ（Build Settings 登録シーン）。
/// Button の On Click () に LoadScene を登録して使用する。
/// </summary>
public class LoadTutorialScene : MonoBehaviour
{
    [Header("シーン設定")]
    [Tooltip("遷移先シーン名（Inspector で選択）")]
    [SerializeField] private string sceneName;

    [Header("サウンド")]
    [Tooltip("ボタンクリック音を再生するか")]
    [SerializeField] private bool playButtonSound = true;

    /// <summary>
    /// ボタンの On Click () から呼び出す。
    /// </summary>
    public void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("LoadTutorialScene: 遷移先シーンが設定されていません");
            return;
        }

        if (playButtonSound && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.buttonClickSound);
        }

        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.LoadSceneWithFade(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>旧メソッド名。On Click に残している場合用。</summary>
    public void LoadTutorial() => LoadScene();
}
