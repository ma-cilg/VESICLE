//**플레이어 좌우 이동과 바라보는 방향 처리**
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //리지드바디 필수
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;     //PlayerInputReader 연결
    [SerializeField] private SpriteRenderer playerSprite;       //플레이어 Sprite
    [SerializeField, Min(0f)] private float moveSpeed = 7f;     //플레이어 이동속도

    private Rigidbody2D rb;                                     //리지드바디
    private float temporaryMoveSpeed;                           //공격 등에서 사용하는 짧은 보조 이동 속도
    private float temporaryMoveRemainingTime;                   //보조 이동이 얼마나 더 유지되는지 저장

    public bool IsFacingRight { get; private set; } = true;     //방향 판단
    public bool CanMove { get; private set; } = true;           //일반 이동을 현재 사용할 수 있는지 판단
    public float HorizontalSpeed => Mathf.Abs(rb.linearVelocity.x); //현재 실제 좌우 이동 속도

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (!CanMove) return;
        UpdateFacing();
        Move();
        UpdateTemporaryMove();
    }

    //*플레이어 이동 처리*
    private void Move()
    {
        float moveX = inputReader.MoveInput.x;  //횡스크롤 게임이므로 x값만 사용

        //플레이어가 직접 방향키를 누르고 있다면 보조 이동보다 플레이어 입력을 우선
        if (Mathf.Abs(moveX) >= 0.01f)
        {
            ClearTemporaryMove();               //방향키를 누르는 순간 즉시 취소
            rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);    //일반 이동 적용
            return;
        }

        //방향키를 누르지 않고 있고 보조 이동 시간이 남아있다면 공격의 짧은 전진 이동 적용
        if (temporaryMoveRemainingTime > 0f)
        {
            rb.linearVelocity = new Vector2(temporaryMoveSpeed, rb.linearVelocity.y);
            return;
        }

        //일반 입력도 없고 보조 이동도 없으면 좌우 이동 정지
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    //*현재 이동 방향 확인*
    private void UpdateFacing()
    {
        float moveX = inputReader.MoveInput.x;          //현재 이동 입력 x값 가져오기
        if (Mathf.Abs(moveX) < 0.01f) return;           //입력값 거의 0이면 방향 전환 X
        bool shouldFaceRight = moveX > 0f;              //moveX가 크면 오른쪽 이동
        if (shouldFaceRight == IsFacingRight) return;   //이동 방향 같으면 스프라이트 그대로
        IsFacingRight = shouldFaceRight;                //새로운 방향 저장
        playerSprite.flipX = !IsFacingRight;            //이미지 반전
    }

    //*짧은 보조 이동 적용*
    public void ApplyTemporaryMove(float horizontalSpeed, float duration)
    {
        if (duration <= 0f) return;
        temporaryMoveSpeed = horizontalSpeed;           //요청받은 좌우 속도 저장
        temporaryMoveRemainingTime = duration;          //보조 이동이 유지될 시간 저장
    }

    //*보조 이동 시간 갱신*
    private void UpdateTemporaryMove()
    {
        if (temporaryMoveRemainingTime <= 0f) return;
        temporaryMoveRemainingTime -= Time.fixedDeltaTime;  //FixedUpdate 기준으로 시간 감소
        if (temporaryMoveRemainingTime > 0f) return;
        ClearTemporaryMove();                               //시간이 끝났다면 보조 이동 초기화
    }

    //*보조 이동 초기화*
    private void ClearTemporaryMove()
    {
        temporaryMoveSpeed = 0f;
        temporaryMoveRemainingTime = 0f;
    }

    //*일반 이동을 켜거나 끌 때 사용*
    public void SetMovementEnabled(bool enabled)
    {
        CanMove = enabled;

        //이동 자체가 잠기면 보조 이동도 제거
        if (!enabled)
        {
            ClearTemporaryMove();
        }
    }
}
