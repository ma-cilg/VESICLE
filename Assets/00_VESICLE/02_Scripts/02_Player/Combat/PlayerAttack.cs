//**플레이어 차징 공격 처리**
//책임: 공격 입력 → 차징 → 거리 확정 → 강제 이동 공격 → 마무리 → 조작 복구
using System;       //Action 이벤트 사용
using UnityEngine;

//플레이어 공격의 현재 진행 상태
public enum PlayerAttackState
{
    Idle,           //공격하지 않는 평상시 상태
    Charging,       //좌클릭을 누르고 차징 중
    Slashing,       //확정된 거리만큼 이동 공격 중
    Finishing       //공격 도착 후 마무리 모션 중
}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;         //좌클릭 입력 확인
    [SerializeField] private PlayerMovement playerMovement;         //현재 바라보는 방향 확인
    [SerializeField] private PlayerJump playerJump;                 //공격 시작 당시 지상/공중 상태 확인
    [SerializeField] private PlayerControlLock controlLock;         //공격 중 플레이어 조작 잠금
    [SerializeField] private PlayerHealth playerHealth;             //차징 중 피격 감지

    [Header("Charge")]
    [SerializeField, Min(0f)] private float minSlashDistance = 1.5f;        //클릭하자마자 뗐을 때 최소 이동 거리
    [SerializeField, Min(0f)] private float maxSlashDistance = 5f;          //최대로 충전했을 때 이동 거리
    [SerializeField, Min(0.01f)] private float maxChargeDuration = 0.8f;    //최대 거리에 도달하기까지 필요한 차징 시간

    [Header("Slash")]
    [SerializeField, Min(0f)] private float slashSpeed = 22f;

    private Rigidbody2D rb;

    private float originalGravityScale;     //공격 동안 복구할 원래 중력값
    private bool isSlashHeightLocked;       //공격 동안 Y축 고정있는지 확인
    public PlayerAttackState CurrentState { get; private set; } = PlayerAttackState.Idle;
    public bool IsAttacking => CurrentState != PlayerAttackState.Idle;
    public bool IsCharging => CurrentState == PlayerAttackState.Charging;
    public bool IsCommittedAttack =>
        CurrentState == PlayerAttackState.Slashing ||
        CurrentState == PlayerAttackState.Finishing;

    public float CurrentChargeDistance { get; private set; }
    public float ChargeNormalized { get; private set; }
    public bool IsAirAttack { get; private set; }
    private float currentChargeTime;
    private float remainingSlashDistance;
    private float slashDirection;
    private bool controlsLocked;

    public event Action OnChargeStarted;
    public event Action<float, float> OnChargeChanged;
    public event Action<float> OnSlashStarted;
    public event Action OnFinishStarted;
    public event Action OnAttackCancelled;
    public event Action OnAttackEnded;
    public event Action OnAttackHit;
    public event Action OnGroundAttackStarted;
    public event Action OnAirAttackStarted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;
    }

    private void OnEnable()
    {
        playerHealth.OnHitReceived += HandlePlayerHit;
    }

    private void OnDisable()
    {
        playerHealth.OnHitReceived -= HandlePlayerHit;
        ReleaseSlashHeight();                           //Slash 도중 오브젝트가 꺼져도 중력값이 0으로 영구적으로 남지 않게 복구
        ReleaseControls();                              //조작 잠금도 복구
        CurrentState = PlayerAttackState.Idle;
    }

    private void Update()
    {
        if (CurrentState == PlayerAttackState.Idle)
        {
            TryStartCharge();
            return;
        }
        if (CurrentState == PlayerAttackState.Charging)
        {
            UpdateCharge();
        }
    }

    private void FixedUpdate()
    {
        if (CurrentState != PlayerAttackState.Slashing) return;
        UpdateSlashMovement();
    }

    //*새로운 차징 공격 시작 시도*
    private void TryStartCharge()
    {
        if (!inputReader.IsAttackPressed()) return;
        if (!controlLock.CanAttack) return;
        StartCharge();
    }

    //*차징 시작*
    private void StartCharge()
    {
        CurrentState = PlayerAttackState.Charging;
        currentChargeTime = 0f;                                             //차징 시간 초기화

        //처음에는 최소 거리부터 시작
        ChargeNormalized = 0f;
        CurrentChargeDistance = minSlashDistance;

        slashDirection = playerMovement.IsFacingRight ? 1f : -1f;           //공격을 누른 순간 바라보던 방향 저장

        LockControls();                                                     //차징부터 마무리가 끝날 때까지 조작 잠금

        OnChargeStarted?.Invoke();                                          //외부에 차징 시작 알림
        OnChargeChanged?.Invoke(ChargeNormalized, CurrentChargeDistance);   //거리 Guide가 처음부터 최소 거리를 보여줄 수 있도록 초기 값 즉시 전달
    }

    //*차징 상태 갱신*
    private void UpdateCharge()
    {
        //좌클릭 계속 누르고 있으면 차징 시간 증가
        if (inputReader.IsAttackHeld())
        {
            currentChargeTime += Time.deltaTime;

            //현재 차징 시간을 0~1 범위로 변환
            ChargeNormalized = Mathf.Clamp01(currentChargeTime / maxChargeDuration);

            //차징 비율에 따라 최소 거리 → 최대 거리로 증가
            CurrentChargeDistance = Mathf.Lerp(minSlashDistance, maxSlashDistance, ChargeNormalized);

            //외부에 현재 차징량 전달
            OnChargeChanged?.Invoke(ChargeNormalized, CurrentChargeDistance);
        }

        //좌클릭 뗀 순간 현재 충전된 거리로 공격 확정
        if (inputReader.IsAttackReleased())
        {
            StartSlash();
        }
    }

    //*차징 완료 후 실제 이동 공격 시작*
    private void StartSlash()
    {
        if (CurrentState != PlayerAttackState.Charging) return;

        CurrentState = PlayerAttackState.Slashing;
        IsAirAttack = !playerJump.IsGrounded;                       //좌클릭을 뗀 순간 공중여부 재판단
        remainingSlashDistance = CurrentChargeDistance;             //이번에 반드시 이동해야 하는 거리 저장
        LockSlashHeight();                                          //Slash가 시작되는 순간 현재 Y 위치 고정
        OnSlashStarted?.Invoke(CurrentChargeDistance);              //외부에 실제 공격 시작 알림
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
        float currentSlashSpeed = stepDistance / Time.fixedDeltaTime;                       //실제 X 속도 계산
        rb.linearVelocity = new Vector2(slashDirection * currentSlashSpeed, 0f);            //같은 Y 위치에서 수평으로 이동
        remainingSlashDistance -= stepDistance;                                             //이번 프레임 이동 예정 거리를 차감

        //부동소수점 오차 방지
        if (remainingSlashDistance < 0.001f)
        {
            remainingSlashDistance = 0f;
        }
    }

    //*이동 공격 완료*
    private void StartFinish()
    {
        if (CurrentState != PlayerAttackState.Slashing) return;
        CurrentState = PlayerAttackState.Finishing;
        rb.linearVelocity = Vector2.zero;               //Finish 프레임 끝날 때까지 높이 유지
        OnFinishStarted?.Invoke();                      // Finish Animation 시작 알림
    }

    //*플레이어가 실제 데미지를 받았을 때*
    private void HandlePlayerHit(Vector2 hitDirection)
    {
        //차징 중일 때만 공격 취소
        if (CurrentState != PlayerAttackState.Charging) return;
        CancelCharge();
    }

    //*차징 중 피격으로 공격 취소*
    private void CancelCharge()
    {
        if (CurrentState != PlayerAttackState.Charging) return;
        CurrentState = PlayerAttackState.Idle;

        currentChargeTime = 0f;
        ChargeNormalized = 0f;
        CurrentChargeDistance = minSlashDistance;

        ReleaseControls();                              //조작 잠금 해제

        OnAttackCancelled?.Invoke();                    //차징 / 거리가이드 / 애니메이션이 알 수 있도록 알림
    }

    //*Finish Animation 마지막 프레임에서 호출*
    public void HandleAttackEndAnimationEvent()
    {
        if (CurrentState != PlayerAttackState.Finishing) return;
        EndAttack();
    }

    //*Animation Event 호환용*
    public void HandleAttackHitAnimationEvent()
    {
        if (CurrentState != PlayerAttackState.Slashing) return;
        OnAttackHit?.Invoke();
    }

    //*공격 전체 정상 종료*
    private void EndAttack()
    {
        CurrentState = PlayerAttackState.Idle;

        currentChargeTime = 0f;
        remainingSlashDistance = 0f;
        ChargeNormalized = 0f;
        CurrentChargeDistance = minSlashDistance;
        ReleaseSlashHeight();                           //중력 복구
        IsAirAttack = false;
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
        controlLock.LockDash();
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
        controlLock.UnlockDash();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();
    }
}
