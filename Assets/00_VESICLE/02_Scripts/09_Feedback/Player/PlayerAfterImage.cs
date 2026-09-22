//**플레이어 대시 잔상 재생과 Pool 반환 처리**
//책임: 현재 Sprite 표시 → DOTween으로 투명화 → Pool 반환
using System;               //Action<T> 사용
using DG.Tweening;          //DOTween 사용
using UnityEngine;

public class PlayerAfterImage : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;     //잔상 SpriteRenderer

    private Tween fadeTween;                                    //현재 실행 중인 Fade Tween
    private Action<PlayerAfterImage> returnToPool;              //재생 종료 후 Pool 반환 메서드

    //*잔상 재생*
    public void Play(Sprite sprite, bool flipX, Color color, float duration, Action<PlayerAfterImage> onFinished)
    {
        returnToPool = onFinished;
        spriteRenderer.sprite = sprite;     //현재 플레이어의 Sprite 복사
        spriteRenderer.flipX = flipX;       //현재 플레이어 방향 복사
        spriteRenderer.color = color;       //잔상 시작 색상 적용
        fadeTween?.Kill();                  //이전 Tween이 남아있다면 제거

        //Alpha 값을 0까지 감소
        fadeTween = spriteRenderer
            .DOFade(0f, duration)           //DoTween이 알아서 부드럽게 투명하게 해줌
            .SetEase(Ease.OutQuad)
            .OnComplete(ReturnToPool);
    }

    //*Fade 종료 후 Pool 반환*
    private void ReturnToPool()
    {
        returnToPool?.Invoke(this);
    }

    //*Pool로 반환될 때 상태 초기화*
    private void OnDisable()
    {
        fadeTween?.Kill();
        fadeTween = null;
        returnToPool = null;
    }
}
