using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// テスト用のUIを手動で作成するスクリプト
/// </summary>
public class CreateTestUI : MonoBehaviour
{
    [Header("テスト用UI作成")]
    [SerializeField] private bool createTestUI = false;
    
    void Start()
    {
        if (createTestUI)
        {
            CreateCraftTestUI();
            CreateBlacksmithTestUI();
            CreateWashTestUI();
        }
    }
    
    private void CreateCraftTestUI()
    {
        // Canvasを作成
        GameObject canvasObj = new GameObject("CraftTestCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        
        // CanvasScalerを追加
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        // GraphicRaycasterを追加
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // スケールを設定
        canvasObj.transform.localScale = Vector3.one * 0.01f;
        
        // テキストを作成
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "クラフト台\n[E] 配置 [R] 実行";
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        // テキストのRectTransformを設定
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(200, 60);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        
        // 初期位置を画面外に設定
        canvasObj.transform.position = new Vector3(0, -1000, 0);
        
        // 非アクティブにする
        canvasObj.SetActive(false);
        
        Debug.Log("CraftTestUI created: " + canvasObj.name);
    }
    
    private void CreateBlacksmithTestUI()
    {
        // 同様の処理でBlacksmithTestUIを作成
        GameObject canvasObj = new GameObject("BlacksmithTestCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        canvasObj.AddComponent<GraphicRaycaster>();
        
        canvasObj.transform.localScale = Vector3.one * 0.01f;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "鍛冶台\n[E] 配置 [R] 鍛造";
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(200, 60);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        
        canvasObj.transform.position = new Vector3(0, -1000, 0);
        canvasObj.SetActive(false);
        
        Debug.Log("BlacksmithTestUI created: " + canvasObj.name);
    }
    
    private void CreateWashTestUI()
    {
        // 同様の処理でWashTestUIを作成
        GameObject canvasObj = new GameObject("WashTestCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        canvasObj.AddComponent<GraphicRaycaster>();
        
        canvasObj.transform.localScale = Vector3.one * 0.01f;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "浄化台\n[E] 配置 [R] 浄化";
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(200, 60);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        
        canvasObj.transform.position = new Vector3(0, -1000, 0);
        canvasObj.SetActive(false);
        
        Debug.Log("WashTestUI created: " + canvasObj.name);
    }
}

