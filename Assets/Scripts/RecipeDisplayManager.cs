using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

public class RecipeDisplayManager : MonoBehaviour
{
    [SerializeField] private RecipeDatabase recipeDatabase;           // 通常レシピ用
    [SerializeField] private RecipeDatabase weaponRecipeDatabase;     // 武器レシピ用
    [SerializeField] private RecipeDatabase washRecipeDatabase;       // 浄化レシピ用
    [SerializeField] private Transform recipeContentParent;          // 通常レシピ用の親Transform
    [SerializeField] private Transform weaponContentParent;           // 武器レシピ用の親Transform
    [SerializeField] private Transform washContentParent;             // 浄化レシピ用の親Transform
    [SerializeField] private GameObject recipeItemPrefab;             // レシピアイテム用プレハブ
    [SerializeField] private GameObject weaponItemPrefab;             // 武器アイテム用プレハブ
    [SerializeField] private GameObject washItemPrefab;               // 浄化レシピ用プレハブ
    
    [Header("統合表示設定")]
    [SerializeField] private bool useUnifiedDisplay = false; // 複数のデータベースを1つのリストに統合するか
    
	[Header("並び替え設定")]
	[SerializeField] private bool sortByItemName = true;      // アイテム名でソートする
	[SerializeField] private bool sortByItemId = false;       // resultItem.itemID でソートする
	[SerializeField] private bool sortAscending = true;       // 昇順(true)/降順(false)
	[SerializeField] private bool groupByDatabase = true;     // データベースごとにグルーピングする
	
    [Header("文字色設定")]
    [SerializeField] private Color recipeTextColor = Color.white;       // 通常レシピの文字色
    [SerializeField] private Color weaponTextColor = Color.red;        // 武器レシピの文字色
    [SerializeField] private Color washTextColor = Color.cyan;         // 浄化レシピの文字色

    private void Start()
    {
        DisplayRecipes();
    }

    private void DisplayRecipes()
    {
        if (useUnifiedDisplay)
        {
            // 統合表示モード：複数のデータベースを1つのリストに統合
            DisplayUnifiedRecipes();
        }
        else
        {
            // 通常レシピを表示
            DisplayRecipeItems(recipeDatabase, recipeContentParent, recipeItemPrefab, recipeTextColor, "通常レシピ");

            // 武器レシピを表示（別リストとして維持する場合）
            if (weaponContentParent != null && weaponItemPrefab != null)
            {
                DisplayRecipeItems(weaponRecipeDatabase, weaponContentParent, weaponItemPrefab, weaponTextColor, "武器レシピ");
            }
            
            // 浄化レシピを表示（別リストとして維持する場合）
            if (washContentParent != null && washItemPrefab != null)
            {
                DisplayRecipeItems(washRecipeDatabase, washContentParent, washItemPrefab, washTextColor, "浄化レシピ");
            }
        }
    }
    
