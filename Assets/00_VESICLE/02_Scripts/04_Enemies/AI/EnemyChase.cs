//**노멀몹 플레이어 추적**
//책임: 방 시작 확인 → 플레이어 방향 추적 → 이동 및 정지
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private EnemyDeath enemyDeath;
    [SerializeField] private EnemyMark enemyMark;
    [SerializeField] private EnemyHitReaction hitReaction;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5.5f;

    //플레이어 중심에서 좌우로 떨리는 현상 방지
    [SerializeField, Min(0f)] private float stopDistance = 0.8f;


    [Header("Spawn Landing")]
    [SerializeField] private bool waitForFirstLanding;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0.01f)]
    private float groundCheckRadius = 0.15f;

    private bool hasLanded;

    private void OnEnable()
    {
        //기존 사용처는 기본값 false이므로 영향 없음
        hasLanded = !waitForFirstLanding;
    }


    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    private void FixedUpdate()
    {
        if (!CanChase())
        {
            StopHorizontalMovement();
            return;
        }


        //공중 소환된 적 첫 착지 전까지 수직 낙하
        if (!hasLanded)
        {
            bool grounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;

            if (!grounded)
            {
                StopHorizontalMovement();
                return;
            }

            hasLanded = true;
        }

        //플레이어까지의 가로 / 세로 거리
        float distanceX = playerTarget.position.x - rb.position.x;
        float distanceY = playerTarget.position.y - rb.position.y;

        //플레이어가 위에 있으면 정지 거리를 줄여
        //점프 중에도 벽 쪽으로 적극적으로 접근
        float currentStopDistance = distanceY > 0.9f ? 0.05f : stopDistance;

        if (Mathf.Abs(distanceX) <= currentStopDistance)
        {
            StopHorizontalMovement();
            return;
        }


        //플레이어가 있는 방향으로 계속 달리기
        float direction = Mathf.Sign(distanceX);

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    //*현재 추적할 수 있는 상태인지 확인*
    private bool CanChase()
    {
        //방에 입장하여 문이 닫히기 전에는 행동하지 않음
        if (combatRoom == null || !combatRoom.IsStarted) return false;

        if (combatRoom.IsCleared) return false;

        //사망 또는 완전 마킹 상태에서는 정지
        if (enemyDeath == null || enemyDeath.IsDead) return false;

        if (enemyMark != null && enemyMark.IsMarked) return false;

        //피격 경직 중에는 이동하지 않음
        if (hitReaction != null && hitReaction.IsStaggered) return false;

        //플레이어가 없거나 사망했다면 정지
        if (playerHealth == null || playerHealth.IsDead) return false;

        if (playerTarget == null) return false;

        return true;
    }

    //*좌우 이동만 정지하고 중력은 유지*
    private void StopHorizontalMovement()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void OnDisable()
    {
        StopHorizontalMovement();
    }
}
