using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// charaImage クリック時に、インスペクタ設定のセリフから
/// 現在表示中以外を抽選して talk に表示する。
/// </summary>
public class CharaTalkRandomizer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image charaImage;
    [SerializeField] private TextMeshProUGUI talk;

    [Header("セリフ（インスペクタで設定）")]
    [SerializeField] private string[] talkLines;

    private Button charaClickButton;

    private void Start()
    {
        RegisterCharaImageClick();

        if (talk != null && talkLines != null && talkLines.Length > 0)
        {
            talk.text = talkLines[0];
        }
    }

    private void OnDestroy()
    {
        if (charaClickButton != null)
        {
            charaClickButton.onClick.RemoveListener(ShowRandomLine);
        }
    }

    private void RegisterCharaImageClick()
    {
        if (charaImage == null) return;

        charaImage.raycastTarget = true;

        charaClickButton = charaImage.GetComponent<Button>();
        if (charaClickButton == null)
        {
            charaClickButton = charaImage.gameObject.AddComponent<Button>();
            charaClickButton.transition = Selectable.Transition.None;
        }

        charaClickButton.onClick.AddListener(ShowRandomLine);
    }

    /// <summary>
    /// 現在表示中の文以外からランダムに1件選び talk に表示する。
    /// </summary>
    public void ShowRandomLine()
    {
        if (talk == null || talkLines == null || talkLines.Length == 0) return;

        if (talkLines.Length == 1)
        {
            talk.text = talkLines[0];
            return;
        }

        string current = talk.text;
        int index = PickRandomIndexExcludingCurrent(current);
        talk.text = talkLines[index];
    }

    private int PickRandomIndexExcludingCurrent(string current)
    {
        int count = talkLines.Length;
        int start = Random.Range(0, count);

        for (int i = 0; i < count; i++)
        {
            int index = (start + i) % count;
            if (talkLines[index] != current)
            {
                return index;
            }
        }

        return start;
    }
}
