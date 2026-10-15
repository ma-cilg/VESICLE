//**플레이어 벽잡기**
//책임: 벽 접촉 확인 → 벽에 고정 → 이탈 및 물리 상태 복구
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerWallGrab : MonoBehaviour
{
    [Header("Wall")]
    [SerializeField] private LayerMask wallLayer;

    [SerializeField, Min(0.01f)]
    private float wallCheckDistance = 0.15f;

    private Rigidbody2D rb;
    private CapsuleCollider2D bodyCollider;
    private PlayerInputReader inputReader;
    private PlayerMovement playerMovement;
    private PlayerJump playerJump;
    private PlayerAttack playerAttack;
    private PlayerHealth playerHealth;
    private PlayerControlLock controlLock;

    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[8];

    private float originalGravityScale;
    private float wallDirection;

    public bool IsWallGrabbing { get; private set; }

    private void Awake()
    {
        //Player 최상위 오브젝트의 기존 컴포넌트 재사용
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        inputReader = GetComponent<PlayerInputReader>();
        playerMovement = GetComponent<PlayerMovement>();
        playerJump = GetComponent<PlayerJump>();
        playerAttack = GetComponent<PlayerAttack>();
        playerHealth = GetComponent<PlayerHealth>();
        controlLock = GetComponent<PlayerControlLock>();
    }

    private void OnEnable()
    {
        playerAttack.OnPreSlashStarted += ReleaseWallGrab;
        playerHealth.OnHitReceived += HandleHit;
    }

    private void OnDisable()
    {
        playerAttack.OnPreSlashStarted -= ReleaseWallGrab;
        playerHealth.OnHitReceived -= HandleHit;

        ReleaseWallGrab();
    }

    private void Update()
    {
        float moveX = inputReader.MoveInput.x;

        //벽에 붙어 있는 상태
        if (IsWallGrabbing)
        {
            bool moveAway = moveX * wallDirection < -0.3f;
            bool jumpPressed = inputReader.IsJumpPressed();

            //반대 방향, 점프, 벽 접촉 해제 시 떨어지기
            if (moveAway || jumpPressed || !IsWallTouching(wallDirection) || playerHealth.IsDead)
            {
                ReleaseWallGrab();
            }

            return;
        }

        //조작 잠금, 사망, 공격, 지상 상태에서는 벽잡기 불가
        if (!playerMovement.CanMove || playerHealth.IsDead) return;

        if (playerAttack.IsAttacking || playerJump.IsGrounded) return;

        //상승 중에는 벽에 붙지 않음
        if (rb.linearVelocity.y > 0.01f) return;

        //벽을 향해 이동 입력 중인지 확인
        if (Mathf.Abs(moveX) < 0.3f) return;

        float direction = Mathf.Sign(moveX);

        if (IsWallTouching(direction)) StartWallGrab(direction);
    }

    private void FixedUpdate()
    {
        if (!IsWallGrabbing) return;

        //붙어 있는 동안 이동 및 낙하 정지
        rb.linearVelocity = Vector2.zero;
    }

    //*플레이어 Collider 바로 옆에 벽이 있는지 검사*
    private bool IsWallTouching(float direction)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(wallLayer);
        filter.useTriggers = false;

        Vector2 castDirection = Vector2.right * direction;

        int hitCount = bodyCollider.Cast(castDirection, filter, wallHits, wallCheckDistance);

        for (int i = 0; i < hitCount; i++)
        {
            //실제 수직 벽면만 벽잡기 대상으로 사용
            if (wallHits[i].normal.x * direction < -0.7f) return true;
        }

        return false;
    }

    //*벽잡기 시작*
    private void StartWallGrab(float direction)
    {
        if (IsWallGrabbing) return;

        IsWallGrabbing = true;
        wallDirection = direction;

        originalGravityScale = rb.gravityScale;

        //다른 시스템과 충돌하지 않도록 기존 Lock 재사용
        controlLock.LockMovement();

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
    }

    //*벽잡기 종료*
    private void ReleaseWallGrab()
    {
        if (!IsWallGrabbing) return;

        IsWallGrabbing = false;

        rb.gravityScale = originalGravityScale;

        //벽잡기가 걸었던 이동 Lock만 해제
        controlLock.UnlockMovement();
    }

    //*피격 시 벽잡기 종료*
    private void HandleHit(Vector2 hitDirection)
    {
        ReleaseWallGrab();
    }
}
