using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// arcade シーンで Tab 長押し中に、所持バフアイテム・アーティファクトのアイコンと個数を表示する。
/// </summary>
public class OwnedItemsHUD : MonoBehaviour
{
    [Header("データベース")]
    [SerializeField] private BaffItemDatabase baffItemDatabase;
    [SerializeField] private ArtifactDatabase artifactDatabase;

    [Header("表示パネルとスロットの親")]
    [Tooltip("BringBaff配下の表示パネルを指定してください（このスクリプトを付けたオブジェクト自身は指定しないでください）")]
    [SerializeField] private GameObject window;
    [SerializeField] private RectTransform baffItemSlotsParent;
    [SerializeField] private RectTransform artifactSlotsParent;
    [SerializeField] private TextMeshProUGUI benefitsText;

    [Header("入力")]
    [SerializeField] private KeyCode showKey = KeyCode.Tab;

    [Header("見た目")]
    [SerializeField] private Vector2 countTextOffset = new Vector2(-8f, 8f);
    [SerializeField] private int countFontSize = 24;
    [SerializeField] private Vector2 slotSize = new Vector2(64f, 64f);
    [SerializeField] private Vector2 slotSpacing = new Vector2(8f, 8f);
    [SerializeField] private int slotsPerRow = 6;

    private readonly List<SlotWidget> baffWidgets = new List<SlotWidget>();
    private readonly List<SlotWidget> artifactWidgets = new List<SlotWidget>();
    private TextMeshProUGUI pageLabel;
    private bool wasVisible;
    private float nextRefreshTime;
    private int currentPage = 1;
    private int pageCount = 1;
    private Vector2 lastTextSize;
    private bool textLayoutDirty = true;

