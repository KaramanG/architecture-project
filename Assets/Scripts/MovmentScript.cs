using UnityEngine;

public class MovementScript : MonoBehaviour
{
    private Rigidbody rb;
    public float speed = 0.5f;
    public float jumpForce = 10f; // ���� ������
    private Vector3 moveVector;
    [SerializeField] private Animator animator;

    private float targetRotationY;
    public float rotationSpeed = 5f;

    private bool isJumping = false; // ���� ��� ������������ ��������� ������

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // �������� ���������
        moveVector.x = Input.GetAxis("Horizontal");
        moveVector.z = Input.GetAxis("Vertical");

        bool isMoving = moveVector.x != 0 || moveVector.z != 0;
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
        }

        // ������� ������� ��������� ��� ������� A ��� D
        if (Input.GetKeyDown(KeyCode.A))
        {
            targetRotationY -= 90f;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            targetRotationY += 90f;
        }

        Quaternion targetRotation = Quaternion.Euler(0, targetRotationY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        moveVector = transform.TransformDirection(moveVector);
        rb.MovePosition(rb.position + moveVector * speed * Time.deltaTime);

        // ������
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            Jump();
        }

        // �������� �����������
        if (isJumping && IsGrounded())
        {
            isJumping = false; // ���������� ���� ������
            if (animator != null)
            {
                animator.SetBool("IsJumping", false); // ������������� �������� ������
            }
        }
    }

    private void Jump()
    {
        isJumping = true; // ������������� ���� ������
        if (animator != null)
        {
            animator.SetBool("IsJumping", true); // ��������� �������� ������
        }
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // ���������� ������������ �������� ����� �������
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // ��������� ���� ������
    }

    private bool IsGrounded()
    {
        float radius = 0.5f; // ������ ����� ��� �������� �����
        float distance = 1.1f; // ���������� �� �����
        return Physics.SphereCast(transform.position, radius, Vector3.down, out _, distance);
    }
}