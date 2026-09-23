//**플레이어 애니메이션 제어**
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //리지드바디 필수
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;             //Visual에 붙은 Animator 연결
    [SerializeField] private PlayerJump playerJump;         //바닥 상태 확인
    [SerializeField] private PlayerAttack playerAttack;     //공격 시작/취소/종료 이벤트 확인
    private Rigidbody2D rb;

    //Animator Parameter의 문자열 이름을 Hash값으로 변환해서 재사용
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int GroundAttackHash = Animator.StringToHash("GroundAttack");
    private static readonly int AirAttackHash = Animator.StringToHash("AirAttack");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        //이벤트 구독
        playerAttack.OnGroundAttackStarted += PlayGroundAttack;
        playerAttack.OnAirAttackStarted += PlayAirAttack;
        playerAttack.OnAttackCancelled += StopAttack;
        playerAttack.OnAttackEnded += StopAttack;
    }

    private void OnDisable()
    {
        //등록했던 이벤트 구독 해제
        playerAttack.OnGroundAttackStarted -= PlayGroundAttack;
        playerAttack.OnAirAttackStarted -= PlayAirAttack;
        playerAttack.OnAttackCancelled -= StopAttack;
        playerAttack.OnAttackEnded -= StopAttack;
    }


    private void Update()
    {
        UpdateMovementAnimation();
    }

    //*애니메이션 갱신*
    private void UpdateMovementAnimation()
    {
        float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x);     //이동속도 가져오기
        float verticalSpeed = rb.linearVelocity.y;                  //실제 수직 속도

        animator.SetFloat(SpeedHash, horizontalSpeed);              //Idle / Run 판단
        animator.SetFloat(VerticalSpeedHash, verticalSpeed);        //Rise / Mid / Fall 판단
        animator.SetBool(IsGroundedHash, playerJump.IsGrounded);    //지상 / 공중 판단
    }

    //*지상 공격 애니메이션 시작*
    private void PlayGroundAttack()
    {
        animator.SetBool(IsAttackingHash, true);                    //현재 공격 상태임을 Animator에 전달
        animator.ResetTrigger(AirAttackHash);                       //혹시 이전 AirAttack Trigger가 남아있다면 초기화
        animator.SetTrigger(GroundAttackHash);                      //지상 공격 시작
    }

    //*공중 공격 애니메이션 시작*
    private void PlayAirAttack()
    {
        animator.SetBool(IsAttackingHash, true);                    //현재 공격 상태 전달
        animator.ResetTrigger(GroundAttackHash);                    //이전 GroundAttack Trigger 초기화
        animator.SetTrigger(AirAttackHash);                         //공중 공격 시작
    }

    //*공격 애니메이션 종료 / 취소*
    private void StopAttack()
    {
        //공격 상태 종료
        animator.SetBool(IsAttackingHash, false);                   //공격 상태 종료

        //혹시 아직 소비되지 않은 Trigger가 있다면 초기화
        animator.ResetTrigger(GroundAttackHash);
        animator.ResetTrigger(AirAttackHash);
    }
}
