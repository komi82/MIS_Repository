using UnityEngine;
using UnityEngine.UI;

public class SlotSelector : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;

    [Header("スロットの親Image（インベントリ枠画像）")]
    [SerializeField] private Image[] slotFrames = new Image[4];

    [Header("通常状態の枠画像")]
    [SerializeField] private Sprite defaultFrameSprite;

    [Header("選択状態の枠画像（スロットごとに異なる）")]
    [SerializeField] private Sprite[] selectedFrameSprites = new Sprite[4];

    [Header("現在選択中のスロット番号（0〜3）")]
    [SerializeField] public int selectedIndex = 0;

    void Start()
    {
        UpdateSlotVisuals();
    }

    void Update()
    {
        HandleScrollInput();
        HandleKeyInput();
    }

    void HandleScrollInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll < 0f)
        {
            selectedIndex = (selectedIndex + 1) % slotFrames.Length;
            UpdateSlotVisuals();
        }
        else if (scroll > 0f)
        {
            selectedIndex = (selectedIndex - 1 + slotFrames.Length) % slotFrames.Length;
            UpdateSlotVisuals();
        }
    }

    void HandleKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
    }

    public void SelectSlot(int index)
    {
        if (index >= 0 && index < slotFrames.Length)
        {
            selectedIndex = index;
            inventoryManager.SelectItem(index);
            UpdateSlotVisuals();
        }
    }

    void UpdateSlotVisuals()
    {
        // 全てのスロットをデフォルトに戻す
        for (int i = 0; i < slotFrames.Length; i++)
        {
            if (slotFrames[i] != null && defaultFrameSprite != null)
            {
                slotFrames[i].sprite = defaultFrameSprite;
                slotFrames[i].SetAllDirty();
            }
        }
        
        // 選択中のスロットのみ選択用スプライトを適用
        if (selectedIndex >= 0 && selectedIndex < slotFrames.Length && 
            selectedIndex < selectedFrameSprites.Length && selectedFrameSprites[selectedIndex] != null)
        {
            if (slotFrames[selectedIndex] != null)
            {
                slotFrames[selectedIndex].sprite = selectedFrameSprites[selectedIndex];
                slotFrames[selectedIndex].SetAllDirty();
                Debug.Log($"スロット {selectedIndex}: 選択中 → 適用画像 = {selectedFrameSprites[selectedIndex].name}");
            }
        }
        
        // InventoryManagerの選択状態を更新
        inventoryManager.SelectItem(selectedIndex);
    }

    /// <summary>
    /// 現在選択中のスロット番号を取得（0〜3）
    /// </summary>
    public int GetSelectedIndex() => selectedIndex;

    /// <summary>
    /// 現在選択中のスロットの Image を取得
    /// </summary>
    public Image GetSelectedSlotFrame() => slotFrames[selectedIndex];
}