//**플레이어 부활 시 시각 피드백 처리**
//책임: Respawn 완료 감지 → Player Sprite 깜빡임 → 원래 Alpha 복구
using DG.Tweening;
using UnityEngine;

public class PlayerRespawnFeedback : MonoBehaviour
{
    [SerializeField] private PlayerRespawn playerRespawn;       //부활 완료 이벤트 확인
    [SerializeField] private SpriteRenderer playerSprite;       //깜빡임을 적용할 Player Sprite

    [Header("Blink")]
    [SerializeField, Range(0f, 1f)]
    private float blinkAlpha = 0.25f;                           //깜빡일 때 투명도

    [SerializeField, Min(0f)]
    private float blinkDuration = 0.08f;                        //한 번 흐려지거나 돌아오는 시간

    [SerializeField, Min(1)]
    private int blinkCount = 3;                                 //깜빡이는 횟수

    private Sequence blinkSequence;
    private float originalAlpha;                                //원래 Sprite Alpha

    private void Awake()
    {
        originalAlpha = playerSprite.color.a;
    }

    private void OnEnable()
    {
        playerRespawn.OnRespawned += PlayRespawnFeedback;
    }

    private void OnDisable()
    {
        playerRespawn.OnRespawned -= PlayRespawnFeedback;

        blinkSequence?.Kill();
        blinkSequence = null;

        RestoreAlpha();
    }

    //*부활 깜빡임 시작*
    private void PlayRespawnFeedback()
    {
        blinkSequence?.Kill();
        blinkSequence = null;

        RestoreAlpha();

        blinkSequence = DOTween.Sequence();

        for (int i = 0; i < blinkCount; i++)
        {
            blinkSequence.Append(
                playerSprite.DOFade(blinkAlpha, blinkDuration)
            );

            blinkSequence.Append(
                playerSprite.DOFade(originalAlpha, blinkDuration)
            );
        }

        blinkSequence.OnComplete(() =>
        {
            RestoreAlpha();
            blinkSequence = null;
        });
    }

    //*Player Sprite Alpha 원상복구*
    private void RestoreAlpha()
    {
        if (playerSprite == null) return;

        Color color = playerSprite.color;
        color.a = originalAlpha;
        playerSprite.color = color;
    }
}
