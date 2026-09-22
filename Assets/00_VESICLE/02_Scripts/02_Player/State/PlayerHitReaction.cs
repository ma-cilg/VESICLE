//**플레이어 피격 시 넉백과 조작 제한 처리**
//책임: 피격 방향 전달받기 → 이동/점프/대시 잠금 → 넉백 적용 → 일정 시간 후 조작 복구
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //리지드바디 필수
public class PlayerHitReaction : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;                     //PlayerHealth.cs 이벤트 구독용

    [SerializeField] private PlayerMovement playerMovement;                 //피격 중 일반 이동 잠그기용(넉백 속도를 덮으면 안됨)
    [SerializeField] private PlayerJump playerJump;                         //피격 중 점프로 잠그기용(점프로 넉백 덮으면 안됨)
    [SerializeField] private PlayerDash playerDash;                         //피격 중 대시 잠그기(대시로 넉백 덮으면 안됨)

    [SerializeField, Min(0f)] private float knockbackSpeedX = 4.5f;         //피격 시 좌우로 밀려나는 속도
    [SerializeField, Min(0f)] private float knockbackSpeedY = 6f;           //피격 시 위쪽으로 튀어 오르는 속도
    [SerializeField, Min(0f)] private float controlLockDuration = 0.18f;    //피격 후 플레이어 조작을 잠깐 막는 시간

    private Rigidbody2D rb;
    private float remainingLockTime;                                        //현재 조작 제한 시간 얼마나 남았는지 저장
    private bool isReacting;                                                //현재 피격 반응이 진행 중인지 저장

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    private void OnEnable()
    {
        playerHealth.OnHitReceived += StartHitReaction;
    }

    private void OnDisable()
    {
        playerHealth.OnHitReceived -= StartHitReaction;

        if (isReacting)
        {
            EndHitReaction();
        }
    }

    private void Update()
    {
        if (!isReacting) return;
        remainingLockTime -= Time.deltaTime;
        if (remainingLockTime > 0f) return;

        EndHitReaction();
    }

    //*피격 반응 시작*
    private void StartHitReaction(Vector2 hitDirection)
    {
        isReacting = true;                              //현재 피격 반응 중이라고 기록
        remainingLockTime = controlLockDuration;        //조작 제한 시간을 처음 값으로 설정

        playerMovement.SetMovementEnabled(false);       //이동 잠금
        playerJump.SetJumpEnabled(false);               //점프 잠금
        playerDash.SetDashEnabled(false);               //대시 잠금

        //hitDirection에서 좌우 방향 가져옴
        float horizontalDirection = Mathf.Sign(hitDirection.x);

        //Player의 현재 속도를 피격 넉백 속도로 변경
        rb.linearVelocity = new Vector2(horizontalDirection * knockbackSpeedX, knockbackSpeedY);
    }

    //*피격 경직 종료*
    private void EndHitReaction()
    {
        if (!isReacting) return;

        isReacting = false;                         //피격 반응 종료 상태로 변경
        remainingLockTime = 0f;                     //남은 시간 초기화

        playerMovement.SetMovementEnabled(true);    //이동 다시 허용
        playerJump.SetJumpEnabled(true);            //점프 다시 허용
        playerDash.SetDashEnabled(true);            //대시 다시 허용
    }
}
