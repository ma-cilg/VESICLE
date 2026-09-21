//**플레이어 달리기 FX 처리**
//책임: 달리기 애니메이션 이벤트 → 상태 확인 → RunDustFX 재생
using UnityEngine;

public class PlayerRunFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerJump playerJump;             //지상 여부 확인
    [SerializeField] private PlayerDash playerDash;             //대시 여부 확인
    [SerializeField] private PlayerMovement playerMovement;     //플레이어 방향 확인
    [SerializeField] private Transform groundCheck;             //FX 생성 위치
    [SerializeField] private Transform poolRoot;                //사용하지 않는 FX 보관 위치

    [Header("Back FX")]
    [SerializeField] private PooledFX backContactPrefab;        //Run 0: 먼쪽 발 바닥에 닿을 때 3프레임
    [SerializeField] private PooledFX backReleasePrefab;        //Run 2: 먼쪽 발 뗄 때 5프레임

    [Header("Front FX")]
    [SerializeField] private PooledFX frontContactPrefab;       //Run 4 : 가까운쪽 발 바닥에 닿을 때 3프레임
    [SerializeField] private PooledFX frontReleasePrefab;       //Run 6 : 가까운쪽 발 뗄 때 5프레임

    [Header("Pool")]
    [SerializeField, Min(1)] private int initialPoolSize = 2;   //처음 생성할 FX 개수

    private ComponentPool<PooledFX> backContactPool;
    private ComponentPool<PooledFX> backReleasePool;
    private ComponentPool<PooledFX> frontContactPool;
    private ComponentPool<PooledFX> frontReleasePool;

    //*Pool생성*
    private void Awake()
    {
        backContactPool = new ComponentPool<PooledFX>(backContactPrefab, poolRoot, initialPoolSize);
        backReleasePool = new ComponentPool<PooledFX>(backReleasePrefab, poolRoot, initialPoolSize);
        frontContactPool = new ComponentPool<PooledFX>(frontContactPrefab, poolRoot, initialPoolSize);
        frontReleasePool = new ComponentPool<PooledFX>(frontReleasePrefab, poolRoot, initialPoolSize);
    }

    //*Run 0 - 첫 번째 발 착지*
    public void PlayBackContact()
    {
        if (!CanPlayRunDust()) return;
        PlayDust(backContactPool);
    }

    //*Run 2 - 첫 번째 발 떼기*
    public void PlayBackRelease()
    {
        if (!CanPlayRunDust()) return;
        PlayDust(backReleasePool);
    }

    //*Run 4 - 반대쪽 발 착지*
    public void PlayFrontContact()
    {
        if (!CanPlayRunDust()) return;
        PlayDust(frontContactPool);
    }

    //*Run 6 - 반대쪽 발 떼기*
    public void PlayFrontRelease()
    {
        if (!CanPlayRunDust()) return;
        PlayDust(frontReleasePool);
    }

    //*FX 생성할 수 있는 상태인지 확인*
    private bool CanPlayRunDust()
    {
        if (!playerJump.IsGrounded) return false;   //공중에서 새 먼지 생성 X
        if (playerDash.IsDashing) return false;     //대시 중 먼지 생성 X

        //나중에 벽 조건 추가

        return true;
    }

    //*Pool에서 FX 가져와 현재 발 위치에서 재생*
    private void PlayDust(ComponentPool<PooledFX> pool)
    {
        PooledFX fx = pool.Get();
        fx.transform.SetPositionAndRotation(groundCheck.position, Quaternion.identity); //생성 순간의 월드 위치에 배치
        float direction = playerMovement.IsFacingRight ? 1f : -1f;                      //플레이어 방향에 맞춰 좌우 반전
        fx.transform.localScale = new Vector3(direction, 1f, 1f);
        fx.Play(pool.Return);                                                           //애니메이션이 끝나면 다시 Pool로 반환
    }
}
