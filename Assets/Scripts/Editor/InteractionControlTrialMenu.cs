#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class InteractionControlTrialMenu
{
    private const string MouseMenu = "MIS/操作比較/マウス操作（左：取得、右：設置、R：加工）";
    private const string KeyboardMenu = "MIS/操作比較/従来操作（F・E・R）";

    [MenuItem(MouseMenu)]
    public static void SelectMouse() => Select(true);
    [MenuItem(KeyboardMenu)]
    public static void SelectKeyboard() => Select(false);

    private static void Select(bool mouse)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(InteractionControlTrial.PreferenceKey, mouse);
        Debug.Log(mouse
            ? "[操作比較] 次のアーケード再生：左クリックで拾う・画面を開く、右クリックで置く、Rで加工開始。QTEは従来どおり。"
            : "[操作比較] 次のアーケード再生：Fで拾う・画面を開く、Eで置く、Rで加工開始。");
    }

    [MenuItem(MouseMenu, true)]
    private static bool ValidateMouse()
    {
        Menu.SetChecked(MouseMenu, SessionState.GetBool(InteractionControlTrial.PreferenceKey, true));
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }
    [MenuItem(KeyboardMenu, true)]
    private static bool ValidateKeyboard()
    {
        Menu.SetChecked(KeyboardMenu, !SessionState.GetBool(InteractionControlTrial.PreferenceKey, true));
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
#endif
