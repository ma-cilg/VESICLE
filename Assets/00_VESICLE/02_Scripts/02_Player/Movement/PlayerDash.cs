//**플레이어 대시 이동, 지속시간, 쿨타임 처리**
using System.Collections;   //IEnumerator, Coroutine 사용
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //리지드바디 필수
public class PlayerDash : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;             //PlayerInputReader 연결
    [SerializeField] private PlayerMovement playerMovement;             //일반 이동 제어
    [SerializeField] private PlayerJump playerJump;                     //점프 제어

    [SerializeField, Min(0f)] private float dashSpeed = 18f;            //대시속도
    [SerializeField, Min(0f)] private float dashDuration = 0.15f;       //대시 유지 시간
    [SerializeField, Min(0f)] private float dashCooldown = 0.35f;       //대시 종료 후 쿨타임

    private Rigidbody2D rb;                                             //리지드바디
    private float originalGravityScale;                                 //기본중력값
    private bool isOnCooldown;                                          //쿨타임중인지 판단
    public bool IsDashing { get; private set; }                         //현재 대시 중인지 판단

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;
    }

    private void Update()
    {
        if (!inputReader.IsDashPressed()) return;
        if (IsDashing || isOnCooldown) return;
        StartCoroutine(DashRoutine());
    }

    //*대시 코루틴*
    private IEnumerator DashRoutine()
    {
        IsDashing = true;                                               //대시 중 기록
        isOnCooldown = true;                                            //쿨타임 시작
        playerMovement.SetMovementEnabled(false);                       //대시 속도 못 덮도록 일반 이동 막기
        playerJump.SetJumpEnabled(false);                               //대시 중 점프 막기
        float dashDirection = playerMovement.IsFacingRight ? 1f : -1f;  //대시 방향 결정 (오른쪽이면 1f : 왼쪽이면 -1f)
        rb.gravityScale = 0f;                                           //대시 중 중력 잠시 제거
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f); //현재 속도 대시속도로 변경
        yield return new WaitForSeconds(dashDuration);                  //대시 유지 시간만큼 코루틴 멈추기
        rb.gravityScale = originalGravityScale;                         //중력 복구
        playerMovement.SetMovementEnabled(true);                        //일반 이동 켜기
        playerJump.SetJumpEnabled(true);                                //점프도 다시 켜기
        IsDashing = false;                                              //대시 상태 종료
        yield return new WaitForSeconds(dashCooldown);                  //남은 쿨타임 동안 기다리기
        isOnCooldown = false;                                           //다시 대시 가능
    }
}
