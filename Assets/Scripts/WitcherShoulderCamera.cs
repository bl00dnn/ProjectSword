using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class WitcherShoulderCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.58f, 0f);
    [SerializeField] private bool autoAimAtUpperBody = true;
    [SerializeField, Range(0.65f, 0.95f)] private float upperBodyAimRatio = 0.82f;

    [Header("Shoulder Rig")]
    [SerializeField] private float distance = 4.8f;
    [SerializeField] private float minDistance = 1.25f;
    [SerializeField] private float shoulderSide = 0.72f;
    [SerializeField] private float shoulderHeight = 0.15f;
    [SerializeField] private float pitch = 14f;
    [SerializeField] private float minPitch = -18f;
    [SerializeField] private float maxPitch = 48f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.11f;
    [SerializeField] private float gamepadSensitivity = 120f;
    [SerializeField] private bool invertY;
    [SerializeField] private float recenterDelay = 0.75f;
    [SerializeField] private float recenterSpeed = 110f;
    [SerializeField] private float recenterMoveSpeed = 0.8f;

    [Header("Smoothing")]
    [SerializeField] private float fieldOfView = 56f;
    [SerializeField] private float lookSmoothTime = 0.05f;
    [SerializeField] private float positionSmoothTime = 0.045f;
    [SerializeField] private float rotationSharpness = 26f;
    [SerializeField] private float collisionReturnSpeed = 18f;

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.28f;
    [SerializeField] private float wallPadding = 0.18f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private Camera controlledCamera;
    private Rigidbody targetBody;
    private Vector3 lookVelocity;
    private Vector3 positionVelocity;
    private Vector3 currentLookPosition;
    private float yaw;
    private float currentDistance;
    private float manualLookTimer = float.PositiveInfinity;
    private bool snapped;
    private readonly RaycastHit[] hits = new RaycastHit[12];

    public Transform Target
    {
        get => target;
        set
        {
            target = value;
            targetBody = target != null ? target.GetComponent<Rigidbody>() : null;
            snapped = false;
        }
    }

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        controlledCamera.orthographic = false;
        controlledCamera.fieldOfView = fieldOfView;
        currentDistance = distance;

        if (target != null)
        {
            targetBody = target.GetComponent<Rigidbody>();
            yaw = target.eulerAngles.y;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        CameraLookInput input = ReadLookInput();
        ApplyManualLook(input);
        RecenterBehindMovement();

        Vector3 desiredLookPosition = GetLookPosition();
        currentLookPosition = snapped
            ? Vector3.SmoothDamp(currentLookPosition, desiredLookPosition, ref lookVelocity, lookSmoothTime)
            : desiredLookPosition;

        Vector3 desiredPosition = GetCameraPosition(currentLookPosition);
        if (!snapped)
        {
            transform.position = desiredPosition;
            positionVelocity = Vector3.zero;
            snapped = true;
            RotateToLookPosition(true);
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);
        RotateToLookPosition(false);
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        targetBody = target.GetComponent<Rigidbody>();
        yaw = target.eulerAngles.y;
        currentDistance = distance;
        currentLookPosition = GetLookPosition();
        transform.position = GetCameraPosition(currentLookPosition);
        lookVelocity = Vector3.zero;
        positionVelocity = Vector3.zero;
        snapped = true;
        RotateToLookPosition(true);
    }

    private void ApplyManualLook(CameraLookInput input)
    {
        Vector2 lookDelta = input.MouseDelta * mouseSensitivity;
        lookDelta += input.GamepadLook * (gamepadSensitivity * Time.deltaTime);

        if (lookDelta.sqrMagnitude < 0.0001f)
        {
            manualLookTimer += Time.deltaTime;
            return;
        }

        manualLookTimer = 0f;
        yaw += lookDelta.x;
        pitch += lookDelta.y * (invertY ? 1f : -1f);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void RecenterBehindMovement()
    {
        if (manualLookTimer < recenterDelay)
        {
            return;
        }

        Vector3 velocity = GetTargetPlanarVelocity();
        if (velocity.magnitude < recenterMoveSpeed)
        {
            return;
        }

        float movementYaw = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
        yaw = Mathf.MoveTowardsAngle(yaw, movementYaw, recenterSpeed * Time.deltaTime);
    }

    private Vector3 GetLookPosition()
    {
        if (!autoAimAtUpperBody)
        {
            return target.position + lookOffset;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return target.position + lookOffset;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float aimY = Mathf.Lerp(bounds.min.y, bounds.max.y, upperBodyAimRatio);
        return new Vector3(target.position.x + lookOffset.x, aimY, target.position.z + lookOffset.z);
    }

    private Vector3 GetCameraPosition(Vector3 lookPosition)
    {
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 shoulderOffset = orbit * new Vector3(shoulderSide, shoulderHeight, 0f);
        Vector3 backDirection = orbit * Vector3.back;
        float safeDistance = ResolveDistance(lookPosition + shoulderOffset, backDirection);
        return lookPosition + shoulderOffset + backDirection * safeDistance;
    }

    private float ResolveDistance(Vector3 shoulderPosition, Vector3 backDirection)
    {
        int hitCount = Physics.SphereCastNonAlloc(
            shoulderPosition,
            collisionRadius,
            backDirection,
            hits,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        float safeDistance = distance;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(target))
            {
                continue;
            }

            safeDistance = Mathf.Min(safeDistance, hit.distance);
        }

        safeDistance = Mathf.Clamp(safeDistance - wallPadding, minDistance, distance);
        float speed = safeDistance < currentDistance ? collisionReturnSpeed * 2f : collisionReturnSpeed;
        currentDistance = Mathf.MoveTowards(currentDistance, safeDistance, speed * Time.deltaTime);
        return currentDistance;
    }

    private void RotateToLookPosition(bool instant)
    {
        Vector3 direction = currentLookPosition - transform.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = instant
            ? targetRotation
            : Quaternion.Slerp(transform.rotation, targetRotation, rotationSharpness * Time.deltaTime);
    }

    private Vector3 GetTargetPlanarVelocity()
    {
        if (targetBody != null)
        {
            Vector3 velocity = targetBody.linearVelocity;
            velocity.y = 0f;
            return velocity;
        }

        return Vector3.zero;
    }

    private static CameraLookInput ReadLookInput()
    {
        CameraLookInput input = default;

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            input.MouseDelta += mouse.delta.ReadValue();
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            input.GamepadLook += gamepad.rightStick.ReadValue();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        input.MouseDelta += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 20f;
#endif

        return input;
    }

    private struct CameraLookInput
    {
        public Vector2 MouseDelta;
        public Vector2 GamepadLook;
    }
}
