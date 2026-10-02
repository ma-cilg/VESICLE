//**플레이어 이동 공격 처리**
//책임: 공격 입력 → 목표 위치 계산 → 이동 공격 → 마무리 → 조작 복구
using System;       //Action 이벤트 사용
using UnityEngine;

//플레이어 공격의 현재 진행 상태
public enum PlayerAttackState
{
    Idle,           //공격하지 않는 평상시 상태
    PreSlash,       //좌클릭을 누르고 공격
    Slashing,       //마우스 방향으로 이동 공격 중
    Finishing       //공격 도착 후 마무리 모션 중
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;             //좌클릭 입력 확인
    [SerializeField] private PlayerMovement playerMovement;             //현재 바라보는 방향 확인
    [SerializeField] private PlayerControlLock controlLock;             //공격 중 플레이어 조작 잠금
    [SerializeField] private PlayerInvincibility playerInvincibility;   //이동 공격 중 무적 처리
    [SerializeField] private PlayerAttackGauge attackGauge;             //이동 공격 사용 가능 여부와 게이지 소비

    [Header("Slash")]
    [SerializeField, Min(0f)] private float slashSpeed = 100f;
    [SerializeField] private LayerMask obstacleLayer;                   //이동 공격을 막는 Ground / Wall 레이어
    [SerializeField, Min(0f)] private float obstacleOffset = 0.05f;     //벽에 너무 깊게 박히지 않게 약간 띄우기

    private Rigidbody2D rb;
    private Collider2D playerCollider;

    private float originalGravityScale;     //공격 동안 복구할 원래 중력값
    private bool isSlashHeightLocked;       //공격 동안 Y축 고정있는지 확인
    public PlayerAttackState CurrentState { get; private set; } = PlayerAttackState.Idle;
    public bool IsAttacking => CurrentState != PlayerAttackState.Idle;
    public bool IsCommittedAttack => CurrentState == PlayerAttackState.Slashing || CurrentState == PlayerAttackState.Finishing;

    public Vector2 AttackDirection => attackDirection;

    private float remainingSlashDistance;
    private Vector2 attackDirection;
    private Vector2 attackTargetPosition;
    private bool controlsLocked;
    private bool attackInvincibilityActive;

