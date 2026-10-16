//**적 사망 폭발 효과음**
//책임: 실제 사망 완료 이벤트 감지 → 폭발 SFX 재생
using UnityEngine;

[RequireComponent(typeof(EnemyDeath))]
public class EnemyDeathSfx : MonoBehaviour
{
    [Header("Death SFX")]
    [SerializeField] private AudioClip deathSfx;                        //적 폭발 효과음
    [SerializeField, Range(0f, 1f)] private float deathVolume = 0.7f;   //개별 폭발음 볼륨

    private EnemyDeath enemyDeath;

    private void Awake()
    {
        //같은 적 오브젝트에 있는 사망 컴포넌트 가져오기
        enemyDeath = GetComponent<EnemyDeath>();
    }

    //*사망 완료 이벤트 구독*
    private void OnEnable()
    {
        enemyDeath.OnDied += HandleEnemyDied;
    }

    //*이벤트 구독 해제*
    private void OnDisable()
    {
        enemyDeath.OnDied -= HandleEnemyDied;
    }

    //*적이 실제 폭발한 순간*
    private void HandleEnemyDied()
    {
        if (deathSfx == null) return;

        //씬과 독립적으로 유지되는 AudioRoot에서 재생
        GameAudioController.Instance?.PlaySfx(deathSfx, deathVolume);
    }
}
