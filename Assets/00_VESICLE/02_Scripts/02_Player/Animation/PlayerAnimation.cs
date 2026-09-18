//**플레이어 애니메이션 제어**
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //리지드바디 필수
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;     //Visual에 붙은 Animator 연결
    private Rigidbody2D rb;

    //이름을 매번 문자열 "Speed"로 찾지 않고 정수 Hash값으로 변환해서 재사용
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

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
        animator.SetFloat(SpeedHash, horizontalSpeed);              //animator에 있는 Speed에 전달
    }
}
