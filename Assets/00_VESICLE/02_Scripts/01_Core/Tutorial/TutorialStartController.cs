//**튜토리얼 시작 상태 제어**
//책임: 타이틀 중 조작 잠금 → 앉은 자세 유지 → 시작 연출 종료 후 조작 복구
using UnityEngine;

public class TutorialStartController : MonoBehaviour
{
    [SerializeField] private PlayerControlLock controlLock;     //Player 조작 잠금
    [SerializeField] private PlayerAnimation playerAnimation;   //Player 애니메이션

    private bool controlsLocked;
    private bool isRespawnReload;                               //사망 후 재시작인지

    private void Awake()
    {
        isRespawnReload = PlayerRespawn.IsRespawnReload;
    }

    private void OnEnable()
    {
        if (isRespawnReload) return;

        LockTutorialControls();
    }

    private void Start()
    {
        if (isRespawnReload) return;

        playerAnimation.HoldTutorialSitPose();
    }

    private void OnDisable()
    {
        ReleaseTutorialControls();
    }

    //*튜토리얼 시작 중 전체 조작 잠금*
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

    //*시작 연출 종료 후 자신의 조작 Lock만 해제*
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
