using UnityEngine;

public class DestinationMarkerController : MonoBehaviour
{
    [Header("Scale Settings")]
    public float baseScale = 1f;        // 基本サイズ
    public float scaleFactor = 0.05f;   // 距離に応じた拡大率

    [Header("Billboard Settings")]
    public bool yAxisOnly = true;       // Y軸だけ回転するか

    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // --- ① Y軸固定ビルボード ---
        Vector3 targetPos = cam.position;

        if (yAxisOnly)
        {
            targetPos.y = transform.position.y; // 上下回転を防ぐ
        }

        transform.LookAt(targetPos);

        // --- ② Constant Size（距離補正） ---
        float distance = Vector3.Distance(cam.position, transform.position);
        float scale = baseScale + distance * scaleFactor;

        transform.localScale = Vector3.one * scale;
    }
}
