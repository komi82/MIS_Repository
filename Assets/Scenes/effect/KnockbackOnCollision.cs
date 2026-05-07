using UnityEngine;

public class SmoothKnockback : MonoBehaviour
{
    [Header("ノックバック設定")]
    public float knockbackForce = 10f;
    public float knockbackSmooth = 8f; // カクつき防止の滑らかさ

    private Rigidbody rb;
    private bool isColliding = false;
    private Vector3 targetVelocity = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionStay(Collision collision)
    {
        // 接触点の法線方向（押し返す方向）
        Vector3 dir = collision.contacts[0].normal;

        // 目標速度を設定（方向 × 強さ）
        targetVelocity = dir * knockbackForce;

        isColliding = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isColliding = false;
        targetVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        // 現在速度 → 目標速度 を滑らかに補間
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, knockbackSmooth * Time.fixedDeltaTime);
    }
}