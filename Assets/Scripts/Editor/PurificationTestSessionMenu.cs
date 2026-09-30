#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PurificationTestSessionMenu
{
    private const string Key = "MIS.PurificationOnlyTest";
    private const string Menu = "MIS/テスト/浄化依頼だけが出るテストを開始";

    static PurificationTestSessionMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            SessionState.EraseBool(Key);
    }

    [MenuItem(Menu)]
    private static void StartTest()
    {
        if (!CanStart()) return;
        SessionState.SetBool(Key, true);
        Debug.Log("[浄化計測] 浄化依頼のみのテストを開始します。再生停止で通常の抽選に戻ります。");
        EditorApplication.isPlaying = true;
    }

    [MenuItem(Menu, true)]
    private static bool CanStart()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
            SceneManager.GetActiveScene().name == SceneNames.Arcade;
    }
}
#endif