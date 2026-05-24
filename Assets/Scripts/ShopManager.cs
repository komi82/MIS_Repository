using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("アイテムデータ")]
    public BaffItemData[] baffitemDatas;

    [Header("配置スロット")]
    public Transform[] slots;

    [Header("生成数")]
    public int spawnCount = 3;

    void Start()
    {
        InitializeItem(); //アイテム価格の初期値化

        SpawnItems(); //ショップにアイテムを生成
    }

    void InitializeItem()
    {
        foreach (BaffItemData B_item in baffitemDatas)
        {
            B_item.ResetShopPrice();
            B_item.ownedCount = OwnedProgressManager.GetBaffOwned(B_item.B_itemID);
        }
    }

    void SpawnItems()
    {
        List<Transform> availableSlots = new List<Transform>(slots);
        List<BaffItemData> availableItems = new List<BaffItemData>(baffitemDatas);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availableSlots.Count == 0 || availableItems.Count == 0)
                break;

            // ランダムスロット
            int slotIndex = Random.Range(0, availableSlots.Count);
            Transform selectedSlot = availableSlots[slotIndex];

            // ランダムアイテム
            int itemIndex = Random.Range(0, availableItems.Count);
            BaffItemData selectedItem = availableItems[itemIndex];

            // アイテム生成
            GameObject itemObj = Instantiate(
                selectedItem.prefab,
                selectedSlot.position,
                Quaternion.identity,
                selectedSlot
            );

            Debug.Log("生成: " + selectedItem.B_itemName);

            // Button取得
            Button button = itemObj.GetComponent<Button>();

            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    // ゴールド消費
                    bool success = MoneyManager.Instance.SpendMoney(selectedItem.price);

                    // 購入成功時
                    if (success)
                    {
                        OwnedProgressManager.AddBaffItem(selectedItem.B_itemID);
                        selectedItem.ownedCount = OwnedProgressManager.GetBaffOwned(selectedItem.B_itemID);

                        selectedItem.price = Mathf.RoundToInt(selectedItem.price * 1.1f); //アイテムの価格上昇

                        Debug.Log(selectedItem.B_itemName + " を購入");

                        itemObj.SetActive(false); //アイテム購入を一度きりに
                    }
                });
            }

            // 重複防止
            availableSlots.RemoveAt(slotIndex);
            availableItems.RemoveAt(itemIndex);
        }
    }
}