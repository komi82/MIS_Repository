using UnityEngine;

/// <summary>
/// ボタン押下で既存UIを非表示にし、別のUIを表示する。
/// Button の On Click () に SwitchUI を登録して使用する。
/// </summary>
public class UISwitchButton : MonoBehaviour
{
    [Header("UI設定")]
    [Tooltip("非表示にするUI")]
    [SerializeField] private GameObject uiToHide;

    [Tooltip("表示するUI")]
    [SerializeField] private GameObject uiToShow;

    [Header("サウンド")]
    [Tooltip("ボタンクリック音を再生するか")]
    [SerializeField] private bool playButtonSound = true;

    /// <summary>
    /// ボタンの On Click () から呼び出す。
    /// </summary>
    public void SwitchUI()
    {
        if (playButtonSound && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.buttonClickSound);
        }

        if (uiToHide != null)
        {
            uiToHide.SetActive(false);
        }

        if (uiToShow != null)
        {
            uiToShow.SetActive(true);
        }
    }
}