    /// <summary>
    /// 複数のデータベースを統合して1つのリストに表示
    /// </summary>
    private void DisplayUnifiedRecipes()
    {
        if (recipeContentParent == null || recipeItemPrefab == null)
        {
            Debug.LogWarning("統合表示: ContentParent または ItemPrefab が null です");
            return;
        }

        // 既存の子オブジェクトを削除
        foreach (Transform child in recipeContentParent)
        {
            Destroy(child.gameObject);
        }

        // アイテム名とそのデータベースの関連を記録
        Dictionary<string, RecipeDatabase> itemDatabaseMap = new Dictionary<string, RecipeDatabase>();
        
        // recipeDatabaseから追加
        if (recipeDatabase != null)
        {
            List<string> recipeNames = GetSortedItemNamesFromDatabase(recipeDatabase);
            foreach (string name in recipeNames)
            {
                if (!itemDatabaseMap.ContainsKey(name))
                {
                    itemDatabaseMap[name] = recipeDatabase;
                }
            }
        }
        
        // weaponRecipeDatabaseから追加
        if (weaponRecipeDatabase != null)
        {
            List<string> weaponNames = GetSortedItemNamesFromDatabase(weaponRecipeDatabase);
            foreach (string name in weaponNames)
            {
                if (!itemDatabaseMap.ContainsKey(name))
                {
                    itemDatabaseMap[name] = weaponRecipeDatabase;
                }
            }
        }
        
        // washRecipeDatabaseから追加
        if (washRecipeDatabase != null)
        {
            List<string> washNames = GetSortedItemNamesFromDatabase(washRecipeDatabase);
            foreach (string name in washNames)
            {
                if (!itemDatabaseMap.ContainsKey(name))
                {
                    itemDatabaseMap[name] = washRecipeDatabase;
                }
            }
        }

		// 並び替え
		List<string> sortedNames;
		if (groupByDatabase)
		{
			// データベースごとに並び替えてから結合
			sortedNames = new List<string>();
			RecipeDatabase[] databases = { recipeDatabase, weaponRecipeDatabase, washRecipeDatabase };
			foreach (var db in databases)
			{
				if (db == null) continue;
				var dbNames = GetSortedItemNamesFromDatabase(db);
				foreach (string name in dbNames)
				{
					if (itemDatabaseMap.ContainsKey(name) && itemDatabaseMap[name] == db)
					{
						sortedNames.Add(name);
					}
				}
			}
		}
		else
		{
			// データベースを問わず全体をソート
			if (sortByItemId)
			{
				// itemIDで整列するため、name->(db,id) を構築
				var mapWithId = new Dictionary<string, (RecipeDatabase db, int id)>();
				foreach (var kv in itemDatabaseMap)
				{
					int id = GetResultItemIdByName(kv.Value, kv.Key);
					if (!mapWithId.ContainsKey(kv.Key))
					{
						mapWithId[kv.Key] = (kv.Value, id);
					}
					else
					{
						// 重複名は小さいIDを優先
						if (id < mapWithId[kv.Key].id) mapWithId[kv.Key] = (kv.Value, id);
					}
				}
				var list = new List<(string name, int id)>() ;
				foreach (var kv in mapWithId)
				{
					list.Add((kv.Key, kv.Value.id));
				}
				list.Sort((a,b) => sortAscending ? a.id.CompareTo(b.id) : b.id.CompareTo(a.id));
				sortedNames = new List<string>(list.Count);
				foreach (var t in list) sortedNames.Add(t.name);
			}
			else
			{
				sortedNames = OrderNames(itemDatabaseMap.Keys);
			}
		}

        if (sortedNames.Count == 0)
        {
            Debug.LogWarning("統合表示: 表示するアイテムがありません");
            return;
        }

        // 各アイテム名でGameObjectを生成
        foreach (string itemName in sortedNames)
        {
            GameObject itemGO = Instantiate(recipeItemPrefab, recipeContentParent);
            
            // データベースに応じた色を決定
            RecipeDatabase itemDB = itemDatabaseMap[itemName];
            Color itemColor = GetColorForDatabase(itemDB);
            
            // TextMeshProコンポーネントを取得してテキストを設定
            TextMeshProUGUI textComponent = itemGO.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = itemName;
                textComponent.color = itemColor; // 文字色を設定
            }
            else
            {
                textComponent = itemGO.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = itemName;
                    textComponent.color = itemColor; // 文字色を設定
                }
            }

            // RecipeItemHoverコンポーネントを追加
            RecipeItemHover hoverComponent = itemGO.GetComponent<RecipeItemHover>();
            if (hoverComponent == null)
            {
                hoverComponent = itemGO.AddComponent<RecipeItemHover>();
            }
            
            // RecipeItemHoverの設定
            hoverComponent.SetItemName(itemName);
            hoverComponent.SetRecipeDatabases(recipeDatabase, weaponRecipeDatabase, washRecipeDatabase);

