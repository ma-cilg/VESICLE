//**게임의 짧은 슬로모 연출 처리**
//책임: 슬로모 시작 → 실제 시간 기준으로 유지 → 원래 시간 배율 복구
using DG.Tweening;
using UnityEngine;

public class TimeSlowmo : MonoBehaviour
{
    public static TimeSlowmo Instance { get; private set; }

    [Header("Enemy Death")]
    [SerializeField, Range(0.01f, 1f)]
    private float enemyDeathTimeScale = 0.2f;               //적 사망 순간 게임 속도

    [SerializeField, Min(0f)]
    private float enemyDeathSlowmoDuration = 0.06f;         //실제 시간 기준 슬로모 지속시간

    private Tween restoreTween;

    private float normalTimeScale;
    private float normalFixedDeltaTime;

    private void Awake()
    {
        Instance = this;
    }

    //*적 사망 순간 슬로모*
    public void PlayEnemyDeathSlowmo()
    {
        PlaySlowmo(enemyDeathTimeScale, enemyDeathSlowmoDuration);
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