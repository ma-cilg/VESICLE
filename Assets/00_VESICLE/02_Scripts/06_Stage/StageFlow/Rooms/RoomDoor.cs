//**방 출입문 개폐 처리**
//책임: 닫힌 위치 저장 → 문 진동 → 위로 열기 → 아래로 닫기
using System;
using DG.Tweening;
using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    [Header("Door Movement")]
    [SerializeField, Min(0f)] private float openDistance = 3f;                      //문이 위로 올라갈 거리
    [SerializeField, Min(0f)] private float shakeDuration = 0.18f;                  //열리기 전 떨리는 시간
    [SerializeField, Min(0f)] private float shakeStrength = 0.08f;                  //문의 떨림 강도
    [SerializeField, Min(1)] private int shakeVibrato = 14;                         //떨림 횟수
    [SerializeField, Min(0f)] private float moveDuration = 0.65f;                   //문 이동 시간

    private Vector3 closedLocalPosition;                                            //완전히 닫힌 위치
    private Vector3 openLocalPosition;                                              //완전히 열린 위치

    private Sequence doorSequence;                                                  //현재 실행 중인 문 연출

    private bool isOpen;                                                            //현재 문이 열려 있는지
    private bool isMoving;                                                          //현재 문이 움직이고 있는지

    public bool IsOpen => isOpen;
    public bool IsMoving => isMoving;

    public event Action OnOpened;                                                   //완전히 열렸을 때
    public event Action OnClosed;                                                   //완전히 닫혔을 때

    private void Awake()
    {
        //Scene에서 배치한 현재 위치를 닫힌 위치로 사용
        closedLocalPosition = transform.localPosition;

        //닫힌 위치에서 위로 올라간 위치가 열린 위치
        openLocalPosition = closedLocalPosition + Vector3.up * openDistance;

        //모든 문은 기본적으로 닫힌 상태에서 시작
        transform.localPosition = closedLocalPosition;
        isOpen = false;
    }

    //*문 열기*
    [ContextMenu("Test Open Door")]
    public void OpenDoor()
    {
        if (isOpen) return;
        if (isMoving) return;

        isMoving = true;

        //문 진동 + 상승 시간 동안 카메라도 같이 흔들림
        CameraHitShake.Instance?.PlayDoorOpenShake(shakeDuration + moveDuration);

        doorSequence?.Kill();

        doorSequence = DOTween.Sequence();

        //열리기 직전에 문 자체가 잠깐 진동
        doorSequence.Append(transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato));

        //진동이 끝난 뒤 벽 안쪽으로 위로 올라감
        doorSequence.Append(transform.DOLocalMove(openLocalPosition, moveDuration).SetEase(Ease.InOutSine));

        doorSequence.OnComplete(() =>
        {
            transform.localPosition = openLocalPosition;

            isOpen = true;
            isMoving = false;

            doorSequence = null;

            OnOpened?.Invoke();
        });
    }

    //*문 닫기*
    [ContextMenu("Test Close Door")]
    public void CloseDoor()
    {
        if (!isOpen) return;
        if (isMoving) return;

        isMoving = true;

        doorSequence?.Kill();

        doorSequence = DOTween.Sequence();

        //벽 안쪽에서 원래 닫힌 위치까지 내려옴
        doorSequence.Append(transform.DOLocalMove(closedLocalPosition, moveDuration).SetEase(Ease.InOutQuad));

        doorSequence.OnComplete(() =>
        {
            transform.localPosition = closedLocalPosition;

            CameraHitShake.Instance?.PlayDoorCloseImpact();     //문 완전히 닫히는 순간 충격

            isOpen = false;
            isMoving = false;

            doorSequence = null;

            OnClosed?.Invoke();
        });
    }

    private void OnDisable()
    {
        doorSequence?.Kill();
        doorSequence = null;

        isMoving = false;
    }
}