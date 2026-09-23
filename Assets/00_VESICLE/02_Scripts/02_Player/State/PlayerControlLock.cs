//**플레이어 조작 잠금 상태 통합 관리**
//책임: 여러 시스템의 조작 잠금 요청 관리 → 모든 잠금이 해제됐을 때 실제 조작 복구
using UnityEngine;

public class PlayerControlLock : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;         //이동 제어
    [SerializeField] private PlayerJump playerJump;                 //점프 제어
    [SerializeField] private PlayerDash playerDash;                 //대시 제어

    private int movementLockCount;                                  //현재 이동을 잠그는 시스템 개수
    private int jumpLockCount;                                      //현재 점프를 잠그는 시스템 개수
    private int dashLockCount;                                      //현재 대시를 잠그는 시스템 개수

    private int attackLockCount;                                    //현재 기본 공격을 잠그는 시스템 개수
    private int throwLockCount;                                     //현재 투척 공격을 잠그는 시스템 개수
    private int detonateLockCount;                                  //현재 폭발 공격을 잠그는 시스템 개수

    public bool CanAttack => attackLockCount == 0;                  //기본 공격 Lock이 없을 때만 ture
    public bool CanThrow => throwLockCount == 0;                    //투척 공격 Lock이 없을 때만 true
    public bool CanDetonate => detonateLockCount == 0;              //폭발 공격 Lock이 없을 때만 true

    //*이동 잠금 추가*
    public void LockMovement()
    {
        movementLockCount++;                                        //이동 Lock 개수 증가
        playerMovement.SetMovementEnabled(false);                   //PlayerMovement 비활성화
    }

    //*이동 잠금 해제*
    public void UnlockMovement()
    {
        movementLockCount = Mathf.Max(0, movementLockCount - 1);    //이동 Lock 개수 감소 (음수 안되도록 제한)
        if (movementLockCount > 0) return;                          //다른 이동 Lock 남아있으면 종료
        playerMovement.SetMovementEnabled(true);                    //다시 PlayerMovement 활성화
    }

    //*점프 잠금 추가*
    public void LockJump()
    {
        jumpLockCount++;                                            //점프 Lock 개수 증가
        playerJump.SetJumpEnabled(false);                           //PlayerJump 비활성화
    }

    //*점프 잠금 해제*
    public void UnlockJump()
    {
        jumpLockCount = Mathf.Max(0, jumpLockCount - 1);            //점프 Lock 개수 감소 (음수 안되도록 제한)
        if (jumpLockCount > 0) return;                              //다른 점프 Lock이 남아있으면 종료
        playerJump.SetJumpEnabled(true);                            //다시 PlayerJump 활성화
    }

    //*대시 잠금 추가*
    public void LockDash()
    {
        dashLockCount++;                                            //대시 Lock 개수 증가
        playerDash.SetDashEnabled(false);                           //playerDash 비활성화
    }

    //*대시 잠금 해제*
    public void UnlockDash()
    {
        dashLockCount = Mathf.Max(0, dashLockCount - 1);            //대시 Lock 개수 감소 (음수 안되도록 제한)
        if (dashLockCount > 0) return;                              //다른 대시 Lock이 남아있으면 종료
        playerDash.SetDashEnabled(true);                            //다시 PlayerDash 활성화
    }

    //*기본 공격 잠금 추가*
    public void LockAttack()
    {
        attackLockCount++;                                          //기본 공격 Lock 개수 증가
    }

    //*기본 공격 잠금 해제*
    public void UnlockAttack()
    {
        attackLockCount = Mathf.Max(0, attackLockCount - 1);        //기본 공격 Lock 개수 감소 (음수 안되도록 제한)
    }

    //*투척 공격 잠금 추가*
    public void LockThrow()
    {
        throwLockCount++;                                           //투척 공격 Lock 개수 증가
    }

    //*검 투척 잠금 해제*
    public void UnlockThrow()
    {
        throwLockCount = Mathf.Max(0, throwLockCount - 1);          //투척 공격 Lock 개수 감소 (음수 안되도록 제한)
    }

    //*폭발 공격 잠금 추가*
    public void LockDetonate()
    {
        detonateLockCount++;                                        //폭발 공격 Lock 개수 증가
    }

    //*폭발 입력 잠금 해제*
    public void UnlockDetonate()
    {
        detonateLockCount = Mathf.Max(0, detonateLockCount - 1);    //폭발 공격 Lock 개수 감소 (음수 안되도록 제한)
    }   
}
