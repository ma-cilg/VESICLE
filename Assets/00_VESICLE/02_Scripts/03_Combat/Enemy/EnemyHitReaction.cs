//**적 피격 경직 상태 처리**
//책임: 피격 이벤트 수신 → 짧은 경직 상태 시작 → 일정 시간 후 경직 해제
using System;           //Action 이벤트 사용
using UnityEngine;

public class EnemyHitReaction : MonoBehaviour
{
    [SerializeField] private EnemyHitReceiver hitReceiver;              //피격됐다는 정보를 전달받는 컴포넌트
    [SerializeField, Min(0f)] private float staggerDuration = 0.1f;     //피격됐을 때 경직 시간

    private float remainingStaggerTime;                                 //경직 시간이 얼마나 남았는지 저장

    public bool IsStaggered { get; private set; }                       //적이 경직 중인지 외부에서 확인

    public event Action OnStaggerStarted;                               //경직이 처음 시작되는 순간 이벤트 알림
    public event Action OnStaggerEnded;                                 //경직이 끝나는 순간 이벤트 알림

    private void OnEnable()
    {
        hitReceiver.OnHitReceived += HandleHit;
    }

    private void OnDisable()
    {
        hitReceiver.OnHitReceived -= HandleHit;

        //비활성화될 때 경직 상태가 남지 않도록 초기화
        IsStaggered = false;
        remainingStaggerTime = 0f;
    }

    private void Update()
    {
        if (!IsStaggered) return;
        remainingStaggerTime -= Time.deltaTime;     //프레임마다 남은 경직 시간 감소
        if (remainingStaggerTime > 0f) return;
        EndStagger();                               //시간이 끝났으면 경직 종료
    }

    //*공격에 맞았을 때*
    private void HandleHit(EnemyHitInfo hitInfo)
    {
        //경직 중이면 두 번째 공격에 맞았을 때 경직 시간만 다시 갱신
        if (IsStaggered)
        {
            remainingStaggerTime = staggerDuration;
            return;
        }
        IsStaggered = true;                         //새로운 경직 시작
        remainingStaggerTime = staggerDuration;     //경직 시간 설정
        OnStaggerStarted?.Invoke();                 //외부 시스템에 경직 시작 알림
    }

    //*경직 종료*
    private void EndStagger()
    {
        if (!IsStaggered) return;
        IsStaggered = false;                        //경직 상태 해제
        remainingStaggerTime = 0f;                  //남은 시간 초기화
        OnStaggerEnded?.Invoke();                   //외부 시스템에 경직 종료 알림
    }
}
