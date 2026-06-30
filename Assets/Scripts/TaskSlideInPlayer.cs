using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TaskSlideInPlayer : MonoBehaviour
{
    Animator _anim;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        if (_anim == null) return;

        // Play the animation state named exactly "TaskSlideIn" in the controller
        _anim.Play("TaskSlideIn");
    }
}
