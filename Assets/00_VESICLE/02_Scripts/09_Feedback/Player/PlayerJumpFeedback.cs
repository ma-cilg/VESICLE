//**플레이어 점프, 2단 점프, 착지 시각 효과 처리**
//책임: PlayerJump 이벤트 → 해당 FX 생성
using UnityEngine;

public class PlayerJumpFeedback : MonoBehaviour
{
    [SerializeField] private PlayerJump playerJump;         //점프 이벤트를 발생시키는 PlayerJump
    [SerializeField] private Transform groundCheck;         //점프와 착지 FX가 나올 위치

    [SerializeField] private GameObject jumpDustPrefab;     //바닥 점프 FX
    [SerializeField] private GameObject doubleJumpPrefab;   //2단 점프 FX
    [SerializeField] private GameObject landPrefab;         //착지 FX

    //*이벤트 구독*
    private void OnEnable()
    {
        playerJump.OnJumped += PlayJumpDust;
        playerJump.OnDoubleJumped += PlayDoubleJumpFX;
        playerJump.OnLanded += PlayLandFX;
    }

    //*이벤트 구독 해제*
    private void OnDisable()
    {
        playerJump.OnJumped -= PlayJumpDust;
        playerJump.OnDoubleJumped -= PlayDoubleJumpFX;
        playerJump.OnLanded -= PlayLandFX;
    }

    //*바닥 점프 FX*
    private void PlayJumpDust()
    {
        Instantiate(jumpDustPrefab, groundCheck.position, Quaternion.identity);
    }

    //*2단 점프 FX*
    private void PlayDoubleJumpFX()
    {
        Instantiate(doubleJumpPrefab, transform.position, Quaternion.identity);
    }

    //*착지 FX*
    private void PlayLandFX()
    {
        Instantiate(landPrefab, groundCheck.position, Quaternion.identity);
    }
}
