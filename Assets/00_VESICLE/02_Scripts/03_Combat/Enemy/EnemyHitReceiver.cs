//**적 피격 정보 전달**
//책임: 받은 공격 정보를 적의 Mark / 경직 / Feedback 시스템에 전달
using System;           //Action 이벤트 사용
using UnityEngine;

public class EnemyHitReceiver : MonoBehaviour
{
    //적 피격 정보 외부 시스템에 전달
    public event Action<EnemyHitInfo> OnHitReceived;

    public void ReceiveHit(EnemyHitInfo hitInfo)
    {
        OnHitReceived?.Invoke(hitInfo);
    }
}