    public event Action OnPreSlashStarted;
    public event Action<float> OnSlashStarted;
    public event Action OnFinishStarted;
    public event Action OnAttackCancelled;
    public event Action OnAttackEnded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        originalGravityScale = rb.gravityScale;
    }

    private void OnDisable()
    {
        StopAttackInvincibility();                      //공격 도중 꺼져도 무적 요청 복구
        ReleaseSlashHeight();                           //Slash 도중 오브젝트가 꺼져도 중력값이 0으로 영구적으로 남지 않게 복구
        ReleaseControls();                              //조작 잠금도 복구
        CurrentState = PlayerAttackState.Idle;
    }

    private void Update()
    {
        //Idle 또는 Finish 모션 중일 때만 이동 공격 입력 허용
        if (CurrentState != PlayerAttackState.Idle &&
            CurrentState != PlayerAttackState.Finishing)
        {
            return;
        }

        if (!inputReader.IsAttackPressed()) return;                 //좌클릭 안했으면 종료
        if (!controlLock.CanAttack) return;                         //공격이 잠겨있다면 종료
        if (!attackGauge.TryUseAttack()) return;                    //공격 1회분 게이지가 부족하면 공격하지 않음

        StartPreSlash();                                            //공격 직전 상태 시작
    }

    private void FixedUpdate()
    {
        if (CurrentState != PlayerAttackState.Slashing) return;
        UpdateSlashMovement();
    }

    //*공격 직전 상태 시작*
    private void StartPreSlash()
    {
        CurrentState = PlayerAttackState.PreSlash;

        //Input System을 통해 현재 마우스 화면 좌표 가져오기
        Vector2 mouseScreenPosition = inputReader.AimPosition;

        //화면 좌표를 월드 좌표로 변환
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, 0f));

        mouseWorldPosition.z = 0f;                                      //Z축은 사용하지 않음
        attackTargetPosition = mouseWorldPosition;                      //좌클릭 순간 목표 위치 저장
        Vector2 direction = (Vector2)mouseWorldPosition - rb.position;  //방향 계산(플레이어 → 마우스 위치)
        attackDirection = direction.normalized;                         //거리는 제거하고 방향만 저장

        if (Mathf.Abs(direction.x) > 0.001f)
        {
            playerMovement.SetFacingDirection(direction.x > 0f);        //이동 공격 방향에 맞춰 캐릭터 좌우 방향 전환
        }

        AdjustTargetByObstacle();                                       //벽 직전까지만 목표 위치 보정
        LockControls();                                                 //공격 종료까지 조작 잠금

        OnPreSlashStarted?.Invoke();                                    //PreSlash 애니메이션 시작
    }

    //*이동 공격 목표 지점을 환경 충돌 기준으로 보정*
    private void AdjustTargetByObstacle()
    {
        //현재 위치에서 목표 위치까지의 거리 계산
        float distance = Vector2.Distance(rb.position, attackTargetPosition);

        if (distance <= 0.001f) return;

        //Player Collider 전체를 이동 방향으로 Cast
        RaycastHit2D[] hits = new RaycastHit2D[8];

        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(obstacleLayer);
        contactFilter.useTriggers = false;

        int hitCount = playerCollider.Cast(attackDirection, contactFilter, hits, distance);

        if (hitCount == 0) return;
        float nearestDistance = distance;
        bool foundBlockingObstacle = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hits[i];

            if (hit.collider == null) continue;

            //공격 방향과 충돌한 면의 방향 비교
            float blockingDot = Vector2.Dot(attackDirection, hit.normal);

            if (blockingDot >= -0.01f) continue;

            //실제로 앞을 막는 환경 중 가장 가까운 것 저장
            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                foundBlockingObstacle = true;
            }
        }
        if (!foundBlockingObstacle) return;

        //충돌 지점보다 아주 조금 앞에서 멈추도록 거리 보정
        float safeDistance = Mathf.Max(0f, nearestDistance - obstacleOffset);

        //최종 이동 공격 목표 위치 변경
        attackTargetPosition = rb.position + attackDirection * safeDistance;
    }

    //*PreSlash 1프레임이 끝났을 때 Animation Event에서 호출*
    public void HandlePreSlashEndAnimationEvent()
    {
        if (CurrentState != PlayerAttackState.PreSlash) return;

        StartSlash();                                               //실제 이동 공격 시작
    }

    //*실제 이동 공격 시작*
    private void StartSlash()
    {
        if (CurrentState != PlayerAttackState.PreSlash) return;

        CurrentState = PlayerAttackState.Slashing;                  //실제 이동 공격 상태 시작

        remainingSlashDistance = Vector2.Distance(rb.position, attackTargetPosition);

        StartAttackInvincibility();                                 //실제 이동 공격이 시작되는 순간 무적 시작
        LockSlashHeight();

        OnSlashStarted?.Invoke(remainingSlashDistance);
    }

    //*이동 공격 무적 시작*
    private void StartAttackInvincibility()
    {
        if (attackInvincibilityActive) return;

        attackInvincibilityActive = true;
        playerInvincibility.AddInvincibility();
    }

    //*이동 공격 무적 종료*
    private void StopAttackInvincibility()
    {
        if (!attackInvincibilityActive) return;

        attackInvincibilityActive = false;
        playerInvincibility.RemoveInvincibility();
    }

    //*공격 시작 시 현재 높이 고정*
    private void LockSlashHeight()
    {
        if (isSlashHeightLocked) return;
        isSlashHeightLocked = true;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);   //현재 상승 / 낙하 속도를 즉시 제거
        rb.gravityScale = 0f;                                       //중력 제거
    }

    //*공격 높이 고정 해제*
    private void ReleaseSlashHeight()
    {
        if (!isSlashHeightLocked) return;
        isSlashHeightLocked = false;
        rb.gravityScale = originalGravityScale;                     //원래 중력값 복구
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);   //Finish 위치에서 Y속도 0으로 시작
    }

    //*이동 공격 이동 처리*
    private void UpdateSlashMovement()
    {
        //마지막 이동이 끝났다면 마무리 상태로 진입
        if (remainingSlashDistance <= 0f)
        {
            StartFinish();
            return;
        }
        float normalStepDistance = slashSpeed * Time.fixedDeltaTime;                        //이번 물리 프레임에서 원래 이동할 거리
        float stepDistance = Mathf.Min(normalStepDistance, remainingSlashDistance);         //마지막 프레임에서 목표 거리를 초과하지 않도록 제한
        float currentSlashSpeed = stepDistance / Time.fixedDeltaTime;                       //실제 이동 속도 계산
        rb.linearVelocity = attackDirection * currentSlashSpeed;                            //마우스 방향으로 이동 공격
        remainingSlashDistance -= stepDistance;                                             //이번 프레임 이동 예정 거리를 차감

        //부동소수점 오차 방지
        if (remainingSlashDistance < 0.001f)
        {
            remainingSlashDistance = 0f;
        }
    }

    //*이동 공격 강제 중단*
    public void InterruptAttack()
    {
        if (CurrentState != PlayerAttackState.Slashing) return; //실제 이동 공격 중이 아닐 때는 무시
        rb.linearVelocity = Vector2.zero;                       //현재 이동 즉시 정지
        remainingSlashDistance = 0f;                            //남은 이동 거리 제거
        CurrentState = PlayerAttackState.Idle;                  //공격 상태 종료
        StopAttackInvincibility();                              //무적 종료

        ReleaseSlashHeight();                                   //공격 중 제거했던 중력 복구
        ReleaseControls();                                      //일반 조작 잠금 해제

        OnAttackCancelled?.Invoke();                            //공격 취소 알림
    }

    //*이동 공격 완료*
    private void StartFinish()
    {
        if (CurrentState != PlayerAttackState.Slashing) return;
        CurrentState = PlayerAttackState.Finishing;

        StopAttackInvincibility();                      //무적 종료

        rb.linearVelocity = Vector2.zero;               //Finish 프레임 끝날 때까지 높이 유지
        OnFinishStarted?.Invoke();                      // Finish Animation 시작 알림
    }

    //*Finish Animation 마지막 프레임에서 호출*
    public void HandleAttackEndAnimationEvent()
    {
        if (CurrentState != PlayerAttackState.Finishing) return;
        EndAttack();
    }

    //*공격 전체 정상 종료*
    private void EndAttack()
    {
        CurrentState = PlayerAttackState.Idle;
        remainingSlashDistance = 0f;
        ReleaseSlashHeight();                           //중력 복구
        ReleaseControls();                              //플레이어 조작 복구
        OnAttackEnded?.Invoke();                        //공격 전체 종료 알림
    }

    //*공격 중 플레이어 조작 잠금*
    private void LockControls()
    {
        if (controlsLocked) return;

        controlsLocked = true;

        controlLock.LockMovement();
        controlLock.LockJump();
        controlLock.LockThrow();
        controlLock.LockDetonate();
    }

    //*조작 잠금 해제*
    private void ReleaseControls()
    {
        if (!controlsLocked) return;

        controlsLocked = false;

        controlLock.UnlockMovement();
        controlLock.UnlockJump();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();
    }
}
