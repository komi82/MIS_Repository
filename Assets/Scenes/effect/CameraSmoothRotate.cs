using UnityEngine;

public class CameraSmoothRotate : MonoBehaviour
{
    public float targetX = 30f;   // 目標角度
    public float accel = 0.02f;   // 加速係数
    public float minSpeed = 0.1f; // 最低速度（停止しないため）

    void Update()
    {
        float currentX = transform.localEulerAngles.x;

        // 差分（-180〜180）
        float diff = Mathf.DeltaAngle(currentX, targetX);

        // 二次関数的に減速（差分が小さいほど速度が小さくなる）
        float speed = accel * diff * diff;

        // 完全停止を防ぐ
        speed = Mathf.Max(speed, minSpeed);

        // 差分の符号で方向を決める
        speed *= Mathf.Sign(diff);

        // 新しい角度
        float newX = currentX + speed * Time.deltaTime;

        Vector3 e = transform.localEulerAngles;
        e.x = newX;
        transform.localEulerAngles = e;
    }
}