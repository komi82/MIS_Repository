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
        InitializeArtifact();

        SpawnItems();
    }

    void InitializeArtifact()
    {
        foreach (ArtifactData A_item in ArtifactDatas)
        {
            A_item.ResetShopPrice();
            A_item.ownedCount = OwnedProgressManager.GetArtifactOwned(A_item.A_itemID);
        }
    }

    void SpawnItems()
    {
        List<Transform> availableSlots = new List<Transform>(slots);
        List<ArtifactData> availableItems = new List<ArtifactData>(ArtifactDatas);

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

            // アイテム生成
            GameObject A_itemObj = Instantiate(
                selectedItem.prefab,
                selectedSlot.position,
                Quaternion.identity,
                selectedSlot
            );

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
                trigger.Setup(selectedItem.itemName, selectedItem.description, itemDetailText, selectedItem.price);
            }

            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    // ゴールド消費
                    bool success = MoneyManager.Instance.SpendMoney(selectedItem.price);

                    // 購入成功時
                    if (success)
                    {
                        OwnedProgressManager.AddArtifact(selectedItem.A_itemID);
                        selectedItem.ownedCount = OwnedProgressManager.GetArtifactOwned(selectedItem.A_itemID);

                        selectedItem.price = Mathf.RoundToInt(selectedItem.price * 1.1f);
                        Debug.Log(selectedItem.A_itemName + " を購入");

                        if (itemDetailText != null)
                        {
                            itemDetailText.text = ""; // 購入時に詳細テキストをクリア
                        }

                        A_itemObj.SetActive(false);
                    }
                });
            }

            // 重複防止
            availableSlots.RemoveAt(slotIndex);
            availableItems.RemoveAt(itemIndex);
        }
    }
}