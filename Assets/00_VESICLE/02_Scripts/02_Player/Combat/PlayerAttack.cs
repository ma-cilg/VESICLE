//**플레이어 기본 공격 처리**
//책임: 공격 입력 처리 → 지상/공중 공격 시작 → 공격 중 일부 조작 제한 → 점프로 공격 캔슬 → 애니메이션 이벤트 전달
using System;       //Action 이벤트 사용
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;     //PlayerInputReader 연결
    [SerializeField] private PlayerMovement playerMovement;     //공격 방향과 보조 이동 처리
    [SerializeField] private PlayerJump playerJump;             //지상/공중 판단 및 점프 이벤트 확인
    [SerializeField] private PlayerDash playerDash;             //대시 시작 시 공격 캔슬
    [SerializeField] private PlayerControlLock controlLock;     //외부 시스템이 공격을 막고 있는지 확인

    [SerializeField, Min(0f)] private float stationaryAdvanceSpeed = 15f;       //제자리 공격 시 타격 순간 앞으로 살짝 이동하는 속도
    [SerializeField, Min(0f)] private float stationaryAdvanceDuration = 0.1f;   //제자리 공격에서 앞으로 살짝 이동하는 시간

    public bool IsAttacking { get; private set; }               //현재 공격 중인지 외부에서도 확인 가능
    public bool IsAirAttack { get; private set; }               //현재 공격이 공중 공격인지 확인

    public event Action OnGroundAttackStarted;                  //지상 공격 이벤트 알림
    public event Action OnAirAttackStarted;                     //공중 공격 이벤트 알림
    public event Action OnAttackHit;                            //실제 칼이 적에게 닿는 프레임 이벤트 알림
    public event Action OnAttackCancelled;                      //공격이 중간에 취소됐다는 이벤트 알림
    public event Action OnAttackEnded;                          //공격 애니메이션이 끝까지 정상적으로 끝났다는 이벤트 알림

    private void OnEnable()
    {
        playerJump.OnJumped += HandleActionStarted;             //1단 점프가 발생하면 공격 취소
        playerJump.OnDoubleJumped += HandleActionStarted;       //더블점프가 발생해도 공격 취소
        playerJump.OnLanded += HandleLanded;                    //공중 공격 도중 착지시 공중 공격 상태 정리
        playerDash.OnDashStarted += HandleActionStarted;        //대시가 실제로 시작된 순간 공격 취소
    }

    private void OnDisable()
    {
        //등록한 이벤트 구독 해제
        playerJump.OnJumped -= HandleActionStarted;
        playerJump.OnDoubleJumped -= HandleActionStarted;
        playerJump.OnLanded -= HandleLanded;
        playerDash.OnDashStarted -= HandleActionStarted;

        //공격 도중 컴포넌트가 꺼졌다면 상태를 남기지 않고 정리
        if (IsAttacking)
        {
            EndAttack(false);
        }
    }

    private void Update()
    {
        if (!inputReader.IsAttackPressed()) return;
        if (!controlLock.CanAttack) return;
        if (IsAttacking) return;

        if (playerJump.IsGrounded)
        {
            StartGroundAttack();   //바닥에 있으면 지상 공격 
        }
        else
        {
            StartAirAttack();       //공중에 있으면 공중 공격
        }
    }

    //*지상 공격 시작*
    private void StartGroundAttack()
    {
        IsAttacking = true;                     //공격 상태 시작
        IsAirAttack = false;                    //지상 공격 상태 기록
        OnGroundAttackStarted?.Invoke();        //지상 공격 애니메이션 시작 알림
    }

    //*공중 공격 시작*
    private void StartAirAttack()
    {
        IsAttacking = true;                     //공격 상태 시작
        IsAirAttack = true;                     //공중 공격 상태 기록
        OnAirAttackStarted?.Invoke();           //공중 공격 애니메이션 시작 알림
    }

    //*실제 타격 프레임에서 Animation Event가 호출*
    public void HandleAttackHitAnimationEvent()
    {
        if (!IsAttacking) return;

        //제자리 공격 보조 이동 적용
        if (!IsAirAttack && Mathf.Abs(inputReader.MoveInput.x) < 0.01f)
        {
            //현재 플레이어가 바라보는 방향 확인
            float direction = playerMovement.IsFacingRight ? 1f : -1f;

            //PlayerMovement에게 짧은 전진 이동 요청
            playerMovement.ApplyTemporaryMove(direction * stationaryAdvanceSpeed, stationaryAdvanceDuration);
        }
        OnAttackHit?.Invoke();          //타격 프레임이 왔다고 알림
    }

    //*공격 애니메이션 마지막 프레임에서 호출*
    public void HandleAttackEndAnimationEvent()
    {
        if (!IsAttacking) return;
        EndAttack(true);                //공격 정상 종료
    }

    //*점프 / 더블점프 / 대시 등 다른 행동이 실제로 시작됨*
    private void HandleActionStarted()
    {
        if (!IsAttacking) return;
        CancelAttack();                 //현재 공격 즉시 취소
    }

    //*착지 처리*
    private void HandleLanded()
    {
        if (!IsAttacking) return;
        if (!IsAirAttack) return;
        CancelAttack();                 //공중 공격 취소
    }

    //*공격 강제 취소*
    private void CancelAttack()
    {
        if (!IsAttacking) return;
        OnAttackCancelled?.Invoke();    //"공격이 중간에 취소됐다"고 알림
        EndAttack(false);               //공격 상태 정리
    }

    //*공격 종료*
    private void EndAttack(bool completedNormally)
    {
        if (!IsAttacking) return;

        //공격 상태 종료
        IsAttacking = false;
        IsAirAttack = false;

        //정상 종료 이벤트 전달
        if (completedNormally)
        {
            OnAttackEnded?.Invoke();
        }
    }
}
