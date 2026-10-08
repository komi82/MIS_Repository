using UnityEngine;
using UnityEngine.UI;

/// <summary>Fits existing purchasable prefabs into Take's animated shop layout.</summary>
public class ShopDisplaySlot : MonoBehaviour
{
    public static void ConfigureItem(Transform slot, GameObject item)
    {
        if (slot.GetComponent<ShopDisplaySlot>() == null) return;
        var rect = item.GetComponent<RectTransform>();
        if (rect == null) return;

        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition3D = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(13.727179f, 13.727179f);
        var image = item.GetComponent<Image>();
        if (image != null) image.preserveAspect = true;

        var outline = item.GetComponent<Outline>();
        if (outline == null) outline = item.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(0.1f, -0.1f);
        if (item.GetComponent<ShopOfferHighlight>() == null)
            item.AddComponent<ShopOfferHighlight>();
    }
}
