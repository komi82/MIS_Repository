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

    [Header("表示情報")]
    public string itemName;
    [TextArea(3, 5)]
    public string description;

    [Header("キノコ依頼の報酬設定")]
    [Min(0)] public int rewardBonusPerOwnedItem = 100;
    public ItemData[] rewardTargetItems;

    [Header("依頼の抽選設定")]
    [Tooltip("1個ごとに加える抽選の重み。0.5なら未所持1倍、1個で1.5倍、2個で2倍。")]
    [Min(0f)] public float requestWeightIncreasePerItem = 0.5f;
    public RequestType[] requestWeightTargetTypes;

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
