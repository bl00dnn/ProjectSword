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
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float jumpVelocity = 8f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float airAcceleration = 28f;
    [SerializeField] private float groundCheckDistance = 0.18f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("3D Movement")]
    [SerializeField] private bool faceMoveDirection = true;
    [SerializeField] private float turnSpeed = 16f;

    private Rigidbody body;
    private Collider bodyCollider;
    private PhysicsMaterial frictionlessMaterial;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private bool sprintHeld;
    private bool jumpQueued;
    private const string AcidHazardName = "Acid";

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();

        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        bodyCollider.material = GetFrictionlessMaterial();
    }

    private void Update()
    {
        moveInput = ReadMoveInput();
        sprintHeld = IsSprintHeld();

        if (WasJumpPressed())
        {
            jumpQueued = true;
        }

        moveDirection = GetCameraRelativeMoveDirection(moveInput);
        if (faceMoveDirection && moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        Vector3 velocity = body.linearVelocity;
        bool grounded = IsGrounded();
        float currentAcceleration = grounded ? acceleration : airAcceleration;
        float currentMoveSpeed = sprintHeld ? moveSpeed * sprintMultiplier : moveSpeed;
        Vector3 targetVelocity = moveDirection * currentMoveSpeed;
        velocity.x = Mathf.MoveTowards(velocity.x, targetVelocity.x, currentAcceleration * Time.fixedDeltaTime);
        velocity.z = Mathf.MoveTowards(velocity.z, targetVelocity.z, currentAcceleration * Time.fixedDeltaTime);

        if (jumpQueued && grounded)
        {
            velocity.y = jumpVelocity;
        }

        jumpQueued = false;
        body.linearVelocity = velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsAcidHazard(collision.gameObject))
        {
            Respawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsAcidHazard(other.gameObject))
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        PlayerSpawnPoint spawnPoint = FindAnyObjectByType<PlayerSpawnPoint>();
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.SpawnPosition : transform.position;

        jumpQueued = false;
        moveInput = Vector2.zero;

        body.position = spawnPosition;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        transform.position = spawnPosition;
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 value = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed)
            {
                value.x -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                value.x += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                value.y -= 1f;
            }

            if (keyboard.wKey.isPressed)
            {
                value.y += 1f;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.A))
        {
            value.x -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            value.x += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            value.y -= 1f;
        }

        if (Input.GetKey(KeyCode.W))
        {
            value.y += 1f;
        }
#endif

        return Vector2.ClampMagnitude(value, 1f);
    }

    private Vector3 GetCameraRelativeMoveDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = right * input.x + forward * input.y;
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }

    private bool WasJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space))
        {
            return true;
        }
#endif

        return false;
    }

    private bool IsSprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
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
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + radius + 0.02f, bounds.center.z);

        return Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _,
            groundCheckDistance + 0.04f,
            groundMask,
            QueryTriggerInteraction.Ignore);
    }

    private static bool IsAcidHazard(GameObject candidate)
    {
        Transform current = candidate.transform;
        while (current != null)
        {
            if (current.name.Contains(AcidHazardName))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
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
