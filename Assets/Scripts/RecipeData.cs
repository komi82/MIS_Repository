using UnityEngine;

/// <summary>
/// 単一レシピの入出力定義データ。
/// `RecipeDatabase` と `PutItem` / `RecipeItemHover` が参照する。
/// </summary>
[CreateAssetMenu(fileName = "RecipeData", menuName = "Data/Recipe")]
public class RecipeData : ScriptableObject
{
    public ItemData[] requiredItems; // 必要なアイテムの組み合わせ
    public ItemData resultItem;      // 結果として返すアイテム
}


