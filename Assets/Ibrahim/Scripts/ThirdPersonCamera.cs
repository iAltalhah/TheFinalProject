using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player transform that the camera follows.")]
    [SerializeField] private Transform target;

    [Tooltip("Vector2 action bound to mouse delta and the gamepad right stick.")]
    [SerializeField] private InputActionReference lookAction;

    [Header("Follow")]
    [Tooltip("World-space offset from the player's position to the camera pivot.")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("How quickly the camera pivot follows the player.")]
    [SerializeField, Min(0f)] private float followSmoothness = 15f;

    [Tooltip("Normal distance between the pivot and camera.")]
    [SerializeField, Min(0f)] private float cameraDistance = 5f;

    [Header("Rotation")]
    [Tooltip("Mouse rotation speed in degrees per pixel.")]
    [SerializeField, Min(0f)] private float mouseRotationSpeed = 0.1f;

    [Tooltip("Gamepad rotation speed in degrees per second.")]
    [SerializeField, Min(0f)] private float controllerRotationSpeed = 140f;

    [SerializeField, Range(-89f, 0f)] private float minimumPitch = -35f;
    [SerializeField, Range(0f, 89f)] private float maximumPitch = 70f;
    [SerializeField] private bool lockCursor = true;

    [Header("Collision")]
    [Tooltip("Radius of the sphere used to keep the camera away from walls and corners.")]
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.25f;

    [Tooltip("Extra space kept between the camera and a wall.")]
    [SerializeField, Min(0f)] private float wallOffset = 0.1f;

    [Tooltip("How quickly the camera returns to its normal distance after a wall clears.")]
    [SerializeField, Min(0f)] private float collisionSmoothness = 20f;

    [Tooltip("Layers that can block the camera. Exclude the Player layer.")]
    [SerializeField] private LayerMask collisionLayers = ~0;

    private Vector3 smoothedPivotPosition;
    private float currentDistance;
    private float yaw;
    private float pitch = 15f;

    private void Awake()
    {
        if (target != null)
        {
            smoothedPivotPosition = target.position + targetOffset;
        }

        currentDistance = cameraDistance;
        Vector3 startingAngles = transform.eulerAngles;
        yaw = startingAngles.y;
        pitch = NormalizeAngle(startingAngles.x);
    }

    private void OnEnable()
    {
        if (lookAction != null)
        {
            lookAction.action.Enable();
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (lookAction != null)
        {
            lookAction.action.Disable();
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        HandleRotation();
        FollowTarget();
        PositionCamera();
    }

    private void HandleRotation()
    {
        if (lookAction == null)
        {
            return;
        }

        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
        bool usingMouse = lookAction.action.activeControl?.device is Mouse;
        float speed = usingMouse ? mouseRotationSpeed : controllerRotationSpeed * Time.deltaTime;

        yaw += lookInput.x * speed;
        pitch -= lookInput.y * speed;
        pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
    }

    private void FollowTarget()
    {
        Vector3 desiredPivotPosition = target.position + targetOffset;
        float followT = 1f - Mathf.Exp(-followSmoothness * Time.deltaTime);
        smoothedPivotPosition = Vector3.Lerp(smoothedPivotPosition, desiredPivotPosition, followT);
    }

    private void PositionCamera()
    {
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 directionToCamera = cameraRotation * Vector3.back;
        float desiredDistance = cameraDistance;

        // Cast a sphere from the player-side pivot toward the camera's normal position.
        // A sphere has thickness, so it prevents the camera from clipping through corners
        // more reliably than a single ray. If it hits a wall, the allowed distance becomes
        // the hit distance minus wallOffset, which stops the camera just before the surface.
        if (Physics.SphereCast(
                smoothedPivotPosition,
                collisionRadius,
                directionToCamera,
                out RaycastHit hit,
                cameraDistance,
                collisionLayers,
                QueryTriggerInteraction.Ignore))
        {
            desiredDistance = Mathf.Max(0f, hit.distance - wallOffset);
        }

        // Move inward immediately so a newly encountered wall can never sit between
        // the camera and pivot. When the path clears, ease back to the normal distance.
        float collisionT = 1f - Mathf.Exp(-collisionSmoothness * Time.deltaTime);
        currentDistance = desiredDistance < currentDistance
            ? desiredDistance
            : Mathf.Lerp(currentDistance, desiredDistance, collisionT);

        transform.SetPositionAndRotation(
            smoothedPivotPosition + directionToCamera * currentDistance,
            cameraRotation);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    private void OnValidate()
    {
        cameraDistance = Mathf.Max(cameraDistance, collisionRadius + wallOffset);
        minimumPitch = Mathf.Min(minimumPitch, maximumPitch);
    }
}
