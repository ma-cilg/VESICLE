//**Trigger 적 방어 피드백**
//책임: 이동 공격 방어 이벤트 → 순간 확대 + 흰색 Flash → 원래 상태 복구
using DG.Tweening;
using UnityEngine;

public class EnemyTriggerBlockFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyMark enemyMark;                   //Trigger 방어 이벤트 수신
    [SerializeField] private Transform visualRoot;                  //중앙 기준 Scale 연출
    [SerializeField] private SpriteRenderer enemySprite;            //현재 적 Sprite
    [SerializeField] private SpriteRenderer hitSprite;              //피격 Sprite도 동일 색상 유지

    [Header("Scale")]
    [SerializeField, Range(0f, 1f)] private float punchScale = 0.28f;
    [SerializeField, Min(0f)] private float punchDuration = 0.16f;

    [Header("Flash")]
    [SerializeField]
    private Color shieldFlashColor = new Color(0.75f, 0.95f, 1f, 1f);

    [SerializeField, Min(0f)] private float flashDuration = 0.03f;
    [SerializeField, Min(0f)] private float restoreDuration = 0.10f;

    private Vector3 originalScale;

    private Tween scaleTween;
    private Sequence flashSequence;

    private void Awake()
    {
        originalScale = visualRoot.localScale;
    }

    private void OnEnable()
    {
        enemyMark.OnMeleeBlocked += PlayBlockFeedback;
        enemyMark.OnPrimeReset += HandlePrimeReset;
    }

    private void OnDisable()
    {
        enemyMark.OnMeleeBlocked -= PlayBlockFeedback;
        enemyMark.OnPrimeReset -= HandlePrimeReset;

        scaleTween?.Kill();
        flashSequence?.Kill();

        scaleTween = null;
        flashSequence = null;

        visualRoot.localScale = originalScale;
    }

    //*Trigger Prime 해제 시 진행 중인 방어 연출 정리*
    private void HandlePrimeReset()
    {
        scaleTween?.Kill();
        flashSequence?.Kill();

        scaleTween = null;
        flashSequence = null;

        //방어 중 확대된 크기가 남지 않도록 복구
        visualRoot.localScale = originalScale;
    }

    //*공격을 막아낸 순간 연출*
    private void PlayBlockFeedback()
    {
        PlayScaleFeedback();
        PlayFlashFeedback();
    }

    //*몸이 부풀었다가 원래 크기로 돌아옴*
    private void PlayScaleFeedback()
    {
        scaleTween?.Kill();

        visualRoot.localScale = originalScale;

        //현재 크기에서 순간적으로 부풀었다가 원래 크기로 복귀
        Vector3 punchAmount = new Vector3(originalScale.x * punchScale, originalScale.y * punchScale, 0f);

        scaleTween = visualRoot
            .DOPunchScale(punchAmount, punchDuration, 1, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                visualRoot.localScale = originalScale;
                scaleTween = null;
            });
    }

    //*순간적으로 하얗게 빛난 뒤 현재 색으로 복귀*
    private void PlayFlashFeedback()
    {
        flashSequence?.Kill();

        //현재 상태의 색을 저장
        //검 안 박혔다면 보라색, 박혔다면 초록색 저장
        Color enemyOriginalColor = enemySprite.color;
        Color hitOriginalColor = hitSprite.color;

        Color enemyFlash = new Color(shieldFlashColor.r, shieldFlashColor.g, shieldFlashColor.b, enemyOriginalColor.a);
        Color hitFlash = new Color(shieldFlashColor.r, shieldFlashColor.g, shieldFlashColor.b, hitOriginalColor.a);

        flashSequence = DOTween.Sequence();
        flashSequence.Join(enemySprite.DOColor(enemyFlash, flashDuration).SetEase(Ease.OutQuad));
        flashSequence.Join(hitSprite.DOColor(hitFlash, flashDuration).SetEase(Ease.OutQuad));
        flashSequence.Append(enemySprite.DOColor(enemyOriginalColor, restoreDuration).SetEase(Ease.OutQuad));
        flashSequence.Join(hitSprite.DOColor(hitOriginalColor, restoreDuration).SetEase(Ease.OutQuad));
        flashSequence.OnComplete(() =>
        {
            flashSequence = null;
        });
    }
}
