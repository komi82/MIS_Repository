using UnityEngine;

[CreateAssetMenu(fileName = "Artifact", menuName = "Game/Artifact")]
public class ArtifactData : ScriptableObject
{
    public int A_itemID;
    public BaffEffectType effecttype;
    public string A_itemName;
    public int price;
    public int startprice;
    public float ownedCount;
    public GameObject prefab;

    [Header("表示情報")]
    public string itemName;
    [TextArea(3, 5)]
    public string description;

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
