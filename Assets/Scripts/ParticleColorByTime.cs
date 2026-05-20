using UnityEngine;

public class ParticleStartColorLerp : MonoBehaviour
{
    public ParticleSystem ps;

    public Color startColor = Color.white;   // 開始時の色
    public Color endColor = Color.red;       // 2秒後の色

    public float delay = 1f;                 // 色変化開始までの遅延時間
    public float duration = 2f;              // 色が変わるまでの時間

    private float startTime;

    void Start()
    {
        startTime = Time.time;               // ゲーム開始時刻を記録
    }

    void Update()
    {
        float elapsed = Time.time - startTime;

        // 遅延中は開始色を維持
        if (elapsed < delay)
        {
            var main = ps.main;
            main.startColor = startColor;
            return;
        }

        // 遅延後の経過時間
        float t = Mathf.Clamp01((elapsed - delay) / duration);

        // 色を補間
        Color current = Color.Lerp(startColor, endColor, t);
        current.a = 1f; // 透明化防止

        var mainModule = ps.main;
        mainModule.startColor = current;
    }
}