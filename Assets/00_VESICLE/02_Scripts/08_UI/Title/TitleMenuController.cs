//**타이틀 메뉴 UI 제어**
//책임: New Game 시작 연출 / Continue 활성화 / Options / Exit
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TitleMenuController : MonoBehaviour
{
    [Header("Groups")]
    [SerializeField] private GameObject titleGroup;
    [SerializeField] private CanvasGroup titleCanvasGroup;
    [SerializeField] private GameObject optionsPanel;

    [Header("Title Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;

    [Header("Options")]
    [SerializeField] private Button backButton;

    [Header("Tutorial Start")]
    [SerializeField] private TutorialStartController tutorialStart;
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private CombatRoom firstRoom;

    [Header("Continue")]
    [SerializeField] private StageFlowController stageFlowController;       //저장된 Room 및 Player / Camera 복구
    [SerializeField] private CanvasGroup continueFadeCanvasGroup;           //화면 전체 암전 연출
    [SerializeField, Min(0f)] private float continueFadeDuration = 0.45f;   //암전 및 화면 복구 시간

    [SerializeField, Min(0f)] private float titleFadeDuration = 0.65f;
    [SerializeField, Min(0f)] private float standUpDuration = 0.8f;

    private bool isStarting;
    private bool isRespawnReload;

    private void Awake()
    {
        //PlayerRespawn이 사망 플래그를 지우기 전에 확인
        isRespawnReload = PlayerRespawn.IsRespawnReload;

        //게임 시작 시 암전 화면 안 보이게 초기화
        if (continueFadeCanvasGroup != null)
        {
            continueFadeCanvasGroup.alpha = 0f;
            continueFadeCanvasGroup.interactable = false;
            continueFadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        newGameButton.onClick.AddListener(HandleNewGame);
        continueButton.onClick.AddListener(HandleContinue);
        optionsButton.onClick.AddListener(ShowOptions);
        exitButton.onClick.AddListener(ExitGame);
        backButton.onClick.AddListener(ShowTitle);
    }

    private void OnDisable()
    {
        newGameButton.onClick.RemoveListener(HandleNewGame);
        continueButton.onClick.RemoveListener(HandleContinue);
        optionsButton.onClick.RemoveListener(ShowOptions);
        exitButton.onClick.RemoveListener(ExitGame);
        backButton.onClick.RemoveListener(ShowTitle);

        titleCanvasGroup.DOKill();
        continueFadeCanvasGroup?.DOKill();                      //이어하기 암전 Tween 정리
    }

    private void Start()
    {
        //사망 후 Scene Reload에서는 타이틀 UI 표시 안 함
        if (isRespawnReload)
        {
            titleGroup.SetActive(false);
            optionsPanel.SetActive(false);
            return;
        }

        ShowTitle();
        RefreshContinueButton();
    }

    //*Continue 활성화 상태 확인*
    private void RefreshContinueButton()
    {
        continueButton.interactable = SaveManager.Instance != null && SaveManager.Instance.HasSave;
    }

    //*메인 메뉴 표시*
    private void ShowTitle()
    {
        titleGroup.SetActive(true);
        optionsPanel.SetActive(false);

        titleCanvasGroup.alpha = 1f;
        titleCanvasGroup.interactable = true;
        titleCanvasGroup.blocksRaycasts = true;

        RefreshContinueButton();
    }

    //*옵션 화면 표시*
    private void ShowOptions()
    {
        titleGroup.SetActive(false);
        optionsPanel.SetActive(true);
    }

    //*새 게임 선택*
    private void HandleNewGame()
    {
        if (isStarting) return;
        if (SaveManager.Instance == null) return;

        StartCoroutine(NewGameRoutine());
    }

    //*NEW GAME 시작 연출*
    private IEnumerator NewGameRoutine()
    {
        isStarting = true;

        //기존 진행도 덮어쓰기 → Stage 0 / Room 0 저장
        SaveManager.Instance.StartNewGame();

        //연출 중 버튼 중복 입력 방지
        titleCanvasGroup.interactable = false;
        titleCanvasGroup.blocksRaycasts = false;

        //타이틀 전체 서서히 사라짐
        Tween fadeTween = titleCanvasGroup
            .DOFade(0f, titleFadeDuration)
            .SetUpdate(true);

        yield return fadeTween.WaitForCompletion();

        titleGroup.SetActive(false);

        //앉아 있던 Player 일어나기 시작
        playerAnimation.PlayTutorialStandUp();

        //StandUp 재생 시간만큼 대기
        yield return new WaitForSeconds(standUpDuration);

        //일어나는 모션 종료 → Idle
        playerAnimation.FinishTutorialStandUp();

        //Room_01 오른쪽 문 + Room_02 왼쪽 문 동시 개방
        firstRoom.OpenPassageWithoutClear();

        //오른쪽 문이 완전히 열릴 때까지 기다림
        if (firstRoom.ExitDoor != null)
        {
            yield return new WaitUntil(() => firstRoom.ExitDoor.IsOpen);
        }

        //문이 열린 뒤 플레이어 조작 허용
        tutorialStart.ReleaseTutorialControls();

        isStarting = false;
    }

    //*이어하기*
    private void HandleContinue()
    {
        if (isStarting) return;                                 //이미 시작 연출 중이면 중복 실행 방지
        if (SaveManager.Instance == null) return;               //SaveManager가 없다면 실행하지 않음
        if (!SaveManager.Instance.HasSave) return;              //저장된 진행도가 없다면 실행하지 않음

        //필요한 컴포넌트가 연결되지 않았다면 실행하지 않음
        if (stageFlowController == null || continueFadeCanvasGroup == null)
        {
            Debug.LogError("TitleMenuController: Continue 관련 컴포넌트가 연결되지 않았습니다.", this);
            return;
        }

        StartCoroutine(ContinueRoutine());                      //이어하기 암전 및 복구 시작
    }

    //*CONTINUE 암전 및 저장된 체크포인트 복구*
    private IEnumerator ContinueRoutine()
    {
        isStarting = true;                                      //다른 메뉴 버튼의 중복 실행 방지

        titleCanvasGroup.interactable = false;                  //이어하기 중 메뉴 입력 차단
        titleCanvasGroup.blocksRaycasts = false;

        continueFadeCanvasGroup.alpha = 0f;                     //투명한 상태에서 시작
        continueFadeCanvasGroup.blocksRaycasts = true;          //암전 중 다른 UI 클릭 방지

        //검은 화면이 서서히 나타남
        Tween fadeOutTween = continueFadeCanvasGroup
            .DOFade(1f, continueFadeDuration)
            .SetUpdate(true);

        yield return fadeOutTween.WaitForCompletion();

        //화면이 완전히 검어졌으므로 타이틀 UI 숨기기
        titleGroup.SetActive(false);
        optionsPanel.SetActive(false);

        stageFlowController.ContinueFromSave();                 //저장된 체크포인트의 Player / Camera / Room 상태 복구
        playerAnimation.FinishTutorialStandUp();                //타이틀에서 멈춰둔 Animator를 다시 실행하고 Idle로 전환

        //튜토리얼 첫 방이라면 출구 문이 완전히 열릴 때까지 대기
        GameSaveData saveData = SaveManager.Instance.CurrentSaveData;

        if (saveData != null &&
            saveData.stageIndex == 0 &&
            saveData.roomIndex == 0 &&
            firstRoom != null &&
            firstRoom.ExitDoor != null)
        {
            yield return new WaitUntil(() => firstRoom.ExitDoor.IsOpen);
        }

        //복구가 끝났으므로 검은 화면을 서서히 제거
        Tween fadeInTween = continueFadeCanvasGroup
            .DOFade(0f, continueFadeDuration)
            .SetUpdate(true);

        yield return fadeInTween.WaitForCompletion();

        continueFadeCanvasGroup.blocksRaycasts = false;         //게임 화면 입력 차단 해제

        tutorialStart.ReleaseTutorialControls();                //화면이 나타난 뒤 플레이어 조작 허용

        isStarting = false;                                     //이어하기 연출 완료
    }

    //*게임 종료*
    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
