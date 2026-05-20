using UnityEngine;

/// <summary>
/// ワールド上アイテムに `ItemData` を紐づける薄いラッパー。
/// `ItemPickup` から参照され、拾得時の実データ取得に使われる。
/// </summary>
public class ItemBehaviour : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    public ItemData ItemData => itemData;
}