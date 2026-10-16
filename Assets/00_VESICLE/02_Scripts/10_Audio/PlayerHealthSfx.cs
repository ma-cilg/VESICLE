//**플레이어 피격 및 사망 효과음**
//책임: 실제 데미지 / 사망 이벤트 감지 → SFX 재생
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerHealthSfx : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip hitSfx;   //일반 피격음
    [SerializeField] private AudioClip deathSfx; //사망 효과음

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float hitVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 0.9f;

    private PlayerHealth playerHealth;

    private void Awake()
    {
        //같은 Player 오브젝트의 체력 컴포넌트 참조
        playerHealth = GetComponent<PlayerHealth>();
    }

    //*체력 관련 이벤트 구독*
    private void OnEnable()
    {
        playerHealth.OnDamaged += PlayHitSfx;
        playerHealth.OnDied += PlayDeathSfx;
    }

    //*이벤트 구독 해제*
    private void OnDisable()
    {
        if (playerHealth == null) return;

        playerHealth.OnDamaged -= PlayHitSfx;
        playerHealth.OnDied -= PlayDeathSfx;
    }

    //*실제 데미지를 받았을 때*
    private void PlayHitSfx(float damage)
    {
        if (hitSfx == null) return;

        //마지막 피격으로 체력이 0이 됐다면
        //일반 피격음을 생략하고 사망음만 재생
        if (playerHealth.CurrentHealth <= 0f) return;

        GameAudioController.Instance?.PlaySfx(hitSfx, hitVolume);
    }

    //*플레이어 사망 시*
    private void PlayDeathSfx()
    {
        if (deathSfx == null) return;

        GameAudioController.Instance?.PlaySfx(deathSfx, deathVolume);
    }
}
