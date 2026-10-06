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

    public bool IsWeaponAvailable =>
        swordInstance != null &&
        !swordInstance.IsFlying &&
        !swordInstance.IsEmbedded &&
        !swordInstance.IsEnvironmentEmbedded;

    private void Awake()
    {
        mainCamera = Camera.main;
        swordInstance = Instantiate(swordPrefab);                           //검은 시작할 때 한 번만 생성해서 재사용
        swordInstance.gameObject.SetActive(false);
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

        //검이 날아가는 중이거나 환경에 박혀있는 동안 추가 투척 금지
        if (swordInstance != null && (swordInstance.IsFlying || swordInstance.IsEnvironmentEmbedded)) return;
        
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

        //마우스 화면 좌표
        Vector2 mouseScreenPosition = inputReader.AimPosition;

        //화면 좌표 → 월드 좌표
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, 0f));

        mouseWorldPosition.z = 0f;

        Vector2 startPosition = throwPoint.position;

        Vector2 direction = (Vector2)mouseWorldPosition - startPosition;

        if (direction.sqrMagnitude <= 0.001f) return;
        
        Vector2 throwDirection = direction.normalized;

        swordInstance.Launch(startPosition, throwDirection);
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