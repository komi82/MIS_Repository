using UnityEngine;

/// <summary>
/// ゲーム内アイテム1件分の基本定義データ。
/// 各管理クラス（在庫・依頼・レシピ）が共通参照する基盤ScriptableObject。
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    public int itemID;
    public string itemType;
    public string itemName;
    public Sprite icon;
    public GameObject prefab;
}