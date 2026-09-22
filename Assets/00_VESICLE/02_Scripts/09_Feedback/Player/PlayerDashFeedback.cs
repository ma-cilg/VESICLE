//**플레이어 대시 FX 처리**
//책임: 대시 시작 이벤트 → 지상 여부 확인 → Pool에서 DashDustFX 재생
using UnityEngine;

public class PlayerDashFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDash playerDash;                 //대시 시작 이벤트
    [SerializeField] private PlayerJump playerJump;                 //지상 여부 확인
    [SerializeField] private PlayerMovement playerMovement;         //플레이어 방향 확인
    [SerializeField] private SpriteRenderer playerSprite;           //현재 플레이어 Sprite 확인
    [SerializeField] private Transform groundCheck;                 //대시 FX 생성 위치
    [SerializeField] private Transform poolRoot;                    //사용하지 않는 FX 보관 위치

    [Header("FX")]
    [SerializeField] private PooledFX dashDustPrefab;               //지상 대시 FX
    [SerializeField, Min(1)] private int dashDustPoolSize = 2;      

    [Header("After Image")]
    [SerializeField] private PlayerAfterImage afterImagePrefab;     //대시 잔상 Prefab
    [SerializeField, Min(1)] private int afterImagePoolSize = 5;    //처음 생성할 잔상 개수
    [SerializeField, Min(0.01f)] private float afterImageInterval = 0.04f; //잔상 생성 간격
    [SerializeField, Min(0.01f)] private float afterImageFadeDuration = 0.15f; //사라지는 시간
    [SerializeField] private Color afterImageColor = new Color(1f, 1f, 1f, 0.45f);

    private ComponentPool<PooledFX> dashDustPool;
    private ComponentPool<PlayerAfterImage> afterImagePool;

    private bool isSpawningAfterImages;
    private float afterImageTimer;

    //*Pool 생성*
    private void Awake()
    {
        dashDustPool = new ComponentPool<PooledFX>(dashDustPrefab, poolRoot, dashDustPoolSize);
        afterImagePool = new ComponentPool<PlayerAfterImage>(afterImagePrefab, poolRoot, afterImagePoolSize);
    }

    //*대시 시작 이벤트 구독*
    private void OnEnable()
    {
        playerDash.OnDashStarted += HandleDashStarted;
    }

    //*대시 시작 이벤트 구독 해제*
    private void OnDisable()
    {
        playerDash.OnDashStarted -= HandleDashStarted;
    }

    private void Update()
    {
        if (!isSpawningAfterImages) return;

        //대시가 끝났으면 새로운 잔상 생성도 종료
        if (!playerDash.IsDashing)
        {
            isSpawningAfterImages = false;
            afterImageTimer = 0f;
            return;
        }

        afterImageTimer += Time.deltaTime;
        if (afterImageTimer < afterImageInterval) return;
        afterImageTimer -= afterImageInterval;
        PlayAfterImage();
    }

    //*대시 시작*
    private void HandleDashStarted()
    {
        PlayDashDust();
        isSpawningAfterImages = true;   //지상 / 공중 관계없이 잔상은 재생
        afterImageTimer = 0f;
        PlayAfterImage();               //대시 시작 순간 첫 잔상 바로 생성
    }

    //*지상 대시 먼지 재생*
    private void PlayDashDust()
    {
        if (!playerJump.IsGrounded) return;   //공중 대시는 먼지 X
        PooledFX fx = dashDustPool.Get();
        fx.transform.SetPositionAndRotation(groundCheck.position, Quaternion.identity);
        float direction = playerMovement.IsFacingRight ? 1f : -1f;
        fx.transform.localScale = new Vector3(direction, 1f, 1f);
        fx.Play(dashDustPool.Return);
    }

    //*대시 잔상 재생*
    private void PlayAfterImage()
    {
        PlayerAfterImage afterImage = afterImagePool.Get();

        //잔상은 생성된 월드 위치에 그대로 남음
        afterImage.transform.SetPositionAndRotation(playerSprite.transform.position, playerSprite.transform.rotation);

        //Player Visual의 크기 복사
        afterImage.transform.localScale = playerSprite.transform.lossyScale;

        //현재 보이는 Player Sprite 한 장을 그대로 복사
        afterImage.Play(
            playerSprite.sprite,
            playerSprite.flipX,
            afterImageColor,
            afterImageFadeDuration,
            afterImagePool.Return
            );
    }
}
