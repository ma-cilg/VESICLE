//**플레이어 사망 후 재시작 처리**
//책임: 사망 감지 → 잠시 대기 → 시작 위치 복귀 → 체력/사망 상태 복구
using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;         //사망 감지 및 체력 복구
    [SerializeField] private PlayerDeath playerDeath;           //사망으로 걸린 조작 Lock 복구
    [SerializeField] private PlayerAnimation playerAnimation;   //Respawn 후 Idle 상태 복귀

    [SerializeField, Min(0f)]
    private float respawnDelay = 1f;                            //사망 후 다시 살아나기까지 대기 시간

    private Rigidbody2D rb;

    private Vector2 respawnPosition;                            //현재 Respawn 위치
    private Coroutine respawnRoutine;                           //중복 Respawn 방지
    public event Action OnRespawned;                            //플레이어 부활 완료 알림

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        //현재 테스트에서는 씬에 배치된 Player의 시작 위치를 Respawn 위치로 사용
        respawnPosition = rb.position;
    }

    private void OnEnable()
    {
        playerHealth.OnDied += StartRespawn;
    }

    private void OnDisable()
    {
        playerHealth.OnDied -= StartRespawn;

        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }
    }

    //*사망 시 Respawn 시작*
    private void StartRespawn()
    {
        if (respawnRoutine != null) return;

        respawnRoutine = StartCoroutine(RespawnRoutine());
    }

    //*일정 시간 후 플레이어 복구*
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        rb.linearVelocity = Vector2.zero;               //남아있는 움직임 제거
        rb.angularVelocity = 0f;
        rb.position = respawnPosition;                  //시작 위치로 이동

        playerHealth.ResetHealth();                     //HP와 사망 상태 복구
        playerAnimation.ResetAfterRespawn();            //사망 애니메이션에서 기본 Idle로 복귀
        playerDeath.ResetDeathState();                  //사망으로 걸었던 조작 Lock 해제

        OnRespawned?.Invoke();                          //부활 완료를 Feedback 등에 알림

        respawnRoutine = null;
    }
}