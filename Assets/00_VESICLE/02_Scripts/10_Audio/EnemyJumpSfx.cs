//**일반몹 점프 효과음**
//책임: 실제 점프 발생 이벤트 감지 → SFX 재생
using UnityEngine;

[RequireComponent(typeof(EnemyJump))]
public class EnemyJumpSfx : MonoBehaviour
{
    [Header("Jump SFX")]
    [SerializeField] private AudioClip jumpSfx;
    [SerializeField, Range(0f, 1f)] private float jumpVolume = 0.2f;

    private EnemyJump enemyJump;

    private void Awake()
    {
        //같은 적 오브젝트의 EnemyJump 참조
        enemyJump = GetComponent<EnemyJump>();
    }

    //*점프 이벤트 구독*
    private void OnEnable()
    {
        enemyJump.OnJumped += PlayJumpSfx;
    }

    //*점프 이벤트 구독 해제*
    private void OnDisable()
    {
        if (enemyJump == null) return;

        enemyJump.OnJumped -= PlayJumpSfx;
    }

    //*실제 점프가 발생했을 때*
    private void PlayJumpSfx()
    {
        if (jumpSfx == null) return;

        GameAudioController.Instance?.PlaySfx(jumpSfx, jumpVolume);
    }
}
