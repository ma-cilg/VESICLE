//**플레이어 투척검 Projectile**
//책임: 발사 → 비행 → 적/장애물 충돌 → Trigger 적에 박힘 → 비행 종료
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ThrownSwordProjectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;                            //검 이동 처리
    [SerializeField] private Collider2D hitCollider;                    //적 / 장애물 충돌 감지
    [SerializeField] private Animator animator;                         //비행 / 박힘 Sprite 애니메이션

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 18f;            //검 비행 속도
    [SerializeField, Min(0f)] private float maxFlightDuration = 1.5f;   //아무것도 안 맞았을 때 최대 비행 시간

    [Header("Collision")]
    [SerializeField] private LayerMask enemyLayer;                      //EnemyHurtbox
    [SerializeField] private LayerMask obstacleLayer;                   //Ground / Wall

    private static readonly int FlyStateHash = Animator.StringToHash("Sword_Fly");
    private static readonly int EmbedStateHash = Animator.StringToHash("Sword_Embed");

    private Vector2 flightDirection;                                    //현재 검 비행 방향
    private float currentFlightTime;                                    //현재까지 비행한 시간
    private bool isFlying;                                              //현재 날아가는 중인지
    private bool isEmbedded;                                            //Trigger 적에 박혀있는지

    public bool IsFlying => isFlying;
    public bool IsEmbedded => isEmbedded;
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
        currentFlightTime = 0f;

        isFlying = true;
        isEmbedded = false;
        EmbeddedEnemy = null;

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
            EndFlight();
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
        currentFlightTime = 0f;

        EmbeddedEnemy = null;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        hitCollider.enabled = false;
    }
}
