using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class SergiusThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.65f;
    [SerializeField] private float runSpeed = 3.15f;
    [SerializeField] private float acceleration = 16f;
    [SerializeField] private float rotationSmoothTime = 0.08f;
    [SerializeField] private float gravity = -22f;
    [SerializeField] private float jumpHeight = 1.25f;

    [Header("References")]
    [SerializeField] private Camera followCamera;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private SergiusAnimationDriver animationDriver;

    private float currentSpeed;
    private float verticalVelocity;
    private float rotationVelocity;
    private float characterHeight = 1.95f;

    public void Configure(CharacterController controller, SergiusAnimationDriver driver, float height)
    {
        characterController = controller;
        animationDriver = driver;
        characterHeight = height;
    }

    public void SetCamera(Camera cameraToFollow)
    {
        followCamera = cameraToFollow;
    }

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (animationDriver == null)
        {
            animationDriver = GetComponent<SergiusAnimationDriver>();
        }

        if (followCamera == null)
        {
            followCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (characterController == null)
        {
            return;
        }

        Vector2 moveInput = SergiusInput.ReadMove();
        bool sprintPressed = SergiusInput.ReadSprint();
        bool jumpPressed = SergiusInput.ReadJump();
        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);

        Vector3 moveDirection = GetCameraRelativeMove(moveInput);
        float targetSpeed = (sprintPressed ? runSpeed : walkSpeed) * inputMagnitude;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (characterController.isGrounded && jumpPressed)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animationDriver?.TriggerJump();
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        float animationAmount = Mathf.InverseLerp(0f, runSpeed, currentSpeed);
        animationDriver?.SetMoveAmount(animationAmount);
    }

    private Vector3 GetCameraRelativeMove(Vector2 moveInput)
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        if (followCamera != null)
        {
            forward = followCamera.transform.forward;
            right = followCamera.transform.right;
        }

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = forward * moveInput.y + right * moveInput.x;
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.95f, 0.8f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * characterHeight, 0.1f);
    }
}
