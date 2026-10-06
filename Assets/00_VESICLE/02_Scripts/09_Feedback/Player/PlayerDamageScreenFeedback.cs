//**플레이어 화면 피격 피드백**
//책임: 플레이어 피격 감지 → 화면 전체 붉은 Overlay 표시 → 자연스럽게 투명 복귀
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDamageScreenFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;                 //실제 데미지 발생 확인
    [SerializeField] private Image damageOverlay;                       //화면 전체를 덮는 붉은 Image

    [Header("Feedback")]
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.16f;     //피격 순간 최대 투명도
    [SerializeField, Min(0f)] private float flashInDuration = 0.035f;   //빠르게 붉어지는 시간
    [SerializeField, Min(0f)] private float holdDuration = 0.025f;      //최대 상태 잠깐 유지
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.20f;    //천천히 사라지는 시간

    private Sequence damageSequence;                                    //현재 재생 중인 화면 피격 Tween

    private void Awake()
    {
        SetAlpha(0f);                                                   //게임 시작 시 Overlay 숨김
    }

    private void OnEnable()
    {
        playerHealth.OnDamaged += PlayDamageFeedback;                   //실제 데미지가 발생하면 연출 시작
    }

    private void OnDisable()
    {
        playerHealth.OnDamaged -= PlayDamageFeedback;

        damageSequence?.Kill();
        damageSequence = null;

        SetAlpha(0f);                                                   //비활성화될 때 화면 원상복구
    }

    //*실제 피격 시 화면 연출*
    private void PlayDamageFeedback(float damage)
    {
        damageSequence?.Kill();                                         //이전 연출 있으면 중단

        SetAlpha(0f);

        damageSequence = DOTween.Sequence();

        //피격 순간 빠르게 붉어짐
        damageSequence.Append(damageOverlay.DOFade(maxAlpha, flashInDuration).SetEase(Ease.OutQuad));

        //최대 상태를 아주 잠깐 유지
        damageSequence.AppendInterval(holdDuration);

        //붉은 화면이 자연스럽게 사라짐
        damageSequence.Append(damageOverlay.DOFade(0f, fadeOutDuration).SetEase(Ease.OutQuad));

        damageSequence.OnComplete(() =>
        {
            damageSequence = null;
        });
    }

    //*RGB는 유지 Alpha만 변경*
    private void SetAlpha(float alpha)
    {
        Color color = damageOverlay.color;
        color.a = alpha;

        damageOverlay.color = color;
    }
}