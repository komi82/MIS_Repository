using UnityEngine;
using UnityEditor;
using System.IO;

public class BoxColliderScalerForPrefabs : EditorWindow
{
    private string targetFolder = "Assets/Prefab/Item";
    private float scaleAmount = 0.2f;

    [MenuItem("Tools/BoxCollider Scaler (Prefabs)")]
    public static void ShowWindow()
    {
        GetWindow<BoxColliderScalerForPrefabs>("BoxCollider Scaler (Prefabs)");
    }

    private void OnGUI()
    {
        GUILayout.Label("BoxCollider サイズ拡張（プレハブ）", EditorStyles.boldLabel);
        targetFolder = EditorGUILayout.TextField("対象フォルダ", targetFolder);
        scaleAmount = EditorGUILayout.FloatField("拡張量", scaleAmount);

        if (GUILayout.Button("フォルダ内のプレハブに適用"))
        {
            ApplyToPrefabsInFolder(targetFolder, scaleAmount);
        }
    }

    private void ApplyToPrefabsInFolder(string folderPath, float amount)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab == null) continue;

            BoxCollider[] colliders = prefab.GetComponentsInChildren<BoxCollider>(true);
            foreach (BoxCollider col in colliders)
            {
                Undo.RecordObject(col, "Scale BoxCollider");
                col.size += Vector3.one * amount;
                EditorUtility.SetDirty(col);
            }

            // 保存
            PrefabUtility.SavePrefabAsset(prefab);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"BoxCollider サイズを {amount} 拡張しました（{guids.Length} 件）");
    }
}