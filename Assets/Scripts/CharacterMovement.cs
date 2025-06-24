using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f; // Default value
    [SerializeField] private float runSpeed = 8f;   // Default value
    [SerializeField] private float rotationSpeed = 720f; // Not used? SmoothRotationSpeed is used
    [SerializeField] private float smoothRotationSpeed = 500f; // Default value
    [SerializeField] private float jumpForce = 7f; // Default value

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.3f; // Default value, distance from pivot downwards

    [Header("References")]
    [SerializeField] private Transform cameraTransform; // Assign the camera transform in inspector
    [SerializeField] private Animator animator; // Assign the animator in inspector (or get with GetComponent)
    [SerializeField] private Rigidbody rb; // Assign the rigidbody in inspector (or get with GetComponent)

    // References to other systems - will try GetComponent if not assigned
    private HealthSystem playerHealth;
    private ManaSystem playerMana;

    [Header("State Flags")]
    private bool isMoving;
    private bool isRunning;
    private float currentMoveSpeed; // Stores either moveSpeed or runSpeed

    private bool isJumping;
    private bool isGrounded;

    private bool isAttacking;
    private bool isMagicAttacking;


    [Header("Combat Settings")]
    [SerializeField] private float magicManaCost = 30f; // Mana cost for magic attack


    // Internal state/input tracking
    private Vector3 cameraForward; // Camera's forward direction projected onto the horizontal plane
    private Vector3 cameraRight;   // Camera's right direction projected onto the horizontal plane

    private KeyCode[] moveKeyCodes = new KeyCode[]
    {
        KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D
    };

    // Animator parameter names
    // Consider using [SerializeField] for these too, in case they change in the Animator Controller
    private string animatorMoveBool = "IsMoving";
    private string animatorRunBool = "IsRunning";
    private string animatorJumpBool = "IsJumping";
    private string animatorPhysicalAttackTrigger = "PhysicalAttack";
    private string animatorMagicalAttackTrigger = "MagicalAttack";
    private string animatorDeathTrigger = "Death"; // Assuming you have a death trigger


    void Awake()
    {
        // Try to get references if not assigned in inspector
        if (cameraTransform == null) cameraTransform = Camera.main?.transform;
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        // Get other system references
        playerHealth = GetComponent<HealthSystem>();
        playerMana = GetComponent<ManaSystem>();

        // --- ИСПРАВЛЕНО: Добавлены проверки на null после GetComponent ---
        if (cameraTransform == null)
        {
            Debug.LogError("Main Camera (or CameraTransform) is not assigned for CharacterMovement on " + gameObject.name + "! Script disabled.", this);
            enabled = false; // Script cannot function without a camera reference
            return; // Stop Awake execution
        }

        if (animator == null)
        {
            Debug.LogWarning("Animator component not found on " + gameObject.name + ". Animations will not work.", this);
            // We don't disable the script entirely, as movement might still function, but animations won't.
        }

        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on " + gameObject.name + ". Movement and physics will not work! Script disabled.", this);
            enabled = false; // Script heavily relies on Rigidbody for movement
            return; // Stop Awake execution
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("HealthSystem component not found on " + gameObject.name + ". Player health and death state will not be managed by this script.", this);
            // Script can still run without HealthSystem, but player death logic won't work here.
        }

        if (playerMana == null)
        {
            Debug.LogWarning("ManaSystem component not found on " + gameObject.name + ". Magic attacks mana cost/reduction will not work.", this);
            // Script can still run without ManaSystem, but magic attacks won't check mana.
        }
        // --- КОНЕЦ ИСПРАВЛЕНИЙ: Добавлены проверки на null ---


        // Initialize state flags
        isMoving = false;
        isRunning = false;
        isJumping = false; // Should start not jumping
        isAttacking = false; // Should start not attacking
        isMagicAttacking = false; // Should start not magic attacking

        // Initial ground check
        CheckForGround();

        // Initial camera axis update
        UpdateCameraAxis();
    }

    // FixedUpdate is generally better for physics (like ground checks if using rays)
    void FixedUpdate()
    {
        CheckForGround();
        // Note: Jump force is applied via an Animation Event for better control,
        // but you might choose to apply it here after input detection if you prefer.
    }

    void Update()
    {
        // --- ИСПРАВЛЕНО: Добавлена проверка на null перед использованием playerHealth ---
        // If playerHealth exists and the player is dead, freeze movement and handle death
        if (playerHealth != null && playerHealth.IsDead())
        {
             if (rb != null) rb.constraints = RigidbodyConstraints.FreezeAll; // Freeze rigidbody if it exists
            OnPlayerDeath(); // Handle death (disables this script)
            return; // Stop Update execution if dead
        }
        // --- КОНЕЦ ИСПРАВЛЕНИЯ ---

        // Update camera directions *before* calculating movement direction
        UpdateCameraAxis();

        // --- ИСПРАВЛЕНО: Убедимся, что rigidbody существует перед попыткой двигаться ---
        if (rb == null) return; // Cannot move if Rigidbody is missing

        // --- ИСПРАВЛЕНО: Убедимся, что Animator существует перед попыткой устанавливать параметры ---
        bool hasAnimator = (animator != null);


        // --- Movement Input and State ---
        Vector3 moveDirection = Vector3.zero;

        // Check if player is pressing any movement key
        bool wasPressingMoveKeys = IsPressingMoveKeys(moveKeyCodes);
        isMoving = wasPressingMoveKeys; // Update isMoving flag

        // Determine run state and current speed
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift);
        isRunning = isShiftPressed && isMoving && CanMove(); // Can only run if moving and allowed to move
        currentMoveSpeed = isRunning ? runSpeed : moveSpeed;

        // Update animator movement flags
        if (hasAnimator)
        {
            animator.SetBool(animatorMoveBool, isMoving);
            animator.SetBool(animatorRunBool, isRunning);
        }


        // --- Calculate and Apply Movement (if moving) ---
        if (isMoving && CanMove()) // Only calculate/apply movement if currently wanting to move and allowed to
        {
            // Calculate movement direction based on camera orientation and input
            if (Input.GetKey(KeyCode.W)) moveDirection += cameraForward;
            if (Input.GetKey(KeyCode.S)) moveDirection -= cameraForward;
            if (Input.GetKey(KeyCode.A)) moveDirection -= cameraRight;
            if (Input.GetKey(KeyCode.D)) moveDirection += cameraRight;

            // Normalize to prevent faster diagonal movement
            if (moveDirection.magnitude > 1) // Check magnitude before normalizing
            {
                 moveDirection.Normalize();
            }

            // Rotate character towards movement direction
            if (moveDirection.magnitude > 0) // Only rotate if there is a direction
            {
                 Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                 transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime);
            }


            // Apply movement (Translate directly based on input direction)
            // Note: For physics-based movement, consider using rb.MovePosition or rb.velocity
            // transform.Translate(moveDirection * currentMoveSpeed * Time.deltaTime, Space.World);
            // Using Rigidbody velocity for potentially better physics interaction, especially with jumping/grounding
            Vector3 movementVelocity = moveDirection * currentMoveSpeed;
            // Keep current vertical velocity for jumping/falling
            rb.velocity = new Vector3(movementVelocity.x, rb.velocity.y, movementVelocity.z);

        } else {
             // If not moving or unable to move, stop horizontal velocity but keep vertical
             rb.velocity = new Vector3(0f, rb.velocity.y, 0f);

             // Ensure animator flags are false when not intending to move
             if (hasAnimator)
             {
                 animator.SetBool(animatorMoveBool, false);
                 animator.SetBool(animatorRunBool, false);
             }
        }

        // --- Jump Input ---
        if (Input.GetKeyDown(KeyCode.Space) && CanJump())
        {
            isJumping = true; // Set jumping flag
            if (hasAnimator) animator.SetBool(animatorJumpBool, true); // Trigger jump animation
            // Actual jump force is often applied via animation event (OnJumpAnimationAddForce)
            // or you could call OnJumpAnimationAddForce() directly here.
        }


        // --- Attack Inputs ---
        // Physical Attack (Left Mouse Button)
        if (Input.GetMouseButtonDown(0) && CanAttack())
        {
            RotateTowardsCamera(true); // Face the camera direction instantly
            isAttacking = true; // Set attacking flag
            if (hasAnimator) animator.SetTrigger(animatorPhysicalAttackTrigger); // Trigger physical attack animation
            // Actual damage/hit detection often happens via animation events
        }

        // Magic Attack (Right Mouse Button)
        // --- ИСПРАВЛЕНО: Добавлена проверка на null перед использованием playerMana ---
        if (Input.GetMouseButtonDown(1) && CanAttack() && playerMana != null && playerMana.GetMana() >= magicManaCost) // Use >= for cost check
        {
            RotateTowardsCamera(true); // Face the camera direction instantly
            isMagicAttacking = true; // Set magic attacking flag
            playerMana.ReduceMana(magicManaCost); // Reduce mana (ManaSystem handles sufficiency check, but we already checked)
            if (hasAnimator) animator.SetTrigger(animatorMagicalAttackTrigger); // Trigger magic attack animation
            // Actual magic effect/damage often happens via animation events
        }
        // --- КОНЕЦ ИСПРАВЛЕНИЯ ---

        // Note: State flags (isAttacking, isMagicAttacking, isJumping) are typically reset
        // via Animation Events (OnPlayerAttackEnd, OnPlayerMagicAttackEnd, OnJumpAnimationEnd).
    }


    // --- Helper Methods ---

    // Rotates the character to face the current camera direction on the horizontal plane
    private void RotateTowardsCamera(bool forceInstantRotation = false)
    {
        // Ensure cameraTransform exists before using it
        if (cameraTransform == null)
        {
            Debug.LogWarning("CameraTransform is null! Cannot rotate towards camera.", this);
            return;
        }

        UpdateCameraAxis(); // Make sure camera axes are fresh

        // Calculate target rotation based on horizontal camera forward
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);

        // Apply rotation
        if (forceInstantRotation)
            transform.rotation = targetRotation;
        else
            // Smoothly rotate over time
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime);
    }

    // Updates cameraForward and cameraRight vectors projected onto the horizontal plane
    private void UpdateCameraAxis()
    {
        // Ensure cameraTransform exists before using it
         if (cameraTransform == null) return;

        cameraForward = cameraTransform.forward;
        cameraForward.y = 0; // Ignore vertical component
        cameraForward.Normalize(); // Ensure unit length

        cameraRight = cameraTransform.right;
        cameraRight.y = 0; // Ignore vertical component
        cameraRight.Normalize(); // Ensure unit length
    }


    // --- Movement Logic Helper ---
    private bool CanMove()
    {
        // Cannot move if jumping, attacking (physical or magic), or potentially other states
        // Add checks for stun, dialogue, etc., if needed.
        return !isJumping && !isAttacking && !isMagicAttacking;
    }

    // Checks if any of the specified move keys are currently pressed
    private bool IsPressingMoveKeys(KeyCode[] keyCodes)
    {
        foreach (KeyCode key in keyCodes)
        {
            if (Input.GetKey(key))
            {
                return true;
            }
        }
        return false;
    }


    // --- Jump Logic ---

    // Performs a raycast downwards to check if the character is grounded
    private void CheckForGround()
    {
        // Ensure rigidbody exists, otherwise ground check based on position might be inaccurate or throw errors
         if (rb == null)
         {
             isGrounded = false; // Assume not grounded if no rigidbody
             return;
         }

        RaycastHit hit;
        // Start raycast slightly above the pivot to avoid hitting self immediately
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        float rayLength = groundCheckDistance + 0.1f; // Add buffer

        // Perform the raycast
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength);

        // Optional: Draw debug ray to visualize the ground check in the Scene view
        // Debug.DrawRay(rayOrigin, Vector3.down * rayLength, isGrounded ? Color.green : Color.red);
    }

    // Checks if the character is currently able to jump
    private bool CanJump()
    {
        // Can jump if grounded and not currently jumping or attacking
        return isGrounded && !isJumping && !isAttacking && !isMagicAttacking;
    }

    // *** Animation Event Method ***
    // Called by an Animation Event on the jump animation timeline to apply upward force
    public void OnJumpAnimationAddForce()
    {
        // Ensure rigidbody exists before applying force
         if (rb != null)
         {
            // Clear existing vertical velocity before adding jump force for consistent jump height
             rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
             rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
         } else {
             Debug.LogWarning("Rigidbody is null! Cannot apply jump force from Animation Event.");
         }
    }

    // *** Animation Event Method ***
    // Called by an Animation Event on the jump animation timeline when the jump is considered "ended"
    public void OnJumpAnimationEnd()
    {
        isJumping = false; // Reset jumping flag
        // If animator exists, ensure the jump animation bool is false
        if (animator != null && !string.IsNullOrEmpty(animatorJumpBool))
        {
             animator.SetBool(animatorJumpBool, false);
        }
    }

    // Public method to get the jumping state (used by CameraOrbit)
    // --- ИСПРАВЛЕНО: Добавлена проверка на null для rigidbody, т.к. она используется в Update для движения ---
    // Возвращаем состояние isJumping, но также учитываем, если Rigidbody отсутствует (движение и прыжок невозможны)
    public bool GetJumpState()
    {
        return isJumping && (rb != null); // Consider not jumping if Rigidbody is missing
    }


    // --- Attack Logic ---

    // Checks if the character is currently able to perform an attack
    private bool CanAttack()
    {
        // Can attack if not currently attacking (physical or magic) and is grounded
        // Add checks for stun, etc., if needed.
        return !isAttacking && !isMagicAttacking && isGrounded;
    }

    // *** Animation Event Method ***
    // Called by an Animation Event on the physical attack animation timeline
    public void OnPlayerAttackEnd()
    {
        isAttacking = false; // Reset physical attacking flag
        // Debug.Log("Physical attack animation ended."); // Optional log
    }

    // *** Animation Event Method ***
    // Called by an Animation Event on the magic attack animation timeline
    public void OnPlayerMagicAttackEnd()
    {
        isMagicAttacking = false; // Reset magic attacking flag
        // Debug.Log("Magic attack animation ended."); // Optional log
    }


    // --- Death Logic ---

    // Called when the player dies (triggered by HealthSystem.OnPlayerDied event or CharacterMovement's Update)
    private void OnPlayerDeath()
    {
        // Only run death logic once
        if (!enabled) return; // If already disabled, we're already dying/dead

        Debug.Log(gameObject.name + " has died.");

        // Stop all movement
        if (rb != null)
        {
             rb.velocity = Vector3.zero;
             rb.constraints = RigidbodyConstraints.FreezeAll; // Freeze physics
        }

        // Trigger death animation
        if (animator != null && !string.IsNullOrEmpty(animatorDeathTrigger))
        {
             animator.SetTrigger(animatorDeathTrigger);
        }
        // Note: Animation Events on the death animation can handle despawning, showing UI, etc.

        // Disable movement script
        enabled = false;
    }

    // Optional: Method to allow external scripts to trigger death (e.g., if HealthSystem doesn't have an event)
    public void TriggerDeath()
    {
        // Ensure HealthSystem exists and mark it as dead (or call InstanceDie if public)
        if (playerHealth != null)
        {
            // Assuming HealthSystem's death logic is called when health <= 0
            playerHealth.TakeDamage(playerHealth.GetHealth()); // Deal remaining damage to trigger death via HealthSystem
        }
        else
        {
            // If no HealthSystem, directly call death logic here
            OnPlayerDeath();
        }
    }

    // Helper to get ground state
    public bool IsGrounded()
    {
        return isGrounded;
    }

     // Helper to get attack state (optional, if other scripts need it)
     public bool IsAttacking()
     {
         return isAttacking || isMagicAttacking;
     }
}