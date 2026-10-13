//**튜토리얼 적 처치 후 출구 개방**
//책임: 적 사망 감지 → 조작 잠금 → 통로 개방 → 문 개방 완료 후 조작 복구
using UnityEngine;

public class TutorialEnemyExitController : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField] private EnemyDeath targetEnemy;                //처치해야 할 튜토리얼 적
    [SerializeField] private CombatRoom tutorialRoom;               //현재 튜토리얼 방
    [SerializeField] private RoomDoor nextEntryDoor;                //다음 방 왼쪽 문 (없으면 비워둠)

    [Header("Player")]
    [SerializeField] private PlayerControlLock controlLock;         //플레이어 조작 잠금
    [SerializeField] private PlayerAttack playerAttack;             //이동 공격 중단
    [SerializeField] private Rigidbody2D playerRigidbody;           //플레이어 이동 속도 정지

    private RoomDoor exitDoor;                                      //현재 방 오른쪽 문
    private bool isTriggered;                                       //중복 실행 방지
    private bool controlsLocked;                                    //이 스크립트가 잠근 조작인지 확인

    //*적 사망 이벤트 구독*
    private void OnEnable()
    {
        if (targetEnemy == null) return;

        targetEnemy.OnDied += HandleEnemyDied;
    }

    //*튜토리얼 적 처치 완료*
    private void HandleEnemyDied()
    {
        if (isTriggered) return;
        if (tutorialRoom == null || controlLock == null) return;

        exitDoor = tutorialRoom.ExitDoor;

        if (exitDoor == null) return;

        isTriggered = true;

        //공격이 자신의 Lock을 해제해도 조작이 풀리지 않도록 먼저 잠금
        LockControls();

        //이동 공격 중이라면 정상적으로 종료
        if (playerAttack != null)
        {
            playerAttack.InterruptAttack();
        }

        //좌우 이동만 정지하고 중력은 유지
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity =
                new Vector2(0f, playerRigidbody.linearVelocity.y);
        }

        //문이 열리기 전에 완료 이벤트 등록
        exitDoor.OnOpened += HandleDoorOpened;

        if (nextEntryDoor != null)
        {
            nextEntryDoor.OnOpened += HandleDoorOpened;
        }

        //이미 열린 문도 처리할 수 있도록 한 번 확인
        HandleDoorOpened();
    }

    //*필요한 문들이 모두 열렸는지 확인*
    private void HandleDoorOpened()
    {
        if (exitDoor == null || !exitDoor.IsOpen) return;

        //다음 방이 연결되어 있다면 두 문 모두 열려야 함
        if (nextEntryDoor != null && !nextEntryDoor.IsOpen) return;

        UnsubscribeDoorEvents();
        ReleaseControls();
    }

    //*플레이어 전체 조작 잠금*
    private void LockControls()
    {
        if (controlsLocked) return;

        controlsLocked = true;

        controlLock.LockMovement();
        controlLock.LockJump();
        controlLock.LockAttack();
        controlLock.LockThrow();
        controlLock.LockDetonate();
    }

    //*이 스크립트가 요청한 조작 잠금 해제*
    private void ReleaseControls()
    {
        if (!controlsLocked) return;

        controlsLocked = false;

        controlLock.UnlockMovement();
        controlLock.UnlockJump();
        controlLock.UnlockAttack();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();
    }

    //*문 이벤트 구독 해제*
    private void UnsubscribeDoorEvents()
    {
        if (exitDoor != null)
        {
            exitDoor.OnOpened -= HandleDoorOpened;
        }

        if (nextEntryDoor != null)
        {
            nextEntryDoor.OnOpened -= HandleDoorOpened;
        }
    }

    private void OnDisable()
    {
        if (targetEnemy != null)
        {
            targetEnemy.OnDied -= HandleEnemyDied;
        }

        UnsubscribeDoorEvents();
        ReleaseControls();
    }
}
