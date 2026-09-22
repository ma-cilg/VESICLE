//**플레이어 무적 상태 관리**
//책임: 여러 시스템의 무적 요청을 받아 현재 무적 여부 제공
using UnityEngine;

public class PlayerInvincibility : MonoBehaviour
{
    private int invincibilityCount;    //현재 활성화된 무적 요청 개수

    public bool IsInvincible => invincibilityCount > 0; //하나라도 있으면 무적

    //*무적 시작*
    public void AddInvincibility()
    {
        invincibilityCount++;
    }

    //*무적 종료*
    public void RemoveInvincibility()
    {
        invincibilityCount = Mathf.Max(0, invincibilityCount - 1);  //0보다 작아지지 않도록 방지
    }
}
