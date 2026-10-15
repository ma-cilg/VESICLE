//**벽잡기 중 투척 불가 피드백**
//책임: 우클릭 실패 시 Player Visual만 짧게 흔들기
using DG.Tweening;
using UnityEngine;

public class PlayerWallGrabFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;

    [Header("Blocked Throw")]
    [SerializeField, Min(0.01f)]
    private float shakeDuration = 0.12f;

    [SerializeField, Min(0f)]
    private float shakeStrength = 0.05f;

    [SerializeField, Min(1)]
    private int shakeVibrato = 14;

    private Tween shakeTween;
    private Vector3 originalLocalPosition;

    private void Awake()
    {
        if (visualRoot != null)
            originalLocalPosition = visualRoot.localPosition;
    }

    //*벽에서 우클릭했지만 투척할 수 없을 때*
    public void PlayBlockedThrow()
    {
        if (visualRoot == null) return;

        //연속 입력 시 이전 흔들림을 정리
        shakeTween?.Kill();
        visualRoot.localPosition = originalLocalPosition;

        //좌우로 짧고 빠르게 떨림
        shakeTween = visualRoot.DOShakePosition(shakeDuration, new Vector3(shakeStrength, 0f, 0f), shakeVibrato, 90f, false, true).OnComplete(() =>
        {
            visualRoot.localPosition = originalLocalPosition;
            shakeTween = null;
        });
    }

    private void OnDisable()
    {
        shakeTween?.Kill();
        shakeTween = null;

        if (visualRoot != null)
            visualRoot.localPosition = originalLocalPosition;
    }
}
