using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LoadTutorialScene))]
public class LoadTutorialSceneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty sceneNameProp = serializedObject.FindProperty("sceneName");
        List<string> sceneNames = GetBuildSceneNames();

        EditorGUILayout.LabelField("シーン設定", EditorStyles.boldLabel);
        if (sceneNames.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Build Settings にシーンがありません。\nFile → Build Settings でシーンを追加してください。",
                MessageType.Warning);
        }
        else
        {
            int currentIndex = sceneNames.IndexOf(sceneNameProp.stringValue);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup("遷移先シーン", currentIndex, sceneNames.ToArray());
            sceneNameProp.stringValue = sceneNames[newIndex];
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("playButtonSound"));

        serializedObject.ApplyModifiedProperties();
    }

    static List<string> GetBuildSceneNames()
    {
        var names = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            names.Add(Path.GetFileNameWithoutExtension(scene.path));
        }
        return names;
    }
}
