//**적 사망 상태 처리**
//책임: Mark 완료 감지 → 마지막 피격 연출 종료 대기 → 사망 연출 시작 → 실제 사망 완료
using System;
using UnityEngine;

public class EnemyDeath : MonoBehaviour
{
    [SerializeField] private EnemyMark enemyMark;               //Mark 완료 상태 확인
    [SerializeField] private EnemyHitFeedback hitFeedback;      //마지막 피격 연출 종료 시점 확인
    [SerializeField] private EnemyHitReceiver hitReceiver;      //사망 확정 후 추가 피격 차단
    [SerializeField] private Collider2D hurtBox;                //이동 공격 탐지용 HurtBox

    private bool isDeathPending;                                //마지막 피격 모션이 끝나길 기다리는 중
    private bool isDeathSequenceStarted;                        //사망 연출이 이미 시작됐는지
    private bool isDead;                                        //실제 사망 완료 여부

    public bool IsDead => isDead;

    public event Action OnDeathStarted;                         //사망 연출 시작 알림
    public event Action OnDied;                                 //실제 사망 완료 알림

    private void OnEnable()
    {
        hitReceiver.SetReceiveHitEnabled(true);

        if (hurtBox != null)
        {
            hurtBox.enabled = true;
        }

        enemyMark.OnMarked += HandleMarked;
        hitFeedback.OnHitFeedbackEnded += HandleHitFeedbackEnded;
    }

    private void OnDisable()
    {
        enemyMark.OnMarked -= HandleMarked;
        hitFeedback.OnHitFeedbackEnded -= HandleHitFeedbackEnded;
    }

    //*Mark가 완성됐을 때*
    private void HandleMarked(EnemyMark mark)
    {
        if (isDead) return;

        isDeathPending = true;                                  //마지막 Hit 모션 종료 대기
        hitReceiver.SetReceiveHitEnabled(false);                //이후 들어오는 공격 정보 차단

        if (hurtBox != null)
        {
            hurtBox.enabled = false;                            //이동 공격 HitBox 탐지에서도 제외
        }
    }

    //*마지막 피격 애니메이션이 끝났을 때*
    private void HandleHitFeedbackEnded()
    {
        if (!isDeathPending) return;
        if (isDead) return;

        StartDeathSequence();
    }

    //*실제 사망 연출 시작*
    private void StartDeathSequence()
    {
        if (isDeathSequenceStarted) return;

        isDeathSequenceStarted = true;
        isDeathPending = false;

        OnDeathStarted?.Invoke();                               //Feedback에게 죽음 연출 시작 알림

        //아직 사망 Feedback이 연결되지 않은 경우를 위한 안전 처리
        if (OnDeathStarted == null)
        {
            CompleteDeath();
        }
    }

    //*사망 연출이 전부 끝난 뒤 호출*
    public void CompleteDeath()
    {
        if (isDead) return;

        isDead = true;

        OnDied?.Invoke();                                       //나중에 Room Clear가 이 이벤트를 사용
        gameObject.SetActive(false);
    }
}