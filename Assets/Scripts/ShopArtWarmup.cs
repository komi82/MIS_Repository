using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ShopArtWarmup : MonoBehaviour
{
    static ShopArtWarmup instance;
    static ShopArtCache cache;
    public static bool IsReady => cache != null;
    public static bool IsLoading { get; private set; }
    public static double Seconds { get; private set; }
    public static double LongestFrameSeconds { get; private set; }
    ThreadPriority previousPriority;
    bool changedPriority;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        instance = null; cache = null; IsLoading = false; Seconds = 0; LongestFrameSeconds = 0;
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
    }
    static void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneNames.Arcade || instance != null) return;
        instance = new GameObject("ShopArtWarmup").AddComponent<ShopArtWarmup>();
        DontDestroyOnLoad(instance.gameObject);
        instance.StartCoroutine(instance.Delayed());
    }
    IEnumerator Delayed()
    {
        // Let the arcade's first screen and managers settle before doing background work.
        yield return new WaitForSecondsRealtime(8f);
        StartNow();
    }
    public static void StartNow()
    {
        if (instance != null && !IsReady && !IsLoading) instance.StartCoroutine(instance.Prepare());
    }
    IEnumerator Prepare()
    {
        IsLoading = true;
        previousPriority = Application.backgroundLoadingPriority;
        Application.backgroundLoadingPriority = ThreadPriority.Low;
        changedPriority = true;
        double start = Time.realtimeSinceStartupAsDouble;
        double last = start;
        var operation = Resources.LoadAsync<ShopArtCache>("ShopArtCache");
        while (!operation.isDone)
        {
            yield return null;
            double now = Time.realtimeSinceStartupAsDouble;
            LongestFrameSeconds = System.Math.Max(LongestFrameSeconds, now - last);
            last = now;
        }
        cache = operation.asset as ShopArtCache;
        Seconds = Time.realtimeSinceStartupAsDouble - start;
        RestorePriority();
        IsLoading = false;
        if (cache != null) Debug.Log("[ShopWarmup] ready in " + Seconds.ToString("F2") + " s; longest frame " + LongestFrameSeconds.ToString("F3") + " s");
    }
    void RestorePriority()
    {
        if (!changedPriority) return;
        Application.backgroundLoadingPriority = previousPriority;
        changedPriority = false;
    }
    void OnDestroy() { RestorePriority(); }
}
