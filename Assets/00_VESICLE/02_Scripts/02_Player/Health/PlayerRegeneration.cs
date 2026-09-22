//**플레이어 체력 자동 회복 처리**
//책임: 살아있는 동안 부족한 체력을 매 프레임 일정량 회복
using UnityEngine;

public class PlayerRegeneration : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;                     //실제 체력 값을 관리하는 PlayerHealth
    [SerializeField, Min(0f)] private float regenerationPerSecond = 15f;    //1초 동안 회복할 체력 양

    private void Update()
    {
        if (playerHealth.IsDead) return;                                    //이미 죽었으면 회복하지 않음
        if (playerHealth.CurrentHealth <= 0f) return;                       //체력이 0 이하라면 회복하지 않음
        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth) return;   //이미 최대 체력이면 회복할 필요가 없으므로 종료

        float healAmount = regenerationPerSecond * Time.deltaTime;          //이번 프레임에 회복할 체력 계산

        playerHealth.Heal(healAmount);                                      //실제 체력 회복은 PlayerHealth에게 요청
    }
}
