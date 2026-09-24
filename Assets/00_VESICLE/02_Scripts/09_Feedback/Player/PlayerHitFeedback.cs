//**플레이어 피격 시 시각 피드백 처리**
//책임: 피격 발생 감지 → Hit Sprite 표시 → 빨간색 Flash → 원래 Visual 복구
using DG.Tweening;
using UnityEngine;

public class PlayerHitFeedback : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;                 //데미지 이벤트 확인
    [SerializeField] private PlayerWeaponThrow playerWeaponThrow;       //투척 중 피격 예외 확인

    [SerializeField] private SpriteRenderer playerSprite;               //평소 Player Sprite
    [SerializeField] private SpriteRenderer hitSprite;                  //피격 순간 표시할 Hit Sprite

    [SerializeField, Min(0f)] private float hitPoseDuration = 0.15f;    //Hit Pose 유지 시간
    [SerializeField, Min(0f)] private float flashDuration = 0.1f;       //빨간 Flash 전체 시간

    [SerializeField] private Color hitColor = new Color(1f, 0.35f, 0.35f, 1f);  //피격 Flash 색

    private Sequence hitSequence;                                       //현재 실행 중인 피격 연출
    private Color originalPlayerColor;                                  //Player Sprite가 원래 가지고 있던 색

    private void Awake()
    {   
        originalPlayerColor = playerSprite.color;                       //Player Sprite 원래 색 저장
        hitSprite.enabled = false;                                      //게임 시작 시 HitVisual 숨김
    }

    private void OnEnable()
    {
        playerHealth.OnDamaged += PlayHitFeedback;                      //데미지 발생시 이벤트 등록
    }

    private void OnDisable()
    {
        playerHealth.OnDamaged -= PlayHitFeedback;                      //이벤트 구독 해제

        //진행 중인 Tween 제거
        hitSequence?.Kill();
        hitSequence = null;

        RestoreVisual();                                                //시각 상태 원상복구
    }

    //*실제 피격이 발생했을 때 시각 연출 시작*
    private void PlayHitFeedback(float damage)
    {
        hitSequence?.Kill();                                            //기존거 있을 수 있으니 연출 먼저 중단
        hitSequence = null;                                             //기존 Sequence 참조도 제거

        RestoreVisual();                                                //먼저 기본 상태 복구

        if (playerWeaponThrow.IsThrowing)                               //검 투척 / 검 재생성 상태라면
        {
            PlayThrowHitFlash();                                        //Hit Pose 없이 빨간 Flash만 실행
            return;
        }

        //일반 피격이라면 기존 Hit Pose 연출 실행
        PlayNormalHitFeedback();
    }

    //*일반 피격 연출*
    private void PlayNormalHitFeedback()
    {
        hitSprite.flipX = playerSprite.flipX;                                   //현재 Player가 바라보는 방향과 맞춤
        hitSprite.color = Color.white;                                          //Hit Sprite 기본 색 초기화
        playerSprite.enabled = false;                                           //평소 Sprite 숨김
        hitSprite.enabled = true;                                               //Hit Pose 표시
        hitSequence = DOTween.Sequence();                                       //Sequence 생성

        float halfFlashDuration = flashDuration * 0.5f;                         //빨강 → 흰색으로 돌아오는 시간 절반씩 사용

        hitSequence.Append(hitSprite.DOColor(hitColor, halfFlashDuration));     //Hit Sprite 빨간 Flash
        hitSequence.Append(hitSprite.DOColor(Color.white, halfFlashDuration));

        float remainingPoseTime = hitPoseDuration - flashDuration;              //Flash 이후에도 Hit Pose를 조금 더 유지

        if (remainingPoseTime > 0f)
        {
            hitSequence.AppendInterval(remainingPoseTime);
        }

        //전체 연출 종료 후 원래 Visual 복구
        hitSequence.OnComplete(() =>
        {
            RestoreVisual();
            hitSequence = null;
        });
    }

    //*투척 중 피격 Flash*
    private void PlayThrowHitFlash()
    {
        playerSprite.enabled = true;                                                        //현재 Player Sprite가 보이는 상태 유지
        hitSprite.enabled = false;                                                          //Hit Sprite는 사용하지 않음
        playerSprite.color = originalPlayerColor;                                           //혹시 이전 색상이 남아있다면 원래 색으로 초기화
        hitSequence = DOTween.Sequence();                                                   //Flash Sequence 생성

        float halfFlashDuration = flashDuration * 0.5f;

        hitSequence.Append(playerSprite.DOColor(hitColor, halfFlashDuration));              //현재 투척 Sprite를 빨간색으로
        hitSequence.Append(playerSprite.DOColor(originalPlayerColor, halfFlashDuration));   //다시 원래 색으로 복구

        hitSequence.OnComplete(() =>
        {
            playerSprite.color = originalPlayerColor;
            hitSequence = null;
        });
    }

    //*평소 Player Visual로 복구*
    private void RestoreVisual()
    {
        hitSprite.enabled = false;                      //Hit Pose 숨김
        hitSprite.color = Color.white;                  //Hit Sprite 색 초기화
        playerSprite.enabled = true;                    //평소 Player Sprite 표시
        playerSprite.color = originalPlayerColor;       //Player 색 원상복구
    }
}
