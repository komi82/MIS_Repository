using System.Collections;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(TutorialSequence))]
public class TutorialSequenceFinishFade : MonoBehaviour
{
    [Tooltip("Finish 後に移動するシーン名（Build Settings のシーン名）")]
    public string sceneToLoad;

    [Tooltip("Finish を検出するためのポーリング間隔（秒）")]
    public float pollInterval = 0.1f;

    TutorialSequence tutorial;

    void Awake()
    {
        tutorial = GetComponent<TutorialSequence>();
    }

    void OnEnable()
    {
        StartCoroutine(MonitorTutorial());
    }

    IEnumerator MonitorTutorial()
    {
        // wait until tutorial becomes active
        yield return new WaitUntil(() => TutorialSequence.IsActive);

        // wait until tutorial finishes
        yield return new WaitUntil(() => !TutorialSequence.IsActive);

        // try to hide postTutorialWindow on the original component (prevent showing)
        var fi = typeof(TutorialSequence).GetField("postTutorialWindow", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi != null)
        {
            var obj = fi.GetValue(tutorial) as GameObject;
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        // Load target scene with FadeManager if available, otherwise normal load
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.LoadSceneWithFade(sceneToLoad);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
            }
        }
    }
}
