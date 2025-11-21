using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 moveInput;

    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Jump")]
    public float jumpForce = 14f;
    public float coyoteTime = 0.1f;
    private float coyoteTimer;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.15f;
    public LayerMask groundMask;

    bool IsGrounded =>
        Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);

    [Header("Ladder")]
    public LayerMask ladderMask;
    public float climbSpeed = 4f;

    bool onLadder = false;
    bool nearLadder = false;

    [Header("Interact")]
    public LayerMask interactMask;
    public Transform interactPoint;
    public float interactRadius = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        HandleCoyoteTime();
        HandleLadderDetection();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleClimbing();
    }

    // ---------------- Movement ---------------- //
    void HandleMovement()
    {
        if (onLadder)
            return;

        float targetX = moveInput.x * moveSpeed;

        // ● velocity → linearVelocity 변경
        Vector2 v = rb.linearVelocity;
        v.x = targetX;
        rb.linearVelocity = v;
    }

    void HandleCoyoteTime()
    {
        if (IsGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;
    }


    // ---------------- 이동 ---------------- //
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }


    // ---------------- 점프 ---------------- //

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;

        Vector2 v = rb.linearVelocity;

        if (onLadder)
        {
            onLadder = false;
            rb.gravityScale = 3f;

            v.y = jumpForce;
            rb.linearVelocity = v;
            return;
        }

        if (coyoteTimer > 0f)
        {
            v.y = jumpForce;
            rb.linearVelocity = v;
            coyoteTimer = 0f;
        }
    }

    // ---------------- 상호작용 ---------------- //
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;

        Vector2 pos = interactPoint ? (Vector2)interactPoint.position : (Vector2)transform.position;

        var hits = Physics2D.OverlapCircleAll(pos, interactRadius, interactMask);
        if (hits.Length == 0) return;

        float bestDist = float.MaxValue;
        IInteractable best = null;

        foreach (var hit in hits)
        {
            float d = Vector2.SqrMagnitude((Vector2)hit.transform.position - pos);
            if (d < bestDist)
            {
                bestDist = d;
                best = hit.GetComponent<IInteractable>();
            }
        }

        if (best != null)
            best.Interact();
    }

    // ---------------- 사다리 ---------------- //
    void HandleLadderDetection()
    {
        nearLadder = Physics2D.OverlapCircle(transform.position, 0.3f, ladderMask);

        if (!nearLadder && onLadder)
        {
            onLadder = false;
            rb.gravityScale = 3f;
        }
    }

    void HandleClimbing()
    {
        if (!nearLadder)
            return;

        if (Mathf.Abs(moveInput.y) > 0.1f)
        {
            onLadder = true;
            rb.gravityScale = 0f;
        }

        if (onLadder)
        {
            Vector2 v = rb.linearVelocity;
            v.y = moveInput.y * climbSpeed;
            rb.linearVelocity = v;
        }
    }


    // ---------------- Gizmos ---------------- //
    private void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }

        Vector3 p = interactPoint ? interactPoint.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(p, interactRadius);
    }
}

public interface IInteractable
{
    void Interact();
}
