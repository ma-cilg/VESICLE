//**노멀몹 이동 애니메이션**
//책임: 물리 상태에 따른 달리기/상승/중간/낙하/착지 Sprite 표시
using UnityEngine;

public class EnemyNormalVisual : MonoBehaviour
{
    private enum Motion { Idle, Run, Rise, Mid, Fall, Land }

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer enemySprite;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private EnemyMark enemyMark;
    [SerializeField] private EnemyDeath enemyDeath;
    [SerializeField] private EnemyHitReaction hitReaction;

    [Header("Idle / Run")]
    [SerializeField] private Sprite[] idleSprites = new Sprite[7];
    [SerializeField, Min(0.01f)] private float idleFrameDuration = 0.08f;
    [SerializeField] private Sprite[] runSprites = new Sprite[8];
    [SerializeField, Min(0.01f)] private float runFrameDuration = 0.08f;

    [Header("Jump")]
    [SerializeField] private Sprite riseSprite;
    [SerializeField] private Sprite midSprite;
    [SerializeField] private Sprite fallSprite;
    [SerializeField, Min(0f)] private float midVelocityThreshold = 1f;

    [Header("Landing")]
    [SerializeField] private Sprite[] landSprites = new Sprite[2];
    [SerializeField, Min(0.01f)] private float landFrameDuration = 0.08f;

    [Header("Ground")]
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.15f;

    private Motion motion = Motion.Idle;
    private int frameIndex;
    private float frameTimer;
    private bool wasGrounded;
    private bool isLanding;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        motion = Motion.Idle;
        frameIndex = 0;
        frameTimer = 0f;
        isLanding = false;
        wasGrounded = IsGrounded();

        if (enemySprite != null && idleSprites.Length > 0 && idleSprites[0] != null)
        {
            enemySprite.sprite = idleSprites[0];
        }
    }

    private void Update()
    {
        if (rb == null || enemySprite == null || groundCheck == null)
            return;

        //기존 피격/사망 연출과 충돌하지 않도록 중단
        if (!enemySprite.enabled) return;
        if (enemyDeath != null && enemyDeath.IsDead) return;
        if (enemyMark != null && enemyMark.IsMarked) return;
        if (hitReaction != null && hitReaction.IsStaggered) return;

        //이동 방향에 맞춰 좌우 반전
        float velocityX = rb.linearVelocity.x;
        if (Mathf.Abs(velocityX) > 0.1f)
            enemySprite.flipX = velocityX < 0f;

        bool grounded = IsGrounded();

        //공중에서 땅에 처음 닿은 순간
        if (grounded && !wasGrounded)
        {
            isLanding = true;
            SetMotion(Motion.Land);
        }

        wasGrounded = grounded;

        //공중 상태
        if (!grounded)
        {
            isLanding = false;

            float velocityY = rb.linearVelocity.y;

            if (velocityY > midVelocityThreshold)
                SetMotion(Motion.Rise);
            else if (velocityY < -midVelocityThreshold)
                SetMotion(Motion.Fall);
            else
                SetMotion(Motion.Mid);

            return;
        }

        //착지 2프레임을 끝까지 재생
        if (isLanding)
        {
            if (AdvanceFrames(landSprites, landFrameDuration, false))
                isLanding = false;

            return;
        }

        //바닥에서는 이동 여부에 따라 Run/Idle
        bool running = Mathf.Abs(velocityX) > 0.1f;
        SetMotion(running ? Motion.Run : Motion.Idle);

        if (running)
            AdvanceFrames(runSprites, runFrameDuration, true);
        else
            AdvanceFrames(idleSprites, idleFrameDuration, true);
    }

    //*발밑의 Ground 확인*
    private bool IsGrounded()
    {
        return groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    //*애니메이션 상태 변경*
    private void SetMotion(Motion next)
    {
        if (motion == next) return;

        motion = next;
        frameIndex = 0;
        frameTimer = 0f;

        Sprite sprite = motion switch
        {
            Motion.Run => FirstSprite(runSprites),
            Motion.Rise => riseSprite,
            Motion.Mid => midSprite,
            Motion.Fall => fallSprite,
            Motion.Land => FirstSprite(landSprites),
            _ => FirstSprite(idleSprites)
        };

        if (sprite != null)
            enemySprite.sprite = sprite;
    }

    //*프레임 재생. 비반복 애니메이션 종료 시 true*
    private bool AdvanceFrames(Sprite[] frames, float duration, bool loop)
    {
        if (frames == null || frames.Length == 0) return true;

        frameTimer += Time.deltaTime;

        if (frameTimer < duration) return false;

        frameTimer -= duration;

        int nextIndex = frameIndex + 1;

        if (nextIndex >= frames.Length)
        {
            if (!loop) return true;
            nextIndex = 0;
        }

        frameIndex = nextIndex;

        if (frames[frameIndex] != null)
            enemySprite.sprite = frames[frameIndex];

        return false;
    }

    //*배열의 첫 번째 Sprite 반환*
    private Sprite FirstSprite(Sprite[] frames)
    {
        return frames != null && frames.Length > 0 ? frames[0] : null;
    }
}

