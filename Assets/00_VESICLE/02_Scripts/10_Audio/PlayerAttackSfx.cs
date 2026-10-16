//**플레이어 이동 공격 효과음**
//책임: 실제 Slash 시작 이벤트 감지 → 검 공격 SFX 재생
using UnityEngine;

[RequireComponent(typeof(PlayerAttack))]
public class PlayerAttackSfx : MonoBehaviour
{
    [Header("SFX")]
    [SerializeField] private AudioClip slashSfx;                        //좌클릭 검 공격음
    [SerializeField, Range(0f, 1f)] private float slashVolume = 0.6f;   //효과음 개별 볼륨

    private PlayerAttack playerAttack;

    private void Awake()
    {
        //같은 Player에 붙은 공격 컴포넌트 참조
        playerAttack = GetComponent<PlayerAttack>();
    }

    //*공격 시작 이벤트 구독*
    private void OnEnable()
    {
        playerAttack.OnSlashStarted += HandleSlashStarted;
    }

    //*이벤트 구독 해제*
    private void OnDisable()
    {
        playerAttack.OnSlashStarted -= HandleSlashStarted;
    }

    //*실제 이동 공격이 시작된 순간*
    private void HandleSlashStarted(float attackDistance)
    {
        if (slashSfx == null) return;

        //기존 오디오 관리자를 통해 효과음 재생
        GameAudioController.Instance?.PlaySfx(slashSfx, slashVolume);
    }
}
