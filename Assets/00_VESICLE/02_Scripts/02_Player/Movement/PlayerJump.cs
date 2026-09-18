//**플레이어 점프**
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //리지드바디 필수
public class PlayerJump : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;             //PlayerInputReader 연결
    [SerializeField] private Transform groundCheck;                     //Player 안의 groundCheck 연결
    [SerializeField] private LayerMask groundLayer;                     //바닥으로 인정할 레이어 지정
    [SerializeField, Min(0f)] private float groundCheckRadius = 0.15f;  //체크용 원 반지름
    [SerializeField, Min(0f)] private float jumpSpeed = 10f;            //점프 속도
    private Rigidbody2D rb;                                             //리지드바디
    private bool jumpRequested;                                         //점프 입력 기억 변수

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (inputReader.IsJumpPressed())
        {
            jumpRequested = true;   //이번 프레임에 점프가 눌렸으면 점프 요청 저장
        }
    }

    private void FixedUpdate()
    {
        if (!jumpRequested) return;
        if (IsGrounded())
        {
            Jump();                 //점프
        }
        jumpRequested = false;      //이번 점프 요청 끝났으니 false로 돌려놓기
    }

    //*점프*
    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
    }

    //*플레이어가 바닥에 서 있는지 반환*
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }
}
