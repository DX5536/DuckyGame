using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Yarn.Unity;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Player2DController : MonoBehaviour
{
    [Header("Key Bindings")]
    [SerializeField] private KeyBinding_ScriptableObject keyBindings;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Air Control")]
    [Tooltip("0 = keep all momentum in air (long drift). 1 = instant stop like on the ground (no drift).")]
    [Range(0f, 0.1f)]
    [SerializeField] private float airControl = 0.05f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;

    [Header("Trigger Event")]
    [SerializeField] private UnityEvent onTriggerEntered;

    [Header("Yarn Spinner (optional)")]
    [Tooltip("Drag the scene's DialogueRunner here so the player auto-stops while dialogue is running.")]
    [SerializeField] private DialogueRunner dialogueRunner;

    private Rigidbody2D rb;
    private BoxCollider2D box;
    private Vector2 defaultColliderSize;
    private Vector2 defaultColliderOffset;
    private bool isCrouching;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        defaultColliderSize = box.size;
        defaultColliderOffset = box.offset;
    }

    private void OnEnable()
    {
        // Enable the actions we use. We don't Disable on OnDisable because these
        // InputActionReferences are shared assets - other consumers (e.g. NPC_TriggerEvents)
        // may need the same actions to stay enabled elsewhere.
        if (keyBindings == null) return;
        keyBindings.Move?.action?.Enable();
        keyBindings.Jump?.action?.Enable();
        keyBindings.Crouch?.action?.Enable();
    }

    private void Update()
    {
        // Auto-check Yarn's dialogue state each frame so we don't need to wire events.
        bool dialogRunning = dialogueRunner != null && dialogueRunner.IsDialogueRunning;
        CharacterMovement(dialogRunning);
    }

    public void CharacterMovement(bool isDialogRunning)
    {
        // Skip input if a dialog is running, the master IsMoving toggle is off,
        // or the required references are missing.
        if (isDialogRunning) return;
        if (keyBindings == null) return;
        if (!keyBindings.IsMoving) return;

        InputAction moveAction = keyBindings.Move != null ? keyBindings.Move.action : null;
        InputAction jumpAction = keyBindings.Jump != null ? keyBindings.Jump.action : null;
        InputAction crouchAction = keyBindings.Crouch != null ? keyBindings.Crouch.action : null;

        bool grounded = IsGrounded();

        // ---- Horizontal Movement ----
        // Read as Vector2 so it works with WASD composites, gamepad sticks, and on-screen joysticks.
        float horizontal = moveAction != null ? moveAction.ReadValue<Vector2>().x : 0f;

        float targetX = horizontal * moveSpeed;
        // Full control on the ground; blended control in the air so momentum carries the player forward.
        float newX = grounded ? targetX : Mathf.Lerp(rb.linearVelocity.x, targetX, airControl);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        // ---- Jump ----
        if (jumpAction != null && jumpAction.WasPressedThisFrame() && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // ---- Crouch ----
        bool wantsCrouch = crouchAction != null && crouchAction.IsPressed();
        if (wantsCrouch != isCrouching) SetCrouch(wantsCrouch);

        // ---- Animation (only if an Animator is assigned) ----
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontal));
            animator.SetBool("IsCrouching", isCrouching);
            animator.SetBool("IsGrounded", grounded);
        }
    }

    private void SetCrouch(bool crouching)
    {
        isCrouching = crouching;
        if (crouching)
        {
            // Halve the height, shift the offset down so the feet stay on the ground.
            box.size = new Vector2(defaultColliderSize.x, defaultColliderSize.y * 0.5f);
            box.offset = new Vector2(defaultColliderOffset.x,
                                     defaultColliderOffset.y - defaultColliderSize.y * 0.25f);
        }
        else
        {
            box.size = defaultColliderSize;
            box.offset = defaultColliderOffset;
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return true;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        onTriggerEntered?.Invoke();
    }
}
