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

    [Header("Inventory / Dash Lock")]
    [Tooltip("인벤토리에 물건이 하나 이상 있으면 true로 설정 (그리드/인벤토리에서 세팅)")]
    public bool hasItemInInventory = false;

    [SerializeField]
    [Tooltip("플레이어 인벤토리로 사용할 Grid (GridType.Inventory)")]
    private Grid inventoryGrid;

    [Header("Dash")]
    [Tooltip("대시 속도 (수평)")]
    public float dashSpeed = 12f;

    [Tooltip("대시가 유지되는 시간(초)")]
    public float dashDuration = 0.2f;

    [Tooltip("다음 대시까지 대기 시간(쿨타임)")]
    public float dashCooldown = 0.3f;

    [Header("Dash FX")]
    [Tooltip("대시할 때만 켜줄 트레일 렌더러")]
    public TrailRenderer dashTrail;


    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private int dashDirection = 1;   // -1 왼쪽, 1 오른쪽

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

        // 인벤토리 그리드 상태를 보고 대시 잠금 여부 갱신
        if (inventoryGrid != null)
        {
            hasItemInInventory = inventoryGrid.HasAnyItem;
        }
        else
        {
            hasItemInInventory = false;
        }
    }

    void FixedUpdate()
    {
        if (UIManager.Instance.Is_panel)
            return;

        HandleDashPhysics();

        // 대시 중이면 일반 이동/점프 중력은 막는다
        if (isDashing)
            return;

        HandleMovement();
        HandleClimbing();

        if (!onLadder)
        {
            UpdateJumpGravity();
        }
    }

    void HandleDashPhysics()
    {
        // 쿨타임 타이머
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.fixedDeltaTime;

        if (!isDashing)
            return;

        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
        {
            // 대시 종료
            isDashing = false;

            // 대시 끝나면 트레일 끄기
            if (dashTrail != null)
                dashTrail.emitting = false;

            return;
        }

        // 대시 중일 때는 수평 속도를 강제로 고정
        Vector2 v = rb.linearVelocity;
        v.x = dashDirection * dashSpeed;
        rb.linearVelocity = v;
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
            TryJump();
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

    #region Dash

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (!ctx.started)
            return;

        if (UIManager.Instance.Is_panel)
            return;

        // 인벤토리에 물건이 있으면 대시 불가
        if (hasItemInInventory)
            return;

        // 사다리 위에서는 대시 안 되게
        if (onLadder)
            return;

        // 이미 대시 중이거나 쿨타임이면 불가
        if (isDashing || dashCooldownTimer > 0f)
            return;

        // 방향 결정: 입력 x가 0이면, 바라보는 방향 기준
        float xInput = moveInput.x;
        if (Mathf.Abs(xInput) < 0.1f)
        {
            xInput = transform.localScale.x >= 0f ? 1f : -1f;
        }

        dashDirection = xInput > 0f ? 1 : -1;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        // 수직 속도는 잠깐 0으로 깔끔하게
        Vector2 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        // 대시 시작 시 트레일 켜기
        if (dashTrail != null)
        {
            dashTrail.Clear();       // 이전 잔상 깔끔히
            dashTrail.emitting = true;
        }
    }

    #endregion

    #region Ladder

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

            Vector2 v = rb.linearVelocity;
            v.x = 0f;
            rb.linearVelocity = v;
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
