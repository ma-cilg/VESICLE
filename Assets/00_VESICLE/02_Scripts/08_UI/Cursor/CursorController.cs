//**마우스 커서 색상 및 클릭 상태 관리**
//책임: 기본 커서 표시 → 클릭 시 색상 변경 → 클릭 해제 시 원래 색상 복구 → 생성한 텍스처 정리
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorController : MonoBehaviour
{
    [Header("Cursor")]
    [SerializeField] private Texture2D whiteCursor;                 //기본 흰색 Crosshairs
    [SerializeField] private Vector2 hotspot = new Vector2(7f, 8f); //조준점 중앙

    //클릭 중 사용할 초록색 (#55D68B)
    private static readonly Color32 pressedColor = new Color32(85, 214, 139, 255);

    private Texture2D greenCursor;                                  //원본을 복제하여 만든 초록색 커서
    private bool wasPressed;                                        //이전 프레임의 마우스 클릭 상태

    private void Awake()
    {
        //원본 이미지가 연결되지 않았다면 작동 중단
        if (whiteCursor == null)
        {
            Debug.LogError("CursorController: White Cursor가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        //원본을 수정하지 않고 실행 중에만 복사본 생성
        greenCursor = Instantiate(whiteCursor);
        greenCursor.name = "Crosshairs_Green_Runtime";

        Color32[] pixels = greenCursor.GetPixels32();               //복사한 이미지의 픽셀 색상 가져오기

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];

            // 원본 밝기를 유지하면서 초록색으로 변환
            // 투명도(alpha)는 그대로 유지
            pixels[i] = new Color32(
                (byte)(pixel.r * pressedColor.r / 255),
                (byte)(pixel.g * pressedColor.g / 255),
                (byte)(pixel.b * pressedColor.b / 255),
                pixel.a
                );
        }

        //초록색 픽셀을 복사본에 적용
        greenCursor.SetPixels32(pixels);
        greenCursor.Apply(false, false);

        //게임 시작 시에는 흰색 커서 표시
        Cursor.SetCursor(whiteCursor, hotspot, CursorMode.Auto);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;                                //마우스가 연결되지 않았다면 처리하지 않음

        if (mouse == null || greenCursor == null) return;

        bool isPressed = mouse.leftButton.isPressed;                //현재 왼쪽 마우스 버튼을 누르고 있는지 확인

        if (isPressed == wasPressed) return;                        //클릭 상태가 바뀌지 않았다면 커서를 다시 설정하지 않음

        wasPressed = isPressed;

        //누르는 동안 초록색, 떼면 흰색
        Cursor.SetCursor(isPressed ? greenCursor : whiteCursor, hotspot, CursorMode.Auto);
    }

    private void OnDisable()
    {
        //스크립트가 비활성화되면 흰색으로 복구
        if (whiteCursor != null)
        {
            Cursor.SetCursor(whiteCursor, hotspot, CursorMode.Auto);
        }
    }

    private void OnDestroy()
    {
        //실행 중 생성한 텍스처만 메모리에서 정리
        if (greenCursor != null)
        {
            Destroy(greenCursor);
        }           
    }
}
