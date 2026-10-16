//**Trigger 적 방어 효과음**
//책임: 좌클릭 공격 방어 이벤트 감지 → 방어 SFX 재생
using UnityEngine;

[RequireComponent(typeof(EnemyMark))]
public class EnemyTriggerBlockSfx : MonoBehaviour
{
    [Header("Block SFX")]
    [SerializeField] private AudioClip blockSfx;                        //Trigger 방어 효과음
    [SerializeField, Range(0f, 1f)] private float blockVolume = 0.7f;   //개별 방어음 볼륨

    private EnemyMark enemyMark;

    private void Awake()
    {
        //같은 적 오브젝트에 있는 EnemyMark 참조
        enemyMark = GetComponent<EnemyMark>();
    }

    //*방어 이벤트 구독*
    private void OnEnable()
    {
        enemyMark.OnMeleeBlocked += PlayBlockSfx;
    }

    //*방어 이벤트 구독 해제*
    private void OnDisable()
    {
        if (enemyMark == null) return;

        enemyMark.OnMeleeBlocked -= PlayBlockSfx;
    }

    //*좌클릭 이동 공격을 막아낸 순간*
    private void PlayBlockSfx()
    {
        if (blockSfx == null) return;

        //기존 AudioRoot를 통해 SFX 재생
        GameAudioController.Instance?.PlaySfx(blockSfx, blockVolume);
    }
}
