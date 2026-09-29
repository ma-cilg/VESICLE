//**플레이어 이동 공격 타격 판정**
//책임: 이동 공격 중 지나간 경로 검사 → 적 감지 → 한 공격당 적 1회 타격
using System.Collections.Generic;   //HashSet 사용
using UnityEngine;

public class PlayerAttackHitBox : MonoBehaviour
{
    [SerializeField] private PlayerAttack playerAttack;                 //현재 이동 공격 상태와 방향 확인
    [SerializeField] private PlayerHealth playerHealth;                 //Trigger 적 충돌 시 플레이어 강제 피격 처리
    [SerializeField, Min(0f)] private float triggerHitDamage = 60f;     //Trigger 적 충돌 데미지

    [Header("Hit Detection")]
    [SerializeField] private LayerMask enemyLayer;                      //EnemyHurtbox Layer
    [SerializeField, Min(0.01f)] private float hitboxThickness = 0.8f;  //이동 경로 주변 공격 판정 두께
    [SerializeField, Min(0f)] private float hitboxPadding = 0.6f;       //이동 경로 앞뒤로 추가할 공격 범위

    private readonly Collider2D[] hitResults = new Collider2D[16];      //한 번의 검사에서 감지된 Collider 저장
    private readonly HashSet<EnemyHitReceiver> hitEnemies =             //한 번의 이동 공격에서 이미 맞은 적 저장
        new HashSet<EnemyHitReceiver>();
    private ContactFilter2D enemyFilter;
    private Vector2 previousPosition;                                   //직전 물리 프레임의 플레이어 위치

    private void Awake()
    {
        //EnemyHurtbox가 Trigger이므로 Trigger도 검사
        enemyFilter = new ContactFilter2D();
        enemyFilter.useTriggers = true;
        enemyFilter.SetLayerMask(enemyLayer);

        previousPosition = transform.position;                          //초기 위치 저장
    }

    private void OnEnable()
    {
        playerAttack.OnSlashStarted += HandleSlashStarted;
    }

    private void OnDisable()
    {
        playerAttack.OnSlashStarted -= HandleSlashStarted;
        hitEnemies.Clear();                                             //남아있는 기록 제거
    }

    private void FixedUpdate()
    {
        if (playerAttack.CurrentState != PlayerAttackState.Slashing)    //이동 공격 중이 아니라면 현재 위치만 계속 기억
        {
            previousPosition = transform.position;
            return;
        }

        Vector2 currentPosition = transform.position;                   //이번 물리 프레임의 현재 위치
        CheckSlashPath(previousPosition, currentPosition);              //직전 위치 → 현재 위치 사이 전체를 공격 판정
        previousPosition = currentPosition;                             //다음 물리 프레임 검사를 위해 현재 위치 저장
    }

    //*새로운 이동 공격 시작*
    private void HandleSlashStarted(float attackDistance)
    {
        hitEnemies.Clear();                                             //이전 공격에서 맞았던 적 기록 제거
        previousPosition = transform.position;                          //이번 이동 공격 시작 위치 저장
    }

    //*직전 위치부터 현재 위치까지 이동 공격 경로 검사*
    private void CheckSlashPath(Vector2 startPosition, Vector2 endPosition)
    {
        //이번 프레임 실제 이동 거리
        Vector2 movement = endPosition - startPosition;
        float distance = movement.magnitude;

        if (distance <= 0.001f) return;

        Vector2 direction = movement.normalized;                        //이동 방향
        Vector2 center = (startPosition + endPosition) * 0.5f;          //직전 위치와 현재 위치의 중간점

        //Box의 X축이 이동 방향을 바라보도록 회전 각도 계산
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        //실제로 이동한 거리 + 검 판정 여유 범위
        Vector2 hitboxSize = new Vector2(distance + hitboxPadding, hitboxThickness);

        //이동 경로 전체에서 EnemyHurtbox 검사
        int hitCount = Physics2D.OverlapBox(center, hitboxSize, angle, enemyFilter, hitResults);

        //감지된 적들 하나씩 처리
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = hitResults[i];

            if (hitCollider == null) continue;

            //Hurtbox의 부모에서 실제 적 피격 처리 컴포넌트 찾기
            EnemyHitReceiver hitReceiver = hitCollider.GetComponentInParent<EnemyHitReceiver>();

            if (hitReceiver == null) continue;

            //적의 Mark 타입 확인
            EnemyMark enemyMark = hitCollider.GetComponentInParent<EnemyMark>();

            //보라 Trigger 적이라면 일반 타격하지 않고 플레이어가 튕겨나감
            if (enemyMark != null && enemyMark.MarkType == EnemyMarkType.Trigger)
            {
                if (!hitEnemies.Add(hitReceiver)) continue;     //같은 이동 공격에서 같은 Trigger 적을 여러 번 처리하지 않도록 기록
                playerAttack.InterruptAttack();                 //이동 공격 즉시 중단

                //적에게서 플레이어 반대 방향으로 밀려나도록 방향 계산
                Vector2 hitDirection = ((Vector2)transform.position - (Vector2)hitCollider.transform.position).normalized;

                playerHealth.TakeForcedDamage(triggerHitDamage, hitDirection);  //이동 공격 무적을 무시하고 강제 데미지 적용
                return;
            }

            if (!hitEnemies.Add(hitReceiver)) continue;         //일반 적은 같은 이동 공격에서 한 번만 타격

            //현재 이동 공격 방향을 그대로 피격 방향으로 전달
            EnemyHitInfo hitInfo = new EnemyHitInfo(EnemyHitType.Melee, playerAttack.AttackDirection);

            hitReceiver.ReceiveHit(hitInfo);                    //적에게 실제 피격 전달
        }
    }
}