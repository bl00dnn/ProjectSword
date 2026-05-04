using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Camera))]
public sealed class SideScrollerCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float fieldOfView = 45f;
    [SerializeField] private float smoothTime = 0.16f;
    [SerializeField] private float rotationSmoothSpeed = 12f;
    [SerializeField] private float distance = 9f;
    [SerializeField] private float minDistance = 4f;
    [SerializeField] private float maxDistance = 18f;
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float pitch = 28f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private float mouseSensitivity = 0.16f;
    [SerializeField] private float zoomSensitivity = 1.5f;
    [SerializeField] private bool rotateOnlyWhileRightMouseHeld;

    private Camera sideCamera;
    private Vector3 followVelocity;
    private bool hasSnappedToTarget;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Awake()
    {
        sideCamera = GetComponent<Camera>();
        sideCamera.orthographic = false;
        sideCamera.fieldOfView = fieldOfView;
        ReadCurrentOrbitFromTransform();
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        UpdateCursorLock();

        if (target == null)
        {
            return;
        }

        ReadCameraInput();
        Vector3 desiredPosition = GetDesiredPosition();

        if (!hasSnappedToTarget)
        {
            transform.position = desiredPosition;
            followVelocity = Vector3.zero;
            hasSnappedToTarget = true;
            RotateToTarget(true);
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, smoothTime);
        RotateToTarget();
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.position = GetDesiredPosition();
        followVelocity = Vector3.zero;
        hasSnappedToTarget = true;
        RotateToTarget(true);
    }

    private Vector3 GetDesiredPosition()
    {
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 orbitOffset = orbitRotation * new Vector3(0f, 0f, -distance);
        return target.position + lookOffset + orbitOffset;
    }

    private void RotateToTarget(bool instant = false)
    {
        Vector3 lookPosition = target.position + lookOffset;
        Vector3 direction = lookPosition - transform.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = instant
            ? targetRotation
            : Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    private void ReadCameraInput()
    {
        Vector2 mouseDelta = Vector2.zero;
        float scroll = 0f;
        bool canRotate = !rotateOnlyWhileRightMouseHeld;

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            mouseDelta += mouse.delta.ReadValue();
            scroll += mouse.scroll.ReadValue().y / 120f;
            canRotate |= mouse.rightButton.isPressed;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        mouseDelta += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 20f;
        scroll += Input.mouseScrollDelta.y;
        canRotate |= Input.GetMouseButton(1);
#endif

        if (canRotate)
        {
            yaw += mouseDelta.x * mouseSensitivity;
            pitch -= mouseDelta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSensitivity, minDistance, maxDistance);
        }
    }

    private static void UpdateCursorLock()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif
    }

    private void ReadCurrentOrbitFromTransform()
    {
        if (target == null)
        {
            return;
        }

        Vector3 toCamera = transform.position - (target.position + lookOffset);
        if (toCamera.sqrMagnitude < 0.001f)
        {
            return;
        }

        distance = Mathf.Clamp(toCamera.magnitude, minDistance, maxDistance);
        Vector3 directionToTarget = -toCamera.normalized;
        Vector3 euler = Quaternion.LookRotation(directionToTarget, Vector3.up).eulerAngles;
        yaw = euler.y;
        pitch = NormalizeAngle(euler.x);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
