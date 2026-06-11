using UnityEngine;

[CreateAssetMenu(fileName = "BaffItem", menuName = "Game/BaffItem")]
public class BaffItemData : ScriptableObject
{
    public int B_itemID;
    public BaffEffectType effecttype;
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