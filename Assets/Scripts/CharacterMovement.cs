using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

    private Transform cameraTransform;

    private Animator animator;
    private Rigidbody rb;
    private HealthSystem playerHealth;
    private AttackSystem playerAttack;

    private bool isMoving;
    private bool isRunning;
    private float currentMoveSpeed;

    private bool isJumping;
    private bool isGrounded;

    private Vector3 cameraForward;
    private Vector3 cameraRight;

    private KeyCode[] moveKeyCodes = new KeyCode[]
    {
        KeyCode.W,
        KeyCode.A,
        KeyCode.S,
        KeyCode.D
    };

    private bool isAttacking;
    private HitboxScript playerAttackHitbox;
    private bool isMagicAttacking;

    private string animatorMoveBool = "IsMoving";
    private string animatorRunBool = "IsRunning";
    private string animatorJumpBool = "IsJumping";
    private string animatorPhysicalAttackTrigger = "PhysicalAttack";
    private string animatorMagicalAttackTrigger = "MagicalAttack";
    private string animatorDeathTrigger = "Death";


    void Awake()
    {
        cameraTransform = Camera.main.transform;
        if (cameraTransform == null)
        {
            enabled = false;
            return;
        }

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<HealthSystem>();
        playerAttack = GetComponent<AttackSystem>();

        isMoving = false;
        isRunning = false;

        isJumping = false;
        CheckForGround();

        isAttacking = false;
        playerAttackHitbox = GetComponentInChildren<HitboxScript>();
        isMagicAttacking = false;

        UpdateCameraAxis();
    }

    void FixedUpdate()
    {
        CheckForGround();
    }

    void Update()
    {
        if (playerHealth.IsDead())
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            OnPlayerDeath();
            return;
        }

        Vector3 moveDirection = Vector3.zero;
        UpdateCameraAxis();

        if (CanMove())
        {
            if (IsPressingMoveKeys(moveKeyCodes)) isMoving = true;
            else
            {
                isMoving = false;
                animator.SetBool(animatorMoveBool, isMoving);
            }

            if (Input.GetKey(KeyCode.LeftShift) && CanMove())
            {
                isRunning = true;
                currentMoveSpeed = runSpeed;
            }
            else
            {
                isRunning = false;
                currentMoveSpeed = moveSpeed;
            }
            animator.SetBool(animatorRunBool, isRunning);

            if (isMoving)
            {
                if (Input.GetKey(KeyCode.W))
                {
                    moveDirection += cameraForward;
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    moveDirection -= cameraForward;
                }

                if (Input.GetKey(KeyCode.A))
                {
                    moveDirection -= cameraRight;
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    moveDirection += cameraRight;
                }

                moveDirection.Normalize();
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime);

                transform.Translate(moveDirection * currentMoveSpeed * Time.deltaTime, Space.World);
                animator.SetBool(animatorMoveBool, isMoving);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && CanJump()) isJumping = true;
        animator.SetBool(animatorJumpBool, isJumping);

        if (Input.GetMouseButtonDown(0) && CanAttack())
        {
            RotateTowardsCamera(true);
            isAttacking = true;
            animator.SetTrigger(animatorPhysicalAttackTrigger);
        }

        if (Input.GetMouseButtonDown(1) && CanAttack())
        {
            RotateTowardsCamera(true);
            isMagicAttacking = true;
            animator.SetTrigger(animatorMagicalAttackTrigger);
        }
    }

    private void RotateTowardsCamera(bool forceInstantRotation = false)
    {
        UpdateCameraAxis();
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);

        if (forceInstantRotation)
            transform.rotation = targetRotation;
        else
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime);
    }

    //Camera Logic
    private void UpdateCameraAxis()
    {
        cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        cameraRight = cameraTransform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();
    }

    //Movement Logic
    private bool CanMove()
    {
        return !isJumping && !isAttacking && !isMagicAttacking;
    }
    private bool IsPressingMoveKeys(KeyCode[] moveKeyCodes)
    {
        foreach (KeyCode key in moveKeyCodes)
        {
            if (Input.GetKey(key))
            {
                return true;
            }
        }
        return false;
    }

    //Jump Logic
    private void CheckForGround()
    {
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance + 0.1f);
    }
    private bool CanJump()
    {
        return isGrounded && !isJumping && !isAttacking && !isMagicAttacking;
    }
    public void OnJumpAnimationAddForce()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    public void OnJumpAnimationEnd()
    {
        isJumping = false;
        animator.SetBool(animatorJumpBool, false);
    }
    public bool GetJumpState()
    {
        return isJumping;
    }

    //Attack Logic
    private bool CanAttack()
    {
        return !isAttacking && !isMagicAttacking && isGrounded;
    }
    public void OnPhysicalAttackStart()
    {
        playerAttackHitbox.EnableHitbox();
    }
    public void OnPhysicalAttackEnd()
    {
        playerAttackHitbox.DisableHitbox();
        isAttacking = false;
    }
    public void OnMagicalAttackStart()
    {
        
    }
    public void OnMagicalAttackEnd()
    {
        isMagicAttacking = false;
    }
    /*
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

    }*/

    private void OnPlayerDeath()
    {
        enabled = false;
    }
}