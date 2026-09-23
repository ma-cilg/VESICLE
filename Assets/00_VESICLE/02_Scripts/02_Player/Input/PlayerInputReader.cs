//**플레이어 조작을 읽어 다른 플레이어 시스템에 전달**
//책임: 키보드 → PlayerInputReader → 현재 입력값 전달
using UnityEngine;
using UnityEngine.InputSystem;  //InputActionReference, Input System 사용

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;   //이동
    [SerializeField] private InputActionReference jumpAction;   //점프
    [SerializeField] private InputActionReference dashAction;   //대시
    [SerializeField] private InputActionReference attackAction; //기본 공격

    //프로퍼티
    public Vector2 MoveInput => moveAction.action.ReadValue<Vector2>();

    //*컴포넌트 활성화 중일때만 입력 가능*
    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        dashAction.action.Enable();
        attackAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        dashAction.action.Disable();
        attackAction.action.Disable();
    }

    //*점프버튼이 새롭게 눌렸는지 반환(이번 프레임에)*
    public bool IsJumpPressed()
    {
        return jumpAction.action.WasPressedThisFrame();
    }

    //*대시버튼이 새롭게 눌렸는지 반환(이번 프레임에)*
    public bool IsDashPressed()
    {
        return dashAction.action.WasPressedThisFrame();
    }

    //*공격버튼이 새롭게 눌렸는지 반환(이번 프레임에)*
    public bool IsAttackPressed()
    {
        return attackAction.action.WasPressedThisFrame();
    }
}
