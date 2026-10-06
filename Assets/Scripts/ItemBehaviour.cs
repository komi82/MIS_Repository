using UnityEngine;

/// <summary>
/// ワールド上アイテムに `ItemData` を紐づける薄いラッパー。
/// `ItemPickup` から参照され、拾得時の実データ取得に使われる。
/// </summary>

public class ItemBehaviour : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    public ItemData ItemData => itemData;
    private ItemInstanceState instanceState;
    public ItemInstanceState InstanceState => instanceState ?? (instanceState = new ItemInstanceState());
    public bool AgingOnTable { get; private set; }

    public void SetInstanceState(ItemInstanceState state, bool onTable = false)
    {
        instanceState = state ?? new ItemInstanceState();
        AgingOnTable = onTable;
    }

    private void Update()
    {
        if (!AgingOnTable) return;
        var effect = FestivalUpgradeRuntime.Owned(BaffEffectType.tableAging);
        if (effect == null || effect.upgradeBonusRate <= 0f) return;
        InstanceState.tableSeconds = Mathf.Min(36000f, InstanceState.tableSeconds + Time.deltaTime);
    }
}
