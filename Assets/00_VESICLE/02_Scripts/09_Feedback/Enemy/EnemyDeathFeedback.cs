//**적 사망 시각 연출 처리**
//책임: 사망 시작 이벤트 → 압축 → 팽창 → 사망 완료 전달
using DG.Tweening;
using UnityEngine;

public class EnemyDeathFeedback : MonoBehaviour
{
    [SerializeField] private EnemyDeath enemyDeath;                     //사망 상태 관리
    [SerializeField] private Transform visualRoot;                      //사망 Tween을 적용할 적 Visual
    [SerializeField] private SpriteRenderer enemySprite;                //현재 적 색상과 중심 위치 확인

    [Header("Debris")]
    [SerializeField] private EnemyDebrisPiece debrisPrefab;             //사망 조각 공용 Prefab
    [SerializeField] private Transform debrisPoolRoot;                  //사용하지 않는 조각 보관 위치

    [SerializeField] private Sprite[] debrisSprites = new Sprite[5];    //머리 / 팔 / 다리 5조각

    [SerializeField, Min(0f)] private float minExplosionSpeed = 5f;     //파츠 최소 폭발 속도
    [SerializeField, Min(0f)] private float maxExplosionSpeed = 8f;     //파츠 최대 폭발 속도

    [SerializeField, Range(0f, 45f)]
    private float directionJitter = 20f;                                //각 파츠 방향의 랜덤 오차

    [SerializeField, Min(0f)] private float maxAngularSpeed = 720f;     //파츠 회전 속도
    [SerializeField, Min(0f)] private float debrisSpawnRadius = 0.15f;

    [Header("Death Scale")]
    [SerializeField, Range(0.01f, 0.2f)]
    private float minBodyScale = 0.05f;                          //거의 사라질 정도까지 수축

    [SerializeField, Min(0f)]
    private float shrinkDuration = 0.12f;                        //본체가 수축하는 시간

    [SerializeField, Min(0f)]
    private float shrinkHoldDuration = 0.025f;                   //최소 크기에서 아주 잠깐 멈춤

    [SerializeField, Min(0f)]
    private float debrisRestoreDuration = 0.09f;                 //파츠가 원래 크기로 돌아오는 시간

    private Vector3 originalScale;
    private Sequence deathSequence;
    private Vector3 originalDebrisScale;                                //사망 연출 전 적의 원래 월드 크기

    private ComponentPool<EnemyDebrisPiece> debrisPool;

    private void Awake()
    {
        originalScale = visualRoot.localScale;
        originalDebrisScale = enemySprite.transform.lossyScale;

        //적 하나당 5개씩 사망 조각 미리 생성후 Pool에 보관
        debrisPool = new ComponentPool<EnemyDebrisPiece>(debrisPrefab, debrisPoolRoot, 5);
    }

    private void OnEnable()
    {
        enemyDeath.OnDeathStarted += PlayDeathFeedback;
    }

    private void OnDisable()
    {
        enemyDeath.OnDeathStarted -= PlayDeathFeedback;

        deathSequence?.Kill();
        deathSequence = null;

        if (visualRoot != null)
        {
            visualRoot.localScale = originalScale;
        }
    }

    //*적 사망 연출 시작*
    private void PlayDeathFeedback()
    {
        deathSequence?.Kill();

        visualRoot.localScale = originalScale;

        deathSequence = DOTween.Sequence();

        //현재 보이고 있는 자세 그대로 수축
        deathSequence.Append(visualRoot.DOScale(originalScale * minBodyScale, shrinkDuration).SetEase(Ease.InQuad));

        //아주 잠깐 유지
        deathSequence.AppendInterval(shrinkHoldDuration);

        //압축됐던 몸이 실제 파츠로 갈라지는 순간
        deathSequence.AppendCallback(() =>
        {
            PlayDeathImpact();
            SpawnDebris();

            //Trigger 적에 박혀 있던 검이 있다면 적이 비활성화되기 전에 검을 분리하고 숨김
            ThrownSwordProjectile embeddedSword = visualRoot.GetComponentInChildren<ThrownSwordProjectile>();

            if (embeddedSword != null)
            {
                embeddedSword.HideEmbeddedSword();
            }

            enemyDeath.CompleteDeath();
        });

        //슬로모 걸려도 수축 연출 원래 속도로 진행
        deathSequence.SetUpdate(true);

        deathSequence.OnComplete(() =>
        {
            deathSequence = null;
        });
    }

    //*적 폭발 직전 충격 연출*
    private void PlayDeathImpact()
    {
        CameraHitShake.Instance?.PlayEnemyDeathShake();
        TimeSlowmo.Instance?.PlayEnemyDeathSlowmo();
    }

    //*적 몸체 5조각 폭발*
    private void SpawnDebris()
    {
        if (debrisSprites == null || debrisSprites.Length == 0) return;

        //DeathVisualRoot의 중심이 곧 수축과 폭발의 중심
        Vector2 explosionCenter = visualRoot.position;

        //죽기 직전의 초록색을 조각에도 그대로 적용
        Color debrisColor = enemySprite.color;

        for (int i = 0; i < debrisSprites.Length; i++)
        {
            Sprite debrisSprite = debrisSprites[i];

            //Inspector에 Sprite가 빠져있다면 해당 조각만 건너뜀
            if (debrisSprite == null) continue;

            EnemyDebrisPiece piece = debrisPool.Get();
            //폭발 중심 주변에서 아주 조금씩 다른 위치로 시작
            Vector2 spawnPosition = explosionCenter + Random.insideUnitCircle * debrisSpawnRadius;

            //5개의 파츠를 폭발 중심 기준으로 사방에 균등하게 배치
            float baseAngle = 360f / debrisSprites.Length * i;

            //너무 기계적으로 퍼지지 않도록 방향에 약간의 랜덤 오차 추가
            float angle = baseAngle + Random.Range(-directionJitter, directionJitter);

            //각도를 실제 2D 방향 벡터로 변환
            float angleRad = angle * Mathf.Deg2Rad;

            Vector2 explosionDirection = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            //파츠마다 폭발 속도를 조금씩 다르게
            float explosionSpeed = Random.Range(minExplosionSpeed, maxExplosionSpeed);

            Vector2 velocity = explosionDirection * explosionSpeed;

            //각 조각마다 회전 방향과 속도를 조금씩 다르게
            float angularVelocity = Random.Range(-maxAngularSpeed, maxAngularSpeed);

            piece.Play(
                debrisSprite,
                debrisColor,
                spawnPosition,
                velocity,
                angularVelocity,
                enemySprite.flipX,
                originalDebrisScale,
                minBodyScale,                   //본체가 줄어든 크기에서 시작
                debrisRestoreDuration,          //원래 크기로 빠르게 복원
                debrisPool.Return
                );
        }
    }
}