using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// シーン開始時にチュートリアル用テキストを順表示する。
/// 左クリックで次の文章へ進み、全文章表示後はテキストボックスを隠して別ウィンドウを表示し、操作を解禁する。
/// </summary>
public class TutorialSequence : MonoBehaviour
{
    public static bool IsActive { get; private set; }

    [Header("UI")]
    [Tooltip("チュートリアル用テキストボックス（親オブジェクト）")]
    [SerializeField] private GameObject textBoxRoot;

    [Tooltip("テキストボックス内の TextMeshPro")]
    [SerializeField] private TextMeshProUGUI tutorialText;

    [Tooltip("全文章表示後に出す別ウィンドウ")]
    [SerializeField] private GameObject postTutorialWindow;

    [Header("文章（Inspector 登録順に表示）")]
    [TextArea(2, 6)]
    [SerializeField] private string[] tutorialLines;

    [Header("操作ブロック対象（未設定時はシーン内を検索）")]
    [SerializeField] private DeliveryStation deliveryStation;
    [SerializeField] private FirstPersonController playerController;

    [Header("postTutorialWindow 表示時")]
    [Tooltip("OffScreenObjectIndicator を持つ Image オブジェクト（未設定時はシーン内検索）")]
    [SerializeField] private GameObject offScreenIndicatorObject;
    [SerializeField] private OffScreenObjectIndicator offScreenIndicator;
    [SerializeField] private Volume globalVolume;

    private int currentLineIndex;
    private bool sequenceFinished;
    private readonly List<Behaviour> disabledBehaviours = new List<Behaviour>();

    void Awake()
    {
        if (deliveryStation == null)
        {
            deliveryStation = FindFirstObjectByType<DeliveryStation>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FirstPersonController>();
        }

        if (offScreenIndicator == null && offScreenIndicatorObject != null)
        {
            offScreenIndicator = offScreenIndicatorObject.GetComponent<OffScreenObjectIndicator>();
        }

        if (offScreenIndicator == null)
        {
            offScreenIndicator = FindOffScreenIndicator();
        }

        if (globalVolume == null)
        {
            globalVolume = FindGlobalVolume();
        }

        if (tutorialText == null && textBoxRoot != null)
        {
            tutorialText = textBoxRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    void Start()
    {
        if (postTutorialWindow != null)
        {
            postTutorialWindow.SetActive(false);
        }

        if (textBoxRoot != null)
        {
            textBoxRoot.SetActive(true);
        }

        BeginTutorial();
    }

    void OnDestroy()
    {
        if (IsActive)
        {
            IsActive = false;
        }
    }

    void Update()
    {
        if (sequenceFinished || !IsActive)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            AdvanceLine();
        }
    }

    void BeginTutorial()
    {
        sequenceFinished = false;
        currentLineIndex = 0;
        IsActive = true;

        BlockGameplayInput();
        ShowCursorForTutorial();

        if (tutorialLines == null || tutorialLines.Length == 0)
        {
            FinishSequence();
            return;
        }

        ApplyLineText();
    }

    void AdvanceLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= tutorialLines.Length)
        {
            FinishSequence();
            return;
        }

        ApplyLineText();
    }

    void ApplyLineText()
    {
        if (tutorialText != null && currentLineIndex < tutorialLines.Length)
        {
            tutorialText.text = tutorialLines[currentLineIndex];
        }
    }

    void FinishSequence()
    {
        if (sequenceFinished)
        {
            return;
        }

        sequenceFinished = true;
        IsActive = false;

        if (textBoxRoot != null)
        {
            textBoxRoot.SetActive(false);
        }

        if (postTutorialWindow != null)
        {
            postTutorialWindow.SetActive(true);
            ApplyPostTutorialWindowEffects();
        }

        RestoreGameplayInput();
        HideCursorForGameplay();
    }

    void ApplyPostTutorialWindowEffects()
    {
        ResolveOffScreenIndicatorReference();

        if (offScreenIndicator != null)
        {
            offScreenIndicator.ShowIndicatorIfHidden();
        }

        DisableGlobalDepthOfField();
    }

    void ResolveOffScreenIndicatorReference()
    {
        if (offScreenIndicator != null)
        {
            return;
        }

        if (offScreenIndicatorObject != null)
        {
            offScreenIndicator = offScreenIndicatorObject.GetComponent<OffScreenObjectIndicator>();
        }

        if (offScreenIndicator == null)
        {
            offScreenIndicator = FindOffScreenIndicator();
        }
    }

    static OffScreenObjectIndicator FindOffScreenIndicator()
    {
        OffScreenObjectIndicator[] indicators = FindObjectsByType<OffScreenObjectIndicator>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        return indicators.Length > 0 ? indicators[0] : null;
    }

    void DisableGlobalDepthOfField()
    {
        Volume volume = globalVolume != null ? globalVolume : FindGlobalVolume();
        if (volume == null)
        {
            return;
        }

        VolumeProfile profile = volume.profile;
        if (profile != null && profile.TryGet(out DepthOfField depthOfField))
        {
            depthOfField.active = false;
        }
    }

    static Volume FindGlobalVolume()
    {
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        for (int i = 0; i < volumes.Length; i++)
        {
            if (volumes[i] != null && volumes[i].isGlobal)
            {
                return volumes[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 左クリック以外のマウス操作とキーボード操作を止める（既存 UI ブロックと同様）。
    /// </summary>
    void BlockGameplayInput()
    {
        disabledBehaviours.Clear();

        GameplayInputUtility.DisableStandardInput(playerController, deliveryStation, disabledBehaviours);
    }

    void RestoreGameplayInput()
    {
        for (int i = 0; i < disabledBehaviours.Count; i++)
        {
            Behaviour behaviour = disabledBehaviours[i];
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        disabledBehaviours.Clear();

        if (deliveryStation != null)
        {
            deliveryStation.CursorActive = false;
        }
    }

    void ShowCursorForTutorial()
    {
        if (deliveryStation != null)
        {
            deliveryStation.CursorActive = true;
        }

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    void HideCursorForGameplay()
    {
        if (deliveryStation != null)
        {
            deliveryStation.CursorActive = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
