using UnityEngine;
using UnityEngine.UI;

public class PutItem : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private SlotSelector slotselector;
    [SerializeField] private ItemPickup itempickup;
    [SerializeField] private float pickupRange = 10f;
    [SerializeField] private Camera mainCamera;
    [Header("アイテム設置")]
	[SerializeField] private float placementOffset = 0.5f; // 任意の高さ（スロットTransform未設定時の後方互換）
	[SerializeField] private RecipeDatabase recipeDatabase; // craft 用
	[SerializeField] private RecipeDatabase weaponRecipeDatabase; // blacksmith 用
	[SerializeField] private RecipeDatabase washRecipeDatabase;   // wash 用


    void Update()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
			GameObject targetObject = hit.collider.gameObject;
			var slotsOnParent = targetObject.GetComponentInParent<PlacementSlots>();
			bool isCraftTarget = targetObject.CompareTag("craft") || (slotsOnParent != null);

			if (isCraftTarget)
            {
                  if (Input.GetKeyDown(KeyCode.E))
                   {
                    slotselector.SelectSlot(slotselector.selectedIndex);
                    PutSelectedItem();
                   }

				// Rキーでクラフト結果を生成
				if (Input.GetKeyDown(KeyCode.R))
				{
					PlacementSlots slots = slotsOnParent != null ? slotsOnParent : targetObject.GetComponent<PlacementSlots>();
					// タグに応じて参照するデータベースを切替
					RecipeDatabase activeDB = null;
					GameObject taggedObject = targetObject;
					if (slotsOnParent != null) taggedObject = slotsOnParent.gameObject;
					if (taggedObject.CompareTag("craft")) activeDB = recipeDatabase;
					else if (taggedObject.CompareTag("blacksmith")) activeDB = weaponRecipeDatabase;
					else if (taggedObject.CompareTag("wash")) activeDB = washRecipeDatabase;

					if (slots != null && activeDB != null)
					{
						var combo = slots.GetCombination();
						RecipeData match = activeDB.FindMatch(combo.Item1, combo.Item2);
						if (match != null && match.resultItem != null && match.resultItem.prefab != null)
						{
							Transform anchor = slots.GetResultAnchor();
							Vector3 pos = anchor != null ? anchor.position : hit.point + targetObject.transform.up * placementOffset;
							Quaternion rot = anchor != null ? anchor.rotation : Quaternion.identity;

							// 既存子（素材）を掃除するなら解除＆破棄
							slots.ClearAllAndDestroyChildren();

							Instantiate(match.resultItem.prefab, pos, rot);
							Debug.Log($"クラフト生成: {match.resultItem.itemName}");
						}
						else
						{
							Debug.Log("クラフト可能なレシピがありません");
						}
					}
					else
					{
						if (slots == null) Debug.LogWarning("PlacementSlots が見つかりません。対象または親に付与してください");
						if (activeDB == null) Debug.LogWarning("対応する RecipeDatabase が未設定です（craft/blacksmith/wash を確認）");
					}
				}
            }
        }

    }


	void PutSelectedItem()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
			GameObject targetObject = hit.collider.gameObject;
			var slotsOnParent = targetObject.GetComponentInParent<PlacementSlots>();
			bool isCraftTarget = targetObject.CompareTag("craft") || targetObject.CompareTag("blacksmith") || targetObject.CompareTag("wash") || (slotsOnParent != null);

			if (isCraftTarget)
            {
                ItemData itemToPlace = inventoryManager.selectedItem;

                if (itemToPlace != null)
                {
                    if (inventoryManager == null)
                    {
                        Debug.LogWarning("InventoryManagerが未設定です");
                        return;
                    }
                    if (inventoryManager.selectedItem == null)
                    {
                        Debug.LogWarning("selectedItem が未設定です");
                        return;
                    }

                    InventorySlotUI slot = inventoryManager.FindSlotByItem(inventoryManager.selectedItem);
                    if (slot == null)
                    {
                        Debug.LogWarning("選択されたアイテムに対応するスロットが見つかりません");
                        return;
                    }

					if (itemToPlace.prefab != null)
                    {
					// スロット優先で配置（親からも取得可）
					PlacementSlots slots = slotsOnParent != null ? slotsOnParent : targetObject.GetComponent<PlacementSlots>();
						Transform placeSlot = null;
						Vector3 spawnPosition;
						Quaternion spawnRotation = Quaternion.identity;
						if (slots != null && slots.TryPlace(itemToPlace, out placeSlot) && placeSlot != null)
						{
							spawnPosition = placeSlot.position;
							spawnRotation = placeSlot.rotation;
						}
						else
						{
							// 従来挙動（スロット未設定または満杯）
							spawnPosition = hit.point + targetObject.transform.up * placementOffset;
						}

						GameObject placed;
						if (slots != null && placeSlot != null)
						{
							// スロットの子として生成
							placed = Instantiate(itemToPlace.prefab, spawnPosition, spawnRotation, placeSlot);
						}
						else
						{
							placed = Instantiate(itemToPlace.prefab, spawnPosition, spawnRotation);
						}

						// タグ名をログに反映
						string stationTag = (slotsOnParent != null ? slotsOnParent.gameObject.tag : targetObject.tag);
						Debug.Log($"アイテム '{itemToPlace.itemName}' を '{stationTag}' に配置しました");
                        inventoryManager.RemoveItem(slot);

                        inventoryManager.selectedItem = null; // 選択状態も解除

						// レシピ照合（スロットがある場合のみ）
						if (slots != null)
						{
							// タグに応じて参照するデータベースを切替
							RecipeDatabase activeDB = null;
							GameObject taggedObject = (slotsOnParent != null ? slotsOnParent.gameObject : targetObject);
							if (taggedObject.CompareTag("craft")) activeDB = recipeDatabase;
							else if (taggedObject.CompareTag("blacksmith")) activeDB = weaponRecipeDatabase;
							else if (taggedObject.CompareTag("wash")) activeDB = washRecipeDatabase;

							if (activeDB != null)
							{
								var combo = slots.GetCombination();
								RecipeData match = activeDB.FindMatch(combo.Item1, combo.Item2);
							if (match != null && match.resultItem != null)
							{
								Debug.Log($"レシピ一致: {match.requiredItems[0]?.itemName} + {(match.requiredItems.Length > 1 ? match.requiredItems[1]?.itemName : "")} -> {match.resultItem.itemName}");
                            }
							else
							{
								Debug.Log("レシピ一致なし");
							}
							}
							else
							{
								Debug.LogWarning("対応する RecipeDatabase が未設定です（craft/blacksmith/wash を確認）");
							}
						}
						else
						{
							if (recipeDatabase == null && weaponRecipeDatabase == null && washRecipeDatabase == null) Debug.LogWarning("RecipeDatabase が未設定です");
							if (slots == null) Debug.LogWarning("PlacementSlots が見つかりません。対象または親に付与してください");
						}
                    }
                    else
                    {
                        Debug.LogWarning("プレハブが設定されていません。配置できませんでした");
                    }
                }
                else
                {
                    Debug.LogWarning("選択中のアイテムがありません");
                }
            }
        }
    }

}