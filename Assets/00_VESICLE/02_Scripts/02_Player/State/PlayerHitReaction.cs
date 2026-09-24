//**플레이어 피격 시 넉백과 조작 제한 처리**
//책임: 피격 방향 전달받기 → 조작 잠금 → 넉백 적용 → 일정 시간 후 조작 복구
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //리지드바디 필수
public class PlayerHitReaction : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;                     //PlayerHealth.cs 이벤트 구독용
    [SerializeField] private PlayerControlLock controlLock;                 //여러 시스템의 조작 잠금을 겹치지 않게 통합 관리
    [SerializeField] private PlayerWeaponThrow playerWeaponThrow;           //투척 중 피격 반응 예외 확인

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
        if (playerWeaponThrow.IsThrowing) return;

        isReacting = true;                              //현재 피격 반응 중이라고 기록
        remainingLockTime = controlLockDuration;        //조작 제한 시간을 처음 값으로 설정

        //피격 중 통합 잠금
        controlLock.LockMovement();
        controlLock.LockJump();
        controlLock.LockDash();
        controlLock.LockAttack();
        controlLock.LockThrow();
        controlLock.LockDetonate();

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

        //피격 종료시 통합 잠금 해제
        controlLock.UnlockMovement();
        controlLock.UnlockJump();
        controlLock.UnlockDash();
        controlLock.UnlockAttack();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();
    }
}
