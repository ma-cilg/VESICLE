//**플레이어 피격 후 무적 시간 처리**
using UnityEngine;

public class PlayerHitInvincibility : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;                     //실제 피격 이벤트 확인
    [SerializeField] private PlayerInvincibility playerInvincibility;       //무적 상태 관리
    [SerializeField, Min(0f)] private float invincibilityDuration = 0.8f;   //피격 후 무적 시간

    private float remainingTime;                                            //남은 무적 시간
    private bool isActive;                                                  //피격 무적이 현재 활성화 중인지

    //*피격 이벤트 구독*
    private void OnEnable()
    {
        playerHealth.OnDamaged += StartHitInvincibility;
    }

    //*피격 이벤트 구독 해제*
    private void OnDisable()
    {
        playerHealth.OnDamaged -= StartHitInvincibility;

        //비활성화될 때 피격 무적이 남아있으면 정상적으로 반환
        if (isActive)
        {
            playerInvincibility.RemoveInvincibility();
            isActive = false;
        }

        remainingTime = 0f;
    }

    private void Update()
    {
        if (!isActive) return;
        remainingTime -= Time.deltaTime;
        if (remainingTime > 0f) return;
        EndHitInvincibility();
    }

    //*실제 데미지를 받았을 때 피격 무적 시작*
    private void StartHitInvincibility(float damage)
    {
        //이미 피격 무적 중이면 새로 Add하지 않고 시간만 갱신
        if (isActive)
        {
            remainingTime = invincibilityDuration;
            return;
        }
        isActive = true;
        remainingTime = invincibilityDuration;
        playerInvincibility.AddInvincibility();
    }

    //*피격 무적 종료*
    private void EndHitInvincibility()
    {
        if (!isActive) return;
        isActive = false;
        remainingTime = 0f;
        playerInvincibility.RemoveInvincibility();
    }
}
