using UnityEngine;

public class YAxisRotator : MonoBehaviour
{
    // 1回転にかかる秒数
    [SerializeField] private float rotationDurationSeconds = 60f;

    // 回転速度（度/秒）
    private float rotationSpeed;

    void Start()
    {
        QualitySettings.vSyncCount = 0; // VSyncを無効化
        Application.targetFrameRate = 60; // フレームレート制限
        // 1回転 = 360度
        rotationSpeed = 360f / rotationDurationSeconds;
    }

    void Update()
    {
        // オブジェクトをY軸に回転させる
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}


