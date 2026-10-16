//**보스방 진입 시 BGM 전환**
//책임: 보스방으로 카메라 이동 시작 감지 → 보스 BGM 전환
using UnityEngine;

public class BossRoomBgmTrigger : MonoBehaviour
{
    [Header("Boss Room Entry")]
    [SerializeField] private RoomTransitionTrigger bossEntryTransition;

    //*카메라 이동 시작 이벤트 구독*
    private void OnEnable()
    {
        if (bossEntryTransition == null)
        {
            Debug.LogError("BossRoomBgmTrigger: 보스방 진입 Trigger가 연결되지 않았습니다.", this);

            enabled = false;
            return;
        }

        bossEntryTransition.OnCameraMoveStarted += HandleCameraMoveStarted;
    }

    //*이벤트 구독 해제*
    private void OnDisable()
    {
        if (bossEntryTransition != null)
        {
            bossEntryTransition.OnCameraMoveStarted -= HandleCameraMoveStarted;
        }
    }

    //*보스방으로 카메라 이동 시작*
    private void HandleCameraMoveStarted()
    {
        //일반 BGM Fade Out → 보스 BGM Fade In
        GameAudioController.Instance?.PlayBossBgm();
    }
}
