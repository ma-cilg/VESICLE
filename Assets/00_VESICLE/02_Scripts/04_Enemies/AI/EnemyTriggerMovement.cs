//**트리거몹 이동과 물리**
//책임: 추적/돌진/정지/낙하 물리 이동, 바닥 판정 (FixedUpdate에서 1회 확인)
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyTriggerMovement : MonoBehaviour
{
    public enum MoveMode { Idle, Chase, Charge, Dash, Stop, Fall }

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [Header("Ground")]
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.15f;
    [SerializeField, Min(0f)] private float normalGravityScale = 3f;

    [Header("Speed")]
    [SerializeField, Min(0f)] private float chaseSpeed = 4f;
    [SerializeField, Min(0f)] private float dashSpeed = 24f;
    [SerializeField, Min(0f)] private float chaseStopDistance = 0.15f;

    private MoveMode mode = MoveMode.Idle;
    private Vector2 dashDirection;

    public MoveMode Mode => mode;
    public bool IsGrounded { get; private set; }
    public bool IsRunning => mode == MoveMode.Chase && IsGrounded &&
                             Mathf.Abs(rb.linearVelocity.x) > 0.1f;
    public float PlayerOffsetX => playerTarget == null ? 0f : playerTarget.position.x - transform.position.x;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = normalGravityScale;
    }

    //*상태 전환 때만 중력/속도 변경*
    public void SetMode(MoveMode nextMode)
    {
        if (mode == nextMode) return;
        mode = nextMode;

        rb.gravityScale = (mode == MoveMode.Dash || mode == MoveMode.Stop)
            ? 0f : normalGravityScale;

        if (mode == MoveMode.Dash)
            rb.linearVelocity = dashDirection * dashSpeed;
        else if (mode == MoveMode.Stop)
            rb.linearVelocity = Vector2.zero;
        else
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    //*차징 시작 시 기억한 방향으로 이후 돌진*
    public void SetDashDirection(Vector2 direction)
    {
        dashDirection = direction.sqrMagnitude > 0.001f
            ? direction.normalized : Vector2.right;
    }

    private void FixedUpdate()
    {
        //바닥 판정은 필요한 이동 상태에서만 물리 프레임당 한 번 수행
        if (mode == MoveMode.Chase || mode == MoveMode.Fall)
        {
            IsGrounded = groundCheck != null &&
                Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
        }

        switch (mode)
        {
            case MoveMode.Chase:
                float distanceX = PlayerOffsetX;
                float horizontal = IsGrounded && Mathf.Abs(distanceX) > chaseStopDistance
                    ? Mathf.Sign(distanceX) * chaseSpeed : 0f;
                rb.linearVelocity = new Vector2(horizontal, rb.linearVelocity.y);
                break;

            case MoveMode.Charge:
            case MoveMode.Idle:
            case MoveMode.Fall:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;

            case MoveMode.Dash:
                rb.linearVelocity = dashDirection * dashSpeed;
                break;

            case MoveMode.Stop:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void OnDisable()
    {
        if (rb == null) return;
        rb.gravityScale = normalGravityScale;
        rb.linearVelocity = Vector2.zero;
        mode = MoveMode.Idle;
    }
}
