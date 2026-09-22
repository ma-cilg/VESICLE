//**플레이어 피격 시 시각 피드백 처리**
//책임: 피격 발생 감지 → Hit Sprite 표시 → 빨간색 Flash → 원래 Visual 복구
using DG.Tweening;
using UnityEngine;

public class PlayerHitFeedback : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;                 //데미지 이벤트 받을 PlayerHealth
    [SerializeField] private SpriteRenderer playerSprite;               //평소 플레이어 애니메이션이 표시되는 SpriteRenderer
    [SerializeField] private SpriteRenderer hitSprite;                  //피격 순간에만 잠깐 표시할 SpriteRenderer

    [SerializeField, Min(0f)] private float hitPoseDuration = 0.15f;    //Hit Sprite가 화면에 유지되는 전체 시간
    [SerializeField, Min(0f)] private float flashDuration = 0.1f;       //빨간색으로 변했다가 원래 색으로 돌아오는 데 사용할 시간

    [SerializeField] private Color hitColor = new Color(1f, 0.35f, 0.35f, 1f);  //피격 순간 사용할 색

    private Sequence hitSequence;                                       //현재 실행 중인 피격 연출 Sequence 저장

    private void Awake()
    {
        hitSprite.enabled = false;                                      //게임 시작 시 HitVisual은 보이지 않도록 설정
    }

    private void OnEnable()
    {
        playerHealth.OnDamaged += PlayHitFeedback;                      //데미지 발생시 이벤트 등록
    }

    private void OnDisable()
    {
        playerHealth.OnDamaged -= PlayHitFeedback;                      //이벤트 구독 해제

        hitSequence?.Kill();                                            //실행 중인 피격 Tween이 있으면 즉시 종료
        hitSequence = null;                                             //Tween 참조 제거

        RestoreVisual();
    }

    //*실제 피격이 발생했을 때 시각 연출 시작*
    private void PlayHitFeedback(float damage)
    {
        hitSequence?.Kill();                                            //기존거 있을 수 있으니 연출 먼저 중단
        hitSequence = null;                                             //기존 Sequence 참조도 제거
        hitSprite.flipX = playerSprite.flipX;                           //Hit Sprite도 기존이랑 방향 맞추기
        hitSprite.color = Color.white;                                  //새로운 피격시 Hit Sprite 색상을 기본 흰색으로 초기화             

        playerSprite.enabled = false;                                   //평소 Player Sprite를 잠깐 숨김
        hitSprite.enabled = true;                                       //대신 HitVisual Sprite 표시

        hitSequence = DOTween.Sequence();                               //DOTween Sequence 생성(여러 DOTween 순서대로 써야함)

        float halfFlashDuration = flashDuration * 0.5f;                 //Flash 시간 절반으로 나누기(빨강/흰 반반)
        hitSequence.Append(hitSprite.DOColor(hitColor, halfFlashDuration));     //현재 흰색에서 hitColor까지 빠르게 변경
        hitSequence.Append(hitSprite.DOColor(Color.white, halfFlashDuration));  //빨간색에서 다시 흰색으로 복구

        float remainingPoseTime = hitPoseDuration - flashDuration;      //Hit Sprite 전체 시간이 Flash 시간보다 길면 남은 시간만큼 Hit Sprite 유지

        //남은 시간이 실제로 있을 때만 Interval 추가
        //음수 시간을 DOTween에 넣지 않기 위한 방어 처리
        if (remainingPoseTime > 0f)
        {
            hitSequence.AppendInterval(remainingPoseTime);
        }

        //전체 피격 연출이 끝나면 원래 Visual로 복구
        hitSequence.OnComplete(() =>
        {
            RestoreVisual();        //Hit Sprite 숨기고 일반 Sprite 다시 표시

            hitSequence = null;     //Sequence가 끝났으므로 참조 제거
        });
    }

    //*평소 플레이어 Visual로 복구*
    private void RestoreVisual()
    {
        hitSprite.enabled = false;      //HitVisual 다시 숨김
        hitSprite.color = Color.white;  //흰색으로 초기화
        playerSprite.enabled = true;    //평소 SpriteRenderer 다시 표시
    }
}
