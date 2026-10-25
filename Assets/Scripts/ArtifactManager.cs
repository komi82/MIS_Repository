using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ArtifactManager : MonoBehaviour
{
    [Header("アイテムデータ")]
    public ArtifactData[] ArtifactDatas;

    [Header("配置スロット")]
    public Transform[] slots;

    [Header("生成数")]
    public int spawnCount = 2;

    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI itemDetailText;

    void Start()
    {
        ShopReadability.Configure(itemDetailText);
        InitializeArtifact();

        SpawnItems();
    }

    void InitializeArtifact()
    {
        foreach (ArtifactData A_item in ArtifactDatas)
        {
            A_item.ownedCount = OwnedProgressManager.GetArtifactOwned(A_item.A_itemID);
            A_item.price = ShopPricing.ForCurrentVisit(A_item.startprice, OwnedProgressManager.GetArtifactOwned(A_item.A_itemID));
        }
    }

    void SpawnItems()
    {
        List<Transform> availableSlots = new List<Transform>(slots);
        List<ArtifactData> availableItems = new List<ArtifactData>(ArtifactDatas);
        availableItems.RemoveAll(x => x == null || (x.uniquePurchase && OwnedProgressManager.GetArtifactOwned(x.A_itemID) > 0));

        for (int i = 0; i < spawnCount; i++)
        {
            if (availableSlots.Count == 0 || availableItems.Count == 0)
                break;

            // ランダムスロット
            int slotIndex = Random.Range(0, availableSlots.Count);
            Transform selectedSlot = availableSlots[slotIndex];

            // ランダムアイテム
            int itemIndex = Random.Range(0, availableItems.Count);
            ArtifactData selectedItem = availableItems[itemIndex];
            // Keep the visible offer and its charge identical for this visit.
            int offerPrice = ShopPricing.ForCurrentVisit(selectedItem.startprice,
                OwnedProgressManager.GetArtifactOwned(selectedItem.A_itemID));
            selectedItem.price = offerPrice;

            // アイテム生成
            GameObject A_itemObj = Instantiate(
                selectedItem.prefab,
                selectedSlot.position,
                Quaternion.identity,
                selectedSlot
            );

            ShopDisplaySlot.ConfigureItem(selectedSlot, A_itemObj);

            // 画像の引き延ばしを防止（アスペクト比を維持）
            Image img = A_itemObj.GetComponent<Image>();
            if (img != null)
            {
                img.preserveAspect = true;
            }

            Debug.Log("生成: " + selectedItem.A_itemName);

            // Button取得
            Button button = A_itemObj.GetComponent<Button>();

            // ホバー時の説明表示を設定
            ShopItemDescriptionTrigger trigger = A_itemObj.AddComponent<ShopItemDescriptionTrigger>();
            if (trigger != null)
            {
                trigger.Setup(selectedItem.itemName, selectedItem.description, itemDetailText, offerPrice);
            }

            if (button != null)
            {
                var stock = ShopStockNotice.Register(A_itemObj, itemDetailText);
                button.onClick.AddListener(() =>
                {
                    if (selectedItem.uniquePurchase && OwnedProgressManager.GetArtifactOwned(selectedItem.A_itemID) > 0) return;
                    if (!A_itemObj.activeSelf) return; // Sold offers cannot be charged twice.
                    // ゴールド消費
                    bool success = MoneyManager.Instance.SpendMoney(offerPrice);

                    // 購入成功時
                    if (success)
                    {
                        OwnedProgressManager.AddArtifact(selectedItem.A_itemID);
                        selectedItem.ownedCount = OwnedProgressManager.GetArtifactOwned(selectedItem.A_itemID);

                        Debug.Log(selectedItem.A_itemName + " を購入");

                        if (itemDetailText != null)
                        {
                            itemDetailText.text = ""; // 購入時に詳細テキストをクリア
                        }

                        A_itemObj.SetActive(false);
                        if (stock != null) stock.Refresh();
                    }
                });
            }

            // 重複防止
            availableSlots.RemoveAt(slotIndex);
            availableItems.RemoveAt(itemIndex);
        }
    }
}