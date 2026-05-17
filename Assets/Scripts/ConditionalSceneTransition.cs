using System.Collections;
using UnityEngine;

/// <summary>
/// 条件達成時に専用UIを表示し、一定時間後に LoadTutorialScene でシーン遷移する。
/// Inspector でトリガー種別（接触 / ボタン / 変数変化）を選択する。
/// </summary>
public class ConditionalSceneTransition : MonoBehaviour
{
    public enum TriggerMode
    {
        PlayerTouch,
        ButtonPress,
        VariableChange
    }

    public enum VariableComparison
    {
        Equal,
        NotEqual,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual
    }

    [Header("トリガー種別")]
    [SerializeField] private TriggerMode triggerMode = TriggerMode.PlayerTouch;

    [Header("表示・遷移")]
    [SerializeField] private GameObject transitionUI;
    [SerializeField] private LoadTutorialScene sceneLoader;
    [SerializeField] private float delayBeforeLoad = 2f;
    [SerializeField] private bool triggerOnce = true;

    [Header("プレイヤー接触")]
    [Tooltip("このオブジェクトに Is Trigger の Collider を付け、プレイヤーが入ったとき発火")]
    [SerializeField] private string playerTag = "Player";

    [Header("ボタン入力")]
    [SerializeField] private KeyCode pressKey = KeyCode.E;
    [Tooltip("true: Update でキー監視 / false: UI Button から OnButtonPressed を呼ぶ")]
    [SerializeField] private bool pollKeyInUpdate = true;

    [Header("変数変化")]
    [SerializeField] private VariableComparison comparison = VariableComparison.GreaterOrEqual;
    [SerializeField] private int targetValue = 1;
    [SerializeField] private int currentValue;
    [Tooltip("true: Update で currentValue の変化を監視")]
    [SerializeField] private bool pollVariableInUpdate = true;

    private bool hasTriggered;
    private int lastPolledValue;
    private Coroutine transitionCoroutine;

    void Awake()
    {
        if (transitionUI != null)
        {
            transitionUI.SetActive(false);
        }

        if (sceneLoader == null)
        {
            sceneLoader = FindFirstObjectByType<LoadTutorialScene>();
        }

        lastPolledValue = currentValue;
    }

    void Update()
    {
        if (hasTriggered && triggerOnce) return;

        if (triggerMode == TriggerMode.ButtonPress && pollKeyInUpdate && Input.GetKeyDown(pressKey))
        {
            StartTransitionSequence();
            return;
        }

        if (triggerMode == TriggerMode.VariableChange && pollVariableInUpdate && currentValue != lastPolledValue)
        {
            int previous = lastPolledValue;
            lastPolledValue = currentValue;
            if (EvaluateVariable(currentValue, previous))
            {
                StartTransitionSequence();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggerMode != TriggerMode.PlayerTouch) return;
        if (hasTriggered && triggerOnce) return;

        if (other.CompareTag(playerTag))
        {
            OnPlayerTouched();
        }
    }

    /// <summary>
    /// プレイヤーが特定オブジェクトに触れたとき（TriggerMode.PlayerTouch）。
    /// 他スクリプトから呼ぶ場合も可。
    /// </summary>
    public void OnPlayerTouched()
    {
        if (triggerMode != TriggerMode.PlayerTouch) return;
        StartTransitionSequence();
    }

    /// <summary>
    /// ボタンを押したとき（TriggerMode.ButtonPress）。
    /// UI Button の On Click () に登録する。
    /// </summary>
    public void OnButtonPressed()
    {
        if (triggerMode != TriggerMode.ButtonPress) return;
        StartTransitionSequence();
    }

    /// <summary>
    /// 監視変数が変化したとき（TriggerMode.VariableChange）。
    /// 他スクリプトから新しい値を渡して呼ぶ。
    /// </summary>
    public void NotifyVariableChanged(int newValue)
    {
        if (triggerMode != TriggerMode.VariableChange) return;

        int previous = currentValue;
        currentValue = newValue;
        lastPolledValue = newValue;

        if (EvaluateVariable(newValue, previous))
        {
            StartTransitionSequence();
        }
    }

    /// <summary>
    /// NotifyVariableChanged の別名。
    /// </summary>
    public void SetVariableValue(int value)
    {
        NotifyVariableChanged(value);
    }

    void StartTransitionSequence()
    {
        if (hasTriggered && triggerOnce) return;
        if (transitionCoroutine != null) return;

        transitionCoroutine = StartCoroutine(TransitionSequence());
    }

    IEnumerator TransitionSequence()
    {
        hasTriggered = true;

        if (transitionUI != null)
        {
            transitionUI.SetActive(true);
        }

        yield return new WaitForSeconds(delayBeforeLoad);

        if (sceneLoader != null)
        {
            sceneLoader.LoadScene();
        }
        else
        {
            Debug.LogWarning("ConditionalSceneTransition: LoadTutorialScene が未設定です");
        }

        transitionCoroutine = null;
    }

    bool EvaluateVariable(int value, int previousValue)
    {
        if (value == previousValue) return false;

        switch (comparison)
        {
            case VariableComparison.Equal:
                return value == targetValue;
            case VariableComparison.NotEqual:
                return value != targetValue;
            case VariableComparison.Greater:
                return value > targetValue;
            case VariableComparison.Less:
                return value < targetValue;
            case VariableComparison.GreaterOrEqual:
                return value >= targetValue;
            case VariableComparison.LessOrEqual:
                return value <= targetValue;
            default:
                return false;
        }
    }
}
