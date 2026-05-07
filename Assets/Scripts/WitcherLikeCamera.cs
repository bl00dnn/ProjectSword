using UnityEngine;

[DisallowMultipleComponent]
public sealed class WitcherLikeCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetName = "PlayerCapsule";
    [SerializeField] private Vector3 cameraOffset = new Vector3(0.45f, 1.5f, -1.5f);

    [Header("Orbit")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 55f;
    [SerializeField] private bool lockCursorOnPlay = true;

    [Header("Feel")]
    [SerializeField] private float followSmoothTime = 0.08f;
    [SerializeField] private float rotationSmooth = 16f;
    [SerializeField] private float collisionRadius = 0.24f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private Vector3 followVelocity;
    private float yaw;
    private float pitch = 18f;

    public void Configure(Transform followTarget, Vector3 offset, float sensitivity, float smoothTime, float minimumPitch, float maximumPitch, bool lockCursor)
    {
        target = followTarget;
        cameraOffset = offset;
        mouseSensitivity = sensitivity;
        followSmoothTime = smoothTime;
        minPitch = minimumPitch;
        maxPitch = maximumPitch;
        lockCursorOnPlay = lockCursor;
        SnapBehindTarget();
    }

    private void Start()
    {
        ResolveTargetIfNeeded();

        if (target != null)
        {
            SnapBehindTarget();
        }

        if (lockCursorOnPlay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        ResolveTargetIfNeeded();

        if (target == null)
        {
            return;
        }

        Vector2 lookInput = SergiusInput.ReadLook();
        yaw += lookInput.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - lookInput.y * mouseSensitivity, minPitch, maxPitch);

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + Vector3.up * Mathf.Max(0.2f, cameraOffset.y);
        Vector3 desiredPosition = pivot + orbitRotation * new Vector3(cameraOffset.x, 0f, cameraOffset.z);
        desiredPosition = ResolveCollision(pivot, desiredPosition);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);

        Quaternion desiredRotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime));
    }

    private void SnapBehindTarget()
    {
        if (target == null)
        {
            return;
        }

        yaw = target.eulerAngles.y;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + Vector3.up * Mathf.Max(0.2f, cameraOffset.y);
        transform.position = ResolveCollision(pivot, pivot + orbitRotation * new Vector3(cameraOffset.x, 0f, cameraOffset.z));
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
    }

    private Vector3 ResolveCollision(Vector3 pivot, Vector3 desiredPosition)
    {
        Vector3 toCamera = desiredPosition - pivot;
        float distance = toCamera.magnitude;
        if (distance <= 0.01f)
        {
            return desiredPosition;
        }

        Vector3 direction = toCamera / distance;
        if (Physics.SphereCast(pivot, collisionRadius, direction, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            return pivot + direction * Mathf.Max(0.2f, hit.distance - collisionRadius);
        }

        return desiredPosition;
    }

    private void ResolveTargetIfNeeded()
    {
        if (target != null || string.IsNullOrWhiteSpace(targetName))
        {
            return;
        }

        GameObject targetObject = GameObject.Find(targetName);
        if (targetObject != null)
        {
            target = targetObject.transform;
        }
    }
}
