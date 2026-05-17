using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ConditionalSceneTransition))]
public class ConditionalSceneTransitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty triggerMode = serializedObject.FindProperty("triggerMode");
        EditorGUILayout.PropertyField(triggerMode);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("表示・遷移", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionUI"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sceneLoader"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBeforeLoad"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerOnce"));

        var mode = (ConditionalSceneTransition.TriggerMode)triggerMode.enumValueIndex;

        EditorGUILayout.Space();
        switch (mode)
        {
            case ConditionalSceneTransition.TriggerMode.PlayerTouch:
                EditorGUILayout.LabelField("プレイヤー接触", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("playerTag"));
                EditorGUILayout.HelpBox(
                    "この GameObject に Collider（Is Trigger）を付けてください。\n" +
                    "プレイヤー側には CharacterController または Rigidbody が必要です。",
                    MessageType.Info);
                break;

            case ConditionalSceneTransition.TriggerMode.ButtonPress:
                EditorGUILayout.LabelField("ボタン入力", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pressKey"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pollKeyInUpdate"));
                if (!serializedObject.FindProperty("pollKeyInUpdate").boolValue)
                {
                    EditorGUILayout.HelpBox(
                        "UI Button の On Click () に\nConditionalSceneTransition → OnButtonPressed() を登録してください。",
                        MessageType.Info);
                }
                break;

            case ConditionalSceneTransition.TriggerMode.VariableChange:
                EditorGUILayout.LabelField("変数変化", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("comparison"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetValue"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("currentValue"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pollVariableInUpdate"));
                EditorGUILayout.HelpBox(
                    "他スクリプトから NotifyVariableChanged(値) または SetVariableValue(値) を呼んでください。\n" +
                    "例: RequestManager の完了時に RequestCompleted を渡す。",
                    MessageType.Info);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
