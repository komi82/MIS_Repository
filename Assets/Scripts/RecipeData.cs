using UnityEngine;

[CreateAssetMenu(fileName = "RecipeData", menuName = "Data/Recipe")]
public class RecipeData : ScriptableObject
{
    public ItemData[] requiredItems; // 必要なアイテムの組み合わせ
    public ItemData resultItem;      // 結果として返すアイテム
}
