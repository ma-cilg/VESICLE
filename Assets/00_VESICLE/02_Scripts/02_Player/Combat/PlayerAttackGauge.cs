//**플레이어 이동 공격 게이지 관리**
//책임: 공격 게이지 보유 → 공격 비용 사용 → 자동 회복 → 변경 이벤트 전달
using System;       //Action 이벤트 사용
using UnityEngine;

public class PlayerAttackGauge : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxGauge = 100f;                    //최대 공격 게이지
    [SerializeField, Min(0f)] private float attackCost = 25f;                   //이동 공격 1회 사용량
    [SerializeField, Min(0f)] private float regenerationPerSecond = 15f;        //초당 자동 회복량

    public float CurrentGauge { get; private set; }                             //현재 공격 게이지
    public float MaxGauge => maxGauge;                                          //최대 게이지 외부 제공
    public float AttackCost => attackCost;                                      //공격 사용량 외부 제공

    public bool CanAttack => CurrentGauge >= attackCost;                        //공격 1회분 이상의 게이지가 있는지 확인
    public bool IsFull => CurrentGauge >= maxGauge;                             //UI에서 게이지가 완전히 찼는지 확인할 때 사용

    public event Action<float, float> OnGaugeChanged;                           //현재 게이지 / 최대 게이지 전달
    public event Action OnAttackFailed;                                         //게이지 부족으로 공격 실패 알림

    private void Awake()
    {
        CurrentGauge = maxGauge;
    }

    private void Update()
    {
        RegenerateGauge();
    }

    //*이동 공격 게이지 사용 시도*
    public bool TryUseAttack()
    {
        //공격 1회분의 게이지가 없다면 공격 실패
        if (!CanAttack)
        {
            OnAttackFailed?.Invoke();    //UI 흔들림 및 효과음
            return false;
        }
        CurrentGauge = Mathf.Max(0f, CurrentGauge - attackCost);                //이동 공격 1회 비용 차감
        OnGaugeChanged?.Invoke(CurrentGauge, maxGauge);                         //UI 등 외부 시스템에 변경 알림
        return true;
    }

    //*공격 게이지 자동 회복*
    private void RegenerateGauge()
    {
        if (CurrentGauge >= maxGauge) return;

        //시간에 따라 천천히 회복
        CurrentGauge = Mathf.Min(maxGauge, CurrentGauge + regenerationPerSecond * Time.deltaTime);

        OnGaugeChanged?.Invoke(CurrentGauge, maxGauge);                         //UI 등 외부 시스템에 변경 알림
    }
}