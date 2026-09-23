//**플레이어 검 공격 범위 처리**
//책임: 공격 타격 이벤트 수신 → 지상/공중 공격에 맞는 범위 계산 → 범위 안 대상 탐색
using System.Collections.Generic;   //HashSet 사용
using UnityEngine;

public class PlayerAttackHitBox : MonoBehaviour
{
    [SerializeField] private PlayerAttack playerAttack;                             //현재 지상/공중 공격 상태 확인
    [SerializeField] private PlayerMovement playerMovement;                         //현재 바라보는 방향 확인

    //지상 공격 범위
    [SerializeField] private Vector2 groundAttackOffset = new Vector2(0.6f, 0.6f);  //지상 공격 중심 위치 보정값
    [SerializeField] private Vector2 groundAttackSize = new Vector2(1f, 0.7f);      //지상 공격 판정 크기

    //공중 공격 범위
    [SerializeField] private Vector2 airAttackOffset = new Vector2(0.8f, 0.85f);    //공중 공격 중심 위치 보정값
    [SerializeField] private Vector2 airAttackSize = new Vector2(1.2f, 0.6f);       //공중 공격 판정 크기

    //공격 대상으로 인정할 Layer
    [SerializeField] private LayerMask enemyLayer;

    private readonly Collider2D[] hitResults = new Collider2D[16];                  //한 번의 공격 판정에서 감지된 Collider를 저장하는 배열
    private ContactFilter2D enemyFilter;                                            //Enemy Layer만 검사하기 위한 필터

    //한 번의 공격에서 중복 타격되는 것을 막기 위한 저장 공간(적 콜라이더가 여러개일 경우)
    private readonly HashSet<EnemyHitReceiver> hitEnemies = new HashSet<EnemyHitReceiver>();

    private void Awake()
    {
        enemyFilter = new ContactFilter2D();        //Physics2D 검사에서 사용할 필터 생성
        enemyFilter.useTriggers = true;             //Trigger Collider도 공격 대상으로 감지할 수 있게 허용
        enemyFilter.SetLayerMask(enemyLayer);       //Inspector에서 지정한 Enemy Layer만 검사하도록 설정
    }

    private void OnEnable()
    {
        //이벤트로 실제 타격 프레임이 발생했을 때 공격 범위 검사
        playerAttack.OnAttackHit += CheckAttackHit;
    }

    private void OnDisable()
    {
        //이벤트 구독 해제
        playerAttack.OnAttackHit -= CheckAttackHit;
    }

    //*현재 공격 범위 검사*
    private void CheckAttackHit()
    {
        //현재 공격이 어떤 공격인지에 따라 사용할 Offset과 Size를 결정
        Vector2 attackOffset = playerAttack.IsAirAttack ? airAttackOffset : groundAttackOffset;
        Vector2 attackSize = playerAttack.IsAirAttack ? airAttackSize : groundAttackSize;

        //현재 플레이어가 바라보는 방향
        float direction = playerMovement.IsFacingRight ? 1f : -1f;

        //현재 공격 판정의 실제 중심 위치 계산
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(attackOffset.x * direction, attackOffset.y);

        hitEnemies.Clear();     //저장된 적 목록 초기화

        //공격 박스 안에 들어온 Collider 검사
        int hitCount = Physics2D.OverlapBox(attackCenter, attackSize, 0f, enemyFilter, hitResults);

        //감지된 Collider 수만큼 반복
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = hitResults[i];                 //이번에 감지된 Collider 하나 가져오기

            //부모 방향까지 올라가면서 EnemyHitReceiver 찾기
            EnemyHitReceiver hitReceiver = hitCollider.GetComponentInParent<EnemyHitReceiver>();

            if (hitReceiver == null) continue;
            if (!hitEnemies.Add(hitReceiver)) continue;

            //현재 근접 공격의 피격 정보 생성
            EnemyHitInfo hitInfo = new EnemyHitInfo( EnemyHitType.Melee, new Vector2(direction, 0f));

            //적에게 피격 정보 전달
            hitReceiver.ReceiveHit(hitInfo);
        }
    }

    //*Scene 창에서 공격 판정 범위 확인*
    private void OnDrawGizmosSelected()
    {
        //기본적으로 지상 공격 범위를 표시
        Vector2 attackOffset = groundAttackOffset;
        Vector2 attackSize = groundAttackSize;

        //공중 공격을 하고 있다면 공중 공격 범위로 변경
        if (Application.isPlaying && playerAttack != null && playerAttack.IsAirAttack)
        {
            attackOffset = airAttackOffset;
            attackSize = airAttackSize;
        }

        //기본 방향은 오른쪽
        float direction = 1f;

        //현재 실제 바라보는 방향 사용
        if (playerMovement != null)
        {
            direction = playerMovement.IsFacingRight ? 1f : -1f;
        }

        //실제 Hitbox 중심 위치 계산
        Vector3 attackCenter = transform.position + new Vector3(attackOffset.x * direction, attackOffset.y, 0f);

        //Scene 창에 판정 범위를 선으로 표시
        Gizmos.DrawWireCube(attackCenter, attackSize);
    }
}
