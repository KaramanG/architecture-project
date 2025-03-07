using UnityEngine;

public class MovementScript : MonoBehaviour
{
    private Rigidbody rb;
    public float speed = 0.5f;
    public float jumpForce = 10f; // Сила прыжка
    private Vector3 moveVector;
    [SerializeField] private Animator animator;

    private float targetRotationY;
    public float rotationSpeed = 5f;

    private bool isJumping = false; // Флаг для отслеживания состояния прыжка

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
        // Движение персонажа
        moveVector.x = Input.GetAxis("Horizontal");
        moveVector.z = Input.GetAxis("Vertical");

        bool isMoving = moveVector.x != 0 || moveVector.z != 0;
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
        }

        // Плавный поворот персонажа при нажатии A или D
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

        // Прыжок
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            Jump();
        }

        // Проверка приземления
        if (isJumping && IsGrounded())
        {
            isJumping = false; // Сбрасываем флаг прыжка
            if (animator != null)
            {
                animator.SetBool("IsJumping", false); // Останавливаем анимацию прыжка
            }
        }
    }

    private void Jump()
    {
        isJumping = true; // Устанавливаем флаг прыжка
        if (animator != null)
        {
            animator.SetBool("IsJumping", true); // Запускаем анимацию прыжка
        }
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Сбрасываем вертикальную скорость перед прыжком
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // Применяем силу прыжка
    }

    private bool IsGrounded()
    {
        float radius = 0.5f; // Радиус сферы для проверки земли
        float distance = 1.1f; // Расстояние до земли
        return Physics.SphereCast(transform.position, radius, Vector3.down, out _, distance);
    }
}