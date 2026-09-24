//**플레이어 조작을 읽어 다른 플레이어 시스템에 전달**
//책임: 키보드 → PlayerInputReader → 현재 입력값 전달
using UnityEngine;
using UnityEngine.InputSystem;  //InputActionReference, Input System 사용

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;   //이동
    [SerializeField] private InputActionReference jumpAction;   //Space키 점프
    [SerializeField] private InputActionReference jumpWAction;  //W키 점프
    [SerializeField] private InputActionReference dashAction;   //대시
    [SerializeField] private InputActionReference attackAction; //기본 공격
    [SerializeField] private InputActionReference throwAction;  //검 투척

    //프로퍼티
    public Vector2 MoveInput => moveAction.action.ReadValue<Vector2>();

    //*컴포넌트 활성화 중일때만 입력 가능*
    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        jumpWAction.action.Enable();
        dashAction.action.Enable();
        attackAction.action.Enable();
        throwAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        jumpWAction.action.Disable();
        dashAction.action.Disable();
        attackAction.action.Disable();
        throwAction.action.Disable();
    }

    //*점프버튼이 새롭게 눌렸는지 반환*
    public bool IsJumpPressed()
    {
        return jumpAction.action.WasPressedThisFrame() || jumpWAction.action.WasPressedThisFrame();
    }

    //*대시버튼이 새롭게 눌렸는지 반환*
    public bool IsDashPressed()
    {
        return dashAction.action.WasPressedThisFrame();
    }

    //*공격버튼이 새롭게 눌렸는지 반환*
    public bool IsAttackPressed()
    {
        return attackAction.action.WasPressedThisFrame();
    }

    //*공격 버튼을 현재 계속 누르고 있는지 확인*
    public bool IsAttackHeld()
    {
        return attackAction.action.IsPressed();
    }

    //*공격 버튼을 이번 프레임에 뗐는지 확인*
    public bool IsAttackReleased()
    {
        return attackAction.action.WasReleasedThisFrame();
    }

    //*투척 버튼이 새롭게 눌렸는지 반환*
    public bool IsThrowPressed()
    {
        return throwAction.action.WasPressedThisFrame();
    }
}
