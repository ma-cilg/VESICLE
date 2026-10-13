//**튜토리얼 방 출구 개방 및 조작 잠금**
//책임: 플레이어 진입 감지 → 조작 잠금 → 양쪽 문 개방 → 개방 완료 후 조작 복구
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TutorialExitTrigger : MonoBehaviour
{
    [Header("Room")]
    [SerializeField] private CombatRoom tutorialRoom;          //현재 튜토리얼 방
    [SerializeField] private RoomDoor nextEntryDoor;           //다음 방 왼쪽 문

    [Header("Player")]
    [SerializeField] private PlayerControlLock controlLock;    //플레이어 조작 잠금

    private RoomDoor exitDoor;                                 //현재 방 오른쪽 문
    private bool isTriggered;                                  //트리거 중복 실행 방지
    private bool controlsLocked;                               //이 트리거가 조작을 잠갔는지

    //*플레이어가 출구 개방 영역에 들어왔을 때*
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;
        if (tutorialRoom == null || nextEntryDoor == null || controlLock == null) return;

        PlayerControlLock enteredControlLock = other.GetComponentInParent<PlayerControlLock>();

        if (enteredControlLock != controlLock) return;

        exitDoor = tutorialRoom.ExitDoor;
        if (exitDoor == null) return;

        isTriggered = true;

        LockControls();

        //진행 중인 이동 공격이 있다면 중단
        PlayerAttack playerAttack = controlLock.GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.InterruptAttack();
        }

        //좌우 이동은 정지하고, 점프 중이었다면 중력과 낙하는 유지
        Rigidbody2D playerRigidbody = other.attachedRigidbody;
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = new Vector2(0f, playerRigidbody.linearVelocity.y);
        }

        //문이 모두 열린 순간을 감지하기 위해 먼저 이벤트 등록
        exitDoor.OnOpened += HandleDoorOpened;
        nextEntryDoor.OnOpened += HandleDoorOpened;

        tutorialRoom.OpenPassageWithoutClear();

        HandleDoorOpened();
    }

    //*양쪽 문이 모두 열렸는지 확인*
    private void HandleDoorOpened()
    {
        if (exitDoor == null || nextEntryDoor == null) return;
        if (!exitDoor.IsOpen || !nextEntryDoor.IsOpen) return;

        exitDoor.OnOpened -= HandleDoorOpened;
        nextEntryDoor.OnOpened -= HandleDoorOpened;

        ReleaseControls();
    }

    //*문 개방 중 플레이어 조작 잠금*
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

    //*양쪽 문 개방 완료 후 조작 복구*
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

    private void OnDisable()
    {
        if (exitDoor != null)
        {
            exitDoor.OnOpened -= HandleDoorOpened;
        }

        if (nextEntryDoor != null)
        {
            nextEntryDoor.OnOpened -= HandleDoorOpened;
        }

        ReleaseControls();
    }
}
