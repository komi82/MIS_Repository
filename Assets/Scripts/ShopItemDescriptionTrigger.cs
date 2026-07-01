using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ShopItemDescriptionTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private string itemName;
    private string description;
    private TextMeshProUGUI detailText;
    private int price;
    private bool isInsufficientFunds = false;
    private string originalText;

    public void Setup(string name, string desc, TextMeshProUGUI textComponent, int itemPrice = 0)
    {
        itemName = name;
        description = desc;
        detailText = textComponent;
        price = itemPrice;
        Debug.Log($"ShopItemDescriptionTrigger Setup - itemName: {itemName}, price: {price}");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (detailText != null)
        {
            isInsufficientFunds = false;
            originalText = $"{itemName}\n{description}\n必要な料金: {price}G";
            detailText.text = originalText;
            Debug.Log($"OnPointerEnter - Displaying price: {price}G");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (detailText != null && !isInsufficientFunds)
        {
            detailText.text = "";
        }
        isInsufficientFunds = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"OnPointerClick - Current money: {MoneyManager.Instance.GetMoney()}, Required: {price}");
        if (MoneyManager.Instance.GetMoney() < price)
        {
            isInsufficientFunds = true;
            if (detailText != null)
            {
                detailText.text = "お金が足りません";
                Debug.Log("Insufficient funds - displaying message");
            }
        }
    }
}
