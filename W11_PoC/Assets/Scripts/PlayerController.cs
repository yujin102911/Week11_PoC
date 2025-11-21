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

    public bool onLadder = false;
    public bool nearLadder = false;

    [Header("Interact")]
    public LayerMask interactMask;
    public Transform interactPoint;
    public float interactRadius = 1f;
    private ItemPickup CurrentHighlighted;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        HandleCoyoteTime();
        HandleLadderDetection();
        FindInteract();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleClimbing();
    }

    void FindInteract()
    {
        Vector2 pos = this.transform ?
        (Vector2)this.transform.position :
        (Vector2)transform.position;

        // 반경 내 모든 오브젝트 탐색
        var hits = Physics2D.OverlapCircleAll(pos, interactRadius, interactMask);

        // 이번 프레임 선택될 가장 가까운 오브젝트
        ItemPickup best = null;

        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float d = Vector2.SqrMagnitude((Vector2)hit.transform.position - pos);
            if (d < bestDist)
            {
                bestDist = d;
                best = hit.GetComponent<ItemPickup>();
            }
        }

        // ⭐ 이전 프레임의 하이라이트 제거
        if (CurrentHighlighted != null && CurrentHighlighted != best)
        {
            CurrentHighlighted.RestoreAlpha();
            CurrentHighlighted = null;
        }

        // ⭐ 새로 선택된 오브젝트 하이라이트 적용
        if (best != null && best != CurrentHighlighted)
        {
            best.SetHighlight();
            CurrentHighlighted = best;
        }
    }

    // ---------------- Movement ---------------- //
    void HandleMovement()
    {
        if (onLadder)
            return;

        float targetX = moveInput.x * moveSpeed;


        // ● velocity → linearVelocity 변경
        Vector2 v = rb.linearVelocity;
        float currentY = v.y;

        v.x = targetX;
        v.y = currentY;

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
            rb.linearVelocityX = 0f;
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

    private void OnTriggerStay2D(Collider2D collision)
    {
        if ((collision.gameObject.layer == LayerMask.NameToLayer("Ladder")) && onLadder)
        {
            Vector2 newPos = this.transform.position;
            newPos.x = collision.transform.position.x;
            this.transform.position = newPos;
        }
    }
}

public interface IInteractable
{
    void Interact();
}
