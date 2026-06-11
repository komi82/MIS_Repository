using System.Collections;
using UnityEngine;

public class SlideInTest : MonoBehaviour
{
    public float startX = -2000f;
    public float duration = 1.8f;

    private RectTransform rect;
    private Vector2 targetPos;
    private Vector2 startPos;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        targetPos = rect.anchoredPosition;
        startPos = targetPos + new Vector2(startX, 0);

        // 最初から左外に置く
        rect.anchoredPosition = startPos;
    }

    void OnEnable()
    {
        StartCoroutine(SlideIn());
    }

    IEnumerator SlideIn()
    {
        rect.anchoredPosition = startPos;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // ゆっくり自然に出る
            t = 1f - Mathf.Pow(1f - t, 3f);

            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        rect.anchoredPosition = targetPos;
    }
}
