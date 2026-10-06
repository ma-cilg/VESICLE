//**플레이어 투척검 Projectile**
//책임: 발사 → 비행 → 적/장애물 충돌 → Trigger 적에 박힘 → 비행 종료
using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ThrownSwordProjectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;                            //검 이동 처리
    [SerializeField] private Collider2D hitCollider;                    //적 / 장애물 충돌 감지
    [SerializeField] private Animator animator;                         //비행 / 박힘 Sprite 애니메이션
    [SerializeField] private SpriteRenderer swordSprite;                //바닥/벽에 박힌 뒤 Fade 처리

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 18f;            //검 비행 속도
    [SerializeField, Min(0f)] private float maxFlightDuration = 1.5f;   //아무것도 안 맞았을 때 최대 비행 시간

    [Header("Environment Embed")]
    [SerializeField, Min(0f)] private float environmentHoldDuration = 0.25f;    //박힌 상태로 잠깐 유지
    [SerializeField, Min(0f)] private float environmentFadeDuration = 0.6f;     //천천히 사라지는 시간
    [SerializeField, Range(-0.2f, 0.2f)] private float environmentEmbedDepth = -0.05f;

    [Header("Collision")]
    [SerializeField] private LayerMask enemyLayer;                      //EnemyHurtbox
    [SerializeField] private LayerMask obstacleLayer;                   //Ground / Wall

    private static readonly int FlyStateHash = Animator.StringToHash("Sword_Fly");
    private static readonly int EmbedStateHash = Animator.StringToHash("Sword_Embed");

    private Vector2 flightDirection;                                    //현재 검 비행 방향
    private float currentFlightTime;                                    //현재까지 비행한 시간
    private bool isFlying;                                              //현재 날아가는 중인지
    private bool isEmbedded;                                            //Trigger 적에 박혀있는지
    private bool isEnvironmentEmbedded;                                 //Ground / Wall에 박힌 상태
    private Tween environmentFadeTween;                                 //환경에 박힌 뒤 Fade Tween

    public bool IsFlying => isFlying;
    public bool IsEmbedded => isEmbedded;
    public bool IsEnvironmentEmbedded => isEnvironmentEmbedded;
    public EnemyMark EmbeddedEnemy { get; private set; }                //현재 검이 박힌 Trigger 적

    //Trigger 적에 검이 박혔을 때
    public event Action<ThrownSwordProjectile, EnemyMark> OnEmbedded;

    //벽 / 일반 적 / 시간 초과로 비행이 끝났을 때
    public event Action<ThrownSwordProjectile> OnFlightEnded;

    private void Update()
    {
        if (!isFlying) return;

        currentFlightTime += Time.deltaTime;

        //검이 아무것도 맞지 않고 너무 오래 날아가는 상황 방지
        if (currentFlightTime >= maxFlightDuration)
        {
            EndFlight();
        }
    }

    //*검 발사*
    public void Launch(Vector2 startPosition, Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f) return;

        transform.SetParent(null, true);                                //이전 Trigger 적의 자식으로 남아있을 수 있으므로 월드로 분리

        transform.position = startPosition;

        flightDirection = direction.normalized;

        //오른쪽으로 날아갈 때는 원본,
        //왼쪽으로 날아갈 때는 Sprite만 좌우 반전
        bool isFlyingLeft = flightDirection.x < 0f;
        swordSprite.flipX = isFlyingLeft;

        float flightAngle;

        if (isFlyingLeft)
        {
            //180도 회전시키지 않고 위/아래 각도만 반대로 계산
            flightAngle = Mathf.Atan2(-flightDirection.y, -flightDirection.x) * Mathf.Rad2Deg;
        }
        else
        {
            //오른쪽은 원래 방향 그대로 각도 계산
            flightAngle = Mathf.Atan2(flightDirection.y, flightDirection.x) * Mathf.Rad2Deg;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, flightAngle);

        currentFlightTime = 0f;

        isFlying = true;
        isEmbedded = false;
        isEnvironmentEmbedded = false;
        EmbeddedEnemy = null;

        //이전 환경 Fade가 혹시 남아있다면 제거
        environmentFadeTween?.Kill();
        environmentFadeTween = null;

        //재사용되는 검의 Alpha를 다시 원래대로
        Color swordColor = swordSprite.color;
        swordColor.a = 1f;
        swordSprite.color = swordColor;

        gameObject.SetActive(true);                                     //비활성화 상태 검 다시 활성화
        animator.Play(FlyStateHash, 0, 0f);                             //검 발사체 4프레임 애니메이션 시작

        rb.simulated = true;
        hitCollider.enabled = true;

        rb.linearVelocity = flightDirection * moveSpeed;
    }

    //*Trigger Collider와 충돌했을 때*
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isFlying) return;

        //적 HurtBox와 충돌
        if (IsInLayerMask(other.gameObject.layer, enemyLayer))
        {
            HandleEnemyHit(other);
            return;
        }

        //벽 / 바닥과 충돌
        if (IsInLayerMask(other.gameObject.layer, obstacleLayer))
        {
            EmbedIntoEnvironment(other);
        }
    }

    //*적 피격 처리*
    private void HandleEnemyHit(Collider2D other)
    {
        EnemyHitReceiver hitReceiver = other.GetComponentInParent<EnemyHitReceiver>();
        EnemyMark enemyMark = other.GetComponentInParent<EnemyMark>();

        if (hitReceiver == null || enemyMark == null) return;

        //적에게 "투척검으로 맞았다"는 정보 전달
        EnemyHitInfo hitInfo = new EnemyHitInfo(EnemyHitType.ThrownSword, flightDirection);

        hitReceiver.ReceiveHit(hitInfo);

        //Trigger 적이 투척검으로 활성화됐다면 검을 몸에 박아둠
        if (enemyMark.MarkType == EnemyMarkType.Trigger && enemyMark.IsPrimed)
        {
            EmbedIntoEnemy(enemyMark);
            return;
        }

        //Normal 적은 검이 몸에 남지 않고 비행 종료
        EndFlight();
    }

    //*Trigger 적에 검 고정*
    private void EmbedIntoEnemy(EnemyMark enemyMark)
    {
        isFlying = false;
        isEmbedded = true;

        EmbeddedEnemy = enemyMark;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        hitCollider.enabled = false;
        animator.Play(EmbedStateHash, 0, 0f);                   //검 박히는 애니메이션 재생
        transform.SetParent(enemyMark.DeathVisualRoot, true);   //Trigger 죽을 때 몸이랑 검이 같은 중심으로 수축

        OnEmbedded?.Invoke(this, enemyMark);
    }

    //*Ground / Wall에 검 고정*
    private void EmbedIntoEnvironment(Collider2D obstacle)
    {
        if (!isFlying) return;

        isFlying = false;
        isEmbedded = false;
        isEnvironmentEmbedded = true;

        EmbeddedEnemy = null;

        //환경에 닿은 현재 위치에서 검 이동 정지
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        //박힌 이후에는 어떤 충돌도 받지 않음
        hitCollider.enabled = false;

        //충돌 순간 위치에서 진행 방향으로 조금 더 밀어 넣어서
        //칼날이 Ground / Wall 표면에 실제로 박힌 것처럼 보이게 처리
        transform.position += (Vector3)(flightDirection * environmentEmbedDepth);

        //적에게 박혔을 때와 같은 박힘 애니메이션 사용
        animator.Play(EmbedStateHash, 0, 0f);

        environmentFadeTween?.Kill();

        //잠깐 박혀있다가 천천히 투명해짐
        environmentFadeTween = DOTween.Sequence()
            .AppendInterval(environmentHoldDuration)
            .Append(swordSprite
            .DOFade(0f, environmentFadeDuration)
            .SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                environmentFadeTween = null;
                FinishEnvironmentEmbed();
            });
    }

    //*환경에 박힌 검 제거*
    private void FinishEnvironmentEmbed()
    {
        if (!isEnvironmentEmbedded) return;

        isEnvironmentEmbedded = false;

        OnFlightEnded?.Invoke(this);

        gameObject.SetActive(false);
    }

    //*일반 비행 종료*
    private void EndFlight()
    {
        if (!isFlying) return;

        isFlying = false;
        currentFlightTime = 0f;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        hitCollider.enabled = false;

        OnFlightEnded?.Invoke(this);

        gameObject.SetActive(false);
    }

    //*Trigger 폭발 후 박힌 검 제거*
    public void HideEmbeddedSword()
    {
        if (!isEmbedded) return;

        isEmbedded = false;
        EmbeddedEnemy = null;

        transform.SetParent(null, true);

        gameObject.SetActive(false);
    }

    //*LayerMask 안에 해당 Layer가 포함되어 있는지 확인*
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    private void OnDisable()
    {
        isFlying = false;
        isEmbedded = false;
        isEnvironmentEmbedded = false;
        currentFlightTime = 0f;

        environmentFadeTween?.Kill();
        environmentFadeTween = null;

        EmbeddedEnemy = null;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        hitCollider.enabled = false;

        if (swordSprite != null)
        {
            Color swordColor = swordSprite.color;
            swordColor.a = 1f;
            swordSprite.color = swordColor;
        }
    }
}
