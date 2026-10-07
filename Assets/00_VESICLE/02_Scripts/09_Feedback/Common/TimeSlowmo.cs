//**게임의 짧은 슬로모 연출 처리**
//책임: 슬로모 시작 → 실제 시간 기준으로 유지 → 원래 시간 배율 복구
using DG.Tweening;
using UnityEngine;

public class TimeSlowmo : MonoBehaviour
{
    public static TimeSlowmo Instance { get; private set; }

    [SerializeField] private PlayerHealth playerHealth;                                 //플레이어 피격 이벤트 수신

    [Header("Player Hit")]
    [SerializeField, Range(0.01f, 1f)] private float playerHitTimeScale = 0.04f;        //플레이어가 실제 데미지를 받았을 때 게임 속도
    [SerializeField, Min(0f)] private float playerHitSlowmoDuration = 0.14f;            //실제 시간 기준 플레이어 피격 슬로모 길이

    private Tween restoreTween;

    private float normalTimeScale;
    private float normalFixedDeltaTime;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        playerHealth.OnDamaged += HandlePlayerDamaged;
    }

    //*플레이어가 실제 데미지를 받은 순간*
    private void HandlePlayerDamaged(float damage)
    {
        PlayPlayerHitSlowmo();
    }

    //*플레이어가 실제 데미지를 받은 순간 슬로모*
    public void PlayPlayerHitSlowmo()
    {
        PlaySlowmo(playerHitTimeScale, playerHitSlowmoDuration);
    }

    //*공통 슬로모 시작*
    private void PlaySlowmo(float timeScale, float duration)
    {
        //이미 슬로모 중이 아니라면 원래 시간값 저장
        if (restoreTween == null)
        {
            normalTimeScale = Time.timeScale;
            normalFixedDeltaTime = Time.fixedDeltaTime;
        }

        //기존 복구 예약이 있다면 취소하고 새 슬로모 시간으로 갱신
        restoreTween?.Kill();
        restoreTween = null;

        Time.timeScale = timeScale;
        Time.fixedDeltaTime = normalFixedDeltaTime * timeScale;

        //Time.timeScale 영향을 받지 않는 실제 시간 기준으로 복구
        restoreTween = DOVirtual
            .DelayedCall(duration, RestoreTime)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                restoreTween = null;
            });
    }

    //*원래 게임 속도로 복구*
    private void RestoreTime()
    {
        Time.timeScale = normalTimeScale;
        Time.fixedDeltaTime = normalFixedDeltaTime;
    }

    private void OnDisable()
    {
        playerHealth.OnDamaged -= HandlePlayerDamaged;

        restoreTween?.Kill();
        restoreTween = null;

        //슬로모 도중 오브젝트가 꺼져도 게임 전체 속도가 느려진 채 남지 않게 복구
        if (normalFixedDeltaTime > 0f)
        {
            RestoreTime();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}