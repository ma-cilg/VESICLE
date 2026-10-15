//**노멀몹 자동 점프 처리**
//책임: 바닥 감지 → 플레이어 높이 / 장애물 확인 → 점프 실행
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyJump : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private EnemyDeath enemyDeath;
    [SerializeField] private EnemyMark enemyMark;
    [SerializeField] private EnemyHitReaction hitReaction;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Rigidbody2D rb;

    [Header("Ground")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.15f;

    [Header("Jump")]
    [SerializeField, Min(0f)] private float jumpSpeed = 20f;
    [SerializeField, Min(0f)] private float jumpCooldown = 0.9f;

    [Header("Jump Conditions")]
    [SerializeField, Min(0f)] private float minPlayerHeight = 0.9f;
    [SerializeField, Min(0f)] private float maxJumpTargetDistance = 3.5f;
    [SerializeField, Min(0f)] private float obstacleCheckDistance = 0.6f;

    [SerializeField] private Vector2 obstacleCheckOffset = new Vector2(0f, 0.5f);

    private float nextJumpTime;

    public event Action OnJumped;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    private void FixedUpdate()
    {
        if (!CanJump()) return;
        if (Time.time < nextJumpTime) return;
        if (!IsGrounded()) return;

        float distanceX = playerTarget.position.x - rb.position.x;
        float distanceY = playerTarget.position.y - rb.position.y;

        //플레이어가 가까운 거리에서 높은 위치에 있는지 확인
        bool isPlayerAbove = distanceY >= minPlayerHeight && Mathf.Abs(distanceX) <= maxJumpTargetDistance;

        //앞을 가로막는 벽이 있는지 확인
        bool hasObstacle = CheckObstacleAhead(distanceX);

        //점프할 필요가 없으면 계속 달리기
        if (!isPlayerAbove && !hasObstacle) return;

        Jump();
    }

    //*현재 점프할 수 있는 상태인지 확인*
    private bool CanJump()
    {
        //문이 닫히고 전투가 시작되어야 점프 가능
        if (combatRoom == null || !combatRoom.IsStarted) return false;
        if (combatRoom.IsCleared) return false;

        if (enemyDeath == null || enemyDeath.IsDead) return false;
        if (enemyMark != null && enemyMark.IsMarked) return false;

        //피격 경직 중에는 점프하지 않음
        if (hitReaction != null && hitReaction.IsStaggered) return false;

        if (playerHealth == null || playerHealth.IsDead) return false;
        if (playerTarget == null || groundCheck == null) return false;

        return true;
    }

    //*발밑에 바닥이 있는지 검사*
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    //*플레이어 방향에 벽이 있는지 확인*
    private bool CheckObstacleAhead(float distanceX)
    {
        //플레이어와 X 위치가 거의 같다면 벽 검사 생략
        if (Mathf.Abs(distanceX) < 0.1f) return false;

        float direction = Mathf.Sign(distanceX);

        Vector2 origin = (Vector2)transform.position + obstacleCheckOffset;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * direction, obstacleCheckDistance, groundLayer);

        return hit.collider != null;
    }

    //*점프 실행*
    private void Jump()
    {
        //EnemyChase가 관리하는 X 속도는 그대로 유지
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);

        nextJumpTime = Time.time + jumpCooldown;

        OnJumped?.Invoke();
    }

    //*Scene 뷰에서 감지 범위 확인*
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (playerTarget == null) return;

        float direction = Mathf.Sign(playerTarget.position.x - transform.position.x);

        Vector3 origin = transform.position + (Vector3)obstacleCheckOffset;

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(origin, origin + Vector3.right * direction * obstacleCheckDistance);
    }
}
