//**트리거몹 공격 패턴 제어**
//책임: 랜덤 추적 대기 → 차징 조준 → 돌진 → 4프레임 정지 → 낙하 → 재추적
//물리 이동, Sprite/Shake, 잔상은 각각 전용 컴포넌트에 위임
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyTriggerMovement))]
[RequireComponent(typeof(EnemyTriggerVisual))]
[RequireComponent(typeof(EnemyDashAfterimage))]
public class EnemyDashAttack : MonoBehaviour
{
    private enum Phase { Chase, Charge, Dash, Stop, Fall }

    [Header("References")]
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private EnemyDeath enemyDeath;
    [SerializeField] private EnemyMark enemyMark;
    [SerializeField] private EnemyHitReaction hitReaction;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private EnemyTriggerMovement movement;
    [SerializeField] private EnemyTriggerVisual visual;
    [SerializeField] private EnemyDashAfterimage afterimage;

    [Header("Chase / Random Timing")]
    [SerializeField, Min(0f)] private float startDelayMin = 0.5f;
    [SerializeField, Min(0f)] private float startDelayMax = 2f;
    [SerializeField, Min(0f)] private float attackIntervalMin = 1.5f;
    [SerializeField, Min(0f)] private float attackIntervalMax = 3f;

    [Header("Attack Timing")]
    [SerializeField, Min(0f)] private float chargeDuration = 0.5f;
    [SerializeField, Min(0f)] private float dashDuration = 0.45f;
    [SerializeField, Min(0.01f)] private float stopFrameDuration = 0.08f;

    private Phase phase = Phase.Chase;
    private Coroutine attackRoutine;
    private bool combatStarted;
    private float nextAttackTime;

    private void Awake()
    {
        if (movement == null) movement = GetComponent<EnemyTriggerMovement>();
        if (visual == null) visual = GetComponent<EnemyTriggerVisual>();
        if (afterimage == null) afterimage = GetComponent<EnemyDashAfterimage>();
    }

    private void OnEnable()
    {
        phase = Phase.Chase;
        combatStarted = false;
        if (combatRoom != null)
        {
            combatRoom.OnRoomStarted += HandleRoomStarted;
            if (combatRoom.IsStarted) HandleRoomStarted();
        }
        if (enemyDeath != null) enemyDeath.OnDeathStarted += HandleDeath;
        if (hitReaction != null) hitReaction.OnStaggerStarted += HandleStagger;
    }

    private void OnDisable()
    {
        if (combatRoom != null) combatRoom.OnRoomStarted -= HandleRoomStarted;
        if (enemyDeath != null) enemyDeath.OnDeathStarted -= HandleDeath;
        if (hitReaction != null) hitReaction.OnStaggerStarted -= HandleStagger;
        CancelAttack();
        movement.SetMode(EnemyTriggerMovement.MoveMode.Idle);
        visual.SetMotion(EnemyTriggerVisual.Motion.Idle);
        combatStarted = false;
    }

    //*방 문이 완전히 닫힌 후 1회만 전투 시작*
    private void HandleRoomStarted()
    {
        if (combatStarted) return;
        combatStarted = true;
        phase = Phase.Chase;
        movement.SetMode(EnemyTriggerMovement.MoveMode.Chase);
        ScheduleNextAttack(true);
    }

