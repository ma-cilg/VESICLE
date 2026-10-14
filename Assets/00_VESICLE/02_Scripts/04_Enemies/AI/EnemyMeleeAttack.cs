//**적 근접 공격 처리**
//책임: 플레이어 공격 범위 확인 → 공격 준비 → 데미지 적용 → 재공격 대기
using System;
using System.Collections;
using UnityEngine;

public class EnemyMeleeAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private EnemyDeath enemyDeath;
    [SerializeField] private EnemyMark enemyMark;
    [SerializeField] private EnemyHitReaction hitReaction;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Attack")]
    [SerializeField, Min(0.1f)] private float attackRange = 1.6f;
    [SerializeField, Min(0f)] private float attackDamage = 40f;
    [SerializeField, Min(0f)] private float attackWindup = 0.45f;
    [SerializeField, Min(0f)] private float attackCooldown = 1.1f;

    private Coroutine attackRoutine;
    private float nextAttackTime;

    //추후 공격 애니메이션과 SFX 연결용 이벤트
    public event Action OnAttackStarted;

    private void OnEnable()
    {
        if (enemyDeath != null)
        {
            enemyDeath.OnDeathStarted += HandleDeathStarted;
        }
    }

    private void OnDisable()
    {
        if (enemyDeath != null)
        {
            enemyDeath.OnDeathStarted -= HandleDeathStarted;
        }

        StopAttack();
    }

    private void Update()
    {
        if (!CanAttack()) return;
        if (attackRoutine != null) return;
        if (Time.time < nextAttackTime) return;
        if (!IsPlayerInRange()) return;

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    //*현재 공격 가능한 상태인지 확인*
    private bool CanAttack()
    {
        if (combatRoom == null || !combatRoom.IsStarted) return false;
        if (combatRoom.IsCleared) return false;

        if (enemyDeath == null || enemyDeath.IsDead) return false;
        if (enemyMark != null && enemyMark.IsMarked) return false;
        if (hitReaction != null && hitReaction.IsStaggered) return false;

        if (playerTarget == null || playerHealth == null) return false;
        if (playerHealth.IsDead) return false;

        return true;
    }

    //*플레이어가 공격 범위 안에 있는지 확인*
    private bool IsPlayerInRange()
    {
        Vector2 enemyPosition = transform.position;
        Vector2 playerPosition = playerTarget.position;

        return Vector2.Distance(enemyPosition, playerPosition) <= attackRange;
    }

    //*공격 준비 후 플레이어에게 데미지 적용*
    private IEnumerator AttackRoutine()
    {
        //공격 시작 이벤트: 추후 모션 및 예고 이펙트 연결
        OnAttackStarted?.Invoke();

        yield return new WaitForSeconds(attackWindup);

        //공격 준비 중 적이 피격되거나 플레이어가 피했다면 데미지 없음
        if (CanAttack() && IsPlayerInRange())
        {
            float directionX = playerTarget.position.x >= transform.position.x ? 1f : -1f;

            Vector2 hitDirection = new Vector2(directionX, 0f);

            playerHealth.TakeDamage(attackDamage, hitDirection);
        }

        nextAttackTime = Time.time + attackCooldown;
        attackRoutine = null;
    }

    //*적 사망 시 진행 중인 공격 중단*
    private void HandleDeathStarted()
    {
        StopAttack();
    }

    //*공격 코루틴 정리*
    private void StopAttack()
    {
        if (attackRoutine == null) return;

        StopCoroutine(attackRoutine);
        attackRoutine = null;
    }

    //*Scene 뷰에서 공격 범위 확인*
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
