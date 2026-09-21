//**플레이어 좌우 이동과 바라보는 방향 처리**
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //리지드바디 필수
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;     //PlayerInputReader 연결
    [SerializeField] private SpriteRenderer playerSprite;       //플레이어 Sprite
    [SerializeField, Min(0f)] private float moveSpeed = 7f;     //플레이어 이동속도

    private Rigidbody2D rb;                                     //리지드바디
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
        Move();
        UpdateFacing();
    }

    //*플레이어 이동 처리*
    private void Move()
    {
        float moveX = inputReader.MoveInput.x;  //횡스크롤 게임이므로 x값만 사용
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
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

    //*일반 이동을 켜거나 끌 때 사용*
    public void SetMovementEnabled(bool enabled)
    {
        CanMove = enabled;
    }
}
