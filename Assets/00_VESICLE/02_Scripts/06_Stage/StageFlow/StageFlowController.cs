//**스테이지 진행 및 체크포인트 복구**
//책임: 저장된 Room 확인 → 이전 방 클리어 상태 복구 → Player / Camera 체크포인트 배치 → 현재 방 시작
using System;
using UnityEngine;

[Serializable]
public class StageRoomData
{
    public CombatRoom room;                 //해당 방의 CombatRoom
    public Transform cameraPoint;           //해당 방 카메라 중심
    public Transform respawnPoint;          //해당 방 체크포인트 위치
}

public class StageFlowController : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField, Min(0)] private int stageIndex = 0;        //현재 씬이 담당하는 Stage 번호

    [SerializeField] private StageRoomData[] rooms;             //Stage 안의 방 순서

    [Header("Player")]
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Header("Camera")]
    [SerializeField] private RoomCameraController roomCamera;

    private bool shouldRestoreAfterDeath;                       //사망 후 씬 재로드 여부를 기억

    private void Awake()
    {
        //PlayerRespawn.Start()가 복구 플래그를 초기화하기 전에
        //사망으로 인한 Scene Reload인지 먼저 저장
        shouldRestoreAfterDeath = PlayerRespawn.IsRespawnReload;
    }

    private void Start()
    {
        if (!shouldRestoreAfterDeath) return;                   //일반 게임 실행이라면 타이틀 화면을 유지

        RestoreStageProgress();
    }

    //*CONTINUE 버튼에서 저장된 체크포인트 복구 요청*
    public void ContinueFromSave()
    {
        if (SaveManager.Instance == null) return;
        if (!SaveManager.Instance.HasSave) return;

        RestoreStageProgress();
    }

    //*저장된 진행 상태를 현재 Stage에 적용*
    private void RestoreStageProgress()
    {
        //SaveManager가 없으면 복구할 수 없음
        if (SaveManager.Instance == null) return;

        GameSaveData saveData = SaveManager.Instance.CurrentSaveData;

        //저장 데이터가 없으면 현재 Stage 첫 방부터 시작
        if (saveData == null)
        {
            RestoreRoom(0);
            return;
        }

        //다른 Stage의 저장값이면 현재 Stage 첫 방부터 시작
        if (saveData.stageIndex != stageIndex)
        {
            RestoreRoom(0);
            return;
        }

        //저장된 Room 번호가 배열 범위를 벗어나지 않도록 제한
        int checkpointRoomIndex = Mathf.Clamp(saveData.roomIndex, 0, rooms.Length - 1);

        RestoreRoom(checkpointRoomIndex);
    }

    //*특정 Room을 체크포인트 시작 상태로 복구*
    private void RestoreRoom(int checkpointRoomIndex)
    {
        if (rooms == null || rooms.Length == 0) return;

        StageRoomData checkpointRoom = rooms[checkpointRoomIndex];

        //체크포인트 이전 방들은 이미 클리어된 상태로 즉시 복구
        for (int i = 0; i < checkpointRoomIndex; i++)
        {
            if (rooms[i].room == null) continue;

            rooms[i].room.ApplySavedClearedState();
        }

        //Player를 현재 체크포인트 위치로 이동
        if (playerRigidbody != null && checkpointRoom.respawnPoint != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;

            playerRigidbody.position = checkpointRoom.respawnPoint.position;
        }

        //Camera를 현재 체크포인트 방으로 즉시 이동
        if (roomCamera != null &&
            checkpointRoom.cameraPoint != null)
        {
            roomCamera.SetRoom(checkpointRoom.cameraPoint);
        }

        //현재 체크포인트 방은 바로 진행 가능한 상태로 시작
        if (checkpointRoom.room != null)
        {
            checkpointRoom.room.StartRoom();
        }
    }
}