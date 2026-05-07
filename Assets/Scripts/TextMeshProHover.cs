using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TextMeshProHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("TextMeshPro設定")]
    public TextMeshProUGUI textMeshPro;
    
    [Header("色設定")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color clickColor = Color.red;
    
    [Header("クリックイベント")]
    public UnityEngine.Events.UnityEvent OnClickEvent;
    
    private Color originalColor;
    private bool isHovering = false;
    
    void Start()
    {
        // TextMeshProの参照を取得
        if (textMeshPro == null)
            textMeshPro = GetComponent<TextMeshProUGUI>();
            
        // 元の色を保存
        if (textMeshPro != null)
        {
            originalColor = textMeshPro.color;
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (textMeshPro != null)
        {
            isHovering = true;
            textMeshPro.color = hoverColor;
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (textMeshPro != null)
        {
            isHovering = false;
            textMeshPro.color = normalColor;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (textMeshPro != null)
        {
            // クリック時の色変更（短時間）
            textMeshPro.color = clickColor;
            Invoke(nameof(ResetToHoverColor), 0.1f);
            
            // クリックイベントを実行
            OnClickEvent?.Invoke();
        }
    }
    
    private void ResetToHoverColor()
    {
        if (textMeshPro != null)
        {
            textMeshPro.color = isHovering ? hoverColor : normalColor;
        }
    }
    
    /// <summary>
    /// 色を手動で設定する
    /// </summary>
    public void SetColors(Color normal, Color hover, Color click)
    {
        normalColor = normal;
        hoverColor = hover;
        clickColor = click;
        
        if (textMeshPro != null)
        {
            textMeshPro.color = isHovering ? hoverColor : normalColor;
        }
    }
    
    /// <summary>
    /// テキストを設定する
    /// </summary>
    public void SetText(string text)
    {
        if (textMeshPro != null)
        {
            textMeshPro.text = text;
        }
    }
    
    /// <summary>
    /// マウスオーバー状態をリセット
    /// </summary>
    public void ResetHoverState()
    {
        isHovering = false;
        if (textMeshPro != null)
        {
            textMeshPro.color = normalColor;
        }
    }
}



