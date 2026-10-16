//**방 출입문 효과음**
//책임: 문 열림 시작 / 닫힘 완료 이벤트 감지 → SFX 재생
using UnityEngine;

[RequireComponent(typeof(RoomDoor))]
public class RoomDoorSfx : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip openSfx;  //문 열림 효과음
    [SerializeField] private AudioClip closeSfx; //문 닫힘 충격음

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float openVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float closeVolume = 0.7f;

    private RoomDoor roomDoor;

    private void Awake()
    {
        roomDoor = GetComponent<RoomDoor>();
    }

    //*문 이벤트 구독*
    private void OnEnable()
    {
        roomDoor.OnOpening += PlayOpenSfx;
        roomDoor.OnClosed += PlayCloseSfx;
    }

    //*문 이벤트 구독 해제*
    private void OnDisable()
    {
        if (roomDoor == null) return;

        roomDoor.OnOpening -= PlayOpenSfx;
        roomDoor.OnClosed -= PlayCloseSfx;
    }

    //*문이 진동하며 열리기 시작하는 순간*
    private void PlayOpenSfx()
    {
        PlaySfx(openSfx, openVolume);
    }

    //*문이 완전히 닫혀 충격이 발생하는 순간*
    private void PlayCloseSfx()
    {
        PlaySfx(closeSfx, closeVolume);
    }

    //*공통 SFX 재생*
    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null) return;

        GameAudioController.Instance?.PlaySfx(clip, volume);
    }
}
