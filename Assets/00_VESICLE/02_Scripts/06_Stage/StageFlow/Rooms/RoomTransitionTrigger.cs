//**방 사이 이동 전환 처리**
//책임: 플레이어 통과 감지 → 공격 중단 → 조작 잠금 → 플레이어/카메라 다음 방 이동 → 양쪽 문 닫기 → 조작 복구
using System;
using UnityEngine;

public class RoomTransitionTrigger : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerControlLock controlLock;         //전환 중 플레이어 조작 잠금
    [SerializeField] private PlayerAttack playerAttack;             //공격이동 중 진입했을 때 공격 강제 종료
    [SerializeField] private Rigidbody2D playerRigidbody;           //플레이어 자동 이동 및 속도 제어

    [Header("Camera")]
    [SerializeField] private RoomCameraController roomCamera;       //다음 방으로 카메라 이동
    [SerializeField] private Transform nextRoomCameraPoint;         //다음 방 CameraPoint
    [SerializeField] private CombatRoom nextRoom;                   //다음 방

    [Header("Player Entry")]
    [SerializeField] private Transform playerEntryPoint;            //다음 방 안쪽의 안전한 진입 위치

    [SerializeField, Min(0f)] private float playerMoveDuration = 0.65f;     //다음 방 안쪽까지 자동 이동하는 시간

    [Header("Doors")]
    [SerializeField] private RoomDoor previousExitDoor;             //이전 방의 오른쪽 문
    [SerializeField] private RoomDoor nextEntryDoor;                //다음 방의 왼쪽 문

    private bool isPlayerAutoMoving;                                //다음 방 안쪽으로 자동 이동 중인지
    private float playerTargetX;                                    //자동 이동 목표 X 위치
    private float playerAutoMoveSpeed;                              //자동 이동에 사용할 실제 Rigidbody 속도

    private bool transitionStarted;                                 //같은 Trigger 중복 실행 방지
    private bool controlsLocked;                                    //이 Trigger가 조작 Lock을 걸었는지

    private bool cameraMoveCompleted;                               //카메라가 다음 방에 도착했는지
    private bool playerMoveCompleted;                               //플레이어가 안전 지점까지 이동했는지

    private int closedDoorCount;                                    //닫힘 완료된 문 개수

    private float originalGravityScale;                             //전환 전 플레이어 중력값
    private bool playerPhysicsOverridden;                           //전환 중 물리값을 변경했는지

    private BoxCollider2D transitionCollider;                       //방 통로의 Trigger 영역
    private CapsuleCollider2D playerBodyCollider;                   //플레이어 실제 충돌 영역
    private bool cameraMoveStarted;                                 //카메라 이동 시작 여부

    public event Action OnCameraMoveStarted;                        //카메라가 다음 방으로 이동하기 시작하는 순간 알림

    private void Awake()
    {
        transitionCollider = GetComponent<BoxCollider2D>();                     //현재 통로 Trigger
        playerBodyCollider = playerRigidbody.GetComponent<CapsuleCollider2D>(); //Player 충돌 영역

        if (transitionCollider != null && playerBodyCollider != null) return;

        Debug.LogError("RoomTransitionTrigger: 통로 또는 Player Collider를 찾을 수 없습니다.", this);
        enabled = false;
    }

    //*플레이어가 방 전환 영역에 처음 진입했을 때*
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStartRoomTransition(other);
    }

    //*문이 열리기 전에 이미 영역에 진입한 경우도 다시 확인*
    private void OnTriggerStay2D(Collider2D other)
    {
        TryStartRoomTransition(other);
    }

    //*전환 가능 조건을 확인하고 다음 방 이동 시작*
    private void TryStartRoomTransition(Collider2D other)
    {
        if (transitionStarted) return;

        //이미 진행 중이거나 시작된 방으로 다시 이동하지 않음
        if (nextRoom == null || nextRoom.IsStarted) return;

        //양쪽 문이 완전히 열릴 때까지 전환 대기
        if (previousExitDoor == null || nextEntryDoor == null) return;
        if (!previousExitDoor.IsOpen || !nextEntryDoor.IsOpen) return;

        //Player 본인 또는 자식 Collider인지 확인
        PlayerControlLock enteredControlLock = other.GetComponentInParent<PlayerControlLock>();

        if (enteredControlLock == null) return;
        if (enteredControlLock != controlLock) return;

        StartRoomTransition();
    }

    //*방 전환 중 플레이어 자동 이동*
    private void FixedUpdate()
    {
        if (!isPlayerAutoMoving) return;

        //플레이어 머리가 통로 위쪽 벽보다 높으면 내려올 때까지 대기
        float passageTopY = transitionCollider.bounds.max.y;

        if (playerBodyCollider.bounds.max.y > passageTopY - 0.05f)
        {
            //좌우 이동만 멈추고 중력에 의한 낙하는 유지
            playerRigidbody.linearVelocity = new Vector2(0f, playerRigidbody.linearVelocity.y);

            return;
        }

        //카메라가 실제로 다음 방으로 이동하는 순간
        if (!cameraMoveStarted)
        {
            cameraMoveStarted = true;

            roomCamera.MoveToRoom(nextRoomCameraPoint);

            //카메라 이동 시작 이벤트 발생
            OnCameraMoveStarted?.Invoke();
        }

        float remainingDistance = playerTargetX - playerRigidbody.position.x;
        float direction = Mathf.Sign(remainingDistance);

        //이번 물리 프레임에서 이동할 거리
        float moveStep = playerAutoMoveSpeed * Time.fixedDeltaTime;

        //목표 지점에 거의 도착했다면 정확한 위치에서 정지
        if (Mathf.Abs(remainingDistance) <= moveStep)
        {
            playerRigidbody.position = new Vector2(playerTargetX, playerRigidbody.position.y);

            playerRigidbody.linearVelocity = Vector2.zero;

            isPlayerAutoMoving = false;
            playerMoveCompleted = true;

            TryCloseDoors();
            return;
        }

        //중력에 의한 수직 이동을 유지하면서 다음 방으로 이동
        playerRigidbody.linearVelocity = new Vector2(direction * playerAutoMoveSpeed, playerRigidbody.linearVelocity.y);
    }

    //*다음 방 전환 시작*
    private void StartRoomTransition()
    {
        if (transitionStarted) return;

        transitionStarted = true;

        //전환 Lock을 먼저 걸어둠
        //이후 공격이 자기 Lock을 해제해도 플레이어 조작이 다시 켜지지 않음
        LockTransitionControls();

        //공격이동으로 Trigger에 들어왔다면 즉시 공격 종료
        playerAttack.InterruptAttack();

        //기존 이동 속도가 다음 방 전환에 영향을 주지 않도록 정지
        playerRigidbody.linearVelocity = Vector2.zero;
        playerRigidbody.angularVelocity = 0f;

        //자동 이동 중에도 중력을 유지해 공중에서 문에 끼이지 않도록 처리
        originalGravityScale = playerRigidbody.gravityScale;
        playerPhysicsOverridden = true;

        cameraMoveCompleted = false;
        playerMoveCompleted = false;
        cameraMoveStarted = false;                      //실제 통로 진입 전까지 카메라 이동 대기

        //카메라 이동 완료 이벤트 대기
        roomCamera.OnMoveCompleted += HandleCameraMoveCompleted;

        //다음 방 안쪽 목표 X 저장
        playerTargetX = playerEntryPoint.position.x;

        //현재 위치부터 목표까지 거리 계산
        float moveDistance = Mathf.Abs(playerTargetX - playerRigidbody.position.x);

        //설정한 시간 안에 도착하도록 실제 이동 속도 계산
        if (playerMoveDuration > 0f)
        {
            playerAutoMoveSpeed =
                moveDistance / playerMoveDuration;
        }
        else
        {
            playerAutoMoveSpeed = moveDistance;
        }

        //Rigidbody 속도를 이용한 자동 이동 시작
        isPlayerAutoMoving = true;
    }

    //*카메라가 다음 방 중심에 도착했을 때*
    private void HandleCameraMoveCompleted()
    {
        roomCamera.OnMoveCompleted -= HandleCameraMoveCompleted;

        cameraMoveCompleted = true;

        TryCloseDoors();
    }

    //*플레이어와 카메라가 모두 다음 방에 도착했을 때만 문 닫기*
    private void TryCloseDoors()
    {
        if (!cameraMoveCompleted) return;
        if (!playerMoveCompleted) return;

        closedDoorCount = 0;

        previousExitDoor.OnClosed += HandleDoorClosed;
        nextEntryDoor.OnClosed += HandleDoorClosed;

        //이전 방 출구 + 다음 방 입구 동시 닫기
        previousExitDoor.CloseDoor();
        nextEntryDoor.CloseDoor();
    }

    //*문 하나가 완전히 닫혔을 때*
    private void HandleDoorClosed()
    {
        closedDoorCount++;

        if (closedDoorCount < 2) return;

        previousExitDoor.OnClosed -= HandleDoorClosed;
        nextEntryDoor.OnClosed -= HandleDoorClosed;

        RestorePlayerPhysics();

        //양쪽 문이 완전히 닫혔으므로 다음 방 시작 및 체크포인트 저장
        if (nextRoom != null)
        {
            nextRoom.StartRoom();
            nextRoom.SaveEntryCheckpoint();                 //진입 완료한 Room의 위치를 저장
        }

        //방이 시작된 뒤 플레이어 조작 복구
        ReleaseTransitionControls();
    }

    //*방 전환 동안 플레이어 전체 조작 잠금*
    private void LockTransitionControls()
    {
        if (controlsLocked) return;

        controlsLocked = true;

        controlLock.LockMovement();
        controlLock.LockJump();
        controlLock.LockAttack();
        controlLock.LockThrow();
        controlLock.LockDetonate();
    }

    //*방 전환 완료 후 이 Trigger가 건 Lock만 해제*
    private void ReleaseTransitionControls()
    {
        if (!controlsLocked) return;

        controlsLocked = false;

        controlLock.UnlockMovement();
        controlLock.UnlockJump();
        controlLock.UnlockAttack();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();
    }

    //*전환 중 변경했던 플레이어 물리 상태 복구*
    private void RestorePlayerPhysics()
    {
        if (!playerPhysicsOverridden) return;

        playerPhysicsOverridden = false;

        playerRigidbody.gravityScale = originalGravityScale;
        playerRigidbody.linearVelocity = Vector2.zero;
    }

    private void OnDisable()
    {
        if (roomCamera != null)
        {
            roomCamera.OnMoveCompleted -= HandleCameraMoveCompleted;
        }

        if (previousExitDoor != null)
        {
            previousExitDoor.OnClosed -= HandleDoorClosed;
        }

        if (nextEntryDoor != null)
        {
            nextEntryDoor.OnClosed -= HandleDoorClosed;
        }

        isPlayerAutoMoving = false;

        RestorePlayerPhysics();
        ReleaseTransitionControls();
    }
}