//**공용 전투 웨이브 관리**
//책임: 웨이브 순서, 등장 시간, 적 사망 감지
//최종 클리어는 기존 CombatRoom에서 처리
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomWaveController : MonoBehaviour
{
    //**몬스터 한 마리의 등장 설정**
    [Serializable]
    private class SpawnEntry
    {
        public EnemyDeath enemy;

        [Min(0f)] public float spawnDelay;
    }

    //**웨이브 하나의 설정**
    [Serializable]
    private class Wave
    {
        [Min(0f)]
        public float delayBeforeWave;

        public SpawnEntry[] enemies;
    }

    [Header("Room")]
    [SerializeField] private CombatRoom combatRoom;

    [Header("Waves")]
    [SerializeField] private Wave[] waves;

    private int currentWaveIndex = -1;
    private int remainingEnemies;

    private void Awake()
    {
        //전투 시작 전 모든 웨이브 몬스터 숨기기
        if (waves == null) return;

        foreach (Wave wave in waves)
        {
            if (wave == null || wave.enemies == null) continue;

            foreach (SpawnEntry entry in wave.enemies)
            {
                if (entry == null || entry.enemy == null) continue;

                entry.enemy.gameObject.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        if (combatRoom == null) return;

        combatRoom.OnRoomStarted += HandleRoomStarted;

        //체크포인트 복구로 이미 시작한 방 처리
        if (combatRoom.IsStarted && !combatRoom.IsCleared)
            HandleRoomStarted();
    }

    private void OnDisable()
    {
        if (combatRoom != null)
            combatRoom.OnRoomStarted -= HandleRoomStarted;

        UnsubscribeCurrentWave();

        StopAllCoroutines();
        currentWaveIndex = -1;
    }

    //*방 전투가 시작되면 첫 웨이브 진행*
    private void HandleRoomStarted()
    {
        if (currentWaveIndex >= 0) return;
        if (combatRoom == null || combatRoom.IsCleared) return;

        if (!ValidateWaves()) return;

        StartWave(0);
    }

    //*지정한 웨이브 시작*
    private void StartWave(int waveIndex)
    {
        currentWaveIndex = waveIndex;

        Wave wave = waves[waveIndex];

        //마지막 웨이브는 CombatRoom이 직접 사망 감지
        bool isLastWave = waveIndex == waves.Length - 1;

        if (!isLastWave)
        {
            remainingEnemies = wave.enemies.Length;

            foreach (SpawnEntry entry in wave.enemies)
                entry.enemy.OnDied += HandleEnemyDied;
        }

        StartCoroutine(SpawnWaveRoutine(wave));
    }

    //*웨이브 및 몬스터별 등장 시간 처리*
    private IEnumerator SpawnWaveRoutine(Wave wave)
    {
        //웨이브 시작 전 대기시간
        if (wave.delayBeforeWave > 0f)
            yield return new WaitForSeconds(wave.delayBeforeWave);

        //Spawn Delay가 빠른 순서대로 등장
        SpawnEntry[] sortedEntries = (SpawnEntry[])wave.enemies.Clone();

        Array.Sort(sortedEntries,
            (a, b) => a.spawnDelay.CompareTo(b.spawnDelay));

        float previousDelay = 0f;

        foreach (SpawnEntry entry in sortedEntries)
        {
            float wait = entry.spawnDelay - previousDelay;

            if (wait > 0f)
                yield return new WaitForSeconds(wait);

            //원래 배치한 위치에서 몬스터 등장
            entry.enemy.gameObject.SetActive(true);

            previousDelay = entry.spawnDelay;
        }
    }

    //*현재 웨이브의 몬스터 사망 감지*
    private void HandleEnemyDied()
    {
        if (currentWaveIndex < 0) return;
        if (currentWaveIndex >= waves.Length - 1) return;

        remainingEnemies--;

        if (remainingEnemies > 0) return;

        //웨이브의 모든 몬스터를 처치함
        UnsubscribeCurrentWave();

        //다음 웨이브 진행
        StartWave(currentWaveIndex + 1);
    }

    //*현재 웨이브 사망 이벤트 해제*
    private void UnsubscribeCurrentWave()
    {
        if (waves == null) return;
        if (currentWaveIndex < 0) return;
        if (currentWaveIndex >= waves.Length - 1) return;

        Wave wave = waves[currentWaveIndex];

        if (wave == null || wave.enemies == null) return;

        foreach (SpawnEntry entry in wave.enemies)
        {
            if (entry != null && entry.enemy != null)
                entry.enemy.OnDied -= HandleEnemyDied;
        }
    }

    //*Inspector 연결 상태 검사*
    private bool ValidateWaves()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("RoomWaveController: 웨이브가 없습니다.", this);
            return false;
        }

        //같은 몬스터 여러 번 등록 방지
        HashSet<EnemyDeath> registeredEnemies = new HashSet<EnemyDeath>();

        foreach (Wave wave in waves)
        {
            if (wave == null ||
                wave.enemies == null ||
                wave.enemies.Length == 0)
            {
                Debug.LogError("RoomWaveController: 비어 있는 웨이브가 있습니다.", this);
                return false;
            }

            foreach (SpawnEntry entry in wave.enemies)
            {
                if (entry == null || entry.enemy == null)
                {
                    Debug.LogError("RoomWaveController: Enemy 참조가 비어 있습니다.", this);
                    return false;
                }

                if (!registeredEnemies.Add(entry.enemy))
                {
                    Debug.LogError("RoomWaveController: 몬스터가 중복 등록되었습니다.", this);
                    return false;
                }
            }
        }

        return true;
    }
}
