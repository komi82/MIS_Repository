using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class ItemDatabaseBuilder
{
    [MenuItem("Tools/Build ItemDatabase")]
    public static void BuildDatabase()
    {
        string folderPath = "Assets/Prefab/Item";
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { folderPath });

        var items = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .ToList();

        string databasePath = "Assets/Scripts/ItemDatabase.asset";
        var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(databasePath);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(database, databasePath);
        }

        database.allItems = items;
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"ItemDatabase ‚ğXV‚µ‚Ü‚µ‚½B“o˜^”: {items.Count}");
    }
}