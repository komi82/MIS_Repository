using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ShopItemDescriptionTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string itemName;
    private string description;
    private TextMeshProUGUI detailText;

    public void Setup(string name, string desc, TextMeshProUGUI textComponent)
    {
        itemName = name;
        description = desc;
        detailText = textComponent;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (detailText != null)
        {
            detailText.text = $"{itemName}\n{description}";
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (detailText != null)
        {
            detailText.text = "";
        }
    }
}
