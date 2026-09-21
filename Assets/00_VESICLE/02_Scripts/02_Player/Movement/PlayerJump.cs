//**플레이어 2단 점프**
//추가 점프 1로 설정해서 플랫폼에서 그냥 떨어져도 1회 점프가 가능하도록 구성
using System;       //Action 이벤트 사용
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //리지드바디 필수
public class PlayerJump : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;             //PlayerInputReader 연결
    [SerializeField] private Transform groundCheck;                     //Player 안의 groundCheck 연결
    [SerializeField] private LayerMask groundLayer;                     //바닥으로 인정할 레이어 지정
    [SerializeField, Min(0f)] private float groundCheckRadius = 0.15f;  //체크용 원 반지름
    [SerializeField, Min(0f)] private float jumpSpeed = 10f;            //점프속도
    [SerializeField, Min(0)] private int extraJumpCount = 1;            //추가 점프 횟수

    private Rigidbody2D rb;                                             //리지드바디
    private int remainingExtraJumps;                                    //현재 남은 추가 점프 횟수
    private bool jumpRequested;                                         //점프 입력 기억 변수
    private bool wasGrounded;                                           //이전 물리 프레임에 바닥에 있었는지 저장

    public bool CanJump { get; private set; } = true;                   //점프 가능한지 판단 외부 제공
    public bool IsGrounded { get; private set; }                        //바닥에 있는지 판단 외부 제공

    //*점프 관련 시점 알리는 이벤트*
    public event Action OnJumped;                                       //1단 점프 발생
    public event Action OnDoubleJumped;                                 //2단 점프 발생
    public event Action OnLanded;                                       //착지 발생
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        remainingExtraJumps = extraJumpCount;
    }

    private void Start()
    {
        //게임 시작 순간 실제 바닥 상태 저장
        IsGrounded = CheckGrounded();
        wasGrounded = IsGrounded;
    }

    private void Update()
    {
        if (!CanJump) return;
        if (inputReader.IsJumpPressed())
        {
            jumpRequested = true;       //이번 프레임에 점프가 눌렸으면 점프 요청 저장
        }
    }

    private void FixedUpdate()
    {
        IsGrounded = CheckGrounded();
        if (!wasGrounded && IsGrounded)
        {
            OnLanded?.Invoke();         //OnLanded를 구독하고 있는 객체가 있을 때만 이벤트 실행
        }
        if (IsGrounded && rb.linearVelocity.y <= 0.01f)
        {
            remainingExtraJumps = extraJumpCount;
        }
        if (!jumpRequested)
        {
            wasGrounded = IsGrounded;   //현재 바닥 상태 저장 후 종료
            return;
        }
        if (IsGrounded && rb.linearVelocity.y <= 0.01f)
        {
            Jump();                     //점프
            OnJumped?.Invoke();         //1단 점프 발생 외부에 알리기
        }
        else if (remainingExtraJumps > 0)
        {
            Jump();                     //2단 점프

            remainingExtraJumps--;      //공중점프 횟수 감소
            OnDoubleJumped?.Invoke();   //2단 점프 발생 외부에 알리기
        }
        jumpRequested = false;          //이번 점프 요청 끝났으니 false로 돌려놓기
        wasGrounded = IsGrounded;       //현재 바닥 상태 저장
    }

    //*점프*
    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
    }

    //*플레이어가 바닥에 서 있는지 반환*
    private bool CheckGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    //*점프를 켜거나 끌 때 사용*
    public void SetJumpEnabled(bool enabled)
    {
        CanJump = enabled;
        if (!enabled)
        {
            jumpRequested = false;  //점프 막는 순간 기존에 남아있던 점프 입력도 제거
        }
    }
}
