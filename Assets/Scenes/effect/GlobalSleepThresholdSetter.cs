using UnityEngine;

public class GlobalSleepThresholdSetter : MonoBehaviour
{
    public float sleepThreshold = 0f;

    void Awake()
    {
        Rigidbody[] bodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        foreach (var rb in bodies)
        {
            rb.sleepThreshold = sleepThreshold;
        }
    }
}