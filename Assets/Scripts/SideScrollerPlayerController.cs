using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class SideScrollerPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpVelocity = 8f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float airAcceleration = 28f;
    [SerializeField] private float groundCheckDistance = 0.18f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("2.5D Plane")]
    [SerializeField] private float lockedZ = 0f;
    [SerializeField] private bool faceMoveDirection = true;

    private Rigidbody body;
    private Collider bodyCollider;
    private PhysicsMaterial frictionlessMaterial;
    private float moveInput;
    private bool jumpQueued;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();

        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        bodyCollider.material = GetFrictionlessMaterial();

        lockedZ = transform.position.z;
        SnapToMovementPlane();
    }

    private void Update()
    {
        moveInput = ReadHorizontalInput();

        if (WasJumpPressed())
        {
            jumpQueued = true;
        }

        if (faceMoveDirection && Mathf.Abs(moveInput) > 0.01f)
        {
            transform.rotation = Quaternion.Euler(0f, moveInput > 0f ? 90f : -90f, 0f);
        }
    }

    private void FixedUpdate()
    {
        SnapToMovementPlane();

        Vector3 velocity = body.linearVelocity;
        bool grounded = IsGrounded();
        float currentAcceleration = grounded ? acceleration : airAcceleration;
        float targetX = moveInput * moveSpeed;
        velocity.x = Mathf.MoveTowards(velocity.x, targetX, currentAcceleration * Time.fixedDeltaTime);
        velocity.z = 0f;

        if (jumpQueued && grounded)
        {
            velocity.y = jumpVelocity;
        }

        jumpQueued = false;
        body.linearVelocity = velocity;
    }

    private float ReadHorizontalInput()
    {
        float value = 0f;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed)
            {
                value -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                value += 1f;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.A))
        {
            value -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            value += 1f;
        }
#endif

        return Mathf.Clamp(value, -1f, 1f);
    }

    private bool WasJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.sKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame))
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
        {
            return true;
        }
#endif

        return false;
    }

    private bool IsGrounded()
    {
        Bounds bounds = bodyCollider.bounds;
        float radius = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.9f);
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + radius + 0.02f, lockedZ);

        return Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _,
            groundCheckDistance + 0.04f,
            groundMask,
            QueryTriggerInteraction.Ignore);
    }

    private void SnapToMovementPlane()
    {
        Vector3 position = transform.position;
        if (!Mathf.Approximately(position.z, lockedZ))
        {
            position.z = lockedZ;
            transform.position = position;
        }
    }

    private PhysicsMaterial GetFrictionlessMaterial()
    {
        if (frictionlessMaterial != null)
        {
            return frictionlessMaterial;
        }

        frictionlessMaterial = new PhysicsMaterial("Player_Frictionless")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        return frictionlessMaterial;
    }
}
