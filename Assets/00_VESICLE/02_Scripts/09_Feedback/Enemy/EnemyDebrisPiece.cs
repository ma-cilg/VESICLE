//**적 사망 조각 물리 연출**
//책임: 조각 Sprite 설정 → 폭발 방향으로 날아감 → 바닥 충돌 → Fade → Pool 반환

using System;
using DG.Tweening;
using UnityEngine;

public class EnemyDebrisPiece : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;                            //조각 물리 이동
    [SerializeField] private SpriteRenderer spriteRenderer;             //실제로 보여줄 조각 Sprite
    [SerializeField] private Transform visualRoot;                      //Fade와 Scale 연출 대상

    [Header("Disappear")]
    [SerializeField, Min(0f)] private float landDelay = 1.2f;           //바닥에 닿은 후 잠깐 남아있는 시간
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.6f;     //사라지는 시간
    [SerializeField, Range(0f, 1f)] private float endScale = 0.35f;     //마지막 크기
    [SerializeField, Min(0.1f)] private float safetyLifetime = 4f;      //땅에 안 닿아도 강제 반환

    private Vector3 originalScale;

    private Tween fadeTween;
    private Tween safetyTween;

    private Action<EnemyDebrisPiece> returnToPool;

    private bool hasLanded;
    private bool isReturning;

    private void Awake()
    {
        originalScale = visualRoot.localScale;
    }

    //*Pool에서 꺼낸 조각 재생*
    public void Play(
        Sprite sprite,
        Color color,
        Vector2 position,
        Vector2 velocity,
        float angularVelocity,
        bool flipX,
        Vector3 worldScale,
        Action<EnemyDebrisPiece> onFinished)
    {
        fadeTween?.Kill();
        safetyTween?.Kill();

        fadeTween = null;
        safetyTween = null;

        returnToPool = onFinished;

        hasLanded = false;
        isReturning = false;

        transform.position = position;
        transform.rotation = Quaternion.identity;

        //Pool 부모나 사망 팽창 Scale을 물려받지 않고
        //적이 살아있을 때의 원래 크기로 고정
        transform.localScale = worldScale;

        visualRoot.localScale = originalScale;

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.flipX = flipX;

        rb.linearVelocity = velocity;
        rb.angularVelocity = angularVelocity;

        //바닥에 안 닿거나 맵 밖으로 날아가도 영원히 남지 않게 안전 반환
        safetyTween = DOVirtual.DelayedCall(safetyLifetime, StartFade);
    }

    //*바닥에 처음 닿았을 때*
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded) return;

        hasLanded = true;

        //바닥에 떨어진 뒤 잠깐 남아있다가 사라짐
        fadeTween = DOVirtual.DelayedCall(landDelay, StartFade);
    }

    //*Fade 시작*
    private void StartFade()
    {
        if (isReturning) return;

        safetyTween?.Kill();
        safetyTween = null;

        fadeTween?.Kill();

        Sequence sequence = DOTween.Sequence();

        sequence.Join(spriteRenderer.DOFade(0f, fadeDuration));

        sequence.Join(visualRoot.DOScale(originalScale * endScale, fadeDuration));

        fadeTween = sequence;

        sequence.OnComplete(ReturnToPool);
    }

    //*Pool로 반환*
    private void ReturnToPool()
    {
        if (isReturning) return;

        isReturning = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        fadeTween = null;

        returnToPool?.Invoke(this);
    }

    private void OnDisable()
    {
        fadeTween?.Kill();
        safetyTween?.Kill();

        fadeTween = null;
        safetyTween = null;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        transform.rotation = Quaternion.identity;

        visualRoot.localScale = originalScale;

        spriteRenderer.sprite = null;
        spriteRenderer.color = Color.white;
        spriteRenderer.flipX = false;

        returnToPool = null;

        hasLanded = false;
        isReturning = false;
    }
}