    private void Update()
    {
        if (!combatStarted) return;
        if (!CanAct())
        {
            if (attackRoutine != null) CancelAttack();
            movement.SetMode(EnemyTriggerMovement.MoveMode.Idle);
            visual.SetMotion(EnemyTriggerVisual.Motion.Idle);
            return;
        }

        if (phase == Phase.Fall)
        {
            visual.SetMotion(EnemyTriggerVisual.Motion.Fall);
            if (!movement.IsGrounded) return;
            phase = Phase.Chase;
            movement.SetMode(EnemyTriggerMovement.MoveMode.Chase);
            ScheduleNextAttack(false);
        }

        if (phase != Phase.Chase) return;

        //피격 경직이 끝나면 Idle에서 다시 Chase로 복귀
        if (movement.Mode != EnemyTriggerMovement.MoveMode.Chase)
            movement.SetMode(EnemyTriggerMovement.MoveMode.Chase);

        float dx = movement.PlayerOffsetX;
        visual.Face(dx);
        visual.SetMotion(!movement.IsGrounded ? EnemyTriggerVisual.Motion.Fall :
            movement.IsRunning ? EnemyTriggerVisual.Motion.Run : EnemyTriggerVisual.Motion.Idle);

        if (attackRoutine != null || !movement.IsGrounded || Time.time < nextAttackTime) return;
        attackRoutine = StartCoroutine(AttackRoutine());
    }

    //*검 박힌 IsPrimed 상태여도 추적, 공격 지속*
    private bool CanAct()
    {
        if (combatRoom == null || !combatRoom.IsStarted || combatRoom.IsCleared) return false;
        if (enemyDeath == null || enemyDeath.IsDead) return false;
        if (enemyMark != null && enemyMark.IsMarked) return false;
        if (hitReaction != null && hitReaction.IsStaggered) return false;
        if (playerHealth == null || playerHealth.IsDead || playerTarget == null) return false;
        return true;
    }

    private IEnumerator AttackRoutine()
    {
        phase = Phase.Charge;
        movement.SetMode(EnemyTriggerMovement.MoveMode.Charge);

        Vector2 direction = (Vector2)playerTarget.position - (Vector2)transform.position;
        if (direction.sqrMagnitude <= 0.001f)
            direction = movement.PlayerOffsetX < 0f ? Vector2.left : Vector2.right;
        direction.Normalize();
        movement.SetDashDirection(direction);
        visual.Face(direction.x);
        visual.BeginCharge(chargeDuration);

        yield return new WaitForSeconds(chargeDuration);
        if (!CanAct()) { CancelAttack(); yield break; }

        phase = Phase.Dash;
        visual.SetMotion(EnemyTriggerVisual.Motion.Dash);
        movement.SetMode(EnemyTriggerMovement.MoveMode.Dash);
        afterimage.StartTrail();
        yield return new WaitForSeconds(dashDuration);
        afterimage.StopTrail();
        if (!CanAct()) { CancelAttack(); yield break; }

        phase = Phase.Stop;
        movement.SetMode(EnemyTriggerMovement.MoveMode.Stop);
        visual.SetMotion(EnemyTriggerVisual.Motion.Stop);
        for (int i = 0; i < 4; i++)
        {
            visual.PlayStopFrame(i);
            yield return new WaitForSeconds(stopFrameDuration);
        }

        phase = Phase.Fall;
        movement.SetMode(EnemyTriggerMovement.MoveMode.Fall);
        visual.SetMotion(EnemyTriggerVisual.Motion.Fall);
        attackRoutine = null;
    }

    private void ScheduleNextAttack(bool first)
    {
        float a = first ? startDelayMin : attackIntervalMin;
        float b = first ? startDelayMax : attackIntervalMax;
        nextAttackTime = Time.time + Random.Range(Mathf.Min(a, b), Mathf.Max(a, b));
    }

    private void HandleDeath() => StopCombat();
    private void HandleStagger()
    {
        CancelAttack();
        movement.SetMode(EnemyTriggerMovement.MoveMode.Idle);
        visual.SetMotion(EnemyTriggerVisual.Motion.Idle);
        ScheduleNextAttack(false);
    }

    private void StopCombat()
    {
        CancelAttack();
        movement.SetMode(EnemyTriggerMovement.MoveMode.Idle);
        visual.SetMotion(EnemyTriggerVisual.Motion.Idle);
        combatStarted = false;
    }

    private void CancelAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        afterimage.StopTrail();
        phase = Phase.Chase;
    }
}
