using UnityEngine;

/// <summary>
/// ランタンなどのオブジェクトから光を発するコンポーネント
/// Point Lightの制御と、炎の揺らぎ効果を実装
/// 複数マテリアルの場合、特定マテリアルのみEmissionを制御
/// </summary>
public class LanternLightSource : MonoBehaviour
{
    [Header("光源設定")]
    [SerializeField] private Light pointLight;
    [SerializeField] private bool autoGenerateLight = true;
    [SerializeField] private Vector3 lightPositionOffset = Vector3.zero;
    [SerializeField] private float intensity = 1.5f;
    [SerializeField] private float range = 15f;
    [SerializeField] private Color lightColor = new Color(1f, 0.8f, 0.5f);
    
    [Header("発光マテリアル設定")]
    [SerializeField] private int glowMaterialIndex = -1;
    [SerializeField] private Color emissionColor = new Color(1f, 0.8f, 0.3f);
    [SerializeField] private float emissionIntensity = 2f;
    
    [Header("揺らぎ設定")]
    [SerializeField] private bool enableFlicker = true;
    [SerializeField] private float flickerSpeed = 2f;
    [SerializeField] private float flickerAmount = 0.2f;

    private Renderer objectRenderer;
    private Material glowMaterial;
    private Color baseEmissionColor;

    void Awake()
    {
        InitializeGlowMaterial();
        InitializeLight();
    }

    void InitializeLight()
    {
        if (pointLight == null && autoGenerateLight)
        {
            // Light オブジェクトを自動生成
            GameObject lightObj = new GameObject("LanternLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = lightPositionOffset;
            pointLight = lightObj.AddComponent<Light>();
            
            Debug.Log($"{gameObject.name}: Light オブジェクトを自動生成しました");
        }
        else if (pointLight == null && !autoGenerateLight)
        {
            Debug.LogError($"{gameObject.name}: Point Lightがインスペクターで指定されていません");
            return;
        }

        pointLight.type = LightType.Point;
        pointLight.intensity = intensity;
        pointLight.range = range;
        pointLight.color = lightColor;
        pointLight.shadows = LightShadows.Soft;
    }

    void InitializeGlowMaterial()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogWarning($"{gameObject.name}: Rendererコンポーネントが見つかりません");
            return;
        }

        // 発光マテリアルを取得
        if (glowMaterialIndex >= 0 && glowMaterialIndex < objectRenderer.materials.Length)
        {
            glowMaterial = objectRenderer.materials[glowMaterialIndex];
            baseEmissionColor = emissionColor;
            UpdateGlowMaterialEmission(emissionColor, emissionIntensity);
        }
        else if (glowMaterialIndex == -1)
        {
            // インデックス指定がない場合、最初のマテリアルを使用
            glowMaterial = objectRenderer.material;
            baseEmissionColor = emissionColor;
            UpdateGlowMaterialEmission(emissionColor, emissionIntensity);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: 指定されたマテリアルインデックス ({glowMaterialIndex}) が範囲外です");
        }
    }

    void Update()
    {
        if (enableFlicker && pointLight != null)
        {
            UpdateFlicker();
        }
    }

    void UpdateFlicker()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float flickerIntensity = Mathf.Lerp(1f - flickerAmount, 1f + flickerAmount, noise);
        
        // Point Lightの揺らぎ
        pointLight.intensity = intensity * flickerIntensity;
        
        // マテリアルのEmission揺らぎ
        if (glowMaterial != null)
        {
            Color flickeredEmission = baseEmissionColor * emissionIntensity * flickerIntensity;
            UpdateGlowMaterialEmission(flickeredEmission, 1f);
        }
    }

    void UpdateGlowMaterialEmission(Color emission, float intensity)
    {
        if (glowMaterial != null)
        {
            glowMaterial.SetColor("_EmissionColor", emission * intensity);
        }
    }

    /// <summary>
    /// 光源の強度を設定
    /// </summary>
    public void SetIntensity(float newIntensity)
    {
        intensity = newIntensity;
        if (pointLight != null)
        {
            pointLight.intensity = intensity;
        }
    }

    /// <summary>
    /// 光源の範囲を設定
    /// </summary>
    public void SetRange(float newRange)
    {
        range = newRange;
        if (pointLight != null)
        {
            pointLight.range = range;
        }
    }

    /// <summary>
    /// 光の色を設定
    /// </summary>
    public void SetLightColor(Color newColor)
    {
        lightColor = newColor;
        if (pointLight != null)
        {
            pointLight.color = lightColor;
        }
    }

    /// <summary>
    /// マテリアルの発光強度を設定
    /// </summary>
    public void SetEmissionIntensity(float newIntensity)
    {
        emissionIntensity = newIntensity;
        if (glowMaterial != null)
        {
            UpdateGlowMaterialEmission(emissionColor, emissionIntensity);
        }
    }

    /// <summary>
    /// マテリアルの発光色を設定
    /// </summary>
    public void SetEmissionColor(Color newColor)
    {
        emissionColor = newColor;
        baseEmissionColor = newColor;
        if (glowMaterial != null)
        {
            UpdateGlowMaterialEmission(emissionColor, emissionIntensity);
        }
    }

    /// <summary>
    /// 揺らぎの有効/無効を切り替え
    /// </summary>
    public void SetFlickerEnabled(bool enabled)
    {
        enableFlicker = enabled;
    }

    /// <summary>
    /// 現在の光源の強度を取得
    /// </summary>
    public float GetIntensity()
    {
        return pointLight != null ? pointLight.intensity : 0f;
    }

    /// <summary>
    /// 光源を点灯
    /// </summary>
    public void TurnOn()
    {
        if (pointLight != null)
        {
            pointLight.enabled = true;
        }
        if (glowMaterial != null)
        {
            UpdateGlowMaterialEmission(emissionColor, emissionIntensity);
        }
    }

    /// <summary>
    /// 光源を消灯
    /// </summary>
    public void TurnOff()
    {
        if (pointLight != null)
        {
            pointLight.enabled = false;
        }
        if (glowMaterial != null)
        {
            UpdateGlowMaterialEmission(Color.black, 0f);
        }
    }

    /// <summary>
    /// 光源が有効かどうかを判定
    /// </summary>
    public bool IsLightActive()
    {
        return pointLight != null && pointLight.enabled;
    }
}
