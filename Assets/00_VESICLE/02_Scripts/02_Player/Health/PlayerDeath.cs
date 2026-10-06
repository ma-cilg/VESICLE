//**플레이어 사망 시 조작 정지 처리**
//책임: PlayerHealth 사망 감지 → 플레이어 조작 전체 잠금 → 현재 이동 정지
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeath : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;             //플레이어 사망 이벤트 확인
    [SerializeField] private PlayerControlLock controlLock;         //플레이어 조작 전체 잠금
    [SerializeField] private PlayerAttack playerAttack;             //사망 순간 진행 중인 공격 강제 종료

    private Rigidbody2D rb;
    private bool controlsLocked;                                    //사망 Lock 중복 적용 방지

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        playerHealth.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        playerHealth.OnDied -= HandleDeath;

        ReleaseDeathControls();
    }

    //*사망으로 걸었던 조작 Lock 해제*
    private void ReleaseDeathControls()
    {
        if (!controlsLocked) return;

        controlsLocked = false;

        controlLock.UnlockMovement();
        controlLock.UnlockJump();
        controlLock.UnlockAttack();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();
    }

    //*Respawn 시 사망 상태 복구*
    public void ResetDeathState()
    {
        //PlayerDeath가 사망 때 걸었던 Lock만 해제
        ReleaseDeathControls();

        //Respawn 직후 이전 속도가 남지 않도록 정지
        rb.linearVelocity = Vector2.zero;
    }

    //*플레이어 사망 처리*
    private void HandleDeath()
    {
        if (controlsLocked) return;

        controlsLocked = true;

        //사망 상태의 Lock을 먼저 걸어서
        //진행 중 공격이 자기 Lock을 풀어도 조작이 다시 켜지지 않게 함
        controlLock.LockMovement();
        controlLock.LockJump();
        controlLock.LockAttack();
        controlLock.LockThrow();
        controlLock.LockDetonate();

        //PreSlash / Slash / Finish 중이었다면 즉시 공격 종료
        playerAttack.InterruptAttack();

        //사망 순간 남아있는 이동 속도 제거
        rb.linearVelocity = Vector2.zero;
    }
}
