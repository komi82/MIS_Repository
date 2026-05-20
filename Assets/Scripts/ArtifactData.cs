using UnityEngine;

[CreateAssetMenu(fileName = "Artifact", menuName = "Game/Artifact")]
public class ArtifactData : ScriptableObject
{
    public int A_itemID;
    public string A_itemType;
    public string A_itemName;
    public int price;
    public int startprice;
    public float ownedCount;
    public GameObject prefab;

    public void Resetprice()
    {
        price = startprice;
        ownedCount = 0;
    }
}
