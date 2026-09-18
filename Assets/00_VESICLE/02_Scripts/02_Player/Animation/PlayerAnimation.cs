//**플레이어 애니메이션 제어**
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //리지드바디 필수
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;     //Visual에 붙은 Animator 연결
    [SerializeField] private PlayerJump playerJump; //바닥 상태 확인
    private Rigidbody2D rb;

    //Animator Parameter의 문자열 이름을 Hash값으로 변환해서 재사용
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
}
