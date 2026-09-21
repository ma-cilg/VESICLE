//**풀에서 재사용되는 FX의 재생과 반환 처리**
//책임: FX재생 → 재생시간 확인 → 끝나면 Pool에 반환 요청
using System;           //Action<T> 사용
using UnityEngine;

public class PooledFX : MonoBehaviour
{
    [SerializeField] private Animator animator;             //FX Animator
    [SerializeField] private AnimationClip animationClip;   //FX 재생시간 확인용 Clip
    [SerializeField] private string stateName;              //Animator에서 재생할 State 이름

    private float remainingTime;                            //FX 남은 재생시간
    private bool isPlaying;                                 //현재 FX 재생 중인지 판단

    private Action<PooledFX> returnToPool;                  //FX가 끝났을 때 호출할 반환 메서드 저장

    public void Play(Action<PooledFX> onFinished)
    {
        returnToPool = onFinished;                  //FX가 끝났을 때 실행할 반환 메서드 저장
        remainingTime = animationClip.length;       //실제 Animation Clip 전체 재생시간을 초 단위로 반환
        isPlaying = true;                           //현재 재생 중 상태로 변경
        gameObject.SetActive(true);                 //이전 사용에서 비활성화시 활성화
        animator.Play(stateName, 0, 0f);            //Animator의 지정된 State를 다시 재생
    }

    private void Update()
    {
        if (!isPlaying) return;
        remainingTime -= Time.deltaTime;            //매 프레임 시간 감소
        if (remainingTime > 0f) return;             //애니메이션 시간 남아있으면 계속 재생
        isPlaying = false;                          //재생 종료
        returnToPool?.Invoke(this);                 //Pool의 Return 메서드 호출
    }

    private void OnDisable()
    {
        //다음 재사용 위해 상태 초기화
        isPlaying = false;
        remainingTime = 0f;
        returnToPool = null;
    }
}
