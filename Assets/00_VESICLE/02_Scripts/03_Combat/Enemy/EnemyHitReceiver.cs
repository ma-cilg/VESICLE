//**적 피격 정보 전달**
//책임: 공격 수신 가능 여부 확인 → 받은 공격 정보를 적 시스템에 전달
using System;
using UnityEngine;

public class EnemyHitReceiver : MonoBehaviour
{
    private bool canReceiveHit = true;                              //현재 새로운 공격을 받을 수 있는지

    public bool CanReceiveHit => canReceiveHit;

    //적 피격 정보 외부 시스템에 전달
    public event Action<EnemyHitInfo> OnHitReceived;

    //*외부 공격 수신*
    public void ReceiveHit(EnemyHitInfo hitInfo)
    {
        if (!canReceiveHit) return;                                 //사망 진행 중이면 추가 피격 무시

        OnHitReceived?.Invoke(hitInfo);
    }

    //*피격 가능 여부 변경*
    public void SetReceiveHitEnabled(bool enabled)
    {
        canReceiveHit = enabled;
    }

    private void OnEnable()
    {
        canReceiveHit = true;                                      //재사용될 경우 피격 상태 초기화
    }
}
