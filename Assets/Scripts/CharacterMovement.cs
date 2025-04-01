using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float runSpeed = 10f;
    public float rotationSpeed = 100f;
    public float smoothRotationSpeed = 10f;
    public float jumpForce = 7f;
    public float groundCheckDistance = 0.1f;

    public GameObject fireballPrefab;
    public float fireballForce = 20f;
    public Vector3 fireballSpawnOffset = Vector3.zero;

    // Система здоровья
    public float maxHealth = 100f; // Максимальное здоровье
    public float health;           // Текущее здоровье, делаем public для наблюдения в инспекторе, но лучше использовать свойство для контроля доступа

    private Transform cameraTransform;
    private Animator animator;
    private Rigidbody rb;
    private bool isJumping = false;
    private bool isGrounded;
    private bool isRunning = false;
    private bool isAttacking = false;
    private bool isMagicAttacking = false;
    private bool isDead = false; // Добавляем флаг смерти

    public bool isActuallyJumping = false;

    // Свойство для доступа к здоровью извне, но с защитой от прямой записи (опционально, но хорошая практика)
    public float Health
    {
        get { return health; }
        private set { health = value; } // Теперь здоровье можно менять только внутри этого класса
    }


    void Start()
    {
        cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
        {
            enabled = false;
            return;
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            enabled = false;
            return;
        }

        if (fireballPrefab == null)
        {
            enabled = false;
            return;
        }

        // Инициализация здоровья при старте игры
        Health = maxHealth; // Используем свойство для установки начального значения
    }

    void FixedUpdate()
    {
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance + 0.1f);

    }

    void Update()
    {
        // Если персонаж мертв, не позволяем ему двигаться или атаковать
        CheckHealth();
        if (isDead) return;

        bool isMoving = false;
        bool isMovingBack = false;
        isRunning = false;
        Vector3 moveDirection = Vector3.zero;
        float currentMoveSpeed = moveSpeed;


        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();


        if (!isJumping && !isAttacking && !isMagicAttacking)
        {
            if (Input.GetKey(KeyCode.A))
            {
                isMoving = true;
                transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.D))
            {
                isMoving = true;
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }



        if (!isJumping && !isAttacking && !isMagicAttacking)
        {
            if (Input.GetKey(KeyCode.W))
            {
                isMoving = true;
                moveDirection += cameraForward;

                if (Input.GetKey(KeyCode.A))
                {
                    moveDirection -= cameraRight;
                }
                if (Input.GetKey(KeyCode.D))
                {
                    moveDirection += cameraRight;
                }

                if (moveDirection != Vector3.zero)
                {
                    moveDirection.Normalize();

                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        isRunning = true;
                        currentMoveSpeed = runSpeed;
                    }

                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime);

                    transform.Translate(moveDirection * currentMoveSpeed * Time.deltaTime, Space.World);
                }
            }
            else if (Input.GetKey(KeyCode.S))
            {
                isMovingBack = true;
                moveDirection -= cameraForward;

                if (moveDirection != Vector3.zero)
                {
                    moveDirection.Normalize();
                    currentMoveSpeed = moveSpeed;

                    Quaternion targetRotation = Quaternion.LookRotation(-moveDirection);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime);

                    transform.Translate(moveDirection * currentMoveSpeed * Time.deltaTime, Space.World);
                }
            }
        }



        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJumping && !isMagicAttacking)
        {
            PerformJump();
        }


        if (Input.GetMouseButtonDown(0) && !isAttacking && !isMagicAttacking && isGrounded)
        {

            RotateTowardsCamera(true);
            isAttacking = true;
            animator.SetTrigger("Attack");
        }


        if (Input.GetMouseButtonDown(1) && !isMagicAttacking && !isAttacking && isGrounded)
        {

            RotateTowardsCamera(true);
            isMagicAttacking = true;
            animator.SetTrigger("MagicAttack");

        }


        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsMovingBack", isMovingBack);
        animator.SetBool("IsJumping", isJumping);
        animator.SetBool("IsRunning", isRunning);

        // Тест урона (убрать потом, это только для проверки)
        if (Input.GetKeyDown(KeyCode.T))
        {
            TakeDamage(10f); // Наносим 10 единиц урона при нажатии 'T'
        }
    }


    private void RotateTowardsCamera(bool forceInstantRotation = false)
    {
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);

        if (forceInstantRotation)
        {

            transform.rotation = targetRotation;
        }
        else
        {

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime);
        }
    }


    public void SpawnFireball()
    {
        if (fireballPrefab != null)
        {

            GameObject fireball = Instantiate(fireballPrefab, transform.position + fireballSpawnOffset, transform.rotation * Quaternion.Euler(-90f, 0f, 0f));


            Rigidbody fireballRb = fireball.GetComponent<Rigidbody>();
            if (fireballRb != null)
            {

                fireballRb.AddForce(transform.forward * fireballForce, ForceMode.Impulse);
            }

        }

    }


    public void PerformJumpForce()
    {
        isActuallyJumping = true;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
    }

    public void OnMagicAttackAnimationEnd()
    {
        isMagicAttacking = false;
    }

    public void OnJumpAnimationEnd()
    {
        isJumping = false;
        animator.SetBool("IsJumping", false);
        isActuallyJumping = false;
    }


    public void PerformJump()
    {
        if (!isJumping && isGrounded)
        {
            isJumping = true;
            animator.SetBool("IsJumping", true);
        }
    }

    // Метод для получения урона
    public void TakeDamage(float damage)
    {
        if (isDead) return; // Если уже мертв, урон не принимаем

        Health -= damage; // Используем свойство для изменения здоровья

        CheckHealth();

        Debug.Log("Персонаж получил урон: " + damage + ". Здоровье: " + Health); // Для отладки
    }

    private void CheckHealth()
    {
        if (Health <= 0)
        {
            Health = 0; // Убедимся, что здоровье не уходит в минус
            Die();
        }
    }

    // Метод смерти
    void Die()
    {
        isDead = true;
        animator.SetTrigger("Death"); // Запускаем анимацию смерти по триггеру "Death"
        Debug.Log("Персонаж умер!");

        // Отключаем скрипт движения, чтобы персонаж перестал двигаться
        enabled = false;

        // Можно добавить другие действия при смерти, например, отключение коллайдера,
        // вызов события смерти, перезапуск уровня и т.д.
    }

    // Функция вызывается анимацией в конце анимации смерти (если нужно что-то сделать после анимации смерти)
    public void OnDeathAnimationEnd()
    {
        // Например, можно здесь отключить игровой объект персонажа через Destroy(gameObject);
        // или перезапустить уровень
        Debug.Log("Анимация смерти завершена.");
    }
}