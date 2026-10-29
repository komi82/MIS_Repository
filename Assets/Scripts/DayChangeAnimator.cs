using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>提供された日付演出。元の位置・時間・イージングを実時間で再生する。</summary>
public sealed class DayChangeAnimator : MonoBehaviour
{
    public Image backgroundFilter;
    public RectTransform topRibbon, centerSymbol, character;
    public TextMeshProUGUI dayText;
    [Range(0f, 1f)] public float filterMaxAlpha = .4f;
    public float inDuration = 1.6f, rotationDelay = .8f, rotateDuration = 1.2f,
        postRotateWait = .1f, outDuration = 1.2f;
    Vector2 ribbonPosition, symbolPosition, characterPosition, textPosition;
    Quaternion symbolRotation;
    bool initialized;
    Coroutine playback;
    public bool IsPlaying { get; private set; }
    public float Duration => Mathf.Max(inDuration, rotationDelay + rotateDuration + postRotateWait) + Mathf.Max(0f, outDuration);

    void Awake() => Initialize();
    void Initialize()
    {
        if (initialized) return;
        initialized = true;
        ribbonPosition = topRibbon.anchoredPosition;
        symbolPosition = centerSymbol.anchoredPosition;
        characterPosition = character.anchoredPosition;
        textPosition = dayText.rectTransform.anchoredPosition;
        symbolRotation = centerSymbol.localRotation;
        Sample(0f);
    }

    public void PlayAnimation(int dayCount, Action onComplete = null)
    {
        Initialize();
        if (playback != null) StopCoroutine(playback);
        gameObject.SetActive(true);
        dayText.text = $"{Mathf.Max(1, dayCount)}日目";
        IsPlaying = true;
        playback = StartCoroutine(Play(onComplete));
    }

    IEnumerator Play(Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < Duration)
        {
            Sample(elapsed);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        Sample(Duration);
        IsPlaying = false;
        playback = null;
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    void Sample(float elapsed)
    {
        float enter = Progress(elapsed, inDuration);
        float outStart = Mathf.Max(inDuration, rotationDelay + rotateDuration + postRotateWait);
        float leave = Progress(elapsed - outStart, outDuration);
        float inEase = 1f - Mathf.Pow(1f - enter, 4f), outEase = Mathf.Pow(leave, 4f);
        topRibbon.anchoredPosition = ribbonPosition + Vector2.right * (-2000f * (1f - inEase) + 2000f * outEase);
        centerSymbol.anchoredPosition = symbolPosition + Vector2.down * (1500f * (1f - inEase) + 1500f * outEase);
        character.anchoredPosition = characterPosition + Vector2.right * (1000f * (1f - inEase) + 1000f * outEase);
        dayText.rectTransform.anchoredPosition = textPosition + Vector2.left * (100f * (1f - inEase));
        // DOTween's default fade ease is OutQuad.
        dayText.alpha = (1f - (1f - enter) * (1f - enter)) * (1f - leave) * (1f - leave);
        if (backgroundFilter != null)
            backgroundFilter.color = new Color(1f, 1f, 1f, filterMaxAlpha * inEase * (1f - outEase));
        float turn = Progress(elapsed - rotationDelay, rotateDuration);
        float turnEase = turn < .5f ? 4f * turn * turn * turn : 1f - Mathf.Pow(-2f * turn + 2f, 3f) / 2f;
        centerSymbol.localRotation = symbolRotation * Quaternion.Euler(0f, 0f, 180f * (1f - turnEase));
    }

    static float Progress(float time, float duration) => time < 0f ? 0f : duration <= 0f ? 1f : Mathf.Clamp01(time / duration);
    void OnDisable()
    {
        if (playback != null) StopCoroutine(playback);
        playback = null;
        IsPlaying = false;
    }
}
