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
    private static readonly int ChargeAttackHash = Animator.StringToHash("ChargeAttack");
    private static readonly int SlashAttackHash = Animator.StringToHash("SlashAttack");
    private static readonly int FinishAttackHash = Animator.StringToHash("FinishAttack");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        //이벤트 구독
        playerAttack.OnChargeStarted += PlayCharge;
        playerAttack.OnSlashStarted += PlaySlash;
        playerAttack.OnFinishStarted += PlayFinish;
        playerAttack.OnAttackCancelled += StopAttack;
        playerAttack.OnAttackEnded += StopAttack;
    }

    private void OnDisable()
    {
        //등록했던 이벤트 구독 해제
        playerAttack.OnChargeStarted -= PlayCharge;
        playerAttack.OnSlashStarted -= PlaySlash;
        playerAttack.OnFinishStarted -= PlayFinish;
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

    //*차징 시작*
    private void PlayCharge()
    {
        animator.SetBool(IsAttackingHash, true);    //공격 시작 상태
        ResetAttackTriggers();                      //이전 Trigger 초기화
        animator.SetTrigger(ChargeAttackHash);      //Charge State 진입
    }

    //*실제 이동 공격 시작*
    private void PlaySlash(float chargeDistance)
    {
        animator.SetBool(IsAttackingHash, true);    //공격 상태는 계속 유지
        ResetAttackTriggers();                      //이전 Trigger 초기화
        animator.SetTrigger(SlashAttackHash);       //Slash State 진입
    }

    //*이동 완료 후 마무리 애니메이션 시작*
    private void PlayFinish()
    {
        animator.SetBool(IsAttackingHash, true);    //마무리 중(공격 끝X)
        ResetAttackTriggers();                      //이전 Trigger 초기화
        animator.SetTrigger(FinishAttackHash);      //Finish State 진입
    }

    //*공격 취소 / 정상 종료*
    private void StopAttack()
    {
        animator.SetBool(IsAttackingHash, false);   //공격 상태 종료
        ResetAttackTriggers();                      //남아있는 공격 Trigger 정리
    }

    //*공격 Trigger 전체 초기화*
    private void ResetAttackTriggers()
    {
        animator.ResetTrigger(ChargeAttackHash);
        animator.ResetTrigger(SlashAttackHash);
        animator.ResetTrigger(FinishAttackHash);
    }
}
