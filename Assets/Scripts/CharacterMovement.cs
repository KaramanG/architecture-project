using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    public float moveSpeed = 5f;         // �R�{������������ �t�r�y�w�u�~�y�� �����t���q�� �r���u���u�t/�~�p�x�p�t
    public float runSpeed = 10f;          // �R�{������������ �q�u�s�p �r���u���u�t/�~�p�x�p�t
    public float rotationSpeed = 100f;    // �R�{������������ �����r���������p
    public float smoothRotationSpeed = 10f; // �R�{������������ ���|�p�r�~���s�� �����r���������p �����y �t�r�y�w�u�~�y�y �r���u���u�t/�~�p�x�p�t
    public float jumpForce = 7f;         // �R�y�|�p �������w�{�p
    // public float jumpAnimationDelay = 0.2f; // �H�p�t�u���w�{�p ���u���u�t �������w�{���} �t�|�� ���y�~�������~�y�x�p���y�y �� �p�~�y�}�p���y�u�z (�r ���u�{���~�t�p��) - Removed
    public float groundCheckDistance = 0.1f; // �Q�p�����������~�y�u �t�|�� �������r�u���{�y �x�u�}�|�y �����t ���u�������~�p�w�u�}

    private Transform cameraTransform;     // �S���p�~���������} �{�p�}�u����
    private Animator animator;             // �K���}�����~�u�~�� Animator �t�|�� �������p�r�|�u�~�y�� �p�~�y�}�p���y���}�y
    private Rigidbody rb;                  // �K���}�����~�u�~�� Rigidbody �t�|�� ���y�x�y�{�y
    private bool isJumping = false;        // �U�|�p�s �t�|�� �������|�u�w�y�r�p�~�y�� �������������~�y�� �������w�{�p
    private bool isGrounded;               // �U�|�p�s �t�|�� �������|�u�w�y�r�p�~�y��, �~�p�����t�y������ �|�y ���u�������~�p�w �~�p �x�u�}�|�u
    private bool isRunning = false;        // �U�|�p�s �t�|�� �������|�u�w�y�r�p�~�y�� �������������~�y�� �q�u�s�p
    private bool isAttacking = false;      // �U�|�p�s �t�|�� �������|�u�w�y�r�p�~�y�� �������������~�y�� ���q�����~���z �p���p�{�y
    private bool isMagicAttacking = false; // �U�|�p�s �t�|�� �������|�u�w�y�r�p�~�y�� �������������~�y�� �}�p�s�y���u���{���z �p���p�{�y

    public bool isActuallyJumping = false; // �D���q�p�r�|�u�~��: �����q�|�y���~�p�� ���u���u�}�u�~�~�p�� �t�|�� CameraOrbit

    void Start()
    {
        // �N�p�����t�y�} �s�|�p�r�~���� �{�p�}�u���� �r �����u�~�u
        cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
        {
            Debug.LogError("�C�|�p�r�~�p�� �{�p�}�u���p �~�u �~�p�z�t�u�~�p �r �����u�~�u. �T�q�u�t�y���u����, ������ �r �����u�~�u �u������ �{�p�}�u���p �� ���u�s���} 'MainCamera'.");
            enabled = false; // �O���{�|�����p�u�} ���{���y����, �u���|�y �{�p�}�u���p �~�u �~�p�z�t�u�~�p
            return;
        }

        // �P���|�����p�u�} �{���}�����~�u�~�� Animator �~�p �������} �w�u ���q���u�{���u (���u�������~�p�w�u)
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("�K���}�����~�u�~�� Animator �~�u �~�p�z�t�u�~ �~�p ���q���u�{���u ���u�������~�p�w�p. �T�q�u�t�y���u����, ������ Animator �t���q�p�r�|�u�~ �{ ���u�������~�p�w��.");
            enabled = false; // �O���{�|�����p�u�} ���{���y����, �u���|�y Animator �~�u �~�p�z�t�u�~
            return;
        }

        // �P���|�����p�u�} �{���}�����~�u�~�� Rigidbody �~�p �������} �w�u ���q���u�{���u (���u�������~�p�w�u)
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("�K���}�����~�u�~�� Rigidbody �~�u �~�p�z�t�u�~ �~�p ���q���u�{���u ���u�������~�p�w�p. �T�q�u�t�y���u����, ������ Rigidbody �t���q�p�r�|�u�~ �{ ���u�������~�p�w��.");
            enabled = false; // �O���{�|�����p�u�} ���{���y����, �u���|�y Rigidbody �~�u �~�p�z�t�u�~
            return;
        }
    }

    void FixedUpdate() // FixedUpdate �t�|�� ���y�x�y�{�y
    {
        // �P�����r�u���{�p �~�p �x�p�x�u�}�|�u�~�y�u �� �����}�������� Raycast
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance + 0.1f); // �N�u�}�~���s�� �����t�~�y�}�p�u�} �������{�� �~�p���p�|�p �|�����p �y ���r�u�|�y���y�r�p�u�} ���p�����������~�y�u
        // Debug.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * (groundCheckDistance + 0.1f), isGrounded ? Color.green : Color.red); // �D�|�� �r�y�x���p�|�y�x�p���y�y raycast �r ���u�w�y�}�u Scene
    }

    void Update()
    {
        bool isMoving = false; // �U�|�p�s �t�|�� �������|�u�w�y�r�p�~�y��, �t�r�y�w�u������ �|�y ���u�������~�p�w
        bool isMovingBack = false; // �U�|�p�s �t�|�� �������|�u�w�y�r�p�~�y��, �t�r�y�w�u������ �|�y ���u�������~�p�w �~�p�x�p�t
        isRunning = false; // �R�q���p�����r�p�u�} ���|�p�s �q�u�s�p �r �~�p���p�|�u �{�p�w�t���s�� �{�p�t���p
        Vector3 moveDirection = Vector3.zero; // �I�~�y���y�p�|�y�x�y�����u�} �~�p�����p�r�|�u�~�y�u �t�r�y�w�u�~�y�� �{�p�{ �~���|�u�r���u
        float currentMoveSpeed = moveSpeed; // �S�u�{�����p�� ���{������������ �t�r�y�w�u�~�y��, ���� ���}���|���p�~�y�� ���{������������ �����t���q��

        // �P�����r�u�����u�}, �~�u �������s�p�u�� �|�y ���u�������~�p�w, �~�u �p���p�{���u�� �|�y ���q�����~���z �p���p�{���z �y �~�u �}�p�s�y���u���{���z �p���p�{���z, �������q�� ���p�x���u���y���� �t�r�y�w�u�~�y�u
        if (!isJumping && !isAttacking && !isMagicAttacking)
        {
            // �P���|�����p�u�} �~�p�����p�r�|�u�~�y�u �{�p�}�u���� �~�p �s�����y�x���~���p�|���~���z ���|�����{�������y
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            // �D�r�y�w�u�~�y�u �r���u���u�t (���� �{�p�}�u����) �y �{���}�q�y�~�y�����r�p�~�~���u �t�r�y�w�u�~�y�u �� A/D
            if (Input.GetKey(KeyCode.W))
            {
                isMoving = true;
                moveDirection += cameraForward; // �D�r�y�w�u�~�y�u �r���u���u�t ���� �{�p�}�u����

                if (Input.GetKey(KeyCode.A))
                {
                    moveDirection -= cameraRight; // �D���q�p�r�|���u�} �t�r�y�w�u�~�y�u �r�|�u�r��
                }
                if (Input.GetKey(KeyCode.D))
                {
                    moveDirection += cameraRight; // �D���q�p�r�|���u�} �t�r�y�w�u�~�y�u �r�����p�r��
                }

                if (moveDirection != Vector3.zero)
                {
                    moveDirection.Normalize(); // �N�����}�p�|�y�x���u�} �{���}�q�y�~�y�����r�p�~�~���u �~�p�����p�r�|�u�~�y�u

                    // �P�����r�u�����u�}, �x�p�w�p�� �|�y LeftShift �D�L�` �A�E�C�@ *�I* �u������ �|�y �t�r�y�w�u�~�y�u �r���u���u�t
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        isRunning = true;
                        currentMoveSpeed = runSpeed; // �T�����p�~�p�r�|�y�r�p�u�} ���{������������ �q�u�s�p
                    }

                    // �P�|�p�r�~���z �����r�������� ���u�������~�p�w�p �r �~�p�����p�r�|�u�~�y�y �t�r�y�w�u�~�y��
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection); // �W�u�|�u�r���u �r���p���u�~�y�u
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime); // �P�|�p�r�~���z �����r��������

                    // �P�u���u�}�u���p�u�} ���u�������~�p�w�p
                    transform.Translate(moveDirection * currentMoveSpeed * Time.deltaTime, Space.World); // �P�u���u�}�u���p�u�} �r �}�y�����r���� �{�������t�y�~�p���p��, �y�������|���x���� ���u�{�������� ���{������������
                }
            }
            // �D�r�y�w�u�~�y�u �~�p�x�p�t (�{ �{�p�}�u���u)
            else if (Input.GetKey(KeyCode.S))
            {
                isMovingBack = true;
                moveDirection -= cameraForward; // �D�r�y�w�u�~�y�u �~�p�x�p�t �{ �{�p�}�u���u (�y�~�r�u�����y�� forward)

                if (moveDirection != Vector3.zero)
                {
                    moveDirection.Normalize(); // �N�����}�p�|�y�x���u�} �~�p�����p�r�|�u�~�y�u

                    // **�T�q���p�|�y �����|���r�y�u �t�|�� �q�u�s�p �����y �t�r�y�w�u�~�y�y �~�p�x�p�t, �r���u�s�t�p �����t���q�p**
                    currentMoveSpeed = moveSpeed; // �C�p���p�~���y�����u�}, ������ ���{������������ �r���u�s�t�p ���{������������ �����t���q�� �����y �t�r�y�w�u�~�y�y �~�p�x�p�t

                    // �P�|�p�r�~���z �����r�������� ���u�������~�p�w�p �r �~�p�����p�r�|�u�~�y�y �t�r�y�w�u�~�y�� �~�p�x�p�t
                    Quaternion targetRotation = Quaternion.LookRotation(-moveDirection); // �W�u�|�u�r���u �r���p���u�~�y�u (���p�x�r�������� �~�p 180 �s���p�t�������r)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime); // �P�|�p�r�~���z �����r��������

                    // �P�u���u�}�u���p�u�} ���u�������~�p�w�p
                    transform.Translate(moveDirection * currentMoveSpeed * Time.deltaTime, Space.World); // �P�u���u�}�u���p�u�} �r �}�y�����r���� �{�������t�y�~�p���p��, �y�������|���x���� ���u�{�������� ���{������������
                }
            }
            // �O���t�u�|���~���u �����r���������� �r�|�u�r�� �y �r�����p�r�� (�q�u�x �t�r�y�w�u�~�y�� �r���u���u�t/�~�p�x�p�t)
            else
            {
                if (Input.GetKey(KeyCode.A))
                {
                    isMoving = true; // �D�|�� �p�~�y�}�p���y�y �����r���������p, �u���|�y �~���w�~��
                    transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime); // �P���r�������� �r���{�����s �����y Y (�r�u�����y�{�p�|���~���z) �r�|�u�r��
                }

                if (Input.GetKey(KeyCode.D))
                {
                    isMoving = true; // �D�|�� �p�~�y�}�p���y�y �����r���������p, �u���|�y �~���w�~��
                    transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);  // �P���r�������� �r���{�����s �����y Y (�r�u�����y�{�p�|���~���z) �r�����p�r��
                }
            }
        }


        // �P�����w���{
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJumping && !isMagicAttacking) // �P�����r�u�����u�} �~�p�w�p���y�u �{�|�p�r�y���y, �x�p�x�u�}�|�u�~�y�u �y �~�u �~�p�����t�y������ �|�y ���u�������~�p�w ���w�u �r �������w�{�u �y�|�y �}�p�s�y���u���{���z �p���p�{�u
        {
            PerformJump(); // �B���x���r�p�u�} PerformJump �~�p�������}����, ���u���u���� ������ �~�u �{���������y�~�p, �p ������������ �x�p�������{�p�u�� �p�~�y�}�p���y�� �y ���|�p�s
        }

        // �@���p�{�p �}�u�����} ���� �L�K�M
        if (Input.GetMouseButtonDown(0) && !isAttacking && !isMagicAttacking && isGrounded) // �P�����r�u�����u�} �~�p�w�p���y�u �|�u�r���z �{�~�����{�y �}�����y, �~�u �p���p�{���u�� �|�y ���w�u, �~�u �}�p�s�y���u���{���z �p���p�{���z �y �x�p�x�u�}�|�u�~ �|�y
        {
            isAttacking = true; // �T�����p�~�p�r�|�y�r�p�u�} ���|�p�s ���q�����~���z �p���p�{�y
            animator.SetTrigger("Attack"); // �S���y�s�s�u���y�} �p�~�y�}�p���y�� ���q�����~���z �p���p�{�y
        }

        // �M�p�s�y���u���{�p�� �p���p�{�p ���� �P�K�M
        if (Input.GetMouseButtonDown(1) && !isMagicAttacking && !isAttacking && isGrounded) // �P�����r�u�����u�} �~�p�w�p���y�u �����p�r���z �{�~�����{�y �}�����y, �~�u �p���p�{���u�� �|�y �}�p�s�y���u���{�y ���w�u, �~�u ���q�����~���z �p���p�{���z �y �x�p�x�u�}�|�u�~ �|�y
        {
            isMagicAttacking = true; // �T�����p�~�p�r�|�y�r�p�u�} ���|�p�s �}�p�s�y���u���{���z �p���p�{�y
            animator.SetTrigger("MagicAttack"); // �S���y�s�s�u���y�} �p�~�y�}�p���y�� �}�p�s�y���u���{���z �p���p�{�y
        }


        // �T�����p�~�p�r�|�y�r�p�u�} ���p���p�}�u������ �p�~�y�}�p�������p
        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsMovingBack", isMovingBack);
        animator.SetBool("IsJumping", isJumping);
        animator.SetBool("IsRunning", isRunning);
    }

    // �U���~�{���y�� �t�|�� �r���x���r�p �y�x Animation Event �r �p�~�y�}�p���y�y �������w�{�p �t�|�� �����y�}�u�~�u�~�y�� ���y�|�� �������w�{�p
    public void PerformJumpForce()
    {
        isActuallyJumping = true; // �D���q�p�r�|�u�~��: �������p�~�p�r�|�y�r�p�u�} ���|�p�s, ������ �������w���{ �~�p���p�|����
        // �P���y�}�u�~���u�} �y�}�����|���� �������w�{�p �r�r�u���� �� �����}�������� Rigidbody
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // �I�������|���x���u�} ForceMode.Impulse �t�|�� �}�s�~���r�u�~�~���s�� �����y�|���w�u�~�y�� ���y�|��
    }


    // �U���~�{���y�� �t�|�� �r���x���r�p �y�x Animation Event �r �{���~���u �p�~�y�}�p���y�y ���q�����~���z �p���p�{�y
    public void OnAttackAnimationEnd()
    {
        isAttacking = false; // �R�q���p�����r�p�u�} ���|�p�s ���q�����~���z �p���p�{�y, �t�r�y�w�u�~�y�u ���~���r�p ���p�x���u���u�~��
    }

    // �U���~�{���y�� �t�|�� �r���x���r�p �y�x Animation Event �r �{���~���u �p�~�y�}�p���y�y �}�p�s�y���u���{���z �p���p�{�y
    public void OnMagicAttackAnimationEnd()
    {
        isMagicAttacking = false; // �R�q���p�����r�p�u�} ���|�p�s �}�p�s�y���u���{���z �p���p�{�y, �t�r�y�w�u�~�y�u ���~���r�p ���p�x���u���u�~��
    }

    // �U���~�{���y�� �t�|�� �r���x���r�p �y�x Animation Event �r �{���~���u �p�~�y�}�p���y�y �������w�{�p (�����y �����y�x�u�}�|�u�~�y�y)
    public void OnJumpAnimationEnd()
    {
        isJumping = false; // �R�q���p�����r�p�u�} ���|�p�s �������w�{�p, �t�r�y�w�u�~�y�u ���~���r�p ���p�x���u���u�~��
        animator.SetBool("IsJumping", false); // �B���{�|�����p�u�} �p�~�y�}�p���y�� �������w�{�p (�u���|�y �~�u �������p�r�|���u������ ���u���u�x state machine)
        isActuallyJumping = false; // �D���q�p�r�|�u�~��: ���q���p�����r�p�u�} ���|�p�s, ������ �������w���{ �x�p�{���~���y�|����
    }


    // �U���~�{���y�� �t�|�� �x�p�������{�p �p�~�y�}�p���y�y �������w�{�p �y �������p�~���r�{�y ���|�p�s�p isJumping
    public void PerformJump() // �I�x�}�u�~�y�|�y �~�p void, �q���|�����u �~�u �{���������y�~�p
    {
        if (!isJumping && isGrounded) // �D���q�p�r�y�|�y �������r�u���{�� �x�p�x�u�}�|�u�~�y�� �x�t�u���� �t�|�� �~�p�t�u�w�~�������y
        {
            isJumping = true;
            animator.SetBool("IsJumping", true); // �B�{�|�����p�u�} �p�~�y�}�p���y�� �������w�{�p
            // �S�u���u���� �������w���{ (rb.AddForce) �q���t�u�� �r���x�r�p�~ ���u���u�x Animation Event "PerformJumpForce"
        }
    }
}