    private class SlotWidget
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI countText;
    }

    void Start()
    {
        OwnedProgressManager.LogOwnedInventory(baffItemDatabase, artifactDatabase);

        if (baffItemDatabase == null || artifactDatabase == null)
            Debug.LogError("OwnedItemsHUD: BaffItemDatabase と ArtifactDatabase を設定してください。", this);
        if (benefitsText == null)
            Debug.LogError("OwnedItemsHUD: 効果一覧を表示する TextMeshProUGUI を設定してください。", this);
        if (baffItemSlotsParent == null || artifactSlotsParent == null)
            Debug.LogError("OwnedItemsHUD: BaffItemとArtifactのスロット親RectTransformを設定してください。", this);
        if (window == null)
            Debug.LogError("OwnedItemsHUD: BringBaff配下の表示パネルをwindowに設定してください。", this);
        if (window == gameObject)
            Debug.LogError("OwnedItemsHUD: BringBaff自身ではなく、表示パネルの子オブジェクトをwindowに設定してください。", this);

        ConfigureReadableLayout();
        SetDisplayVisible(false);
        SetWindowVisible(false);
    }

    void Update()
    {
        if (!Input.GetKey(showKey))
        {
            SetDisplayVisible(false);
            SetWindowVisible(false);
            wasVisible = false;
            return;
        }

        SetWindowVisible(true);
        if (!wasVisible)
        {
            currentPage = 1;
            textLayoutDirty = true;
            Canvas.ForceUpdateCanvases();
        }
        if (!wasVisible || Time.unscaledTime >= nextRefreshTime)
        {
            RefreshDisplay();
            nextRefreshTime = Time.unscaledTime + 0.25f;
        }
        SetDisplayVisible(true);
        UpdatePage();
        wasVisible = true;
    }

    private void ConfigureReadableLayout()
    {
        if (window == null || window == gameObject || benefitsText == null) return;
        RectTransform panel = window.GetComponent<RectTransform>();
        if (panel == null) return;

        // Use the canvas bounds instead of the old fixed-size, transparent overlay.
        RectTransform root = transform as RectTransform;
        if (root != null) SetBounds(root, Vector2.zero, Vector2.one);
        SetBounds(panel, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
        Image background = window.GetComponent<Image>();
        if (background == null) background = window.AddComponent<Image>();
        background.sprite = null;
        background.color = new Color(0.035f, 0.045f, 0.06f, 1f);
        background.raycastTarget = false;

        Canvas parentCanvas = transform.GetComponentInParent<Canvas>();
        Canvas overlay = window.GetComponent<Canvas>();
        if (overlay == null) overlay = window.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        if (parentCanvas != null) overlay.sortingLayerID = parentCanvas.sortingLayerID;
        overlay.sortingOrder = (parentCanvas != null ? parentCanvas.sortingOrder : 0) + 50;
        if (window.GetComponent<RectMask2D>() == null) window.AddComponent<RectMask2D>();

        if (baffItemSlotsParent != null)
            SetBounds(baffItemSlotsParent, new Vector2(0.035f, 0.46f), new Vector2(0.28f, 0.81f));
        if (artifactSlotsParent != null)
            SetBounds(artifactSlotsParent, new Vector2(0.035f, 0.15f), new Vector2(0.28f, 0.37f));
        SetBounds(benefitsText.rectTransform, new Vector2(0.32f, 0.14f), new Vector2(0.96f, 0.85f));
        benefitsText.fontSize = 34f;
        benefitsText.enableAutoSizing = false;
        benefitsText.alignment = TextAlignmentOptions.TopLeft;
        benefitsText.textWrappingMode = TextWrappingModes.Normal;
        benefitsText.overflowMode = TextOverflowModes.Page;
        benefitsText.margin = Vector4.zero;
        benefitsText.raycastTarget = false;
        benefitsText.color = Color.white;

        CreateLabel("OwnedItemsTitle", "所持効果", panel,
            new Vector2(0.035f, 0.89f), new Vector2(0.96f, 0.97f), 44f);
        CreateLabel("BaffItemsTitle", "所持アイテム", panel,
            new Vector2(0.035f, 0.82f), new Vector2(0.28f, 0.88f), 28f);
        CreateLabel("ArtifactsTitle", "アーティファクト", panel,
            new Vector2(0.035f, 0.39f), new Vector2(0.28f, 0.45f), 28f);
        pageLabel = CreateLabel("OwnedItemsPages", "", panel,
            new Vector2(0.035f, 0.035f), new Vector2(0.96f, 0.11f), 28f);
    }

    private TextMeshProUGUI CreateLabel(string objectName, string text, RectTransform parent,
        Vector2 min, Vector2 max, float fontSize)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        SetBounds(label.rectTransform, min, max);
        label.font = benefitsText.font;
        label.fontSharedMaterial = benefitsText.fontSharedMaterial;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = text;
        return label;
    }

    private static void SetBounds(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void UpdatePage()
    {
        if (benefitsText == null) return;
        Vector2 size = benefitsText.rectTransform.rect.size;
        if (textLayoutDirty || size != lastTextSize)
        {
            // TMP paginates using the actual font and available space, including after a resize.
            benefitsText.ForceMeshUpdate();
            pageCount = Mathf.Max(1, benefitsText.textInfo.pageCount);
            lastTextSize = size;
            textLayoutDirty = false;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.PageDown)) currentPage++;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.PageUp)) currentPage--;
        currentPage = Mathf.Clamp(currentPage, 1, pageCount);
        benefitsText.pageToDisplay = currentPage;
        if (pageLabel != null)
            pageLabel.text = $"{currentPage} / {pageCount} ページ    ← →：ページ切替    {showKey}：押している間だけ表示";
    }

    private void ConfigureGrid(RectTransform parent, int count)
    {
        if (parent == null) return;

        GridLayoutGroup grid = parent.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = parent.gameObject.AddComponent<GridLayoutGroup>();

        int columns = Mathf.Clamp(slotsPerRow, 1, 4);
        int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
        float cell = Mathf.Max(1f, Mathf.Min(slotSize.x, slotSize.y,
            (parent.rect.width - slotSpacing.x * (columns - 1)) / columns,
            (parent.rect.height - slotSpacing.y * (rows - 1)) / rows));
        grid.cellSize = new Vector2(cell, cell);
        grid.spacing = slotSpacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
    }

    private void EnsureWidgetCount(RectTransform parent, List<SlotWidget> widgets, int count)
    {
        if (parent == null) return;

        while (widgets.Count < count)
        {
            GameObject slotObject = new GameObject("OwnedItemSlot", typeof(RectTransform));
            slotObject.transform.SetParent(parent, false);
            RectTransform slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.localScale = Vector3.one;

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(slotRect, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            StretchFull(iconRect);
            Image icon = iconObj.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            GameObject countObj = new GameObject("Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            countObj.transform.SetParent(slotRect, false);
            RectTransform countRect = countObj.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1f, 0f);
            countRect.anchorMax = new Vector2(1f, 0f);
            countRect.pivot = new Vector2(1f, 0f);
            countRect.anchoredPosition = countTextOffset;
            countRect.sizeDelta = new Vector2(80f, 40f);

            TextMeshProUGUI countText = countObj.GetComponent<TextMeshProUGUI>();
            countText.alignment = TextAlignmentOptions.BottomRight;
            countText.fontSize = countFontSize;
            countText.color = Color.white;
            countText.raycastTarget = false;

            widgets.Add(new SlotWidget
            {
                root = slotObject,
                icon = icon,
                countText = countText
            });
        }

        while (widgets.Count > count)
        {
            int lastIndex = widgets.Count - 1;
            Destroy(widgets[lastIndex].root);
            widgets.RemoveAt(lastIndex);
        }
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void RefreshDisplay()
    {
        List<BaffItemData> ownedBaff = CollectOwnedBaff();
        List<ArtifactData> ownedArtifacts = CollectOwnedArtifacts();

        ConfigureGrid(baffItemSlotsParent, ownedBaff.Count);
        ConfigureGrid(artifactSlotsParent, ownedArtifacts.Count);
        EnsureWidgetCount(baffItemSlotsParent, baffWidgets, ownedBaff.Count);
        EnsureWidgetCount(artifactSlotsParent, artifactWidgets, ownedArtifacts.Count);
        PopulateSlots(ownedBaff, baffWidgets, item => GetSpriteFromPrefab(item.prefab), item => OwnedProgressManager.GetBaffOwned(item.B_itemID));
        PopulateSlots(ownedArtifacts, artifactWidgets, item => GetSpriteFromPrefab(item.prefab), item => OwnedProgressManager.GetArtifactOwned(item.A_itemID));
        RefreshBenefitsText(ownedBaff, ownedArtifacts);
    }

    private void RefreshBenefitsText(List<BaffItemData> ownedBaff, List<ArtifactData> ownedArtifacts)
    {
        if (benefitsText == null) return;

        StringBuilder text = new StringBuilder();
        AppendBenefits(text, ownedBaff, item => OwnedProgressManager.GetBaffOwned(item.B_itemID), item => item.itemName, item => item.description, item => item.effecttype.ToString());
        AppendBenefits(text, ownedArtifacts, item => OwnedProgressManager.GetArtifactOwned(item.A_itemID), item => item.itemName, item => item.description, item => item.effecttype.ToString());
        string content = text.Length > 0 ? text.ToString() : "所持している効果はありません";
        if (benefitsText.text != content)
        {
            benefitsText.text = content;
            textLayoutDirty = true;
        }
    }

    private static void AppendBenefits<T>(
        StringBuilder text,
        List<T> items,
        System.Func<T, int> getCount,
        System.Func<T, string> getName,
        System.Func<T, string> getDescription,
        System.Func<T, string> getFallback)
    {
        foreach (T item in items)
        {
            string description = getDescription(item);
            if (string.IsNullOrWhiteSpace(description))
                description = getFallback(item);

            text.Append("・")
                .Append(getName(item))
                .Append(" x")
                .Append(getCount(item))
                .AppendLine()
                .AppendLine(description)
                .AppendLine();
        }
    }

    private List<BaffItemData> CollectOwnedBaff()
    {
        var list = new List<BaffItemData>();
        if (baffItemDatabase == null || baffItemDatabase.allBaffItems == null) return list;

        foreach (BaffItemData item in baffItemDatabase.allBaffItems)
        {
            if (item == null) continue;
            if (OwnedProgressManager.GetBaffOwned(item.B_itemID) > 0)
                list.Add(item);
        }
        return list;
    }

    private List<ArtifactData> CollectOwnedArtifacts()
    {
        var list = new List<ArtifactData>();
        if (artifactDatabase == null || artifactDatabase.allArtifacts == null) return list;

        foreach (ArtifactData item in artifactDatabase.allArtifacts)
        {
            if (item == null) continue;
            if (OwnedProgressManager.GetArtifactOwned(item.A_itemID) > 0)
                list.Add(item);
        }
        return list;
    }

    private void PopulateSlots<T>(
        List<T> ownedItems,
        List<SlotWidget> widgets,
        System.Func<T, Sprite> getSprite,
        System.Func<T, int> getCount)
    {
        for (int i = 0; i < widgets.Count; i++)
        {
            SlotWidget widget = widgets[i];
            if (i < ownedItems.Count)
            {
                T item = ownedItems[i];
                Sprite sprite = getSprite(item);
                int count = getCount(item);

                widget.icon.sprite = sprite;
                widget.icon.enabled = sprite != null;
                widget.countText.text = count.ToString();
                widget.root.SetActive(true);
            }
            else
            {
                widget.root.SetActive(false);
            }
        }

    }

    private static Sprite GetSpriteFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        Image image = prefab.GetComponent<Image>();
        if (image == null)
            image = prefab.GetComponentInChildren<Image>(true);

        return image != null ? image.sprite : null;
    }

    private void SetDisplayVisible(bool visible)
    {
        SetWidgetListVisible(baffWidgets, visible);
        SetWidgetListVisible(artifactWidgets, visible);
    }

    private void SetWindowVisible(bool visible)
    {
        if (window == null) return;
        if (window == gameObject) return;

        window.SetActive(visible);
    }

    private static void SetWidgetListVisible(List<SlotWidget> widgets, bool visible)
    {
        if (!visible)
        {
            for (int i = 0; i < widgets.Count; i++)
            {
                if (widgets[i].root != null)
                    widgets[i].root.SetActive(false);
            }
        }
    }
}
