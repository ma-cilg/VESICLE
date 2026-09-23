//**적 피격 시 시각적 움찔 효과 처리**
//책임: 검 피격 이벤트 수신 → Visual에 짧은 위치/크기 반동 적용 → 원래 상태 복구
using DG.Tweening;      //DOTween 사용
using UnityEngine;

public class EnemyHitFeedback : MonoBehaviour
{
    [SerializeField] private EnemyHitReceiver hitReceiver;                      //공격 받았다는 이벤트를 받기 위한 컴포넌트
    [SerializeField] private Transform visualTransform;                         //실제 적의 Sprite가 들어있는 Visual Transform

    [SerializeField, Min(0f)] private float recoilDistance = 0.08f;             //피격 순간 공격 방향으로 밀리는 거리
    [SerializeField, Min(0f)] private float recoilUp = 0.03f;                   //피격 순간 아주 조금 위로 튀는 시각 효과
    [SerializeField, Min(0.01f)] private float recoilDuration = 0.1f;           //피격 움찔 효과 전체 시간
    [SerializeField] private Vector2 scalePunch = new Vector2(-0.06f, 0.08f);   //움찔하면서 적용할 크기 변화

    private Tween positionTween;                                                //현재 실행 중인 위치 Tween
    private Tween scaleTween;                                                   //현재 실행 중인 Scale Tween

    private Vector3 originalLocalPosition;                                      //Visual 원래 위치
    private Vector3 originalLocalScale;                                         //Visual 원래 크기

    private void Awake()
    {
        originalLocalPosition = visualTransform.localPosition;                  //게임 시작 시 Visual 원래 위치 저장
        originalLocalScale = visualTransform.localScale;                        //게임 시작 시 Visual 원래 Scale 저장
    }

    private void OnEnable()
    {
        hitReceiver.OnHitReceived += PlayHitFeedback;                           //검 공격에 맞았을 때 피격 Feedback 실행
    }

    private void OnDisable()
    {
        hitReceiver.OnHitReceived -= PlayHitFeedback;                           //이벤트 구독 해제

        //실행 중인 Tween 제거
        positionTween?.Kill();
        scaleTween?.Kill();

        positionTween = null;
        scaleTween = null;

        //비활성화될 때 Visual 상태 원상복구
        visualTransform.localPosition = originalLocalPosition;
        visualTransform.localScale = originalLocalScale;
    }

    //*적 피격 움찔 효과 재생*
    private void PlayHitFeedback(EnemyHitInfo hitInfo)
    {
        //이전 피격 Tween이 아직 진행 중이라면 제거
        positionTween?.Kill();
        scaleTween?.Kill();

        //새 피격 효과를 시작전 항상 원래 위치와 Scale로 복구
        visualTransform.localPosition = originalLocalPosition;
        visualTransform.localScale = originalLocalScale;

        //공격이 들어온 방향으로 Visual이 잠깐 밀려나는 방향 계산
        Vector3 recoil = new Vector3(hitInfo.HitDirection.x * recoilDistance, recoilUp, 0f);

        //Visual 위치를 짧게 움찔시킨 뒤 자동으로 원래 위치로 돌아오게 함
        positionTween = visualTransform.DOPunchPosition(recoil, recoilDuration, 4, 0.5f).SetEase(Ease.OutQuad);

        //몸이 살짝 눌렸다 펴지는 타격감 추가
        Vector3 punchScale = new Vector3(scalePunch.x, scalePunch.y, 0f);
        scaleTween = visualTransform.DOPunchScale(punchScale, recoilDuration, 4, 0.5f).SetEase(Ease.OutQuad);
    }
}
