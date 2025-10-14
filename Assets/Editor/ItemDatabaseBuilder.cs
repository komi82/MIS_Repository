using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class ItemDatabaseBuilder
{
    [MenuItem("Tools/Build ItemDatabase (Add)")]
    public static void BuildDatabase()
    {
        string folderPath = "Assets/Prefab/Item";
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { folderPath });

        var newItems = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .ToList();

        string databasePath = "Assets/Scripts/ItemDatabase.asset";
        var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(databasePath);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<ItemDatabase>();
            database.allItems = new List<ItemData>();
            AssetDatabase.CreateAsset(database, databasePath);
        }

        // 既存リストに追加（重複チェック）
        foreach (var item in newItems)
        {
            if (!database.allItems.Contains(item))
            {
                database.allItems.Add(item);
            }
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"ItemDatabase に追加しました。現在の登録数: {database.allItems.Count}");
    }
}