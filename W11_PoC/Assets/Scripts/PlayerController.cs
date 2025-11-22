using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    #region Fields

    private Rigidbody2D rb;
    private Vector2 moveInput;

    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Jump")]
    [Tooltip("최고점까지 얼마나 높이 점프할지(유닛)")]
    public float jumpHeight = 4f;

    [Tooltip("바닥에서 최고점까지 올라가는 데 걸리는 시간(초)")]
    public float timeToJumpApex = 0.4f;

    [Header("Gravity Multipliers")]
    [Tooltip("올라갈 때 중력 배수")]
    public float upwardMovementMultiplier = 1f;

    [Tooltip("떨어질 때 중력 배수 (값이 클수록 더 빨리 떨어짐)")]
    public float downwardMovementMultiplier = 4f;

    [Tooltip("점프 버튼 길게/짧게로 점프 높이 조절할지 여부")]
    public bool variableJumpHeight = true;

    [Tooltip("점프 도중 버튼을 뗐을 때 적용되는 중력 배수")]
    public float jumpCutOff = 3f;

    [Tooltip("최대 낙하 속도(터미널 속도)")]
    public float speedLimit = 25f;

    [Header("Coyote & Buffer")]
    [Tooltip("코요테 타임(발이 떨어진 뒤에도 점프 허용 시간)")]
    public float coyoteTime = 0.1f;
    private float coyoteTimer;

    [Tooltip("점프 버퍼 시간(점프가 막혀도 입력을 기억하는 시간)")]
    public float jumpBuffer = 0.1f;
    private float jumpBufferTimer;

    [Tooltip("사다리에서 점프할 때 쓸 점프 힘")]
    public float ladderJumpForce = 14f;

    // 점프 상태
    private bool desiredJump;
    private bool pressingJump;
    private bool currentlyJumping;
    private float gravMultiplier = 1f;

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

    #endregion

    #region Unity Callbacks

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        HandleCoyoteTime();
        HandleJumpBuffer();
        HandleLadderDetection();
        FindInteract();
    }

    void FixedUpdate()
    {
        if (UIManager.Instance.Is_panel)
            return;

        HandleMovement();
        HandleClimbing();

        if (!onLadder)
        {
            UpdateJumpGravity();
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

    #endregion

    #region Movement

    // ---------------- Movement ---------------- //
    void HandleMovement()
    {
        if (onLadder)
            return;

        float targetX = moveInput.x * moveSpeed;

        Vector2 v = rb.linearVelocity;
        float currentY = v.y;

        v.x = targetX;
        v.y = currentY;

        rb.linearVelocity = v;
    }

    // ---------------- 이동 ---------------- //
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (UIManager.Instance.Is_panel)
            return;

        moveInput = ctx.ReadValue<Vector2>();
    }

    #endregion

    #region Jump

    void HandleCoyoteTime()
    {
        if (IsGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;
    }

    void HandleJumpBuffer()
    {
        if (jumpBuffer <= 0f || !desiredJump)
            return;

        // 점프 시도 (성공하면 desiredJump가 false로 꺼짐)
        TryJump();

        if (desiredJump)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (jumpBufferTimer <= 0f)
            {
                desiredJump = false;
            }
        }
    }

    // ---------------- 점프 ---------------- //
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (UIManager.Instance.Is_panel)
            return;

        if (ctx.started)
        {
            pressingJump = true;

            if (onLadder)
            {
                onLadder = false;
                rb.gravityScale = 0f; // 다음 FixedUpdate에서 자동으로 설정됨

                Vector2 v = rb.linearVelocity;
                v.y = ladderJumpForce;
                rb.linearVelocity = v;
                return;
            }

            desiredJump = true;
            jumpBufferTimer = jumpBuffer;
            TryJump(); // 즉시 한 번 시도
        }
        else if (ctx.canceled)
        {
            pressingJump = false;
        }
    }

    void TryJump()
    {
        if (onLadder)
            return;

        bool canJumpNow = IsGrounded || coyoteTimer > 0f;
        if (!canJumpNow)
            return;

        Vector2 v = rb.linearVelocity;

        float safeTimeToApex = Mathf.Max(0.01f, timeToJumpApex);
        float baseGravity = (-2f * jumpHeight) / (safeTimeToApex * safeTimeToApex);
        float gravityScale = baseGravity / Physics2D.gravity.y;
        float jumpSpeed = Mathf.Sqrt(-2f * Physics2D.gravity.y * gravityScale * jumpHeight);

        if (v.y > 0f)
            jumpSpeed = Mathf.Max(jumpSpeed - v.y, 0f);
        else if (v.y < 0f)
            jumpSpeed += Mathf.Abs(v.y);

        v.y += jumpSpeed;
        rb.linearVelocity = v;

        desiredJump = false;
        currentlyJumping = true;
        coyoteTimer = 0f;
    }

    void UpdateJumpGravity()
    {
        Vector2 v = rb.linearVelocity;
        bool grounded = IsGrounded;

        if (v.y > 0.01f)
        {
            if (grounded)
            {
                gravMultiplier = 1f;
            }
            else
            {
                if (variableJumpHeight)
                {
                    if (pressingJump && currentlyJumping)
                        gravMultiplier = upwardMovementMultiplier;
                    else
                        gravMultiplier = jumpCutOff;
                }
                else
                {
                    gravMultiplier = upwardMovementMultiplier;
                }
            }
        }
        else if (v.y < -0.01f)
        {
            if (grounded)
                gravMultiplier = 1f;
            else
                gravMultiplier = downwardMovementMultiplier;
        }
        else
        {
            if (grounded)
                currentlyJumping = false;

            gravMultiplier = 1f;
        }

        float safeTimeToApex = Mathf.Max(0.01f, timeToJumpApex);
        float baseGravity = (-2f * jumpHeight) / (safeTimeToApex * safeTimeToApex);
        rb.gravityScale = (baseGravity / Physics2D.gravity.y) * gravMultiplier;

        v.y = Mathf.Clamp(v.y, -speedLimit, float.MaxValue);
        rb.linearVelocity = v;
    }

    #endregion

    #region Ladder

    // ---------------- 사다리 ---------------- //
    void HandleLadderDetection()
    {
        nearLadder = Physics2D.OverlapCircle(transform.position, 0.3f, ladderMask);

        if (!nearLadder && onLadder)
        {
            onLadder = false;
            // rb.gravityScale는 FixedUpdate에서 다시 세팅
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

    #endregion

    #region Interact

    void FindInteract()
    {
        Vector2 pos = this.transform ?
            (Vector2)this.transform.position :
            (Vector2)transform.position;

        var hits = Physics2D.OverlapCircleAll(pos, interactRadius, interactMask);

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

        if (CurrentHighlighted != null && CurrentHighlighted != best)
        {
            CurrentHighlighted.RestoreAlpha();
            CurrentHighlighted = null;
        }

        if (best != null && best != CurrentHighlighted)
        {
            best.SetHighlight();
            CurrentHighlighted = best;
        }
    }

    // ---------------- 상호작용 ---------------- //
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;

        if (UIManager.Instance.Is_panel)
            return;

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

        moveInput = Vector2.zero;
    }

    #endregion
}

public interface IInteractable
{
    void Interact();
}
