using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// オプション機能の実装例
/// ゲーム画面でオプションボタンを押すと設定画面を開く例
/// </summary>
public class OptionsButtonController : MonoBehaviour
{
    [SerializeField]
    private OptionsUIPanel optionsUIPanel;
    
    [SerializeField]
    private Button optionsButton;
    
    [SerializeField]
    private KeyCode optionsMenuKey = KeyCode.O;
    
    void Start()
    {
        if (optionsButton != null)
        {
            optionsButton.onClick.AddListener(OpenOptions);
        }
        
        if (optionsUIPanel == null)
        {
            Debug.LogWarning("OptionsButtonController: OptionsUIPanelが割り当てられていません");
        }
    }
    
    void Update()
    {
        // キーボードショートカット (Oキー)
        if (Input.GetKeyDown(optionsMenuKey))
        {
            OpenOptions();
        }
    }
    
    private void OpenOptions()
    {
        if (optionsUIPanel != null)
        {
            optionsUIPanel.Open();
        }
    }
    
    void OnDestroy()
    {
        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveListener(OpenOptions);
        }
    }
}
