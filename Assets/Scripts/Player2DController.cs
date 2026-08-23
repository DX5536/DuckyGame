using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
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
    [SerializeField] private float gravityExtraScale = 500f;
    [SerializeField] private float gravityOGScale = 1.3f;

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

    [Header("-1=left, 0=idle, 1=right")]
    [SerializeField] private float horizontalInputValue; // For debugging in the Inspector

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
        // Enable the actions we use. We don't Disable on OnDisable because these InputActionReferences are shared assets/other consumers
        // (e.g. NPC_TriggerEvents) may need the same actions to stay enabled elsewhere.
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
        // Skip input if a dialog is running, the master IsMoving toggle is off or the required references are missing.
        if (isDialogRunning)
        { 
            keyBindings.CanMove = false;
            rb.linearVelocity = Vector2.zero;

            horizontalInputValue = 0;
            ChangeAnimation_WALKING(false);

            return;
        }
        else
        {
            keyBindings.CanMove = true;
        }



        if (keyBindings == null) return;
        

        InputAction moveAction = keyBindings.Move != null ? keyBindings.Move.action : null;
        InputAction jumpAction = keyBindings.Jump != null ? keyBindings.Jump.action : null;
        InputAction crouchAction = keyBindings.Crouch != null ? keyBindings.Crouch.action : null;

        bool grounded = IsGrounded();

        // Horizontal Movement
        // Read as Vector2 so it works with WASD composites, gamepad sticks, and on-screen joysticks.
        float horizontal = moveAction != null ? moveAction.ReadValue<Vector2>().x : 0f;
        float targetX = horizontal * moveSpeed;

        horizontalInputValue = horizontal; // For debugging in the Inspector


        //-------Animation/Sprite---------

        //-1=left, 0=idle, 1=right

        if (horizontalInputValue == 0f) //BACKUP: THIS WORK
        //if (keyBindings.CanMove == false)
        {
            ChangeAnimation_WALKING(false);
        }

        else
        {
            ChangeAnimation_WALKING(true);
        }

        //Flip the sprite based on movement direction (only if an Animator is assigned)
        if (targetX < 0f)
        {
            //If Player goes  left, flip the sprite to face left
            this.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        else if (targetX > 0f)
        {
            //If Player goes right, flip the sprite to face right
            this.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        // Full control on the ground; blended control in the air so momentum carries the player forward.
        float newX = grounded ? targetX : Mathf.Lerp(rb.linearVelocity.x, targetX, airControl);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        // Jump
        if (jumpAction != null && jumpAction.WasPressedThisFrame() && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        //-------Crouch---------
        //
        bool wantsCrouch = crouchAction != null && crouchAction.IsPressed();
        if (wantsCrouch != isCrouching) SetCrouch(wantsCrouch);

        //Animation (only if an Animator is assigned)
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontal));
            animator.SetBool("IsCrouching", isCrouching);
            animator.SetBool("IsGrounded", grounded);
        }
    }

    //Public method to access in other NPC/Item story collider)
    //When Dialog is running, to avoid slide, I will increase gravity

    //---OLD: Better to use rb.linearVelocity---
    /*public void StopPlayerSlidingViaGravity(string slidingBehaviour)
    {
        switch (slidingBehaviour)
        {
            case "STOP":
                rb.gravityScale = gravityExtraScale;
                break;

            case "GO":
                rb.gravityScale = gravityOGScale;
                break;

            case "STOP_AND_GO":
                StartCoroutine(DelayUnStopSliding(1f));
                break;
        }
    }*/

    private void ChangeAnimation_WALKING(bool isWalking)
    {
        animator.SetBool("isWalking", isWalking);
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

    /*IEnumerator DelayUnStopSliding(float delayTime)
    {
        rb.bodyType = RigidbodyType2D.Static;

        //Wait for the specified delay time before continuing.
        yield return new WaitForSeconds(delayTime);

        rb.bodyType = RigidbodyType2D.Dynamic;
    }*/
}
