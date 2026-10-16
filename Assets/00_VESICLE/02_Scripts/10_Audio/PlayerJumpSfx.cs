//**플레이어 점프 및 착지 효과음**
//책임: 점프 이벤트 감지 → 해당 SFX 재생
using UnityEngine;

[RequireComponent(typeof(PlayerJump))]
public class PlayerJumpSfx : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip jumpSfx;         //1단 점프
    [SerializeField] private AudioClip doubleJumpSfx;   //2단 점프
    [SerializeField] private AudioClip landSfx;         //착지

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float jumpVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float doubleJumpVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float landVolume = 0.4f;

    private PlayerJump playerJump;

    private void Awake()
    {
        //같은 Player에 있는 점프 컴포넌트 참조
        playerJump = GetComponent<PlayerJump>();
    }

    //*점프 및 착지 이벤트 구독*
    private void OnEnable()
    {
        playerJump.OnJumped += PlayJumpSfx;
        playerJump.OnDoubleJumped += PlayDoubleJumpSfx;
        playerJump.OnLanded += PlayLandSfx;
    }

    //*이벤트 구독 해제*
    private void OnDisable()
    {
        if (playerJump == null) return;

        playerJump.OnJumped -= PlayJumpSfx;
        playerJump.OnDoubleJumped -= PlayDoubleJumpSfx;
        playerJump.OnLanded -= PlayLandSfx;
    }

    //*1단 점프 효과음*
    private void PlayJumpSfx()
    {
        PlaySfx(jumpSfx, jumpVolume);
    }

    //*2단 점프 효과음*
    private void PlayDoubleJumpSfx()
    {
        PlaySfx(doubleJumpSfx, doubleJumpVolume);
    }

    //*착지 효과음*
    private void PlayLandSfx()
    {
        PlaySfx(landSfx, landVolume);
    }

    //*공통 효과음 재생*
    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null) return;

        GameAudioController.Instance?.PlaySfx(clip, volume);
    }
}
