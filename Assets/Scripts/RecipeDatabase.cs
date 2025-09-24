using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Game/RecipeDatabase")]
public class RecipeDatabase : ScriptableObject
{
	public List<RecipeData> allRecipes = new List<RecipeData>();

	public RecipeData FindMatch(ItemData itemA, ItemData itemB)
	{
		if (itemA == null || itemB == null) return null;

		foreach (var recipe in allRecipes)
		{
			if (recipe == null || recipe.requiredItems == null || recipe.requiredItems.Length == 0) continue;

			// 2つ組み合わせ想定。順不同で一致判定
			if (recipe.requiredItems.Length == 2)
			{
				var r0 = recipe.requiredItems[0];
				var r1 = recipe.requiredItems[1];
				if ((r0 == itemA && r1 == itemB) || (r0 == itemB && r1 == itemA))
				{
					return recipe;
				}
			}
			// 1つだけ指定のレシピに対しても安全に動作
			else if (recipe.requiredItems.Length == 1)
			{
				if ((recipe.requiredItems[0] == itemA && itemB == null) || (recipe.requiredItems[0] == itemB && itemA == null))
				{
					return recipe;
				}
			}
		}

		return null;
	}
}


