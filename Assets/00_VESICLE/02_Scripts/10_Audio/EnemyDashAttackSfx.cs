//**Trigger 적의 돌진 공격 효과음**
//책임: Charge / Dash 이벤트 감지 → 해당 SFX 재생
using UnityEngine;

[RequireComponent(typeof(EnemyDashAttack))]
public class EnemyDashAttackSfx : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip chargeSfx;   //돌진 준비
    [SerializeField] private AudioClip dashSfx;     //실제 돌진

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float chargeVolume = 0.3f;
    [SerializeField, Range(0f, 1f)] private float dashVolume = 0.45f;

    private EnemyDashAttack dashAttack;

    private void Awake()
    {
        //같은 적 오브젝트의 공격 제어 컴포넌트 참조
        dashAttack = GetComponent<EnemyDashAttack>();
    }

    //*공격 이벤트 구독*
    private void OnEnable()
    {
        dashAttack.OnChargeStarted += PlayChargeSfx;
        dashAttack.OnDashStarted += PlayDashSfx;
    }

    //*공격 이벤트 구독 해제*
    private void OnDisable()
    {
        if (dashAttack == null) return;

        dashAttack.OnChargeStarted -= PlayChargeSfx;
        dashAttack.OnDashStarted -= PlayDashSfx;
    }

    //*돌진 준비 효과음*
    private void PlayChargeSfx()
    {
        PlaySfx(chargeSfx, chargeVolume);
    }

    //*실제 돌진 효과음*
    private void PlayDashSfx()
    {
        PlaySfx(dashSfx, dashVolume);
    }

    //*공통 효과음 재생*
    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null) return;

        GameAudioController.Instance?.PlaySfx(clip, volume);
    }
}
