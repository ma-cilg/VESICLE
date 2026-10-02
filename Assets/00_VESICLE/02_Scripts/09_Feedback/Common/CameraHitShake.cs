//**플레이어 피격 시 카메라 쉐이크 처리**
//책임: 실제 피격 이벤트 감지 → 짧은 카메라 흔들림 재생 → 원래 위치 복구
using DG.Tweening;
using UnityEngine;

public class CameraHitShake : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;                     //데미지 받았는지 이벤트 받을 PlayerHealth
    [SerializeField] private Transform cameraTransform;                     //흔들 카메라
    [SerializeField] private PlayerAttack playerAttack;                     //이동 공격 시작 이벤트를 받을 PlayerAttack

    [Header("Player Hit")]
    [SerializeField, Min(0f)] private float hitShakeDuration = 0.1f;        //플레이어 피격 쉐이크 시간
    [SerializeField, Min(0f)] private float hitShakeStrength = 0.05f;       //플레이어 피격 쉐이크 강도
    [SerializeField, Min(1)] private int hitVibrato = 8;                    //플레이어 피격 흔들림 횟수

    [Header("Movement Attack")]
    [SerializeField, Min(0f)] private float attackShakeDuration = 0.06f;    //이동 공격 발동 쉐이크 시간
    [SerializeField, Min(0f)] private float attackShakeStrength = 0.025f;   //이동 공격 발동 쉐이크 강도
    [SerializeField, Min(1)] private int attackVibrato = 5;                 //이동 공격 흔들림 횟수

    [Header("Enemy Death")]
    [SerializeField, Min(0f)] private float enemyDeathShakeDuration = 0.16f;    //적 폭발 쉐이크 시간
    [SerializeField, Min(0f)] private float enemyDeathShakeStrength = 0.14f;    //적 폭발 쉐이크 강도
    [SerializeField, Min(1)] private int enemyDeathVibrato = 14;                //적 폭발 흔들림 횟수

    private Tween shakeTween;                                               //현재 실행 중인 Shake Tween 저장
    private Vector3 originalLocalPosition;                                  //Main Camera의 원래 Local Position 저장

    public static CameraHitShake Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        originalLocalPosition = cameraTransform.localPosition;              //게임 시작 시 Main Camera의 기본 Local Position 저장
    }

    private void OnEnable()
    {
        playerHealth.OnDamaged += HandlePlayerDamaged;
        playerAttack.OnSlashStarted += HandleMovementAttackStarted;
    }

    private void OnDisable()
    {
        playerHealth.OnDamaged -= HandlePlayerDamaged;
        playerAttack.OnSlashStarted -= HandleMovementAttackStarted;

        shakeTween?.Kill();                                                 //실행 중인 카메라 Shake가 있다면 중단
        shakeTween = null;                                                  //Tween 참조 제거

        cameraTransform.localPosition = originalLocalPosition;              //오브젝트가 Shake 도중 꺼져도 원래 위치 복구
    }

    //*플레이어가 실제 데미지를 받았을 때*
    private void HandlePlayerDamaged(float damage)
    {
        PlayShake(hitShakeDuration, hitShakeStrength, hitVibrato);
    }

    //*이동 공격이 실제로 시작됐을 때*
    private void HandleMovementAttackStarted(float attackDistance)
    {
        PlayShake(attackShakeDuration, attackShakeStrength, attackVibrato);
    }

    //*적 사망 폭발 쉐이크*
    public void PlayEnemyDeathShake()
    {
        PlayShake(enemyDeathShakeDuration, enemyDeathShakeStrength, enemyDeathVibrato);
    }

    //*공통 카메라 쉐이크 재생*
    private void PlayShake(float duration, float strength, int shakeVibrato)
    {
        shakeTween?.Kill();                                                 //기존 쉐이크 남아있으면 먼저 중단
        cameraTransform.localPosition = originalLocalPosition;              //이전 흔들림의 위치 오차 제거

        //Main Camera의 Local Position 흔들기
        shakeTween = cameraTransform
            .DOShakePosition(duration, strength, shakeVibrato)
            .SetUpdate(true)                                                //카메라 쉐이크 자체는 느려지지 않도록 실제 시간 기준으로 실행
            .OnComplete(() =>
            {
                //정확한 원위치로 복구
                cameraTransform.localPosition = originalLocalPosition;
                shakeTween = null;
            });
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
