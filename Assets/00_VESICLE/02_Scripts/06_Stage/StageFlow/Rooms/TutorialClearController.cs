//**튜토리얼 클리어 연출**
//책임: 정화 완료 감지 → 플레이어 조작 정지 → 타자 효과 → 암전 → 씬 전환
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialClearController : MonoBehaviour
{
    [Header("Room")]
    [SerializeField] private CombatRoom bossRoom;

    [Header("Player")]
    [SerializeField] private PlayerControlLock controlLock;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Header("Clear UI")]
    [SerializeField] private TMP_Text clearText;
    [SerializeField] private CanvasGroup clearGroup;
    [SerializeField] private CanvasGroup blackFadeGroup;

    [Header("Text")]
    [SerializeField, TextArea(2, 3)]
    private string clearMessage = "TUTORIAL\nCLEAR";

    [SerializeField, Min(0.01f)] private float letterInterval = 0.08f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float clearHoldDuration = 1f;

    [SerializeField, Min(0f)]
    private float blackFadeDuration = 0.8f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "00_Tutorial";

    private bool isClearing;
    private bool controlsLocked;
    private bool physicsFrozen;
    private RigidbodyConstraints2D originalConstraints;
    private Coroutine clearRoutine;

    private void Awake()
    {
        //게임 시작 시 글자와 암전 모두 숨김
        if (clearText != null)
        {
            clearText.text = clearMessage;
            clearText.maxVisibleCharacters = 0;
        }

        if (clearGroup != null)
        {
            clearGroup.alpha = 0f;
            clearGroup.blocksRaycasts = false;
        }

        if (blackFadeGroup != null)
        {
            blackFadeGroup.alpha = 0f;
            blackFadeGroup.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        if (bossRoom != null)
            bossRoom.OnPurified += HandlePurified;
    }

    private void OnDisable()
    {
        if (bossRoom != null)
            bossRoom.OnPurified -= HandlePurified;

        if (clearRoutine != null)
        {
            StopCoroutine(clearRoutine);
            clearRoutine = null;
        }

        blackFadeGroup?.DOKill();
        RestorePlayer();
    }

    //*방 정화가 완전히 끝난 순간*
    private void HandlePurified()
    {
        if (isClearing) return;

        if (clearText == null || clearGroup == null ||
            blackFadeGroup == null || controlLock == null ||
            playerRigidbody == null)
        {
            Debug.LogError("TutorialClearController: Inspector 참조를 확인하세요.", this);
            return;
        }

        isClearing = true;

        LockPlayer();
        clearRoutine = StartCoroutine(PlayClearRoutine());
    }

    //*플레이어 전체 조작 및 물리 이동 정지*
    private void LockPlayer()
    {
        controlsLocked = true;

        controlLock.LockMovement();
        controlLock.LockJump();
        controlLock.LockAttack();
        controlLock.LockThrow();
        controlLock.LockDetonate();

        //진행 중이던 이동 공격 종료
        playerAttack?.InterruptAttack();

        //움직임과 중력을 포함한 물리 위치 고정
        originalConstraints = playerRigidbody.constraints;
        physicsFrozen = true;

        playerRigidbody.linearVelocity = Vector2.zero;
        playerRigidbody.angularVelocity = 0f;
        playerRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    //*타자 효과 → 1초 대기 → 암전 → 씬 전환*
    private IEnumerator PlayClearRoutine()
    {
        clearText.text = clearMessage;
        clearText.maxVisibleCharacters = 0;
        clearText.ForceMeshUpdate();

        int characterCount = clearText.textInfo.characterCount;

        clearGroup.alpha = 1f;

        //전체 글자 배치는 유지하고 보이는 글자 수만 증가
        for (int i = 1; i <= characterCount; i++)
        {
            clearText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(letterInterval);
        }


        //모든 글자가 나온 뒤 1초 유지
        yield return new WaitForSecondsRealtime(clearHoldDuration);

        //마지막 글자부터 하나씩 지우기 (백스페이스 효과)
        for (int i = characterCount - 1; i >= 0; i--)
        {
            clearText.maxVisibleCharacters = i;

            yield return new WaitForSecondsRealtime(letterInterval);
        }

        //모든 글자가 사라진 뒤 검은 화면으로 암전
        blackFadeGroup.blocksRaycasts = true;


        Tween fadeTween = blackFadeGroup
            .DOFade(1f, blackFadeDuration)
            .SetUpdate(true);

        yield return fadeTween.WaitForCompletion();

        Time.timeScale = 1f;

        //지금은 타이틀, 나중에는 다음 스테이지로 변경 가능
        SceneManager.LoadScene(nextSceneName);
    }

    //*중단 또는 씬 전환 시 자신이 변경한 상태 복구*
    private void RestorePlayer()
    {
        if (physicsFrozen && playerRigidbody != null)
        {
            playerRigidbody.constraints = originalConstraints;
            physicsFrozen = false;
        }

        if (!controlsLocked || controlLock == null) return;

        controlsLocked = false;

        controlLock.UnlockMovement();
        controlLock.UnlockJump();
        controlLock.UnlockAttack();
        controlLock.UnlockThrow();
        controlLock.UnlockDetonate();
    }
}
