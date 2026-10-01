//**플레이어 이동 공격 이동 연출 처리**
//책임: 실제 이동 공격 시작 → 지상 먼지 재생 → 이동 경로에 잔상 생성
using UnityEngine;
using UnityEngine.Serialization;    //SerializeField 이름 변경 시 기존 Inspector 연결 유지

public class PlayerAttackMovementFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttack playerAttack;                 //이동 공격 시작 / 종료 상태 확인
    [SerializeField] private PlayerJump playerJump;                     //지상 여부 확인
    [SerializeField] private PlayerMovement playerMovement;             //플레이어 좌우 방향 확인
    [SerializeField] private SpriteRenderer playerSprite;               //현재 플레이어 Sprite 복사
    [SerializeField] private Transform groundCheck;                     //지상 먼지 생성 위치
    [SerializeField] private Transform poolRoot;                        //사용하지 않는 FX 보관 위치

    [Header("Ground FX")]
    [FormerlySerializedAs("dashDustPrefab")]
    [SerializeField] private PooledFX movementAttackDustPrefab;             //이동 공격 시작 시 지상 먼지 FX

    [FormerlySerializedAs("dashDustPoolSize")]
    [SerializeField, Min(1)] private int movementAttackDustPoolSize = 2;    //이동 공격 FX 초기 Pool 크기

    [Header("After Image")]
    [SerializeField] private PlayerAfterImage afterImagePrefab;         //잔상 Prefab
    [SerializeField, Min(1)] private int afterImagePoolSize = 8;

    [SerializeField, Min(0.01f)]
    private float afterImageSpacing = 0.45f;                            //얼마나 이동할 때마다 잔상을 하나 남길지

    [SerializeField, Min(0.01f)]
    private float afterImageFadeDuration = 0.12f;                       //잔상이 사라지는 시간

    [SerializeField]
    private Color afterImageColor = new Color(1f, 1f, 1f, 0.45f);

    private ComponentPool<PooledFX> movementAttackDustPool;
    private ComponentPool<PlayerAfterImage> afterImagePool;

    private Vector3 lastAfterImagePosition;                             //마지막 잔상 생성 위치

    private void Awake()
    {
        movementAttackDustPool = new ComponentPool<PooledFX>(movementAttackDustPrefab, poolRoot, movementAttackDustPoolSize);
        afterImagePool = new ComponentPool<PlayerAfterImage>(afterImagePrefab, poolRoot, afterImagePoolSize);
    }

    private void OnEnable()
    {
        playerAttack.OnSlashStarted += HandleSlashStarted;              //실제 이동 공격이 시작되는 순간만 구독
    }

    private void OnDisable()
    {
        playerAttack.OnSlashStarted -= HandleSlashStarted;
    }

    private void Update()
    {
        if (playerAttack.CurrentState != PlayerAttackState.Slashing) return;       
        UpdateAfterImage();
    }

    //*실제 이동 공격 시작*
    private void HandleSlashStarted(float attackDistance)
    {
        PlayGroundDust();
        lastAfterImagePosition = playerSprite.transform.position;       //잔상 거리 계산 시작점 저장
        PlayAfterImage(lastAfterImagePosition);                         //날아가기 시작하는 순간 첫 잔상 즉시 생성
    }

    //*이동 공격 중 잔상 생성*
    private void UpdateAfterImage()
    {
        Vector3 currentPosition = playerSprite.transform.position;
        float movedDistance = Vector3.Distance(lastAfterImagePosition, currentPosition);    //마지막 잔상 위치에서 현재까지 얼마나 움직였는지 확인

        if (movedDistance < afterImageSpacing) return;

        PlayAfterImage(currentPosition);                                //현재 위치에 새 잔상 생성
        lastAfterImagePosition = currentPosition;                       //새 기준 위치 저장
    }

    //*지상 이동 공격 시작 먼지*
    private void PlayGroundDust()
    {
        if (!playerJump.IsGrounded) return;

        PooledFX fx = movementAttackDustPool.Get();
        fx.transform.SetPositionAndRotation(groundCheck.position, Quaternion.identity);

        float direction = playerMovement.IsFacingRight ? 1f : -1f;      //플레이어가 바라보는 방향에 맞게 FX 좌우 방향 설정
        fx.transform.localScale = new Vector3(direction, 1f, 1f);

        fx.Play(movementAttackDustPool.Return);
    }

    //*잔상 한 장 생성*
    private void PlayAfterImage(Vector3 position)
    {
        PlayerAfterImage afterImage = afterImagePool.Get();
        afterImage.transform.SetPositionAndRotation(position, playerSprite.transform.rotation);
        afterImage.transform.localScale = playerSprite.transform.lossyScale;
        afterImage.Play(playerSprite.sprite, playerSprite.flipX, afterImageColor, afterImageFadeDuration, afterImagePool.Return);
    }
}