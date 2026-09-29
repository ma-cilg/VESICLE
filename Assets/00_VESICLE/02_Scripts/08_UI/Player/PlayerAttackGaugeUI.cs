//**플레이어 공격 게이지 UI 표시 처리**
//책임: 공격 게이지 비율 표시 → 사용 시 표시 → 완전 회복 시 Fade Out → 사용 실패 시 흔들림
using DG.Tweening;  //DOTween
using UnityEngine;

public class PlayerAttackGaugeUI : MonoBehaviour
{
    [SerializeField] private PlayerAttackGauge attackGauge;     //공격 게이지 관리
    [SerializeField] private RectTransform fillRect;            //실제 게이지 Fill
    [SerializeField] private CanvasGroup canvasGroup;           //게이지 전체 Alpha 조절
    [SerializeField] private RectTransform barRoot;             //실패 시 흔들 게이지 전체 Root

    [Header("Fade")]
    [SerializeField, Min(0f)]
    private float fadeDuration = 0.5f;                          //완충 후 사라지는 시간

    [Header("Fail Shake")]
    [SerializeField, Min(0f)]
    private float shakeDuration = 0.12f;                        //공격 실패 시 흔들리는 시간

    [SerializeField, Min(0f)]
    private float shakeStrength = 0.6f;                         //공격 실패 시 흔들림 강도

    private float fullWidth;                                    //100%일 때 Fill Width
    private Vector2 originalAnchoredPosition;                   //게이지 원래 위치

    private Tween fadeTween;                                    //현재 Fade Tween
    private Tween shakeTween;                                   //현재 Shake Tween

    private void Awake()
    {
        fullWidth = fillRect.rect.width;                        //세로로 회전하기 전 기준 Width를 저장
        originalAnchoredPosition = barRoot.anchoredPosition;    //흔들림 후 정확히 복귀할 위치 저장
    }

    private void OnEnable()
    {
        attackGauge.OnGaugeChanged += HandleGaugeChanged;       //게이지 값이 변할 때
        attackGauge.OnAttackFailed += HandleAttackFailed;       //게이지 부족으로 공격에 실패했을 때
    }

    private void Start()
    {
        //게임 시작 시 현재 게이지에 맞춰 Fill 갱신
        UpdateFill(attackGauge.CurrentGauge, attackGauge.MaxGauge);

        //처음에는 100%이므로 숨김
        if (attackGauge.CurrentGauge >= attackGauge.MaxGauge)
        {
            HideImmediately();
        }
        else
        {
            ShowImmediately();
        }
    }

    private void OnDisable()
    {
        //이벤트 구독 해제
        attackGauge.OnGaugeChanged -= HandleGaugeChanged;
        attackGauge.OnAttackFailed -= HandleAttackFailed;

        //실행 중인 Tween 정리
        fadeTween?.Kill();
        fadeTween = null;

        shakeTween?.Kill();
        shakeTween = null;

        //원래 위치 복구
        if (barRoot != null)
        {
            barRoot.anchoredPosition = originalAnchoredPosition;
        }
    }

    //*공격 게이지 변경*
    private void HandleGaugeChanged(float currentGauge, float maxGauge)
    {
        UpdateFill(currentGauge, maxGauge);         //현재 값에 맞게 Fill 길이 변경
        if (currentGauge < maxGauge)                //100%가 아니라면 계속 표시
        {
            ShowImmediately();
            return;
        }
        FadeOut();                                  //100%까지 회복되면 천천히 숨김
    }

    //*게이지 부족으로 공격 실패*
    private void HandleAttackFailed()
    {
        ShowImmediately();                          //사용 불가능하다는 걸 보이기 위해 게이지 표시
        PlayFailShake();                            //게이지를 짧게 흔들어 피드백
    }

    //*현재 공격 게이지 비율에 맞춰 Fill 길이 변경*
    private void UpdateFill(float currentGauge, float maxGauge)
    {
        if (maxGauge <= 0f)
        {
            SetFillWidth(0f);
            return;
        }

        float gaugeRatio = currentGauge / maxGauge;
        gaugeRatio = Mathf.Clamp01(gaugeRatio);

        float targetWidth = fullWidth * gaugeRatio;

        SetFillWidth(targetWidth);
    }

    //*Fill 실제 가로 길이 변경*
    private void SetFillWidth(float width)
    {
        //Bar 자체를 90도 돌렸기 때문에 Width 변화가 화면에서는 세로 게이지 변화로 보임
        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    //*공격 게이지 즉시 표시*
    private void ShowImmediately()
    {
        fadeTween?.Kill();
        fadeTween = null;

        canvasGroup.alpha = 1f;
    }

    //*완충 후 공격 게이지 Fade Out*
    private void FadeOut()
    {
        fadeTween?.Kill();

        fadeTween = canvasGroup
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                fadeTween = null;
            });
    }

    //*공격 불가능할 때 게이지 흔들림*
    private void PlayFailShake()
    {
        //기존 흔들림이 있다면 제거
        shakeTween?.Kill();

        //Tween 중간 위치가 남지 않도록 원위치 복구
        barRoot.anchoredPosition = originalAnchoredPosition;

        shakeTween = barRoot.DOShakeAnchorPos(shakeDuration, shakeStrength, 12, 90f, false, true)
            .OnComplete(() =>
            {
                //항상 정확한 원위치로 복구
                barRoot.anchoredPosition = originalAnchoredPosition;
                shakeTween = null;
            });
    }

    //*공격 게이지 즉시 숨기기*
    private void HideImmediately()
    {
        fadeTween?.Kill();
        fadeTween = null;

        canvasGroup.alpha = 0f;
    }
}
