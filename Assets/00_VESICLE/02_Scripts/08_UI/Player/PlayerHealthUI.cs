//**플레이어 머리 위 체력 UI 표시 처리**
//책임: 체력 비율에 맞춰 Fill 길이 변경 → 피격 시 표시 → 완전 회복 시 Fade Out
using DG.Tweening;  //DOTween
using UnityEngine;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;                 //현재 HP와 최대 HP를 관리하는 컴포넌트(이벤트로 전달받음)
    [SerializeField] private RectTransform fillRect;                    //화면에 보이는 빨간색 체력 Fill의 RectTransform
    [SerializeField] private CanvasGroup canvasGroup;                   //캔버스 그룹(HP바 알파값 조절용)
    [SerializeField, Min(0f)] private float fadeDuration = 0.5f;        //풀피시 HP바가 사라지는 데 걸리는 시간

    private float fullWidth;                                            //체력이 100%일 때 Fill
    private Tween fadeTween;                                            //현재 실행 중인 Fade Tween을 저장

    private void Awake()
    {
        fullWidth = fillRect.rect.width;                                //게임 시작 시 Fill의 현재 Width를 저장
    }

    private void OnEnable()
    {
        playerHealth.OnDamaged += HandleDamaged;                        //피격시 이벤트
        playerHealth.OnHealthChanged += HandleHealthChanged;            //HP감소 또는 회복 때마다 이벤트
    }

    private void Start()
    {
        UpdateFill(playerHealth.CurrentHealth, playerHealth.MaxHealth); //겜 시작시 HP 최대로 맞추기

        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            HideImmediately();                                          //이미 최대면 처음부터 안보이게
        }
        else
        {
            ShowImmediately();                                          //아닌경우 HP바 보여주기
        }
    }

    private void OnDisable()
    {
        //OnEnable에서 등록한 이벤트들 해제
        playerHealth.OnDamaged -= HandleDamaged;
        playerHealth.OnHealthChanged -= HandleHealthChanged;

        fadeTween?.Kill();              //Fade가 실행 중이었으면 중단
        fadeTween = null;               //기존 Tween 참조도 제거
    }

    //*실제로 데미지를 받았을 때 호출*
    private void HandleDamaged(float damage)
    {
        ShowImmediately();
    }

    //*현재 체력이 변경될 때마다 호출*
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        UpdateFill(currentHealth, maxHealth);   //현재 HP에 맞춰 Fill 길이 갱신

        if (currentHealth < maxHealth)          //풀피 아니면
        {
            ShowImmediately();                  //HP바를 계속 완전히 보이게 유지
            return;                             //아직 풀피 아니면 Fade Out 실행 X
        }
        FadeOut();
    }

    //*현재 체력 비율에 맞춰 Fill의 Width 변경*
    private void UpdateFill(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0f)    //예외처리
        {
            SetFillWidth(0f);
            return;
        }

        float healthRatio = currentHealth / maxHealth;  //현재 체력 비율 계산
        healthRatio = Mathf.Clamp01(healthRatio);       //0 ~ 1 사이로 제한
        float targetWidth = fullWidth * healthRatio;    //풀피 Width × 현재 체력 비율
        SetFillWidth(targetWidth);                      //계산된 Width를 실제 Fill에 적용
    }

    //*Fill의 실제 가로 길이 변경*
    private void SetFillWidth(float width)
    {
        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    //*HP바 즉시 표시*
    private void ShowImmediately()
    {
        fadeTween?.Kill();                              //현재 Fade Out이 진행 중이었다면 즉시 중단
        fadeTween = null;                               //기존 Tween 참조 제거
        canvasGroup.alpha = 1f;                         //알파값 1
    }

    //*HP바 서서히 숨기기*
    private void FadeOut()
    {
        fadeTween?.Kill();                              //혹시 이전 Fade Tween이 남아있다면 먼저 제거

        fadeTween = canvasGroup
            .DOFade(0f, fadeDuration)                   //Alpha 0까지 서서히 감소
            .SetEase(Ease.OutQuad)                      //끝부분으로 갈수록 자연스럽게 느려지는 Ease
            .OnComplete(() =>                           //Fade가 완전히 끝나면 호출
            {
                fadeTween = null;                       //참조 제거
            });
    }

    //*HP바 즉시 숨기기*
    private void HideImmediately()
    {
        fadeTween?.Kill();                              //실행 중인 Fade가 있다면 제거
        fadeTween = null;                               //Tween 참조 제거
        canvasGroup.alpha = 0f;                         //Alpha 0
    }
}
