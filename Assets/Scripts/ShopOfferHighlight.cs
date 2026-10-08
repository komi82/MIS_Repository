using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Take's yellow hover outline, applied to the actual purchase button.</summary>
[RequireComponent(typeof(Outline))]
public class ShopOfferHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponent<Outline>().effectColor = Color.yellow;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponent<Outline>().effectColor = Color.black;
    }

    void OnDisable()
    {
        GetComponent<Outline>().effectColor = Color.black;
    }
}
