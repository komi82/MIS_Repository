#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

// Explicit local test request only. No commands are executed on normal project startup.
[InitializeOnLoad]
public static class MisValidationRequest
{
    static double nextCheck;
    const string RequestPath = "Logs/mis-validation.request";
    static MisValidationRequest() => EditorApplication.update += Check;
    static void Check()
    {
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 2;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
        string request = File.ReadAllText(RequestPath).Trim();
        File.Delete(RequestPath);
        try
        {
            switch (request)
            {
                case "tutorial": TutorialMouseValidation.Run(); break;
                case "arcade": InteractionControlTrialValidation.Run(); break;
                case "content": KouaContentValidation.Run(); break;
                default: throw new InvalidOperationException("Unknown validation request: " + request);
            }
        }
        catch (Exception ex) { File.WriteAllText("Logs/mis-validation-request-error.txt", ex.ToString()); }
    }
}
#endif
