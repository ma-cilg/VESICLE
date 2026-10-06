//**적 피격 시 시각 피드백 처리**
//책임: 피격 이벤트 수신 → Stuck Sprite 표시 → 짧은 시간 후 원래 Sprite 복구
using System;
using DG.Tweening;
using UnityEngine;

public class EnemyHitFeedback : MonoBehaviour
{
    [SerializeField] private EnemyHitReceiver hitReceiver;          //피격 이벤트 수신
    [SerializeField] private EnemyMark enemyMark;                   //마지막 피격인지 확인

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer enemySprite;            //평소 적 SpriteRenderer
    [SerializeField] private SpriteRenderer stuckSprite;            //피격 순간 보여줄 Stuck SpriteRenderer
    [SerializeField] private Transform hitVfxPoint;                 //피격 VFX가 생성될 기준 위치
    [SerializeField, Min(0f)] private float hitVfxHorizontalOffset = 0.6f;
    [SerializeField] private bool stuckSpriteFacesRightByDefault = true;
    [SerializeField] private Animator hitAnimator;                  //HitVisual의 피격 애니메이션 재생
    [SerializeField] private AnimationClip hitAnimationClip;        //피격 애니메이션 전체 길이 확인

    [Header("Hit VFX")]
    [SerializeField] private PooledFX hitVfxPrefab;                 //적 피격 순간 재생할 VFX
    [SerializeField] private Transform poolRoot;                    //사용하지 않는 VFX를 보관할 Pool Root
    [SerializeField, Min(1)] private int hitVfxPoolSize = 3;        //처음 생성해둘 Hit VFX 개수

    private Tween stuckTween;                                       //Stuck Sprite 유지 시간 Tween
    private ComponentPool<PooledFX> hitVfxPool;                     //Hit VFX 전용 Pool

    public event Action OnHitFeedbackEnded;                         //피격 애니메이션 종료 알림

    private void Awake()
    {
        stuckSprite.enabled = false;
        hitVfxPool = new ComponentPool<PooledFX>(hitVfxPrefab, poolRoot, hitVfxPoolSize);
    }

    private void OnEnable()
    {
        hitReceiver.OnHitReceived += PlayHitFeedback;
    }

    private void OnDisable()
    {
        hitReceiver.OnHitReceived -= PlayHitFeedback;

        //진행 중인 Stuck 연출 제거
        stuckTween?.Kill();
        stuckTween = null;

        RestoreVisual();                                    //Visual 원상복구
    }

    //*적이 공격에 맞았을 때*
    private void PlayHitFeedback(EnemyHitInfo hitInfo)
    {
        //이전 피격 연출이 아직 있다면 제거
        stuckTween?.Kill();
        stuckTween = null;

        RestoreVisual();                                    //먼저 정상 상태로 복구

        //이동 공격의 가로 방향에 맞춰 HitVisual 방향 결정
        if (Mathf.Abs(hitInfo.HitDirection.x) > 0.001f)
        {
            bool shouldFaceRight = hitInfo.HitDirection.x < 0f;
            bool shouldFlipX = stuckSpriteFacesRightByDefault ? !shouldFaceRight : shouldFaceRight;

            stuckSprite.flipX = shouldFlipX;                //피격 Sprite 방향
            enemySprite.flipX = shouldFlipX;                //피격 종료 후 Idle도 같은 방향 유지
        }

        enemySprite.enabled = false;                        //평소 Visual 숨김
        stuckSprite.enabled = true;                         //HitVisual 표시

        //피격 애니메이션을 항상 첫 프레임부터 다시 재생
        hitAnimator.Play("Enemy_Hit", 0, 0f);
        hitAnimator.Update(0f);

        PlayHitVFX(hitInfo);                                //같은 순간 피격 VFX 재생

        stuckTween = DOVirtual.DelayedCall(hitAnimationClip.length, () =>
        {
            //마지막 공격으로 사망 상태가 됐다면
            //Idle로 돌아가지 않고 피격 애니메이션 마지막 프레임 유지    
            if (enemyMark != null && enemyMark.IsMarked)
            {
                HoldLastHitFrame();
            }     
            else
            {
                RestoreVisual();
            }
            stuckTween = null;
            OnHitFeedbackEnded?.Invoke();
        });
    }

    //*적 피격 순간 Hit VFX 재생*
    private void PlayHitVFX(EnemyHitInfo hitInfo)
    {
        PooledFX fx = hitVfxPool.Get();                     //Pool에서 사용 가능한 VFX 하나 가져오기
        Vector3 hitPosition = hitVfxPoint.position;         //기본 위치는 적 배 중앙의 HitVFXPoint

        //플레이어가 움직인 방향의 반대쪽이 실제로 칼이 들어온 면
        if (Mathf.Abs(hitInfo.HitDirection.x) > 0.001f)
        {
            float hitSide = -Mathf.Sign(hitInfo.HitDirection.x);

            hitPosition.x += hitSide * hitVfxHorizontalOffset;
        }

        //VFX 위치는 공격 방향과 관계없이 항상 HitVFXPoint에 고정
        fx.transform.SetPositionAndRotation(hitPosition, Quaternion.identity);

        float direction = hitInfo.HitDirection.x >= 0f ? 1f : -1f;  //공격 방향에 따라 좌우만 뒤집기
        Vector3 currentScale = fx.transform.localScale;
        fx.transform.localScale = new Vector3(Mathf.Abs(currentScale.x) * direction, Mathf.Abs(currentScale.y), Mathf.Abs(currentScale.z));

        fx.Play(hitVfxPool.Return);                         //VFX 재생이 끝나면 다시 Pool로 반환
    }

    //*사망 직전 피격 애니메이션 마지막 프레임 유지*
    public void HoldLastHitFrame()
    {
        if (enemySprite != null)
        {
            enemySprite.enabled = false;
        }

        if (stuckSprite != null)
        {
            stuckSprite.enabled = true;
        }

        if (hitAnimator != null)
        {
            hitAnimator.speed = 1f;

            //Enemy_Hit의 마지막 지점으로 즉시 이동
            hitAnimator.Play("Enemy_Hit", 0, 1f);
            hitAnimator.Update(0f);

            //마지막 프레임에서 완전히 정지
            hitAnimator.speed = 0f;
        }
    }

    //*평소 적 Visual로 복구*
    private void RestoreVisual()
    {
        if (hitAnimator != null)
        {
            hitAnimator.speed = 1f;
        }

        if (enemySprite != null)
        {
            enemySprite.enabled = true;
        }

        if (stuckSprite != null)
        {
            stuckSprite.enabled = false;
        }
    }
}