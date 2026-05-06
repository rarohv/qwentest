using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.2f;

    private CharacterController controller;
    private Vector3 velocity;
    private Transform cameraTransform;
    private bool isGrounded;
    private bool movementEnabled = true;

    public float WalkSpeed => walkSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        isGrounded = controller.isGrounded;

        if (movementEnabled && cameraTransform != null)
        {
            Move();
            HandleJump();
        }
        ApplyGravity();
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            Vector3 moveDir = CalculateWorldDirection(direction);
            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            controller.Move(moveDir * speed * Time.deltaTime);
        }
    }

    Vector3 CalculateWorldDirection(Vector3 inputDir)
    {
        float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
        return Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
    }

    void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public float GetWalkSpeed()
    {
        return walkSpeed;
    }

    public void SetWalkSpeed(float speed)
    {
        walkSpeed = speed;
    }
}
