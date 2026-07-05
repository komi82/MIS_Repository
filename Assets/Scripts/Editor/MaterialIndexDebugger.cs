using UnityEngine;
using UnityEditor;

/// <summary>
/// オブジェクトのマテリアルインデックスをデバッグ表示するエディタスクリプト
/// </summary>
public class MaterialIndexDebugger : EditorWindow
{
    private GameObject selectedObject;
    private Vector2 scrollPosition;

    [MenuItem("Window/Material Index Debugger")]
    public static void ShowWindow()
    {
        GetWindow<MaterialIndexDebugger>("Material Index Debugger");
    }

    void OnGUI()
    {
        GUILayout.Label("マテリアルインデックス確認ツール", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        selectedObject = (GameObject)EditorGUILayout.ObjectField("チェック対象オブジェクト", selectedObject, typeof(GameObject), true);

        if (selectedObject == null)
        {
            EditorGUILayout.HelpBox("シーンからオブジェクトを選択してください", MessageType.Info);
            return;
        }

        Renderer renderer = selectedObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            EditorGUILayout.HelpBox("Rendererコンポーネントが見つかりません", MessageType.Warning);
            return;
        }

        Material[] materials = renderer.materials;

        EditorGUILayout.LabelField($"マテリアル数: {materials.Length}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < materials.Length; i++)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"Index: {i}", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("マテリアル", materials[i], typeof(Material), false);
            
            if (materials[i] != null)
            {
                EditorGUILayout.LabelField($"名前: {materials[i].name}");
                EditorGUILayout.LabelField($"シェーダー: {materials[i].shader.name}");
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        GUILayout.EndScrollView();
    }
}
