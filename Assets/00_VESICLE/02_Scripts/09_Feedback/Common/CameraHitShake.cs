//**플레이어 피격 시 카메라 쉐이크 처리**
//책임: 실제 피격 이벤트 감지 → 짧은 카메라 흔들림 재생 → 원래 위치 복구
using DG.Tweening;
using UnityEngine;

public class CameraHitShake : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;             //데미지 받았는지 이벤트 받을 PlayerHealth
    [SerializeField] private Transform cameraTransform;             //흔들 카메라

    [SerializeField, Min(0f)] private float shakeDuration = 0.1f;   //쉐이크가 유지되는 시간
    [SerializeField, Min(0f)] private float shakeStrength = 0.05f;  //카메라가 원래 위치에서 얼마나 크게 벗어날지
    [SerializeField, Min(1)] private int vibrato = 8;               //쉐이크 시간 동안 몇 번 정도 흔들릴지

    private Tween shakeTween;                                       //현재 실행 중인 Shake Tween 저장
    private Vector3 originalLocalPosition;                          //Main Camera의 원래 Local Position 저장

    private void Awake()
    {
        originalLocalPosition = cameraTransform.localPosition;      //게임 시작 시 Main Camera의 기본 Local Position 저장
    }

    private void OnEnable()
    {
        playerHealth.OnDamaged += PlayShake;
    }

    private void OnDisable()
    {
        playerHealth.OnDamaged -= PlayShake;

        shakeTween?.Kill();                                         //실행 중인 카메라 Shake가 있다면 중단
        shakeTween = null;                                          //Tween 참조 제거

        cameraTransform.localPosition = originalLocalPosition;      //오브젝트가 Shake 도중 꺼져도 원래 위치 복구
    }

    //*플레이어가 실제 데미지를 받았을 때 카메라 쉐이크 시작*
    private void PlayShake(float damage)
    {
        shakeTween?.Kill();                                         //이전 Shake가 아직 실행 중이면 종료
        cameraTransform.localPosition = originalLocalPosition;      //새 Shake를 시작하기 전에 기준 위치를 정확하게 복구

        //Main Camera의 Local Position을 흔듦
        shakeTween = cameraTransform
            .DOShakePosition(shakeDuration, shakeStrength, vibrato)

            //쉐이크가 끝난 뒤 실행
            .OnComplete(() =>
            {
                cameraTransform.localPosition = originalLocalPosition;      //오차가 남지 않도록 원래 Local Position으로 확실하게 복구

                shakeTween = null;                                          //완료된 Tween 참조 제거
            });
    }
}
