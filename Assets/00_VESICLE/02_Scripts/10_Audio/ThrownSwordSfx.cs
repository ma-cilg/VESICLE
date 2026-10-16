//**투척검 효과음 관리**
//책임: 검 발사 / 환경에 박힘 / Trigger 적에 박힘 SFX 재생
using UnityEngine;

[RequireComponent(typeof(ThrownSwordProjectile))]
public class ThrownSwordSfx : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip throwSfx;
    [SerializeField] private AudioClip environmentEmbedSfx;
    [SerializeField] private AudioClip triggerEmbedSfx;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float throwVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float environmentEmbedVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float triggerEmbedVolume = 0.7f;

    private ThrownSwordProjectile projectile;

    private void Awake()
    {
        projectile = GetComponent<ThrownSwordProjectile>();
    }

    //*투척검 이벤트 구독*
    private void OnEnable()
    {
        projectile.OnLaunched += PlayThrowSfx;
        projectile.OnEnvironmentEmbedded += PlayEnvironmentEmbedSfx;
        projectile.OnTriggerEmbedded += PlayTriggerEmbedSfx;
    }

    //*투척검 이벤트 구독 해제*
    private void OnDisable()
    {
        if (projectile == null) return;

        projectile.OnLaunched -= PlayThrowSfx;
        projectile.OnEnvironmentEmbedded -= PlayEnvironmentEmbedSfx;
        projectile.OnTriggerEmbedded -= PlayTriggerEmbedSfx;
    }

    //*검 발사 효과음*
    private void PlayThrowSfx()
    {
        PlaySfx(throwSfx, throwVolume);
    }

    //*벽·바닥·일반 적에 박히는 효과음*
    private void PlayEnvironmentEmbedSfx()
    {
        PlaySfx(environmentEmbedSfx, environmentEmbedVolume);
    }

    //*Trigger 적에게 박히는 효과음*
    private void PlayTriggerEmbedSfx()
    {
        PlaySfx(triggerEmbedSfx, triggerEmbedVolume);
    }

    //*공통 SFX 재생*
    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null) return;

        GameAudioController.Instance?.PlaySfx(clip, volume);
    }
}
