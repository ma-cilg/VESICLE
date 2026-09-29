//**플레이어 체력 증감과 사망 상태 처리**
//책임: HP 관리 → 데미지/회복 적용 → 체력 변경 및 사망 이벤트 전달
using System;       //Action 이벤트 사용
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerInvincibility playerInvincibility;   //현재 무적 여부 확인
    [SerializeField, Min(1f)] private float maxHealth = 100f;           //최대 체력

    public float CurrentHealth { get; private set; }                    //현재 체력
    public float MaxHealth => maxHealth;                                //최대 체력 외부 제공
    public bool IsDead { get; private set; }                            //사망 여부

    public event Action<float, float> OnHealthChanged;                  //현재 체력, 최대 체력 전달
    public event Action<float> OnDamaged;                               //실제로 받은 데미지 전달
    public event Action<Vector2> OnHitReceived;                         //플레이어가 밀려날 방향 전달
    public event Action OnDied;                                         //사망 알림

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    //*데미지 처리*
    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (damage <= 0f) return;                       //잘못된 데미지 무시
        if (IsDead) return;                             //사망 후 추가 데미지 무시
        if (playerInvincibility.IsInvincible) return;   //일반 공격은 무적 중 무시

        ApplyDamage(damage, hitDirection);              //실제 데미지 적용
    }

    //*무적을 무시하는 강제 데미지 처리*
    public void TakeForcedDamage(float damage, Vector2 hitDirection)
    {
        if (damage <= 0f) return;       //잘못된 데미지 무시
        if (IsDead) return;             //사망 후 추가 데미지 무시

        //PlayerInvincibility 상태와 관계없이 실제 데미지 적용
        ApplyDamage(damage, hitDirection);
    }

    //*실제 체력 감소와 피격 이벤트 처리*
    private void ApplyDamage(float damage, Vector2 hitDirection)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);  //체력이 0 아래로 내려가지 않도록 제한
        OnDamaged?.Invoke(damage);                              //피격 관련 시스템에 알림
        OnHitReceived?.Invoke(hitDirection.normalized);         //넉백 방향 등 피격 반응에 사용할 방향 전달
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);      //UI와 회복 시스템 등에 현재 체력 전달
        if (CurrentHealth <= 0f)
        {
            Die();                                              //체력이 모두 소진됐다면 사망 처리
        }
    }

    //*체력 회복*
    public void Heal(float amount)
    {
        if (amount <= 0f) return;                                       //잘못된 회복량 무시
        if (IsDead) return;                                             //사망 상태에서는 회복 X
        if (CurrentHealth >= maxHealth) return;                         //이미 최대 체력이면 종료

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);   //최대 체력 초과 방지

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    //*사망 처리*
    private void Die()
    {
        if (IsDead) return;

        IsDead = true;
        OnDied?.Invoke();
    }
}
