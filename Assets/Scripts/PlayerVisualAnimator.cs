using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerVisualAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private float speedDampTime = 0.12f;
    [SerializeField] private float fullWalkSpeed = 4f;
    [SerializeField] private string locomotionStateName = "Locomotion";
    [SerializeField] private bool keepVisualRootFixed = true;

    private Rigidbody body;
    private Transform visualRoot;
    private Vector3 visualLocalPosition;
    private Vector3 visualLocalScale;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            visualRoot = animator.transform;
            visualLocalPosition = visualRoot.localPosition;
            visualLocalScale = visualRoot.localScale;
        }
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        Vector3 velocity = body.linearVelocity;
        velocity.y = 0f;
        float normalizedSpeed = Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.01f, fullWalkSpeed));
        animator.SetFloat(speedParameter, normalizedSpeed, speedDampTime, Time.deltaTime);
        KeepLocomotionLooping(normalizedSpeed);
    }

    private void LateUpdate()
    {
        if (!keepVisualRootFixed || visualRoot == null)
        {
            return;
        }

        visualRoot.localPosition = visualLocalPosition;
        visualRoot.localScale = visualLocalScale;
    }

    private void KeepLocomotionLooping(float normalizedSpeed)
    {
        if (normalizedSpeed < 0.05f)
        {
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!state.IsName(locomotionStateName))
        {
            animator.Play(locomotionStateName, 0, 0f);
            return;
        }

        if (state.normalizedTime >= 1f && !animator.IsInTransition(0))
        {
            animator.Play(locomotionStateName, 0, state.normalizedTime % 1f);
        }
    }
}
