//**플레이어 카메라 추적 처리**
//책임: 플레이어 위치 추적 → Offset 적용 → 부드럽게 CameraRig 이동
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;                      //카메라가 따라갈 대상
    [SerializeField] private Vector2 framingOffset;                 //기본 구도에서 추가로 조절하고 싶은 값
    [SerializeField, Min(0f)] private float smoothTime = 0.12f;     //카메라 부드럽게 따라가는 시간

    private Vector2 initialOffset;                                  //현재 배치된 카메라와 Player 사이의 기본 거리
    private Vector3 followVelocity;                                 //Vector3.SmoothDamp가 내부적으로 사용하는 현재 이동 속도
    private float fixedZ;                                           //CameraRig이 가지고 있어야 하는 원래 Z 위치(Z는 캐릭터를 따라가지 않음)

    private void Awake()
    {
        if (target == null) return;

        fixedZ = transform.position.z;                              //게임 시작 당시 CameraRig의 Z값을 저장

        //현재 Scene에서 배치해둔 CameraRig과 Player 사이의 거리를 자동으로 저장
        initialOffset = new Vector2(transform.position.x - target.position.x, transform.position.y - target.position.y);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        //카메라가 최종적으로 이동해야 할 목표 위치 계산
        Vector3 targetPosition = new Vector3(
            target.position.x + initialOffset.x + framingOffset.x, 
            target.position.y + initialOffset.y + framingOffset.y, 
            fixedZ
            );

        //현재 CameraRig 위치에서 targetPosition까지 부드럽게 이동
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, smoothTime);
    }
}
