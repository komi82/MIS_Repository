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

        ConfigureGrid(baffItemSlotsParent);
        ConfigureGrid(artifactSlotsParent);
        SetDisplayVisible(false);
        SetWindowVisible(false);
    }

    void Update()
    {
        if (Input.GetKey(showKey))
        {
            RefreshDisplay();
            SetDisplayVisible(true);
            SetWindowVisible(true);
        }
        else
        {
            SetDisplayVisible(false);
            SetWindowVisible(false);
        }
    }

    private void ConfigureGrid(RectTransform parent)
    {
        if (parent == null) return;

        GridLayoutGroup grid = parent.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = parent.gameObject.AddComponent<GridLayoutGroup>();

        grid.cellSize = slotSize;
        grid.spacing = slotSpacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, slotsPerRow);
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
        benefitsText.text = text.Length > 0 ? text.ToString() : "・所持している効果はありません";
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
                .Append(": ")
                .AppendLine(description);
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
