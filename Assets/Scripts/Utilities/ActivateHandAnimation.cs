using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class ActivateHandAnimation : MonoBehaviour
{
    public InputActionReference triggerValueActionReference, gripValueActionReference;
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("Grip", gripValueActionReference.action.ReadValue<float>());
        animator.SetFloat("Trigger", triggerValueActionReference.action.ReadValue<float>());
    }
}
