using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class SideScrollerCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 followOffset = new Vector2(0f, 1.2f);
    [SerializeField] private float cameraDistance = 18f;
    [SerializeField] private float fieldOfView = 45f;
    [SerializeField] private float smoothTime = 0.16f;

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
        transform.rotation = Quaternion.identity;
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;

        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = GetDesiredPosition();

        if (!hasSnappedToTarget)
        {
            transform.position = desiredPosition;
            followVelocity = Vector3.zero;
            hasSnappedToTarget = true;
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, smoothTime);
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.rotation = Quaternion.identity;
        transform.position = GetDesiredPosition();
        followVelocity = Vector3.zero;
        hasSnappedToTarget = true;
    }

    private Vector3 GetDesiredPosition()
    {
        return new Vector3(
            target.position.x + followOffset.x,
            target.position.y + followOffset.y,
            target.position.z - Mathf.Abs(cameraDistance));
    }
}
