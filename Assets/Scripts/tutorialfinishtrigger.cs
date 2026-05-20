using UnityEngine;

public class tutorialfinishtrigger : MonoBehaviour
{
    public static void OnSlot0ItemAdded()
    {
        ConditionalSceneTransition.TriggerTransitionStatic();
    }
}