            // クリックイベントを追加（オプション）
            var button = itemGO.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                string capturedItemName = itemName;
                button.onClick.AddListener(() => OnItemClicked(capturedItemName));
            }
        }

        // ZigZagLayoutGroupでレイアウト再計算
        var layout = recipeContentParent.GetComponent<ZigZagLayoutGroup>();
        if (layout != null)
        {
            layout.SetLayoutHorizontal();
            layout.SetLayoutVertical();
        }

        Debug.Log("統合表示: " + sortedNames.Count + " 件表示");
    }
    
    /// <summary>
    /// データベースに応じた色を取得
    /// </summary>
    private Color GetColorForDatabase(RecipeDatabase database)
    {
        if (database == recipeDatabase)
        {
            return recipeTextColor;
        }
        else if (database == weaponRecipeDatabase)
        {
            return weaponTextColor;
        }
        else if (database == washRecipeDatabase)
        {
            return washTextColor;
        }
        
        return Color.white; // デフォルト
    }

    /// <summary>
    /// レシピアイテムをGameObjectとして生成・配置
    /// </summary>
    private void DisplayRecipeItems(RecipeDatabase database, Transform contentParent, GameObject itemPrefab, Color textColor, string logPrefix)
    {
        if (contentParent == null || itemPrefab == null)
        {
            Debug.LogWarning(logPrefix + ": ContentParent または ItemPrefab が null です");
            return;
        }

        // 既存の子オブジェクトを削除
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

		// データベースからアイテム名を取得（並び替え適用）
		List<string> itemNames = GetSortedItemNamesFromDatabase(database);
        
        if (itemNames.Count == 0)
        {
            Debug.LogWarning(logPrefix + ": 表示するアイテムがありません");
            return;
        }

        // 各アイテム名でGameObjectを生成
        foreach (string itemName in itemNames)
        {
            GameObject itemGO = Instantiate(itemPrefab, contentParent);
            
            // TextMeshProコンポーネントを取得してテキストを設定
            TextMeshProUGUI textComponent = itemGO.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = itemName;
                textComponent.color = textColor; // 文字色を設定
            }
            else
            {
                // 子オブジェクトにTextMeshProがある場合
                textComponent = itemGO.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = itemName;
                    textComponent.color = textColor; // 文字色を設定
                }
            }

            // RecipeItemHoverコンポーネントを追加
            RecipeItemHover hoverComponent = itemGO.GetComponent<RecipeItemHover>();
            if (hoverComponent == null)
            {
                hoverComponent = itemGO.AddComponent<RecipeItemHover>();
            }
            
            // RecipeItemHoverの設定
            hoverComponent.SetItemName(itemName);
            hoverComponent.SetRecipeDatabase(database);
            
            // 全てのデータベースを設定
            hoverComponent.SetRecipeDatabases(recipeDatabase, weaponRecipeDatabase, washRecipeDatabase);

            // クリックイベントを追加（オプション）
            var button = itemGO.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                string capturedItemName = itemName; // クロージャ用
                button.onClick.AddListener(() => OnItemClicked(capturedItemName));
            }
        }

        // ZigZagLayoutGroupでレイアウト再計算
        var layout = contentParent.GetComponent<ZigZagLayoutGroup>();
        if (layout != null)
        {
            layout.SetLayoutHorizontal();
            layout.SetLayoutVertical();
        }

        Debug.Log(logPrefix + ": " + itemNames.Count + " 件表示");
    }

    /// <summary>
    /// アイテムクリック時の処理
    /// </summary>
    private void OnItemClicked(string itemName)
    {
        Debug.Log("クリックされたアイテム: " + itemName);
        // ここで詳細表示やツールチップなどの処理を追加
    }

    /// <summary>
    /// RecipeDatabaseからResultItemの名前を取得し、50音順でソート
    /// </summary>
	private List<string> GetSortedItemNamesFromDatabase(RecipeDatabase database)
    {
        List<string> itemNames = new List<string>();

        if (database == null)
        {
            Debug.LogWarning("RecipeDatabase が null です");
            return itemNames;
        }

        // allRecipes フィールドからRecipeDataを取得
        List<RecipeData> recipes = database.allRecipes;

        if (recipes == null || recipes.Count == 0)
        {
            Debug.LogWarning("RecipeDatabase に登録されているレシピがありません");
            return itemNames;
        }

		// 重複を避ける
		Dictionary<string, int> nameToId = new Dictionary<string, int>();

		foreach (var recipe in recipes)
        {
            if (recipe != null && recipe.resultItem != null)
            {
				string itemName = recipe.resultItem.itemName;
				int itemId = recipe.resultItem.itemID;
                if (!string.IsNullOrEmpty(itemName))
                {
					if (!nameToId.ContainsKey(itemName)) nameToId[itemName] = itemId;
					else nameToId[itemName] = Mathf.Min(nameToId[itemName], itemId);
                }
            }
        }

		// 並び替え適用
		if (sortByItemId)
		{
			var list = new List<(string name, int id)>();
			foreach (var kv in nameToId) list.Add((kv.Key, kv.Value));
			list.Sort((a,b) => sortAscending ? a.id.CompareTo(b.id) : b.id.CompareTo(a.id));
			itemNames = new List<string>(list.Count);
			foreach (var t in list) itemNames.Add(t.name);
		}
		else
		{
			itemNames = OrderNames(nameToId.Keys);
		}

        return itemNames;
    }

	private List<string> OrderNames(IEnumerable<string> names)
	{
		if (!sortByItemName)
		{
			return new List<string>(names);
		}
		CultureInfo japanCulture = new CultureInfo("ja-JP");
		var list = names
			.OrderBy(x => x, Comparer<string>.Create((a, b) => japanCulture.CompareInfo.Compare(a, b)))
			.ToList();
		if (!sortAscending)
		{
			list.Reverse();
		}
		return list;
	}

	// 公開API: 並び替え設定の更新と再描画
	public void SetSortByItemName(bool enabled, bool ascending)
	{
		sortByItemName = enabled;
		sortAscending = ascending;
		DisplayRecipes();
	}

	public void SetSortByItemId(bool enabled, bool ascending)
	{
		sortByItemId = enabled;
		sortAscending = ascending;
		if (enabled) sortByItemName = false; // 名前ソートと排他
		DisplayRecipes();
	}

	public void SetGroupByDatabase(bool enabled)
	{
		groupByDatabase = enabled;
		DisplayRecipes();
	}

	private int GetResultItemIdByName(RecipeDatabase db, string itemName)
	{
		if (db == null || string.IsNullOrEmpty(itemName)) return int.MaxValue;
		var recipes = db.allRecipes;
		if (recipes == null) return int.MaxValue;
		int best = int.MaxValue;
		for (int i = 0; i < recipes.Count; i++)
		{
			var r = recipes[i];
			if (r == null || r.resultItem == null) continue;
			if (r.resultItem.itemName == itemName)
			{
				best = Mathf.Min(best, r.resultItem.itemID);
			}
		}
		return best;
	}

}


