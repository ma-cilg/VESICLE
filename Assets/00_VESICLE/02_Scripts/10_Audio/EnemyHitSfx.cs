//**적 피격 효과음**
//책임: 실제 피격 이벤트 감지 → 검 타격 SFX 재생
using UnityEngine;

[RequireComponent(typeof(EnemyHitReceiver))]
public class EnemyHitSfx : MonoBehaviour
{
    [Header("Melee Hit SFX")]
    [SerializeField] private AudioClip meleeHitSfx;                         //검 적중 효과음
    [SerializeField, Range(0f, 1f)] private float meleeHitVolume = 0.7f;    //개별 타격음 볼륨

    private EnemyHitReceiver hitReceiver;

    private void Awake()
    {
        //같은 적 오브젝트의 피격 이벤트 컴포넌트 가져오기
        hitReceiver = GetComponent<EnemyHitReceiver>();
    }

    //*피격 이벤트 구독*
    private void OnEnable()
    {
        hitReceiver.OnHitReceived += HandleHitReceived;
    }

    //*피격 이벤트 구독 해제*
    private void OnDisable()
    {
        hitReceiver.OnHitReceived -= HandleHitReceived;
    }

    //*적이 실제 공격을 받았을 때*
    private void HandleHitReceived(EnemyHitInfo hitInfo)
    {
        //이번 단계에서는 좌클릭 이동 공격만 처리
        if (hitInfo.HitType != EnemyHitType.Melee) return;

        //SFX가 설정되지 않았다면 재생하지 않음
        if (meleeHitSfx == null) return;

        //기존 오디오 관리자를 통해 타격음 재생
        GameAudioController.Instance?.PlaySfx(meleeHitSfx, meleeHitVolume);
    }
}
