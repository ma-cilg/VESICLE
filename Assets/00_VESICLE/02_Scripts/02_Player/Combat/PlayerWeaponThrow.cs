//**플레이어 검 투척 처리**
//책임: 우클릭 입력 확인 → 검 발사 → Trigger 적 폭발 → 검 상태 관리
using UnityEngine;

public class PlayerWeaponThrow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;                 //우클릭 / 마우스 위치 입력
    [SerializeField] private PlayerControlLock controlLock;                 //현재 투척 가능 여부 확인
    [SerializeField] private Transform throwPoint;                          //검이 생성되어 날아가기 시작할 위치
    [SerializeField] private ThrownSwordProjectile swordPrefab;             //투척검 Prefab

    private Camera mainCamera;                                              //마우스 화면 좌표 → 월드 좌표 변환
    private ThrownSwordProjectile swordInstance;                            //게임 중 하나만 생성해서 재사용

    public bool IsThrowing { get; private set; }                            //향후 Player 투척 애니메이션용 상태
    public bool IsWeaponAvailable =>                                        //현재 새로운 검을 던질 수 있는지
        swordInstance != null &&
        !swordInstance.IsFlying &&
        !swordInstance.IsEmbedded;

    private void Awake()
    {
        mainCamera = Camera.main;
        swordInstance = Instantiate(swordPrefab);                           //검은 시작할 때 한 번만 생성해서 재사용
        swordInstance.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (swordInstance == null) return;

        swordInstance.OnFlightEnded += HandleFlightEnded;
        swordInstance.OnEmbedded += HandleSwordEmbedded;
    }

    private void Start()
    {
        //Awake에서 생성된 후 OnEnable보다 늦게 연결될 가능성을 대비
        //이미 연결된 경우 중복되지 않도록 먼저 제거 후 등록
        swordInstance.OnFlightEnded -= HandleFlightEnded;
        swordInstance.OnEmbedded -= HandleSwordEmbedded;

        swordInstance.OnFlightEnded += HandleFlightEnded;
        swordInstance.OnEmbedded += HandleSwordEmbedded;
    }

    private void OnDisable()
    {
        if (swordInstance == null) return;

        swordInstance.OnFlightEnded -= HandleFlightEnded;
        swordInstance.OnEmbedded -= HandleSwordEmbedded;
    }

    private void Update()
    {
        if (!inputReader.IsThrowPressed()) return;

        //검이 Trigger 적에 박혀 있다면 같은 우클릭으로 폭발
        if (swordInstance != null && swordInstance.IsEmbedded)
        {
            TryDetonateEmbeddedEnemy();
            return;
        }

        //이미 검이 날아가는 중이면 추가 투척 금지
        if (swordInstance != null && swordInstance.IsFlying) return;

        //현재 투척이 잠겨있다면 실행하지 않음
        if (!controlLock.CanThrow) return;

        ThrowSword();
    }

    //*현재 마우스 방향으로 검 발사*
    private void ThrowSword()
    {
        if (mainCamera == null) return;
        if (swordInstance == null) return;
        if (throwPoint == null) return;

        IsThrowing = true;

        //마우스 화면 좌표
        Vector2 mouseScreenPosition = inputReader.AimPosition;

        //화면 좌표 → 월드 좌표
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, 0f));

        mouseWorldPosition.z = 0f;

        Vector2 startPosition = throwPoint.position;

        Vector2 direction = (Vector2)mouseWorldPosition - startPosition;

        //방향 계산 불가능한 경우 발사 취소
        if (direction.sqrMagnitude <= 0.001f)
        {
            IsThrowing = false;
            return;
        }

        Vector2 throwDirection = direction.normalized;

        swordInstance.Launch(startPosition, throwDirection);

        //현재는 별도 투척 모션을 기다리지 않으므로 즉시 종료
        IsThrowing = false;
    }

    //*검이 Trigger 적에 박혔을 때*
    private void HandleSwordEmbedded(
        ThrownSwordProjectile projectile,
        EnemyMark enemyMark)
    {
        IsThrowing = false;
    }

    //*검 비행이 일반적으로 종료됐을 때*
    private void HandleFlightEnded(ThrownSwordProjectile projectile)
    {
        IsThrowing = false;
    }

    //*박혀 있는 Trigger 적 폭발*
    private void TryDetonateEmbeddedEnemy()
    {
        if (!controlLock.CanDetonate) return;

        EnemyMark embeddedEnemy = swordInstance.EmbeddedEnemy;

        if (embeddedEnemy == null) return;
        if (!embeddedEnemy.TryDetonate()) return;
    }
}