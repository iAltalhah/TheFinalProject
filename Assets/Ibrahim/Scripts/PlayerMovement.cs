using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input Actions")]
    [Tooltip("Vector2 action bound to WASD and the gamepad left stick.")]
    [SerializeField] private InputActionReference moveAction;

    [Tooltip("Button action bound to keyboard and gamepad jump buttons.")]
    [SerializeField] private InputActionReference jumpAction;

    [Tooltip("Button action bound to keyboard and gamepad run buttons.")]
    [SerializeField] private InputActionReference runAction;

    [Header("References")]
    [Tooltip("Assign the Main Camera child. If empty, the script tries to find one.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 5f;
    [SerializeField, Min(0f)] private float runSpeed = 8f;
    [Tooltip("How quickly the player turns toward the movement direction.")]
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private float verticalVelocity;
    [SerializeField] float flatVerticalVelocity = -2;

    bool isDoubleJumped;
    [SerializeField] float doubleJumpHeight = 1.6f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            cameraTransform = childCamera != null ? childCamera.transform : Camera.main?.transform;
        }
    }

    private void OnEnable()
    {
        SetActionsEnabled(true);
    }

    private void OnDisable()
    {
        SetActionsEnabled(false);
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Vector2 moveInput = ReadVector2(moveAction);

        Quaternion cameraYawRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);

        Vector3 moveDirection = cameraYawRotation * new Vector3(moveInput.x, 0f, moveInput.y);
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        bool isRunning = runAction != null && runAction.action.IsPressed();
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        bool isGrounded = characterController.isGrounded;
        bool jumpPressed = jumpAction != null && jumpAction.action.WasPressedThisFrame();
        bool isFalling = jumpAction.action.IsPressed() && isDoubleJumped;

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            isDoubleJumped = false;
        }

        if (jumpPressed)
        {
            if (isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            else if (!isDoubleJumped)
            {
                verticalVelocity = Mathf.Sqrt(doubleJumpHeight * -2f * gravity);
                isDoubleJumped = true;
            }
        }

        float currentGravity = gravity;


        if (isFalling && verticalVelocity <= 0f)
        {
            verticalVelocity = flatVerticalVelocity;
        }
        else
        {
            currentGravity = gravity;
        }

            verticalVelocity += currentGravity * Time.deltaTime;

        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    private static Vector2 ReadVector2(InputActionReference actionReference)
    {
        return actionReference == null ? Vector2.zero : actionReference.action.ReadValue<Vector2>();
    }

    private void SetActionsEnabled(bool enabled)
    {
        SetActionEnabled(moveAction, enabled);
        SetActionEnabled(jumpAction, enabled);
        SetActionEnabled(runAction, enabled);
    }

    private static void SetActionEnabled(InputActionReference actionReference, bool enabled)
    {
        if (actionReference == null)
        {
            return;
        }

        if (enabled)
        {
            actionReference.action.Enable();
        }
        else
        {
            actionReference.action.Disable();
        }
    }
}
