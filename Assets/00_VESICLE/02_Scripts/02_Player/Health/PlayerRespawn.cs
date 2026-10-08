//**플레이어 사망 후 체크포인트 재시작 처리**
//책임: 사망 감지 → 잠시 대기 → 현재 Scene 재로드 → 저장된 Room에서 재시작 알림
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;             //플레이어 사망 감지
    [SerializeField, Min(0f)] private float respawnDelay = 1f;      //사망 후 재시작까지 대기 시간

    private Coroutine respawnRoutine;                               //중복 Respawn 방지

    private static bool respawnAfterSceneReload;                    //Scene Reload 뒤 새 PlayerRespawn이 부활 상황임을 알 수 있도록 유지

    public event Action OnRespawned;                                //부활 Feedback 등에 알림

    private void Start()
    {
        if (!respawnAfterSceneReload) return;

        respawnAfterSceneReload = false;

        StartCoroutine(NotifyRespawnedNextFrame());                 //StageFlowController가 Player / Camera 위치를 먼저 복구할 시간을 줌
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

    //*플레이어 사망 시 재시작 시작*
    private void StartRespawn()
    {
        if (respawnRoutine != null) return;

        respawnRoutine = StartCoroutine(RespawnRoutine());
    }

    //*사망 연출을 잠시 보여준 뒤 현재 Stage Scene 재로드*
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        respawnAfterSceneReload = true;     //다음 Scene 시작이 사망으로 인한 Respawn임을 기록

        //현재 Stage Scene 전체를 새로 로드 (적 / 방 / 투사체 / Mark 등의 런타임 상태도 함께 초기화)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    //*Scene Reload 후 체크포인트 복구가 끝난 다음 부활 Feedback 실행*
    private IEnumerator NotifyRespawnedNextFrame()
    {
        yield return null;

        OnRespawned?.Invoke();
    }
}