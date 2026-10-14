//**적과 플레이어의 접촉 데미지 처리**
//책임: HurtBox 접촉 감지 → 이동 공격 예외 확인 → 플레이어 데미지 적용
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyContactDamage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private EnemyDeath enemyDeath;
    [SerializeField] private EnemyMark enemyMark;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAttack playerAttack;

    [Header("Damage")]
    [SerializeField, Min(0f)] private float contactDamage = 70f;

    //*플레이어가 HurtBox에 처음 닿았을 때*
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyContactDamage(other);
    }

    //*플레이어가 HurtBox와 계속 겹쳐 있을 때*
    private void OnTriggerStay2D(Collider2D other)
    {
        TryApplyContactDamage(other);
    }

    //*몸통박치기 데미지 처리*
    private void TryApplyContactDamage(Collider2D other)
    {
        //방의 전투가 시작되기 전에는 공격하지 않음
        if (combatRoom == null || !combatRoom.IsStarted) return;
        if (combatRoom.IsCleared) return;

        //사망하거나 마킹 완료된 적은 공격하지 않음
        if (enemyDeath == null || enemyDeath.IsDead) return;
        if (enemyMark != null && enemyMark.IsMarked) return;

        if (playerHealth == null || playerHealth.IsDead) return;
        if (playerAttack == null) return;

        //실제로 닿은 대상이 플레이어인지 확인
        PlayerHealth hitPlayer = other.GetComponentInParent<PlayerHealth>();

        if (hitPlayer != playerHealth) return;

        //좌클릭 이동 공격 중에는 일반 접촉 데미지 무시
        //Trigger 적 반격은 PlayerAttackHitBox에서 별도 처리
        if (playerAttack.CurrentState == PlayerAttackState.Slashing) return;

        //적의 위치를 기준으로 플레이어가 튕겨날 방향
        float directionX = playerHealth.transform.position.x >= enemyDeath.transform.position.x ? 1f : -1f;

        Vector2 hitDirection = new Vector2(directionX, 0f);

        //기존 무적 시간, 피격, 넉백 시스템 재사용
        playerHealth.TakeDamage(contactDamage, hitDirection);
    }
}
