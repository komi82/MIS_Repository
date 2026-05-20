using UnityEngine;

[CreateAssetMenu(fileName = "BaffItem", menuName = "Game/BaffItem")]
public class BaffItemData : ScriptableObject
{
    public int B_itemID;
    public string B_itemType;
    public string B_itemName;
    public int price;
    public int startprice;
    public int ownedCount;
    public GameObject prefab;

    public void Resetprice()
    {
        price = startprice;
        ownedCount = 0;
    }
}