#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// A hidden gameplay cursor must never survive a return to the Unity editor.
[InitializeOnLoad]
public static class EditorCursorRecovery
{
    static EditorCursorRecovery()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += RestoreInEditMode;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        RestoreInEditMode();
        // Run once more after teardown callbacks that may still change cursor state.
        EditorApplication.delayCall += RestoreInEditMode;
    }

    public static void RestoreInEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    [MenuItem("MIS/カーソルを表示に戻す")]
    public static void RestoreFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("カーソルの復旧前に再生を停止してください。");
            return;
        }
        RestoreInEditMode();
        Debug.Log("[CursorRecovery] visible=" + Cursor.visible + ", lock=" + Cursor.lockState);
    }
}
#endif
