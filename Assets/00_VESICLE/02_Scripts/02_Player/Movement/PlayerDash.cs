//**플레이어 대시 이동, 지속시간, 쿨타임 처리**
//책임: 대시 입력 확인 → 조작 잠금/무적 → 대시 이동 → 상태 복구 → 쿨타임 처리
using System;               //Action 이벤트 사용
using System.Collections;   //IEnumerator, Coroutine 사용
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //리지드바디 필수
public class PlayerDash : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;             //PlayerInputReader 연결
    [SerializeField] private PlayerMovement playerMovement;             //Player가 바라보는 방향 확인용
    [SerializeField] private PlayerControlLock controlLock;             //통합 잠금 시스템
    [SerializeField] private PlayerInvincibility playerInvincibility;   //대시 중 무적 처리

    [SerializeField, Min(0f)] private float dashSpeed = 18f;            //대시속도
    [SerializeField, Min(0f)] private float dashDuration = 0.15f;       //대시 유지 시간
    [SerializeField, Min(0f)] private float dashCooldown = 0.35f;       //대시 종료 후 쿨타임

    private Rigidbody2D rb;                                             //리지드바디
    private float originalGravityScale;                                 //기본중력값
    private bool isOnCooldown;                                          //쿨타임중인지 판단
    public bool IsDashing { get; private set; }                         //현재 대시 중인지 판단
    public bool CanDash { get; private set; } = true;                   //외부 시스템에서 대시 사용 가능 여부 제어

    public event Action OnDashStarted;                                  //대시 시작 시점 알리는 이벤트

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;
    }

    private void Update()
    {
        if (!CanDash) return;
        if (!inputReader.IsDashPressed()) return;
        if (IsDashing || isOnCooldown) return;
        StartCoroutine(DashRoutine());
    }

    //*대시 코루틴*
    private IEnumerator DashRoutine()
    {
        IsDashing = true;                                               //대시 중 기록
        isOnCooldown = true;                                            //쿨타임 시작
        playerInvincibility.AddInvincibility();                         //대시 무적 시작

        //대시 중 다른 행동 잠금
        controlLock.LockMovement();
        controlLock.LockJump();
        controlLock.LockAttack();
        controlLock.LockThrow();
        controlLock.LockDetonate();

        OnDashStarted?.Invoke();                                        //대시 이벤트 외부에 알림

        float dashDirection = playerMovement.IsFacingRight ? 1f : -1f;  //대시 방향 결정 (오른쪽이면 1f : 왼쪽이면 -1f)
        rb.gravityScale = 0f;                                           //대시 중 중력 잠시 제거
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f); //현재 속도 대시속도로 변경
        yield return new WaitForSeconds(dashDuration);                  //대시 유지 시간만큼 코루틴 멈추기
        rb.gravityScale = originalGravityScale;                         //중력 복구

        //다른 행동 잠금 해제
        controlLock.UnlockMovement();
        controlLock.UnlockJump();
        controlLock.UnlockAttack();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();

        playerInvincibility.RemoveInvincibility();                      //대시 무적 종료
        IsDashing = false;                                              //대시 상태 종료
        yield return new WaitForSeconds(dashCooldown);                  //남은 쿨타임 동안 기다리기
        isOnCooldown = false;                                           //다시 대시 가능
    }

    //*외부에서 대시 사용 가능 여부 변경*
    public void SetDashEnabled(bool isEnabled)
    {
        CanDash = isEnabled;
    }
}
