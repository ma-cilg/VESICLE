//**게임 오디오 재생 관리**
//책임: BGM 반복 재생 → SFX 중복 재생 → 각 AudioSource 관리
using DG.Tweening;
using UnityEngine;

public class GameAudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource; //배경음 전용
    [SerializeField] private AudioSource sfxSource; //효과음 전용

    [Header("BGM")]
    [SerializeField] private AudioClip startingBgm; //현재 씬의 시작 BGM
    [SerializeField] private AudioClip normalBgm;   //Room_01~04 BGM
    [SerializeField] private AudioClip bossBgm;     //Room_05 전용 BGM

    [Header("BGM Transition")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.5f;

    [Header("Victory BGM")]
    [SerializeField, Range(0f, 1f)] private float victoryBgmVolumeRatio = 0.2f; //승리 중 BGM 음량 20%
    [SerializeField, Min(0f)] private float victoryBgmLowerDuration = 0.2f;     //BGM을 낮추는 시간

    private float originalBgmVolume; //Inspector에서 설정한 BGM 볼륨
    private Sequence bgmTransition;  //진행 중인 BGM 전환 연출

    public static GameAudioController Instance { get; private set; }    //싱글톤 오디오 관리자

    private void Awake()
    {
        //이미 오디오 관리자가 존재하면 새로 생성된 중복 객체 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        //필수 AudioSource 연결 확인
        if (bgmSource == null || sfxSource == null)
        {
            Debug.LogError("GameAudioController: AudioSource 참조를 확인하세요.", this);

            enabled = false;
            return;
        }

        Instance = this;                            //현재 오브젝트를 게임 전체 오디오 관리자로 등록

        DontDestroyOnLoad(gameObject);              //씬을 다시 불러와도 AudioRoot와 AudioSource 유지

        //BGM 반복 재생
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;

        //SFX는 필요할 때 개별적으로 재생
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        originalBgmVolume = bgmSource.volume;       //기존 BGM AudioSource의 Volume을 기준으로 저장
    }

    private void Start()
    {
        if (Instance != this) return;               //중복 생성된 오디오 관리자는 BGM을 다시 시작하지 않음

        PlayBgm(startingBgm);                       //게임을 처음 실행할 때만 타이틀 BGM 시작
    }

    //*BGM 즉시 재생*
    public void PlayBgm(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;

        //기존 페이드가 진행 중이라면 종료
        bgmTransition?.Kill();
        bgmTransition = null;

        //기본 BGM 볼륨 복구
        bgmSource.volume = originalBgmVolume;

        //이미 같은 BGM을 재생 중이면 다시 시작하지 않음
        if (bgmSource.isPlaying && bgmSource.clip == clip) return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    //*타이틀 BGM 재생 요청*
    public void PlayTitleBgm()
    {
        if (startingBgm == null)
        {
            Debug.LogWarning("GameAudioController: Title BGM이 연결되지 않았습니다.", this);
            return;
        }

        //현재 BGM Fade Out → 타이틀 BGM Fade In
        ChangeBgmWithFade(startingBgm);
    }

    //*타이틀 BGM 즉시 재생*
    //승리 연출 후 완전히 암전된 상태에서 사용
    public void PlayTitleBgmImmediately()
    {
        if (startingBgm == null)
        {
            Debug.LogWarning("GameAudioController: Title BGM이 연결되지 않았습니다.", this);
            return;
        }

        PlayBgm(startingBgm);
    }

    //*일반 전투 BGM 재생 요청*
    public void PlayNormalBgm()
    {
        if (normalBgm == null)
        {
            Debug.LogWarning("GameAudioController: Normal BGM이 연결되지 않았습니다.", this);
            return;
        }

        ChangeBgmWithFade(normalBgm);
    }

    //*보스방 BGM 재생 요청*
    public void PlayBossBgm()
    {
        //보스 BGM이 연결되지 않았다면 오류 안내 후 종료
        if (bossBgm == null)
        {
            Debug.LogWarning("GameAudioController: Boss BGM이 연결되지 않았습니다.", this);
            return;
        }

        //기존 BGM Fade Out → 보스 BGM Fade In
        ChangeBgmWithFade(bossBgm);
    }

    //**BGM 전환 공통 처리**
    //책임: 현재 BGM 감소 → 새 BGM 재생 → 새 BGM 증가
    private void ChangeBgmWithFade(AudioClip nextClip)
    {
        if (nextClip == null || bgmSource == null) return;

        //같은 음악이라면 재생 위치를 유지
        if (bgmSource.isPlaying && bgmSource.clip == nextClip) return;

        //이전 전환 연출 정리
        bgmTransition?.Kill();
        bgmTransition = null;

        bgmTransition = DOTween.Sequence();

        //1. 기존 BGM의 볼륨을 0까지 줄임
        bgmTransition.Append(bgmSource.DOFade(0f, fadeOutDuration));

        //2. 음량 0이 된 뒤 음악 교체
        bgmTransition.AppendCallback(() =>
        {
            bgmSource.Stop();
            bgmSource.clip = nextClip;
            bgmSource.volume = 0f;
            bgmSource.Play();
        });

        //3. 새 BGM을 기존 볼륨까지 증가
        bgmTransition.Append(bgmSource.DOFade(originalBgmVolume, fadeInDuration));

        //Time.timeScale과 관계없이 전환 가능
        bgmTransition.SetUpdate(true);

        bgmTransition.OnComplete(() =>
        {
            bgmTransition = null;
        });
    }

    //*BGM 즉시 정지*
    public void StopBgm()
    {
        if (bgmSource == null) return;

        //진행 중인 BGM 페이드가 있다면 중단
        bgmTransition?.Kill();
        bgmTransition = null;

        //BGM을 서서히 줄이지 않고 즉시 정지
        bgmSource.Stop();

        //다음 BGM 재생을 위해 기본 음량 복구
        bgmSource.volume = originalBgmVolume;
    }

    //*승리 연출 시작 시 BGM 음량 낮추기*
    public void LowerBgmForVictory()
    {
        if (bgmSource == null || !bgmSource.isPlaying) return;

        //기존 BGM 전환이 남아 있다면 정리
        bgmTransition?.Kill();
        bgmTransition = null;

        //기본 볼륨의 20%를 목표 음량으로 사용
        float targetVolume = originalBgmVolume * victoryBgmVolumeRatio;

        bgmTransition = DOTween.Sequence();

        bgmTransition.Append(bgmSource.DOFade(targetVolume, victoryBgmLowerDuration));

        //게임 시간과 무관하게 음량 조절
        bgmTransition.SetUpdate(true);

        bgmTransition.OnComplete(() =>
        {
            bgmTransition = null;
        });
    }

    //*승리 암전 중 BGM 서서히 종료*
    public void FadeOutVictoryBgm(float duration)
    {
        if (bgmSource == null || !bgmSource.isPlaying) return;

        bgmTransition?.Kill();
        bgmTransition = null;

        bgmTransition = DOTween.Sequence();

        //현재 낮아진 음량에서 0까지 감소
        bgmTransition.Append(bgmSource.DOFade(0f, Mathf.Max(0f, duration)));

        bgmTransition.SetUpdate(true);

        bgmTransition.OnComplete(() =>
        {
            //완전히 작아진 뒤 실제 BGM 정지
            bgmSource.Stop();
            bgmSource.volume = originalBgmVolume;
            bgmTransition = null;
        });
    }

    //*SFX 재생*
    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;

        //PlayOneShot은 다른 효과음이 재생 중이어도 겹쳐 재생 가능
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void OnDestroy()
    {
        //오디오 관리자가 파괴될 때 진행 중인 Tween 정리
        bgmTransition?.Kill();
        bgmTransition = null;

        //실제 오디오 관리자가 파괴될 때만 참조 초기화
        if (Instance == this)
        {
            Instance = null;
        }
    }

}
