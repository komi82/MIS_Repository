using UnityEngine;

public class PlacementSlots : MonoBehaviour
{
	[Header("設置スロット(最大2)")]
	[SerializeField] private Transform slot1;
	[SerializeField] private Transform slot2;

	[Header("クラフト結果の配置アンカー")]
	[SerializeField] private Transform resultAnchor; // 結果アイテムを置く座標

	[Header("状態")] 
	[SerializeField] private ItemData occupiedItem1; // スロット1に置かれているItemData
	[SerializeField] private ItemData occupiedItem2; // スロット2に置かれているItemData

	public bool TryPlace(ItemData itemData, out Transform targetSlot)
	{
		targetSlot = null;
		if (itemData == null) return false;

		// 空きスロット探索（1優先 → 2）
		if (slot1 != null && occupiedItem1 == null)
		{
			occupiedItem1 = itemData;
			targetSlot = slot1;
			return true;
		}
		if (slot2 != null && occupiedItem2 == null)
		{
			occupiedItem2 = itemData;
			targetSlot = slot2;
			return true;
		}
		return false;
	}

	public void ClearSlotByTransform(Transform slotTransform)
	{
		if (slotTransform == slot1)
		{
			occupiedItem1 = null;
		}
		else if (slotTransform == slot2)
		{
			occupiedItem2 = null;
		}
	}

	public Transform GetSlotTransform(int index)
	{
		if (index == 0) return slot1;
		if (index == 1) return slot2;
		return null;
	}

	public Transform GetResultAnchor()
	{
		return resultAnchor;
	}

	public void ClearAllSlots()
	{
		occupiedItem1 = null;
		occupiedItem2 = null;
	}

	public void DestroyChildrenOf(Transform parent)
	{
		if (parent == null) return;
		for (int i = parent.childCount - 1; i >= 0; i--)
		{
			var child = parent.GetChild(i);
			Destroy(child.gameObject);
		}
	}

	public void ClearAllAndDestroyChildren()
	{
		DestroyChildrenOf(slot1);
		DestroyChildrenOf(slot2);
		ClearAllSlots();
	}

	public ItemData GetItemInSlot(int index)
	{
		return index == 0 ? occupiedItem1 : occupiedItem2;
	}

	public (ItemData, ItemData) GetCombination()
	{
		return (occupiedItem1, occupiedItem2);
	}

	public bool HasEmptySlot()
	{
		if (slot1 != null && occupiedItem1 == null) return true;
		if (slot2 != null && occupiedItem2 == null) return true;
		return false;
	}
}


