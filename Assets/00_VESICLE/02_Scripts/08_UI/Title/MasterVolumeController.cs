//**마스터 볼륨 조절 및 설정 저장**
//책임: 저장된 볼륨 불러오기 → 슬라이더 초기화 → 볼륨 변경 → 설정 저장
using UnityEngine;
using UnityEngine.UI;

public class MasterVolumeController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Slider masterVolumeSlider;         //마스터 볼륨 슬라이더

    private const string VolumeKey = "MasterVolume";            //PlayerPrefs 저장 키

    private void Awake()
    {
        if (masterVolumeSlider == null)
        {
            Debug.LogError("MasterVolumeController: Slider가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        //이전에 저장한 볼륨 불러오기 (기본값 100%)
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);

        masterVolumeSlider.SetValueWithoutNotify(savedVolume);  //이벤트 호출 없이 슬라이더 초기화
        AudioListener.volume = savedVolume;                     //저장된 볼륨 적용
    }

    //*슬라이더 이벤트 구독*
    private void OnEnable()
    {
        if (masterVolumeSlider == null) return;

        masterVolumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
    }

    //*슬라이더 이벤트 해제 및 설정 저장*
    private void OnDisable()
    {
        if (masterVolumeSlider == null) return;

        masterVolumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
        PlayerPrefs.Save();                                     //변경한 볼륨 설정 저장
    }

    //*마스터 볼륨 변경*
    private void HandleVolumeChanged(float volume)
    {
        AudioListener.volume = volume;                          //전체 게임 볼륨 변경
        PlayerPrefs.SetFloat(VolumeKey, volume);                //변경한 볼륨값 기록
    }
}
