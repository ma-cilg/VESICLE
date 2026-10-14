//**트리거몹 추적 및 돌진 공격**
//책임: 플레이어 추적 → 랜덤 차징 → 돌진 → 정지 모션 → 반복
using System.Collections;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDashAttack : MonoBehaviour
{
    private enum AttackPhase
    {
        Chase,
        Charge,
        Dash,
        Stop,
        Fall
    }

    [Header("References")]
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private EnemyDeath enemyDeath;
    [SerializeField] private EnemyMark enemyMark;
    [SerializeField] private EnemyHitReaction hitReaction;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Rigidbody2D rb;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer enemySprite;
    [SerializeField] private Sprite chargeSprite;
    [SerializeField] private Sprite dashSprite;
    [SerializeField] private Sprite[] stopSprites = new Sprite[4];
    [SerializeField, Min(0.01f)] private float stopFrameDuration = 0.08f;

    [Header("Chase")]
    [SerializeField, Min(0f)] private float chaseSpeed = 4f;
    [SerializeField, Min(0f)] private float startDelayMin = 0.5f;
    [SerializeField, Min(0f)] private float startDelayMax = 2f;
    [SerializeField, Min(0f)] private float attackIntervalMin = 1.5f;
    [SerializeField, Min(0f)] private float attackIntervalMax = 3f;

    [Header("Dash")]
    [SerializeField, Min(0f)] private float chargeDuration = 0.5f;
    [SerializeField, Min(0f)] private float dashSpeed = 24f;
    [SerializeField, Min(0f)] private float dashDuration = 0.45f;

    [Header("Charge Feedback")]
    [SerializeField, Min(0f)] private float shakeStrength = 0.1f;
    [SerializeField, Min(1)] private int shakeVibrato = 25;

    [Header("Afterimage")]
    [SerializeField, Range(1, 16)] private int afterimagePoolSize = 8;
    [SerializeField, Min(0.01f)] private float afterimageInterval = 0.04f;
    [SerializeField, Min(0.01f)] private float afterimageLifetime = 0.2f;
    [SerializeField, Range(0f, 1f)] private float afterimageAlpha = 0.45f;

    [Header("Ground")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.15f;
    [SerializeField, Min(0f)] private float normalGravityScale = 3f;

    [Header("Run Animation")]
    [SerializeField] private Sprite[] runSprites;
    [SerializeField, Min(0.01f)] private float runFrameDuration = 0.08f;

    [Header("Fall Animation")]
    [SerializeField] private Sprite[] fallSprites;
    [SerializeField, Min(0.01f)] private float fallFrameDuration = 0.08f;

    //현재 재생하는 달리기/낙하 애니메이션 정보
    private Sprite[] currentMotionSprites;
    private int motionFrameIndex;
    private float motionFrameTimer;

    private AttackPhase phase = AttackPhase.Chase;
    private Coroutine attackRoutine;
    private Tween chargeTween;

    private Sprite originalSprite;
    private bool originalFlipX;
    private Vector3 originalVisualPosition;

    private bool combatStarted;
    private float nextAttackTime;
    private Vector2 dashDirection;

    private SpriteRenderer[] afterimages;
    private Tween[] afterimageTweens;
    private int afterimageIndex;
    private float nextAfterimageTime;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (enemySprite != null)
        {
            originalSprite = enemySprite.sprite;
            originalFlipX = enemySprite.flipX;
        }

        if (visualRoot != null) originalVisualPosition = visualRoot.localPosition;

        CreateAfterimagePool();
    }

    private void OnEnable()
    {
        combatStarted = false;
        phase = AttackPhase.Chase;
        rb.gravityScale = normalGravityScale;
        currentMotionSprites = null;

        if (enemyDeath != null) enemyDeath.OnDeathStarted += StopAttack;
    }

    private void OnDisable()
    {
        if (enemyDeath != null) enemyDeath.OnDeathStarted -= StopAttack;

        StopAttack();
        ClearAfterimages();

        if (enemySprite != null) enemySprite.flipX = originalFlipX;
    }

    private void OnDestroy()
    {
        if (afterimages == null) return;

        foreach (SpriteRenderer image in afterimages)
        {
            if (image != null) Destroy(image.gameObject);
        }
    }


    //*현재 AI 상태 갱신 및 다음 공격 시작*
    private void Update()
    {
        //공격할 수 없는 상태면 진행 중인 공격 정리
        if (!CanAct())
        {
            if (attackRoutine != null)
            {
                StopAttack();
                ScheduleNextAttack();
            }

            return;
        }

        //방 전투 시작 후 첫 차징 시간 랜덤 설정
        if (!combatStarted)
        {
            combatStarted = true;

            nextAttackTime = Time.time + Random.Range(startDelayMin, startDelayMax);
        }

        //추가: 달리기 및 낙하 애니메이션 갱신
        UpdateMotionAnimation();

        //돌진 중 일정 간격으로 잔상 생성
        if (phase == AttackPhase.Dash &&
            Time.time >= nextAfterimageTime)
        {
            SpawnAfterimage();

            nextAfterimageTime =
                Time.time + afterimageInterval;
        }

        //평상시 추적 상태가 아니라면 새로운 차징 시작 불가
        if (phase != AttackPhase.Chase) return;

        //추가: 공중에서는 새로운 차징을 시작하지 않음
        if (!IsGrounded()) return;

        //이미 공격 코루틴 실행 중이라면 중복 실행 방지
        if (attackRoutine != null) return;

        //아직 다음 공격 시간이 아니라면 대기
        if (Time.time < nextAttackTime) return;

        //차징 → 돌진 → 정지 모션 시작
        attackRoutine = StartCoroutine(AttackRoutine());
    }



    //*현재 행동 상태에 따라 이동 및 중력 제어*
    private void FixedUpdate()
    {
        if (!CanAct())
        {
            if (rb == null) return;

            rb.gravityScale = normalGravityScale;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            return;
        }

        switch (phase)
        {
            case AttackPhase.Chase:
                rb.gravityScale = normalGravityScale;

                if (IsGrounded())
                    ChasePlayer();
                else
                    StopHorizontalMovement();

                break;

            case AttackPhase.Charge:
                //차징 중에도 중력이 작동함
                rb.gravityScale = normalGravityScale;
                StopHorizontalMovement();
                break;

            case AttackPhase.Dash:
                //공중 돌진 중에만 중력 제거
                rb.gravityScale = 0f;
                rb.linearVelocity = dashDirection * dashSpeed;
                break;

            case AttackPhase.Stop:
                //4프레임 정지 모션 동안 현재 위치 고정
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
                break;

            case AttackPhase.Fall:
                //정지 모션 종료 후 중력으로 자연 낙하
                rb.gravityScale = normalGravityScale;
                StopHorizontalMovement();

                //바닥에 착지하면 다시 추적 시작
                if (IsGrounded())
                {
                    phase = AttackPhase.Chase;
                    currentMotionSprites = null;
                    ScheduleNextAttack();
                }

                break;
        }
    }

    //*좌우 이동만 정지하고 수직 속도는 유지*
    private void StopHorizontalMovement()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    //*적이 바닥에 닿아 있는지 확인*
    private bool IsGrounded()
    {
        if (groundCheck == null) return false;

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }


    //*플레이어를 향해 지상 추적*
    private void ChasePlayer()
    {
        float distanceX = playerTarget.position.x - rb.position.x;

        float speed = Mathf.Abs(distanceX) > 0.15f ? Mathf.Sign(distanceX) * chaseSpeed : 0f;

        //중력과 점프 등 Y축 속도를 유지
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);

        //플레이어 방향 바라보기
        if (enemySprite != null &&
            Mathf.Abs(distanceX) > 0.15f)
        {
            enemySprite.flipX = distanceX < 0f;
        }
    }

    //*지상 달리기 및 공중 낙하 모션 재생*
    private void UpdateMotionAnimation()
    {
        if (enemySprite == null) return;

        Sprite[] frames;
        float frameDuration;

        //낙하 중이거나 공중에 떠 있다면 Fall
        if (phase == AttackPhase.Fall || (phase == AttackPhase.Chase && !IsGrounded()))
        {
            frames = fallSprites;
            frameDuration = fallFrameDuration;
        }
        else if (phase == AttackPhase.Chase)
        {
            //움직이지 않을 때는 기본 스프라이트
            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
            {
                if (currentMotionSprites != null)
                    RestoreSprite();

                currentMotionSprites = null;
                return;
            }

            frames = runSprites;
            frameDuration = runFrameDuration;
        }
        else
        {
            //차징 / 돌진 / 정지 모션은 기존 코드가 처리
            currentMotionSprites = null;
            return;
        }

        //아직 스프라이트를 연결하지 않았다면 기본 이미지
        if (frames == null || frames.Length == 0)
        {
            if (currentMotionSprites != frames)
                RestoreSprite();

            currentMotionSprites = frames;
            return;
        }

        //모션이 변경되면 첫 프레임부터 재생
        if (currentMotionSprites != frames)
        {
            currentMotionSprites = frames;
            motionFrameIndex = 0;
            motionFrameTimer = 0f;

            if (frames[0] != null) enemySprite.sprite = frames[0];
        }

        motionFrameTimer += Time.deltaTime;

        if (motionFrameTimer < frameDuration) return;

        motionFrameTimer -= frameDuration;

        motionFrameIndex = (motionFrameIndex + 1) % frames.Length;

        if (frames[motionFrameIndex] != null) enemySprite.sprite = frames[motionFrameIndex];
    }

    //*차징 → 돌진 → 정지 모션*
    private IEnumerator AttackRoutine()
    {
        phase = AttackPhase.Charge;
        rb.linearVelocity = Vector2.zero;

        //차징 시작 순간의 플레이어 위치를 기억
        dashDirection = (Vector2)playerTarget.position - rb.position;

        if (dashDirection.sqrMagnitude < 0.001f)
        {
            dashDirection = enemySprite != null && enemySprite.flipX ? Vector2.left : Vector2.right;
        }

        dashDirection.Normalize();

        if (enemySprite != null)
        {
            enemySprite.flipX = dashDirection.x < 0f;

            if (chargeSprite != null) enemySprite.sprite = chargeSprite;
        }

        //충돌 판정이 아닌 Visual만 DOTween으로 흔들기
        if (visualRoot != null)
        {
            StopChargeShake();

            chargeTween = visualRoot.DOShakePosition(chargeDuration, shakeStrength, shakeVibrato, 90f, false, false);
        }

        yield return new WaitForSeconds(chargeDuration);

        StopChargeShake();

        if (!CanAct())
        {
            StopAttack();
            yield break;
        }

        //기억한 방향으로 돌진
        phase = AttackPhase.Dash;

        if (enemySprite != null && dashSprite != null) enemySprite.sprite = dashSprite;

        nextAfterimageTime = Time.time;
        rb.linearVelocity = dashDirection * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        //도착 후 4프레임 정지 애니메이션
        phase = AttackPhase.Stop;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        for (int i = 0; i < 4; i++)
        {
            if (enemySprite != null &&
                stopSprites != null &&
                i < stopSprites.Length &&
                stopSprites[i] != null)
            {
                enemySprite.sprite = stopSprites[i];
            }

            yield return new WaitForSeconds(stopFrameDuration);
        }

        //정지 모션이 끝났으므로 낙하 상태 진입
        RestoreSprite();

        phase = AttackPhase.Fall;
        rb.gravityScale = normalGravityScale;

        currentMotionSprites = null;
        attackRoutine = null;
    }

    //*다음 차징 타이밍을 몹마다 다르게 결정*
    private void ScheduleNextAttack()
    {
        float min = Mathf.Min(attackIntervalMin, attackIntervalMax);
        float max = Mathf.Max(attackIntervalMin, attackIntervalMax);

        nextAttackTime = Time.time + Random.Range(min, max);
    }

    //*공격 가능 여부 확인*
    private bool CanAct()
    {
        if (combatRoom == null || !combatRoom.IsStarted) return false;
        if (combatRoom.IsCleared) return false;
        if (enemyDeath == null || enemyDeath.IsDead) return false;
        if (enemyMark != null && enemyMark.IsMarked) return false;
        if (hitReaction != null && hitReaction.IsStaggered) return false;
        if (playerHealth == null || playerHealth.IsDead) return false;
        if (playerTarget == null || rb == null) return false;

        return true;
    }

    //*차징 흔들림 원상복구*
    private void StopChargeShake()
    {
        chargeTween?.Kill();
        chargeTween = null;

        if (visualRoot != null) visualRoot.localPosition = originalVisualPosition;
    }


    //*공격 취소 또는 사망 시 정리*
    private void StopAttack()
    {
        //진행 중인 공격 코루틴 정리
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        //차징 DOTween 연출 정리
        StopChargeShake();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;

            //추가: 중력값 원상복구
            rb.gravityScale = normalGravityScale;
        }

        phase = AttackPhase.Chase;

        //추가: 달리기 / 낙하 모션 상태 초기화
        currentMotionSprites = null;

        RestoreSprite();
    }


    private void RestoreSprite()
    {
        if (enemySprite != null) enemySprite.sprite = originalSprite;
    }

    //*잔상 SpriteRenderer를 미리 생성해 재사용*
    private void CreateAfterimagePool()
    {
        if (enemySprite == null) return;

        afterimages = new SpriteRenderer[afterimagePoolSize];
        afterimageTweens = new Tween[afterimagePoolSize];

        Transform parent = combatRoom != null ? combatRoom.transform : transform.parent;

        for (int i = 0; i < afterimagePoolSize; i++)
        {
            GameObject ghost = new GameObject("DashAfterimage_" + i);

            ghost.transform.SetParent(parent, false);

            SpriteRenderer renderer = ghost.AddComponent<SpriteRenderer>();

            renderer.sharedMaterial = enemySprite.sharedMaterial;
            renderer.sortingLayerID = enemySprite.sortingLayerID;
            renderer.sortingOrder = enemySprite.sortingOrder - 1;

            ghost.SetActive(false);
            afterimages[i] = renderer;
        }
    }

    //*돌진 중 현재 모습을 잔상으로 남기기*
    private void SpawnAfterimage()
    {
        if (afterimages == null || afterimages.Length == 0) return;

        if (enemySprite == null || enemySprite.sprite == null) return;

        int index = afterimageIndex;
        afterimageIndex = (afterimageIndex + 1) % afterimages.Length;

        afterimageTweens[index]?.Kill();

        SpriteRenderer ghost = afterimages[index];

        ghost.sprite = enemySprite.sprite;
        ghost.flipX = enemySprite.flipX;
        ghost.sortingLayerID = enemySprite.sortingLayerID;
        ghost.sortingOrder = enemySprite.sortingOrder - 1;

        ghost.transform.position = enemySprite.transform.position;

        ghost.transform.rotation = enemySprite.transform.rotation;

        ghost.transform.localScale = enemySprite.transform.lossyScale;

        Color color = enemySprite.color;
        color.a *= afterimageAlpha;

        ghost.color = color;
        ghost.gameObject.SetActive(true);

        //DOTween으로 천천히 사라지게
        afterimageTweens[index] = ghost
            .DOFade(0f, afterimageLifetime)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                ghost.gameObject.SetActive(false);
                afterimageTweens[index] = null;
            });
    }

    //*사용한 잔상 전체 정리*
    private void ClearAfterimages()
    {
        if (afterimages == null) return;

        for (int i = 0; i < afterimages.Length; i++)
        {
            afterimageTweens[i]?.Kill();
            afterimageTweens[i] = null;

            if (afterimages[i] != null) afterimages[i].gameObject.SetActive(false);
        }
    }
}
