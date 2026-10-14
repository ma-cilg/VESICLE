//**적과 플레이어의 접촉 데미지 처리**
//책임: 플레이어 몸통 충돌 감지 → 공격 가능 여부 확인 → 데미지 전달
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyContactDamage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private EnemyDeath enemyDeath;
    [SerializeField] private EnemyMark enemyMark;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Damage")]
    [SerializeField, Min(0f)] private float contactDamage = 40f;

    //*플레이어와 처음 부딪힌 순간*
    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryApplyContactDamage(collision);
    }

    //*계속 접촉하고 있는 경우*
    private void OnCollisionStay2D(Collision2D collision)
    {
        TryApplyContactDamage(collision);
    }

    //*실제 접촉 데미지 처리*
    private void TryApplyContactDamage(Collision2D collision)
    {
        //현재 방의 전투가 시작되기 전에는 공격하지 않음
        if (combatRoom == null || !combatRoom.IsStarted) return;
        if (combatRoom.IsCleared) return;

        //적이 죽었거나 완전히 마킹됐다면 데미지 없음
        if (enemyDeath == null || enemyDeath.IsDead) return;
        if (enemyMark != null && enemyMark.IsMarked) return;

        if (playerHealth == null || playerHealth.IsDead) return;

        //충돌한 대상이 지정한 플레이어인지 확인
        PlayerHealth hitPlayer = collision.gameObject.GetComponentInParent<PlayerHealth>();

        if (hitPlayer != playerHealth) return;

        //적을 기준으로 플레이어가 밀려날 좌우 방향
        float directionX = playerHealth.transform.position.x >= transform.position.x ? 1f : -1f;

        Vector2 hitDirection = new Vector2(directionX, 0f);

        //플레이어의 기존 무적 여부까지 포함해서 데미지 처리
        playerHealth.TakeDamage(contactDamage, hitDirection);
    }
}
