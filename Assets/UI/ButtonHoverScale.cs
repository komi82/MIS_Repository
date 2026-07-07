using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;

    [Tooltip("Hover時に拡大する倍率")] public float scaleSize = 1.1f;

    void Awake()
    {
        // Awakeで元スケールを確実にキャプチャ（非アクティブ時も安全）
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        // 有効化されたときは元スケールに戻す（他の処理でスケールが変わっていた場合の保険）
        transform.localScale = originalScale;
    }

    void OnDisable()
    {
        // UIが非アクティブ化されたときにスケールをリセット
        transform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * scaleSize;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }
}