//**튜토리얼 시작 상태 제어**
//책임: 튜토리얼 시작 시 플레이어 조작 잠금 → 시작 연출 완료 후 자기 Lock만 해제
using UnityEngine;

public class TutorialStartController : MonoBehaviour
{
    [SerializeField] private PlayerControlLock controlLock;     //튜토리얼 동안 잠글 Player 조작 관리

    private bool controlsLocked;                                //튜토리얼 Lock 중복 적용 방지

    private void OnEnable()
    {
        LockTutorialControls();
    }

    private void OnDisable()
    {
        ReleaseTutorialControls();
    }

    //*튜토리얼 시작 시 플레이어 전체 조작 잠금*
    private void LockTutorialControls()
    {
        if (controlsLocked) return;

        controlsLocked = true;

        controlLock.LockMovement();
        controlLock.LockJump();
        controlLock.LockAttack();
        controlLock.LockThrow();
        controlLock.LockDetonate();
    }

    //*튜토리얼 시작 연출 종료 후 조작 복구*
    public void ReleaseTutorialControls()
    {
        if (!controlsLocked) return;

        controlsLocked = false;

        controlLock.UnlockMovement();
        controlLock.UnlockJump();
        controlLock.UnlockAttack();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();
    }
}