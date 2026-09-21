//**플레이어 점프 관련 FX 처리**
//책임: PlayerJump 이벤트 → Pool에서 FX 가져오기 → 위치 지정 → 재생
using UnityEngine;

public class PlayerJumpFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerJump playerJump;             //점프 이벤트를 발생시키는 PlayerJump
    [SerializeField] private Transform groundCheck;             //점프와 착지 FX가 나올 위치
    [SerializeField] private Transform poolRoot;                //사용하지 않는 FX 보관 위치

    [Header("FX Prefabs")]
    [SerializeField] private PooledFX jumpDustPrefab;           //바닥 점프 FX
    [SerializeField] private PooledFX doubleJumpPrefab;         //2단 점프 FX
    [SerializeField] private PooledFX landPrefab;               //착지 FX

    [Header("Pool")]
    [SerializeField, Min(1)] private int initialPoolSize = 2;   //FX별 처음 생성 개수

    private ComponentPool<PooledFX> jumpDustPool;
    private ComponentPool<PooledFX> doubleJumpPool;
    private ComponentPool<PooledFX> landPool;

    //*Pool 생성*
    private void Awake()
    {
        jumpDustPool = new ComponentPool<PooledFX>(jumpDustPrefab, poolRoot, initialPoolSize);
        doubleJumpPool = new ComponentPool<PooledFX>(doubleJumpPrefab, poolRoot, initialPoolSize);
        landPool = new ComponentPool<PooledFX>(landPrefab, poolRoot, initialPoolSize);
    }

    //*이벤트 구독*
    private void OnEnable()
    {
        playerJump.OnJumped += PlayJumpDust;
        playerJump.OnDoubleJumped += PlayDoubleJumpFX;
        playerJump.OnLanded += PlayLandFX;
    }

    //*이벤트 구독 해제*
    private void OnDisable()
    {
        playerJump.OnJumped -= PlayJumpDust;
        playerJump.OnDoubleJumped -= PlayDoubleJumpFX;
        playerJump.OnLanded -= PlayLandFX;
    }

    //*바닥 점프 FX*
    private void PlayJumpDust()
    {
        PooledFX fx = jumpDustPool.Get();
        fx.transform.SetPositionAndRotation(groundCheck.position, Quaternion.identity);
        fx.Play(jumpDustPool.Return);
    }

    //*2단 점프 FX*
    private void PlayDoubleJumpFX()
    {
        PooledFX fx = doubleJumpPool.Get();
        fx.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
        fx.Play(doubleJumpPool.Return);
    }

    //*착지 FX*
    private void PlayLandFX()
    {
        PooledFX fx = landPool.Get();
        fx.transform.SetPositionAndRotation(groundCheck.position, Quaternion.identity);
        fx.Play(landPool.Return);
    }
}
