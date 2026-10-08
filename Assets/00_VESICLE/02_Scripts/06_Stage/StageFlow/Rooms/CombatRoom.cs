//**전투 방 상태 및 정화 처리**
//책임: 방 종류에 따른 시작 색상 적용 → 방 클리어 연출 → 출구 문 개방
using System;
using DG.Tweening;
using UnityEngine;

//방의 종류에 따라 정화 전 색상을 구분
public enum CombatRoomType
{
    Normal,     //일반 방: 검붉은색
    Boss        //보스 방: 보라색
}

public class CombatRoom : MonoBehaviour
{
    [Header("Room")]
    [SerializeField] private CombatRoomType roomType = CombatRoomType.Normal;
    [SerializeField] private Transform roomFrame;                                           //방 테두리 전체 부모
    [SerializeField] private SpriteRenderer[] frameRenderers;                               //바닥 / 천장 / 벽 / 문 Sprite

    [SerializeField] private RoomDoor entryDoor;                                            //이전 방에서 들어오는 왼쪽 문
    [SerializeField] private RoomDoor exitDoor;                                             //다음 방으로 나가는 오른쪽 문
    [SerializeField] private CombatRoom nextRoom;                                           //다음으로 이어지는 방

    [Header("Checkpoint")]
    [SerializeField, Min(0)] private int stageIndex;                                        //현재 Stage 번호
    [SerializeField, Min(0)] private int roomIndex;                                         //현재 Room 번호

    [Header("Enemies")]
    [SerializeField] private EnemyDeath[] enemies;                                          //이 방에 배치된 적들

    [Header("Room Colors")]
    [SerializeField] private Color normalRoomColor = new Color32(0xDB, 0x4D, 0x60, 0xFF);   //일반 방 붉은색
    [SerializeField] private Color bossRoomColor = new Color32(0x79, 0x3E, 0xA1, 0xFF);     //보스 방 보라색
    [SerializeField] private Color purifiedRoomColor = new Color32(0x55, 0xD6, 0x8B, 0xFF); //클리어 방 초록색

    [Header("Purification")]
    [SerializeField, Min(1f)] private float purificationScale = 1.04f;                      //클리어 순간 방 전체 확대 크기
    [SerializeField, Min(0f)] private float expandDuration = 0.16f;                         //방이 커지는 시간
    [SerializeField, Min(0f)] private float returnDuration = 0.22f;                         //원래 크기로 돌아오는 시간
    [SerializeField, Min(0f)] private float colorDuration = 0.38f;                          //초록색으로 변하는 시간

    private Vector3 originalFrameScale;
    private Sequence purificationSequence;

    private bool isStarted;
    private bool isCleared;

    private int remainingEnemyCount;                                                        //현재 살아있는 적 수

    public bool IsStarted => isStarted;
    public bool IsCleared => isCleared;

    public event Action OnRoomStarted;                                                      //문이 닫힌 뒤 실제 방 시작 알림
    public event Action OnPurified;                                                         //클리어 연출 완료 알림

    private void Awake()
    {
        originalFrameScale = roomFrame.localScale;

        ApplyInitialRoomColor();
    }

    //*방 종류에 맞는 클리어 전 색상 적용*
    private void ApplyInitialRoomColor()
    {
        Color startColor = roomType == CombatRoomType.Boss ? bossRoomColor : normalRoomColor;

        ApplyFrameColor(startColor);
    }

    //*방 테두리 전체 색상 즉시 변경*
    private void ApplyFrameColor(Color color)
    {
        foreach (SpriteRenderer renderer in frameRenderers)
        {
            if (renderer == null) continue;

            renderer.color = color;
        }
    }

