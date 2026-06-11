
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 追跡対象がカメラに映っていないとき、対象の方向に応じて画面端へ UI を表示する。
/// 映っている間は UI を非表示にする。
/// インジケーター UI（Screen Space Overlay の Canvas 配下）にアタッチする。
/// 追跡対象はリスト先頭から順に追跡し、それ以外の登録オブジェクトは非表示にする。

/// </summary>
[RequireComponent(typeof(RectTransform))]
public class OffScreenObjectIndicator : MonoBehaviour
{
    [Header("追跡対象")]
    [SerializeField] private List<Transform> targets = new List<Transform>();


    [Header("カメラ・Canvas")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RectTransform canvasRect;

    [Header("表示設定")]
    [Tooltip("画面端からの余白（ビューポート 0〜1）")]
    [Range(0f, 0.45f)]
    [SerializeField] private float edgePadding = 0.05f;

    [Tooltip("インジケーターを対象方向へ回転するか")]
    [SerializeField] private bool rotateIndicator = false;

    [Tooltip("回転オフセット（矢印スプライトの向き調整用）")]
    [SerializeField] private float rotationOffset = -90f;

    private RectTransform indicatorRect;
    private CanvasGroup canvasGroup;
    private bool isIndicatorVisible;
    private Transform currentTarget;
    private int currentTargetIndex = -1;


    void Awake()
    {
        indicatorRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (canvasRect == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvasRect = canvas.GetComponent<RectTransform>();
            }
        }

        InitializeTargets();
        SetIndicatorVisible(false);
    }

    void OnDestroy()
    {
        RestoreAllTargetsVisibility();
    }

    void LateUpdate()
    {
        if (currentTarget == null && !TryAdvanceToNextTarget())

        {
            SetIndicatorVisible(false);
            return;
        }

        if (targetCamera == null || canvasRect == null)
        {
            SetIndicatorVisible(false);
            return;
        }

        Vector3 viewportPos = targetCamera.WorldToViewportPoint(currentTarget.position);

        if (IsTargetInView(viewportPos))
        {
            SetIndicatorVisible(false);
            return;
        }

        Vector2 edgeViewport = GetEdgeViewportPosition(viewportPos);
        if (!TrySetIndicatorPosition(edgeViewport))
        {
            SetIndicatorVisible(false);
            return;
        }

        SetIndicatorVisible(true);

        if (rotateIndicator)
        {
            Vector2 direction = edgeViewport - new Vector2(0.5f, 0.5f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            indicatorRect.localRotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }
    }

    void InitializeTargets()
    {
        currentTargetIndex = FindFirstValidTargetIndex(0);
        currentTarget = currentTargetIndex >= 0 ? targets[currentTargetIndex] : null;
        ApplyTargetVisibility();
    }

    bool TryAdvanceToNextTarget()
    {
        int nextIndex = FindFirstValidTargetIndex(currentTargetIndex + 1);
        if (nextIndex < 0)
        {
            currentTarget = null;
            currentTargetIndex = -1;
            return false;
        }

        currentTargetIndex = nextIndex;
        currentTarget = targets[currentTargetIndex];
        ApplyTargetVisibility();
        return true;
    }

    int FindFirstValidTargetIndex(int startIndex)
    {
        for (int i = startIndex; i < targets.Count; i++)
        {
            if (targets[i] != null)
            {
                return i;
            }
        }

        return -1;
    }

    void ApplyTargetVisibility()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            Transform t = targets[i];
            if (t == null)
            {
                continue;
            }

            bool shouldShow = i == currentTargetIndex;
            if (t.gameObject.activeSelf != shouldShow)
            {
                t.gameObject.SetActive(shouldShow);
            }
        }
    }

    void RestoreAllTargetsVisibility()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            Transform t = targets[i];
            if (t != null)
            {
                t.gameObject.SetActive(true);
            }
        }
    }


    bool IsTargetInView(Vector3 viewportPos)
    {
        if (viewportPos.z <= 0f)
        {
            return false;
        }

        float min = edgePadding;
        float max = 1f - edgePadding;
        return viewportPos.x > min && viewportPos.x < max
            && viewportPos.y > min && viewportPos.y < max;
    }

    Vector2 GetEdgeViewportPosition(Vector3 viewportPos)
    {
        if (viewportPos.z < 0f)
        {
            viewportPos.x = 1f - viewportPos.x;
            viewportPos.y = 1f - viewportPos.y;
        }

        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 point = new Vector2(viewportPos.x, viewportPos.y);
        Vector2 direction = point - center;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.up;
        }
        else
        {
            direction.Normalize();
        }

        float min = edgePadding;
        float max = 1f - edgePadding;
        float distance = float.MaxValue;

        if (Mathf.Abs(direction.x) > 0.0001f)
        {
            float boundX = direction.x > 0f ? max : min;
            float t = (boundX - center.x) / direction.x;
            if (t > 0f) distance = Mathf.Min(distance, t);
        }

        if (Mathf.Abs(direction.y) > 0.0001f)
        {
            float boundY = direction.y > 0f ? max : min;
            float t = (boundY - center.y) / direction.y;
            if (t > 0f) distance = Mathf.Min(distance, t);
        }

        return center + direction * distance;
    }

    bool TrySetIndicatorPosition(Vector2 viewportPos)
    {
        Vector2 screenPoint = new Vector2(
            viewportPos.x * Screen.width,
            viewportPos.y * Screen.height);

        Canvas canvas = canvasRect.GetComponent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        indicatorRect.anchoredPosition = localPoint;
        return true;
    }

    void SetIndicatorVisible(bool visible)
    {
        if (isIndicatorVisible == visible) return;
        isIndicatorVisible = visible;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    public void SetTarget(Transform newTarget)
    {
        targets.Clear();
        if (newTarget != null)
        {
            targets.Add(newTarget);
        }

        InitializeTargets();

    }
}
