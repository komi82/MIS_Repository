using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One stock notice shared by both the item and artifact offers in this shop.</summary>
public sealed class ShopStockNotice : MonoBehaviour
{
    readonly List<GameObject> offers = new List<GameObject>();
    GameObject panel;
    public bool IsSoldOut => panel != null && panel.activeSelf;

    public static ShopStockNotice Register(GameObject offer, TextMeshProUGUI detail)
    {
        if (offer == null || detail == null || detail.canvas == null) return null;
        var root = detail.canvas.rootCanvas.gameObject;
        var stock = root.GetComponent<ShopStockNotice>();
        if (stock == null)
        {
            stock = root.AddComponent<ShopStockNotice>();
            stock.CreatePanel(detail);
        }
        if (!stock.offers.Contains(offer)) stock.offers.Add(offer);
        stock.Refresh();
        return stock;
    }

    public void Refresh()
    {
        if (panel == null) return;
        bool soldOut = offers.Count > 0;
        foreach (var offer in offers)
            if (offer != null && offer.activeSelf) { soldOut = false; break; }
        panel.SetActive(soldOut);
    }

    void CreatePanel(TextMeshProUGUI detail)
    {
        panel = new GameObject("ShopSoldOutNotice", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)panel.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.30f, 0.56f);
        rect.sizeDelta = new Vector2(860f, 260f);
        var background = panel.GetComponent<Image>();
        background.color = new Color(0.11f, 0.07f, 0.045f, 0.96f);
        background.raycastTarget = false;
        var edge = panel.AddComponent<Outline>();
        edge.effectColor = new Color(0.78f, 0.60f, 0.34f, 1f);
        edge.effectDistance = new Vector2(3f, -3f);
        var label = new GameObject("SoldOutText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(rect, false);
        label.font = detail.font;
        label.fontSize = 64f;
        label.color = new Color(1f, 0.97f, 0.88f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.text = "本日完売\n<size=34>ご購入ありがとうございます\n右上のEXITから次の日へ進めます</size>";
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(24f, 20f);
        label.rectTransform.offsetMax = new Vector2(-24f, -20f);
        panel.SetActive(false);
    }
}
