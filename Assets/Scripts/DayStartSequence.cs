using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Shopから戻る間、日付演出とフェードが終わるまでゲームの進行を待つ。</summary>
public sealed class DayStartSequence : MonoBehaviour
{
    static DayStartSequence current;
    readonly List<Behaviour> disabledInput = new List<Behaviour>();
    GameObject presentation;
    DayChangeAnimator animator;
    bool released;
    bool initializing;
    public static bool IsBlocking => current != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => current = null;

    public static DayStartSequence Begin()
    {
        if (current != null) return current;
        current = new GameObject("DayStartSequence").AddComponent<DayStartSequence>();
        DontDestroyOnLoad(current.gameObject);
        SceneManager.sceneLoaded += current.Loaded;
        return current;
    }

    void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneNames.Arcade) { Complete(); return; }
        // sceneLoaded runs after Awake, before the first Start/Update in Arcade.
        GameplayInputUtility.DisableStandardInput(
            FindFirstObjectByType<FirstPersonController>(),
            FindFirstObjectByType<DeliveryStation>(), disabledInput);
        GameplayInputUtility.DisableBehaviour(FindFirstObjectByType<OwnedItemsHUD>(), disabledInput);
        initializing = true;
        StartCoroutine(PlayAfterSceneInitialization(scene));
    }

    IEnumerator PlayAfterSceneInitialization(Scene scene)
    {
        // Let inventory restoration and gameplay UI creation finish first. Their font
        // template searches must not pick this temporary presentation's Canvas.
        yield return null;
        if (released || !scene.IsValid() || !scene.isLoaded) yield break;
        initializing = false;
        var prefab = Resources.Load<GameObject>("DayTransition/DayChange");
        if (prefab == null)
        {
            Debug.LogError("DayStartSequence: DayChange prefab is missing.");
            yield break; // Still finish the fade and release the gate.
        }
        presentation = Instantiate(prefab);
        SceneManager.MoveGameObjectToScene(presentation, scene);
        animator = presentation.GetComponentInChildren<DayChangeAnimator>(true);
        if (animator == null)
        {
            Debug.LogError("DayStartSequence: DayChangeAnimator is missing.");
            RemovePresentation();
            yield break;
        }
        animator.PlayAnimation(DayAdvanceButton.GetDayStatic(), RemovePresentation);
    }

    public IEnumerator WaitForAnimation()
    {
        while (!released && (initializing || animator != null && animator.IsPlaying)) yield return null;
    }

    void RemovePresentation()
    {
        if (presentation != null)
        {
            presentation.SetActive(false);
            Destroy(presentation);
            presentation = null;
        }
        animator = null;
    }

    public void Complete()
    {
        Release();
        Destroy(gameObject);
    }

    void Release()
    {
        if (released) return;
        released = true;
        SceneManager.sceneLoaded -= Loaded;
        RemovePresentation();
        foreach (var input in disabledInput) if (input != null) input.enabled = true;
        disabledInput.Clear();
        if (current == this) current = null;
        InteractionControlTrial.BlockCurrentFrame();
    }

    void OnDisable() => Release();
    void OnDestroy() => Release();
}
