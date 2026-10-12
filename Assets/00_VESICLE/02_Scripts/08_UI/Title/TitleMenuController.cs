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

    [SerializeField, Min(0f)]
    private float titleFadeDuration = 0.65f;

    [SerializeField, Min(0f)]
    private float standUpDuration = 0.8f;

    private bool isStarting;
    private bool isRespawnReload;

    private void Awake()
    {
        //PlayerRespawn이 사망 플래그를 지우기 전에 확인
        isRespawnReload = PlayerRespawn.IsRespawnReload;
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
        if (isStarting) return;
        if (SaveManager.Instance == null) return;
        if (!SaveManager.Instance.HasSave) return;

        //암전 + StageFlowController 복구 연결 예정
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
