//**방 기준 카메라 위치 제어**
//책임: 현재 방에 카메라 고정 → 다음 방으로 이동
using System;
using DG.Tweening;
using UnityEngine;

public class RoomCameraController : MonoBehaviour
{
    [SerializeField] private Transform currentRoomPoint;            //현재 방의 카메라 기준 위치

    [Header("Room Transition")]
    [SerializeField, Min(0f)] private float moveDuration = 0.65f;   //다음 방까지 이동하는 시간

    private float fixedZ;                                           //CameraRig의 기존 Z 위치
    private Tween moveTween;                                        //현재 진행 중인 카메라 이동 Tween

    public bool IsMoving { get; private set; }

    public event Action OnMoveCompleted;                             //다음 방 도착 완료 알림

    private void Awake()
    {
        fixedZ = transform.position.z;
    }

    private void Start()
    {
        SnapToCurrentRoom();
    }

    //*현재 방 CameraPoint로 즉시 이동*
    public void SnapToCurrentRoom()
    {
        if (currentRoomPoint == null) return;

        moveTween?.Kill();
        moveTween = null;

        IsMoving = false;

        transform.position = new Vector3(currentRoomPoint.position.x, currentRoomPoint.position.y, fixedZ);
    }

    //*현재 방을 변경하고 해당 CameraPoint로 즉시 이동*
    public void SetRoom(Transform roomCameraPoint)
    {
        if (roomCameraPoint == null) return;

        currentRoomPoint = roomCameraPoint;                     //새로운 방의 CameraPoint를 현재 기준점으로 저장

        SnapToCurrentRoom();                                    //체크포인트 복구이므로 이동 연출 없이 즉시 해당 방으로 이동
    }

    //*다음 방 CameraPoint로 부드럽게 이동*
    public void MoveToRoom(Transform roomCameraPoint)
    {
        if (roomCameraPoint == null) return;
        if (IsMoving) return;

        currentRoomPoint = roomCameraPoint;
        IsMoving = true;

        moveTween?.Kill();

        Vector3 targetPosition = new Vector3(currentRoomPoint.position.x, currentRoomPoint.position.y, fixedZ);

        //현재 방에서 다음 방으로 이동
        moveTween = transform
            .DOMove(targetPosition, moveDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                transform.position = targetPosition;

                IsMoving = false;
                moveTween = null;

                OnMoveCompleted?.Invoke();
            });
    }

    private void OnDisable()
    {
        moveTween?.Kill();
        moveTween = null;

        IsMoving = false;
    }
}