    //*방 클리어 시작*
    [ContextMenu("Test Clear Room")]
    public void ClearRoom()
    {
        if (isCleared) return;

        isCleared = true;

        //다음에 시작할 방을 체크포인트로 저장
        if (SaveManager.Instance != null && nextRoom != null)
        {
            SaveManager.Instance.SaveCheckpoint(stageIndex, roomIndex + 1);
        }

        purificationSequence?.Kill();
        roomFrame.localScale = originalFrameScale;
        purificationSequence = DOTween.Sequence();

        //방 전체가 동시에 살짝 팽창
        purificationSequence.Append(roomFrame.DOScale(originalFrameScale * purificationScale, expandDuration).SetEase(Ease.OutQuad));

        //팽창과 동시에 모든 테두리 초록색으로 변화
        foreach (SpriteRenderer renderer in frameRenderers)
        {
            if (renderer == null) continue;

            purificationSequence.Join(renderer.DOColor(purifiedRoomColor, colorDuration).SetEase(Ease.OutQuad));
        }

        //다시 원래 방 크기로 복귀
        purificationSequence.Append(roomFrame.DOScale(originalFrameScale, returnDuration).SetEase(Ease.InOutSine));

        purificationSequence.OnComplete(() =>
        {
            roomFrame.localScale = originalFrameScale;
            purificationSequence = null;
            OnPurified?.Invoke();

            //현재 방 출구 + 다음 방 입구 동시 개방
            exitDoor?.OpenDoor();
            nextRoom?.OpenEntryDoor();
        });
    }

    //*이전 방이 클리어됐을 때 이 방 입구 개방*
    public void OpenEntryDoor()
    {
        entryDoor?.OpenDoor();
    }

    private void OnDisable()
    {
        UnsubscribeEnemyDeaths();

        purificationSequence?.Kill();
        purificationSequence = null;

        if (roomFrame != null)
        {
            roomFrame.localScale = originalFrameScale;
        }
    }

    //*세이브 불러오기 시 이미 클리어한 방 상태 즉시 복구*
    public void ApplySavedClearedState()
    {
        //진행 중이던 클리어 연출 제거
        purificationSequence?.Kill();
        purificationSequence = null;

        UnsubscribeEnemyDeaths();                       //적 사망 이벤트 구독 정리

        isStarted = true;
        isCleared = true;
        remainingEnemyCount = 0;

        roomFrame.localScale = originalFrameScale;      //방 크기 원상복구

        ApplyFrameColor(purifiedRoomColor);             //연출 없이 바로 클리어 색상 적용

        //이미 클리어한 방의 적은 다시 나타나지 않도록 비활성화
        foreach (EnemyDeath enemy in enemies)
        {
            if (enemy == null) continue;

            enemy.gameObject.SetActive(false);
        }
    }

    //*플레이어 입장 완료 후 방 진행 시작*
    public void StartRoom()
    {
        if (isStarted) return;

        isStarted = true;

        //이번 방에서 실제 사용할 적 수 초기화
        remainingEnemyCount = 0;

        foreach (EnemyDeath enemy in enemies)
        {
            if (enemy == null) continue;
            if (enemy.IsDead) continue;

            remainingEnemyCount++;

            //적이 실제 사망했을 때 방에서 확인
            enemy.OnDied += HandleEnemyDied;
        }

        //문이 닫힌 뒤 실제 방 시작 알림
        OnRoomStarted?.Invoke();
    }

    //*방 안의 적 하나가 사망했을 때*
    private void HandleEnemyDied()
    {
        if (!isStarted) return;
        if (isCleared) return;

        remainingEnemyCount--;

        //아직 살아있는 적이 있으면 방 유지
        if (remainingEnemyCount > 0) return;

        //모든 적이 사망했으므로 구독 정리
        UnsubscribeEnemyDeaths();

        //방 정화 + 다음 통로 개방
        ClearRoom();
    }

    //*등록된 적 사망 이벤트 구독 해제*
    private void UnsubscribeEnemyDeaths()
    {
        foreach (EnemyDeath enemy in enemies)
        {
            if (enemy == null) continue;

            enemy.OnDied -= HandleEnemyDied;
        }
    }
}