//**게임 일시정지 관리**
//책임: ESC 입력 → 기존 OptionsPanel 표시 → 시간 정지 및 복구 → 타이틀 복귀
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject titleGroup;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button titleMenuButton;

    [Header("Return To Title Fade")]
    [SerializeField] private CanvasGroup returnFadeGroup;               //기존 ContinueFadeOverlay
    [SerializeField, Min(0f)] private float returnFadeDuration = 0.7f;  //암전 시간

    [Header("Player")]
    [SerializeField] private PlayerControlLock controlLock;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "00_Tutorial";

    public bool IsPaused { get; private set; }  //타이틀 메뉴에서 BACK 버튼의 동작을 구분하기 위한 상태
    private bool isReturningToTitle;            //타이틀 복귀 연출 중복 실행 방지

    private void Awake()
    {
        //필수 참조가 빠져 있으면 일시정지 기능 비활성화
        if (titleGroup == null ||
            optionsPanel == null ||
            titleMenuButton == null ||
            controlLock == null ||
            playerHealth == null ||
            returnFadeGroup == null)
        {
            Debug.LogError("PauseMenuController: Inspector 참조를 확인하세요.", this);

            enabled = false;
            return;
        }

        //타이틀의 OPTIONS에서는 TITLE MENU 버튼을 숨김
        titleMenuButton.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (titleMenuButton != null)
            titleMenuButton.onClick.AddListener(ReturnToTitle);
    }

    private void Update()
    {
        //타이틀 암전 중에는 ESC로 일시정지를 해제하지 않음
        if (isReturningToTitle) return;

        //ESC를 새로 누른 순간만 확인
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        //일시정지 상태에서는 ESC로 게임 재개
        if (IsPaused)
        {
            ResumeGame();
            return;
        }

        //타이틀이나 타이틀 OPTIONS에서는 일시정지 불가
        if (titleGroup.activeInHierarchy || optionsPanel.activeInHierarchy) return;

        //플레이어 사망 상태에서는 일시정지 불가
        if (playerHealth.IsDead) return;

        //시작 / 방 전환 / 클리어 등 조작 잠금 중에는 불가
        if (!controlLock.CanAttack) return;

        //피격 슬로모 도중 시간 배율 충돌 방지
        if (!Mathf.Approximately(Time.timeScale, 1f)) return;

        PauseGame();
    }

    //*ESC 일시정지*
    private void PauseGame()
    {
        if (IsPaused) return;

        IsPaused = true;

        //일시정지 중에도 Update는 실행되므로 공격 입력 잠금
        controlLock.LockJump();
        controlLock.LockAttack();
        controlLock.LockThrow();
        controlLock.LockDetonate();

        //기존 OptionsPanel을 일시정지 메뉴로 사용
        titleMenuButton.gameObject.SetActive(true);
        optionsPanel.SetActive(true);

        //물리 및 일반 게임 시간 정지
        Time.timeScale = 0f;
    }

    //*ESC 또는 BACK으로 게임 재개*
    public void ResumeGame()
    {
        if (!IsPaused) return;

        IsPaused = false;

        //일시정지 UI 닫기
        optionsPanel.SetActive(false);
        titleMenuButton.gameObject.SetActive(false);

        //일시정지 시스템이 추가한 잠금만 해제
        controlLock.UnlockJump();
        controlLock.UnlockAttack();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();

        //게임 시간 복구
        Time.timeScale = 1f;
    }


    //*TITLE MENU 버튼 클릭*
    private void ReturnToTitle()
    {
        if (!IsPaused || isReturningToTitle) return;

        StartCoroutine(ReturnToTitleRoutine());
    }

    //*타이틀 복귀 암전 연출*
    private IEnumerator ReturnToTitleRoutine()
    {
        isReturningToTitle = true;

        //타이틀 복귀 암전과 동시에 BGM 전환 시작
        GameAudioController.Instance?.PlayTitleBgm();

        //기존 암전 UI를 투명한 상태에서 시작
        returnFadeGroup.alpha = 0f;
        returnFadeGroup.blocksRaycasts = true;

        //게임이 정지된 상태에서도 암전 진행
        Tween fadeTween = returnFadeGroup
            .DOFade(1f, returnFadeDuration)
            .SetUpdate(true);

        //화면이 완전히 검어질 때까지 대기
        yield return fadeTween.WaitForCompletion();

        //게임 시간과 조작 잠금을 복구
        ResumeGame();

        //체크포인트 데이터는 유지하고 타이틀로 복귀
        SceneManager.LoadScene(titleSceneName);
    }

    private void OnDisable()
    {
        //타이틀 복귀 연출이 중간에 종료된 경우 Tween과 화면 상태 정리
        if (isReturningToTitle && returnFadeGroup != null)
        {
            returnFadeGroup.DOKill();
            returnFadeGroup.alpha = 0f;
            returnFadeGroup.blocksRaycasts = false;
            isReturningToTitle = false;
        }

        if (titleMenuButton != null)
            titleMenuButton.onClick.RemoveListener(ReturnToTitle);

        //씬 종료 등의 상황에서 일시정지가 남지 않도록 복구
        if (IsPaused)
            ResumeGame();
    }
}
