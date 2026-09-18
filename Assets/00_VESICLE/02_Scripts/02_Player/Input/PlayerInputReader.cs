//**플레이어 이동 입력을 읽어 다른 플레이어 시스템에 전달**
//책임: 키보드 → PlayerInputReader → 현재 입력값 전달
using UnityEngine;
using UnityEngine.InputSystem;  //InputActionReference, Input System 사용

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;

    //프로퍼티
    public Vector2 MoveInput => moveAction.action.ReadValue<Vector2>();

    //컴포넌트 활성화 중일때만 입력 가능
    private void OnEnable()
    {
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
    }
}
