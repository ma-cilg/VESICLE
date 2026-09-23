//**플레이어 Animation Event를 실제 기능 스크립트에 전달**
//책임: Visual의 Animation Event → 해당 Feedback에 전달
using UnityEngine;

public class PlayerAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private PlayerRunFeedback runFeedback;
    [SerializeField] private PlayerAttack playerAttack;

    //*Run 0 - 첫 번째 발 착지*
    public void OnRunBackContact()
    {
        runFeedback.PlayBackContact();
    }

    //*Run 2 - 첫 번째 발 떼기*
    public void OnRunBackRelease()
    {
        runFeedback.PlayBackRelease();
    }

    //*Run 4 - 반대쪽 발 착지*
    public void OnRunFrontContact()
    {
        runFeedback.PlayFrontContact();
    }

    //*Run 6 - 반대쪽 발 떼기*
    public void OnRunFrontRelease()
    {
        runFeedback.PlayFrontRelease();
    }

    //*공격 타격 프레임*
    public void OnAttackHit()
    {
        playerAttack.HandleAttackHitAnimationEvent();
    }


    //*공격 애니메이션 마지막 프레임*
    public void OnAttackEnd()
    {
        playerAttack.HandleAttackEndAnimationEvent();
    }
}
