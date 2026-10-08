//**게임 진행 세이브 관리**
//책임: 현재 Stage / 재시작할 Room 저장 → 불러오기 → 최종 클리어 시 세이브 삭제
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

//저장 파일 안에 실제로 기록될 진행 데이터
[Serializable]
public class GameSaveData
{
    public int stageIndex;      //현재 진행 중인 Stage 번호
    public int roomIndex;       //죽거나 Continue 했을 때 시작할 Room 번호
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private GameSaveData currentSaveData;
    private string saveFilePath;

    //Continue 버튼 활성화 판단용
    public bool HasSave => File.Exists(saveFilePath);

    //현재 저장된 진행 정보
    public GameSaveData CurrentSaveData => currentSaveData;

    private void Awake()
    {
        //SaveManager는 게임 전체에서 하나만 유지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //PC마다 Unity가 제공하는 안전한 저장 폴더 사용
        saveFilePath = Path.Combine(Application.persistentDataPath, "vesicle_save.json");

        LoadSave();
    }

    //*현재 Stage의 시작 지점을 저장*
    public void SaveStageStart(int stageIndex)
    {
        SaveCheckpoint(stageIndex, 0);
    }

    //*방 클리어 후 다음에 시작할 Room 저장*
    public void SaveCheckpoint(int stageIndex, int roomIndex)
    {
        currentSaveData = new GameSaveData
        {
            stageIndex = stageIndex,
            roomIndex = roomIndex
        };

        string json = JsonUtility.ToJson(currentSaveData, true);

        File.WriteAllText(saveFilePath, json);
    }

    //*기존 세이브 불러오기*
    public bool LoadSave()
    {
        if (!File.Exists(saveFilePath))
        {
            currentSaveData = null;
            return false;
        }

        string json = File.ReadAllText(saveFilePath);

        currentSaveData = JsonUtility.FromJson<GameSaveData>(json);

        return currentSaveData != null;
    }

    //*New Game 시작 시 새 진행 데이터 생성*
    public void StartNewGame()
    {
        //새 게임은 Stage 0 / Room 0부터 시작
        SaveCheckpoint(0, 0);
    }

    //*최종 보스 클리어 후 모든 진행 데이터 삭제*
    public void DeleteSave()
    {
        currentSaveData = null;

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
    }

    //*개발 중 전체 진행도 초기화 테스트*
    [ContextMenu("Test Reset All Progress")]
    private void TestResetAllProgress()
    {
        DeleteSave();                   //현재 저장 파일과 메모리의 세이브 데이터 삭제

        //Play 중이면 현재 Scene도 다시 불러와 모든 방 / 적 / 플레이어 런타임 상태까지 함께 초기화
        if (Application.isPlaying)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}