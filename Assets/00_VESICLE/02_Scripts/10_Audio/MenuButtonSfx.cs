//**메뉴 버튼 효과음**
//책임: 버튼 Hover / Click 이벤트 → 해당 SFX 재생
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class MenuButtonSfx : MonoBehaviour, IPointerEnterHandler
{
    [Header("Hover SFX")]
    [SerializeField] private AudioClip hoverSfx;                        //마우스 진입 효과음
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.3f;

    [Header("Click SFX")]
    [SerializeField] private AudioClip clickSfx;                        //버튼 클릭 효과음
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.5f;

    private Button button;

    private void Awake()
    {
        //같은 오브젝트의 Button 컴포넌트 참조
        button = GetComponent<Button>();
    }

    //*버튼 클릭 이벤트 구독*
    private void OnEnable()
    {
        button.onClick.AddListener(PlayClickSfx);
    }

    //*버튼 클릭 이벤트 구독 해제*
    private void OnDisable()
    {
        if (button == null) return;

        button.onClick.RemoveListener(PlayClickSfx);
    }

    //*마우스가 버튼 영역에 진입한 순간*
    public void OnPointerEnter(PointerEventData eventData)
    {
        //비활성화 또는 클릭 불가능한 버튼은 무시
        if (!button.IsActive() || !button.IsInteractable()) return;

        PlaySfx(hoverSfx, hoverVolume);
    }

    //*버튼 클릭 효과음*
    private void PlayClickSfx()
    {
        PlaySfx(clickSfx, clickVolume);
    }

    //*공통 효과음 재생*
    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null) return;

        GameAudioController.Instance?.PlaySfx(clip, volume);
    }
}
