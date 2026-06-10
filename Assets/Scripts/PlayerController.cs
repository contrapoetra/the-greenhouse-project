using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public float walkSpeed = 6.5f;
    public float runSpeed = 9f;
    public float mouseSensitivity = 1f;
    public Transform cameraPivot;

    float xRotation = 0f;
    CharacterController controller;

    float yVelocity = 0f;
    float gravity = -9.81f;

    [Header("── Zoom Settings ──")]
    public float minZoomFOV = 10f;
    public float maxZoomFOV = 40f;
    public float zoomSmoothSpeed = 10f;
    public float scrollSensitivity = 5f;
    private float _defaultFOV;
    private float _currentZoomTarget = 30f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (Camera.main != null)
        {
            _defaultFOV = Camera.main.fieldOfView;
            _currentZoomTarget = 30f; // Initial zoom target
        }
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    void HandleMovement()
    {
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (controller.isGrounded && yVelocity < 0)
        {
            yVelocity = -2f;
        }

        yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * currentSpeed;
        velocity.y = yVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleZoom()
    {
        if (Camera.main == null) return;

        // Condition: Right Click is held, AND holding nothing, AND not viewing computer tasks
        bool isHoldingNothing = ClickEvent.Instance != null && ClickEvent.Instance.GetHeldItemType() == "none";
        bool isViewingComputer = DayProgressionManager.Instance != null && DayProgressionManager.Instance.IsViewingTasks;

        if (Input.GetMouseButton(1) && isHoldingNothing && !isViewingComputer)
        {
            // Scroll to adjust zoom target
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentZoomTarget -= scroll * scrollSensitivity * 10f;
                _currentZoomTarget = Mathf.Clamp(_currentZoomTarget, minZoomFOV, maxZoomFOV);
            }

            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, _currentZoomTarget, Time.deltaTime * zoomSmoothSpeed);
        }
        else if (!isViewingComputer) // Don't fight computer zoom if active
        {
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, _defaultFOV, Time.deltaTime * zoomSmoothSpeed);
        }
    }
}
