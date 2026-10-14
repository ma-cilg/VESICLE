//**트리거몹 시각 표현**
//책임: 상태별 스프라이트, 좌우 방향, 차징 DOTween 떨림
using DG.Tweening;
using UnityEngine;

public class EnemyTriggerVisual : MonoBehaviour
{
    public enum Motion { Idle, Run, Fall, Charge, Dash, Stop }

    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer enemySprite;

    [Header("Attack Sprites")]
    [SerializeField] private Sprite chargeSprite;
    [SerializeField] private Sprite dashSprite;
    [SerializeField] private Sprite[] stopSprites = new Sprite[4];

    [Header("Movement Sprites")]
    [SerializeField] private Sprite[] runSprites;
    [SerializeField, Min(0.01f)] private float runFrameDuration = 0.08f;
    [SerializeField] private Sprite[] fallSprites;
    [SerializeField, Min(0.01f)] private float fallFrameDuration = 0.08f;

    [Header("Charge Shake")]
    [SerializeField, Min(0f)] private float shakeStrength = 0.1f;
    [SerializeField, Min(1)] private int shakeVibrato = 25;

    private Motion motion = Motion.Idle;
    private Sprite originalSprite;
    private bool originalFlipX;
    private Vector3 originalLocalPosition;
    private Tween chargeTween;
    private int frameIndex;
    private float frameTimer;

    private void Awake()
    {
        if (enemySprite != null)
        {
            originalSprite = enemySprite.sprite;
            originalFlipX = enemySprite.flipX;
        }
        if (visualRoot != null)
            originalLocalPosition = visualRoot.localPosition;
    }

    //*처음 그림이 오른쪽을 본다는 기존 설정을 유지*
    public void Face(float directionX)
    {
        if (enemySprite == null || Mathf.Abs(directionX) <= 0.001f) return;
        enemySprite.flipX = directionX < 0f;
    }

    public void SetMotion(Motion next)
    {
        if (motion == next) return;
        if (motion == Motion.Charge) StopChargeShake();
        motion = next;
        frameIndex = 0;
        frameTimer = 0f;

        if (enemySprite == null) return;
        switch (motion)
        {
            case Motion.Idle: enemySprite.sprite = originalSprite; break;
            case Motion.Charge: enemySprite.sprite = chargeSprite != null ? chargeSprite : originalSprite; break;
            case Motion.Dash: enemySprite.sprite = dashSprite != null ? dashSprite : originalSprite; break;
            case Motion.Run: SetFirstFrame(runSprites); break;
            case Motion.Fall: SetFirstFrame(fallSprites); break;
            case Motion.Stop: break; //공격 컨트롤러가 4프레임 진행
        }
    }

    public void BeginCharge(float duration)
    {
        SetMotion(Motion.Charge);
        StopChargeShake();
        if (visualRoot == null) return;
        chargeTween = visualRoot.DOShakePosition(
            duration, shakeStrength, shakeVibrato, 90f, false, false);
    }

    public void PlayStopFrame(int index)
    {
        if (enemySprite == null || stopSprites == null ||
            index < 0 || index >= stopSprites.Length || stopSprites[index] == null) return;
        enemySprite.sprite = stopSprites[index];
    }

    private void Update()
    {
        if (enemySprite == null) return;
        if (motion != Motion.Run && motion != Motion.Fall) return;

        Sprite[] frames = motion == Motion.Run ? runSprites : fallSprites;
        if (frames == null || frames.Length <= 1) return;

        float duration = motion == Motion.Run ? runFrameDuration : fallFrameDuration;
        frameTimer += Time.deltaTime;
        if (frameTimer < duration) return;
        frameTimer -= duration;
        frameIndex = (frameIndex + 1) % frames.Length;
        if (frames[frameIndex] != null) enemySprite.sprite = frames[frameIndex];
    }

    private void SetFirstFrame(Sprite[] sprites)
    {
        if (sprites != null && sprites.Length > 0 && sprites[0] != null)
            enemySprite.sprite = sprites[0];
        else
            enemySprite.sprite = originalSprite;
    }

    private void StopChargeShake()
    {
        chargeTween?.Kill();
        chargeTween = null;
        if (visualRoot != null) visualRoot.localPosition = originalLocalPosition;
    }

    private void OnDisable()
    {
        StopChargeShake();
        motion = Motion.Idle;
        if (enemySprite == null) return;
        enemySprite.sprite = originalSprite;
        enemySprite.flipX = originalFlipX;
    }
}
