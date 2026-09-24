//**플레이어 검 투척 상태 처리**
//책임: 투척 입력 확인 → 투척 상태 시작 → 조작 잠금 → 투척/재생성 Animation Event 전달 → 조작 복구
using System;       //Action 이벤트 사용
using UnityEngine;

public class PlayerWeaponThrow : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;         //C 투척 입력 확인
    [SerializeField] private PlayerControlLock controlLock;         //투척 중 플레이어 조작 잠금 관리

    public bool IsThrowing { get; private set; }                    //투척 동작 진행 중인지 외부에서 확인
    public bool IsWeaponAvailable { get; private set; } = true;     //현재 검을 사용할 수 있는 상태인지 확인

    public event Action OnThrowStarted;                             //투척이 시작되는 순간 이벤트 알림
    public event Action OnSwordReleased;                            //실제 검을 손에서 놓는 프레임 이벤트 알림
    public event Action OnThrowFinished;                            //검을 다시 사용할 수 있게 된 순간 이벤트 알림

    private void Update()
    {
        if (!inputReader.IsThrowPressed()) return;
        if (!controlLock.CanThrow) return;
        if (IsThrowing) return;
        if (!IsWeaponAvailable) return;

        StartThrow();                                               //투척 시작
    }

    //*검 투척 시작*
    private void StartThrow()
    {
        IsThrowing = true;                                          //투척 전체 과정 시작
        IsWeaponAvailable = false;                                  //현재 검이 Player 손에 없는 상태
        OnThrowStarted?.Invoke();                                   //투척 시작 알림

        //투척 시작 후 플레이어 조작 잠금
        controlLock.LockMovement();
        controlLock.LockJump();
        controlLock.LockDash();
        controlLock.LockAttack();
        controlLock.LockThrow();
        controlLock.LockDetonate();
    }

    //*투척 Animation의 실제 검 발사 프레임에서 호출*
    public void HandleSwordReleaseAnimationEvent()
    {
        if (!IsThrowing) return;

        OnSwordReleased?.Invoke();                                  //외부 Projectile 시스템에 검 발사 알림
    }

    //*검 재생성 Animation의 마지막 프레임에서 호출*
    public void HandleWeaponReadyAnimationEvent()
    {
        if (!IsThrowing) return;

        IsWeaponAvailable = true;                                   //검이 다시 Player 손에 생긴 상태
        IsThrowing = false;                                         //투척 전체 과정 종료

        //조작 잠금 해제
        controlLock.UnlockMovement();
        controlLock.UnlockJump();
        controlLock.UnlockDash();
        controlLock.UnlockAttack();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();

        OnThrowFinished?.Invoke();                                  //투척 과정 종료 알림
    }

    private void OnDisable()
    {
        if (!IsThrowing) return;

        IsThrowing = false;
        IsWeaponAvailable = true;

        controlLock.UnlockMovement();
        controlLock.UnlockJump();
        controlLock.UnlockDash();
        controlLock.UnlockAttack();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();
    }
}
