using System.Collections.Generic;
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

    [Header("表示スロット（空の UI RectTransform を指定）")]
    [SerializeField] private GameObject window;
    [SerializeField] private RectTransform[] baffItemSlots;
    [SerializeField] private RectTransform[] artifactSlots;

    [Header("入力")]
    [SerializeField] private KeyCode showKey = KeyCode.Tab;

    [Header("見た目")]
    [SerializeField] private Vector2 countTextOffset = new Vector2(-8f, 8f);
    [SerializeField] private int countFontSize = 24;

    private readonly List<SlotWidget> baffWidgets = new List<SlotWidget>();
    private readonly List<SlotWidget> artifactWidgets = new List<SlotWidget>();
    private bool widgetsBuilt;

    private class SlotWidget
    {
        public RectTransform slot;
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI countText;
    }

    void Start()
    {
        OwnedProgressManager.LogOwnedInventory(baffItemDatabase, artifactDatabase);
        BuildWidgetsIfNeeded();
        SetDisplayVisible(false);
    }

    void Update()
    {
        if (Input.GetKey(showKey))
        {
            RefreshDisplay();
            SetDisplayVisible(true);
            window.SetActive(true);
        }
        else
        {
            SetDisplayVisible(false);
            window.SetActive(false);
        }
    }

    private void BuildWidgetsIfNeeded()
    {
        if (widgetsBuilt) return;

        BuildWidgetsForSlots(baffItemSlots, baffWidgets);
        BuildWidgetsForSlots(artifactSlots, artifactWidgets);
        widgetsBuilt = true;
    }

    private void BuildWidgetsForSlots(RectTransform[] slots, List<SlotWidget> widgetList)
    {
        widgetList.Clear();
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            RectTransform slot = slots[i];
            if (slot == null) continue;

            SlotWidget widget = new SlotWidget { slot = slot };

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(slot, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            StretchFull(iconRect);
            widget.icon = iconObj.GetComponent<Image>();
            widget.icon.raycastTarget = false;
            widget.icon.preserveAspect = true;

            GameObject countObj = new GameObject("Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            countObj.transform.SetParent(slot, false);
            RectTransform countRect = countObj.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1f, 0f);
            countRect.anchorMax = new Vector2(1f, 0f);
            countRect.pivot = new Vector2(1f, 0f);
            countRect.anchoredPosition = countTextOffset;
            countRect.sizeDelta = new Vector2(80f, 40f);

            widget.countText = countObj.GetComponent<TextMeshProUGUI>();
            widget.countText.alignment = TextAlignmentOptions.BottomRight;
            widget.countText.fontSize = countFontSize;
            widget.countText.color = Color.white;
            widget.countText.raycastTarget = false;

            widget.root = slot.gameObject;
            widget.root.SetActive(false);
            widgetList.Add(widget);
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
        BuildWidgetsIfNeeded();

        List<BaffItemData> ownedBaff = CollectOwnedBaff();
        List<ArtifactData> ownedArtifacts = CollectOwnedArtifacts();

        PopulateSlots(ownedBaff, baffWidgets, item => GetSpriteFromPrefab(item.prefab), item => OwnedProgressManager.GetBaffOwned(item.B_itemID));
        PopulateSlots(ownedArtifacts, artifactWidgets, item => GetSpriteFromPrefab(item.prefab), item => OwnedProgressManager.GetArtifactOwned(item.A_itemID));
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

        if (ownedItems.Count > widgets.Count)
        {
            Debug.LogWarning($"OwnedItemsHUD: スロット数({widgets.Count})より多くの所持品({ownedItems.Count})があります。表示しきれない分があります。");
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
