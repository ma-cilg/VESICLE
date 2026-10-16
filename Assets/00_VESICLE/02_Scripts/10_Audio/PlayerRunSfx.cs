//**플레이어 달리기 발소리**
//책임: 달리기 애니메이션 발 착지 시점 → 발소리 재생
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerJump))]
public class PlayerRunSfx : MonoBehaviour
{
    [Header("Footstep SFX")]
    [SerializeField] private AudioClip backStepSfx;  //뒤쪽 발 착지
    [SerializeField] private AudioClip frontStepSfx; //앞쪽 발 착지

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.2f;

    [Header("Condition")]
    [SerializeField, Min(0f)] private float minHorizontalSpeed = 0.5f;

    private PlayerMovement playerMovement;
    private PlayerJump playerJump;

    private void Awake()
    {
        //같은 Player의 이동 및 점프 상태 참조
        playerMovement = GetComponent<PlayerMovement>();
        playerJump = GetComponent<PlayerJump>();
    }

    //*뒤쪽 발 착지 소리*
    public void PlayBackStep()
    {
        PlayFootstep(backStepSfx);
    }

    //*앞쪽 발 착지 소리*
    public void PlayFrontStep()
    {
        //두 번째 음원이 없다면 첫 번째 음원을 재사용
        AudioClip clip = frontStepSfx != null
            ? frontStepSfx
            : backStepSfx;

        PlayFootstep(clip);
    }

    //*발소리 재생 가능 여부 확인*
    private void PlayFootstep(AudioClip clip)
    {
        if (clip == null) return;

        //공중에서는 달리기 발소리 재생하지 않음
        if (!playerJump.IsGrounded) return;

        //실제로 움직이지 않으면 재생하지 않음
        if (playerMovement.HorizontalSpeed < minHorizontalSpeed) return;

        //기존 AudioRoot를 통해 SFX 재생
        GameAudioController.Instance?.PlaySfx(clip, footstepVolume);
    }
}
