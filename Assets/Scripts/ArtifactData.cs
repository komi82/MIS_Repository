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

    /// <summary>ショップ再入場時: 価格のみ初期化（所持数は維持）</summary>
    public void ResetShopPrice()
    {
        price = startprice;
    }

    /// <summary>新規ゲーム開始時: 価格と所持数を初期化</summary>
    public void Resetprice()
    {
        price = startprice;
        ownedCount = 0;
    }
}
