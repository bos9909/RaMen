using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class BowlCameraController : MonoBehaviour
{
    [Header("라면 그릇 연결")]
    [SerializeField] private BowlBoundary bowlBoundary;
    [SerializeField] private float bowlRadiusFallback = 4.2f;

    [Header("줌 설정")]
    [Tooltip("최대 줌인 시 카메라 크기 (작을수록 극도로 확대됨)")]
    [SerializeField] private float minOrthoSize = 1.0f;
    [Tooltip("줌인/줌아웃 속도 감도")]
    [SerializeField] private float zoomSensitivity = 0.6f;
    [Tooltip("줌 및 카메라 이동의 부드러운 속도")]
    [SerializeField] private float smoothSpeed = 12f;
    [Tooltip("최대 줌아웃 시 그릇 바깥 여백 비율 (0.08 = 8% 여백)")]
    [SerializeField] private float bowlPadding = 0.08f;

    private Camera cam;
    private float targetOrthoSize;
    private Vector3 targetPosition;
    private float maxOrthoSize;
    private float currentBowlRadius;
    private Vector2 lastMouseScreenPos;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetPosition = transform.position;
    }

    private void Start()
    {
        // 그릇의 실제 반지름 가져오기
        currentBowlRadius = (bowlBoundary != null) ? bowlBoundary.BowlRadius : bowlRadiusFallback;

        CalculateMaxZoomOut();

        // 게임 시작 시 그릇 전체 풀 뷰로 시작
        targetOrthoSize = maxOrthoSize;
        cam.orthographicSize = maxOrthoSize;
        targetPosition = new Vector3(0f, 0f, -10f);
        transform.position = targetPosition;
    }

    public void CalculateMaxZoomOut()
    {
        float aspect = cam.aspect;
        float sizeForHeight = currentBowlRadius;
        float sizeForWidth = currentBowlRadius / aspect;

        maxOrthoSize = Mathf.Max(sizeForHeight, sizeForWidth) * (1.0f + bowlPadding);
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // 1. 휠 스크롤 줌 (Ctrl 미입력 시 작동)
        bool isCtrlPressed = Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;
        float scrollY = Mouse.current.scroll.ReadValue().y;

        if (!isCtrlPressed && Mathf.Abs(scrollY) > 0.01f)
        {
            ZoomTowardsCursor(Mathf.Sign(scrollY));
        }

        // 2. 휠 클릭 드래그: 화면 자유 이동 (패닝)
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            lastMouseScreenPos = Mouse.current.position.ReadValue();
        }
        else if (Mouse.current.middleButton.isPressed)
        {
            Vector2 currentMouseScreen = Mouse.current.position.ReadValue();
            Vector2 deltaScreen = currentMouseScreen - lastMouseScreenPos;

            // 1:1 손에 붙는 느낌으로 월드 이동량 변환
            Vector3 deltaWorld = new Vector3(-deltaScreen.x, -deltaScreen.y, 0f) * (cam.orthographicSize * 2f / Screen.height);
            targetPosition += deltaWorld;
            lastMouseScreenPos = currentMouseScreen;
        }

        // 3. 줌아웃이 90% 이상 풀렸을 때는 서서히 그릇 중앙(0,0)으로 복귀
        float zoomProgress = Mathf.InverseLerp(maxOrthoSize, minOrthoSize, targetOrthoSize); // 0(줌아웃) ~ 1(최대줌인)
        if (zoomProgress < 0.1f)
        {
            targetPosition.x = Mathf.Lerp(targetPosition.x, 0f, Time.deltaTime * 6f);
            targetPosition.y = Mathf.Lerp(targetPosition.y, 0f, Time.deltaTime * 6f);
        }

        // 4. [핵심 수정] 그릇 가장자리 끝까지 시원하게 둘러볼 수 있도록 이동 반경 제한 완화
        ClampCameraPosition(zoomProgress);

        // 5. 부드러운 최종 보간 적용
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, Time.deltaTime * smoothSpeed);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
    }

    private void ZoomTowardsCursor(float scrollDirection)
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldBefore = cam.ScreenToWorldPoint(mouseScreenPos);

        float oldSize = targetOrthoSize;
        float newSize = targetOrthoSize - (scrollDirection * zoomSensitivity);
        newSize = Mathf.Clamp(newSize, minOrthoSize, maxOrthoSize);

        if (Mathf.Approximately(oldSize, newSize)) return;

        targetOrthoSize = newSize;

        // 마우스 커서 지점이 화면의 제자리를 유지하도록 앵커 이동
        float sizeRatio = newSize / oldSize;
        Vector3 camToCursor = mouseWorldBefore - targetPosition;
        targetPosition = mouseWorldBefore - (camToCursor * sizeRatio);
        targetPosition.z = -10f;
    }

    private void ClampCameraPosition(float zoomProgress)
    {
        // 줌인을 많이 할수록 카메라가 그릇 테두리 끝(currentBowlRadius)까지 자유롭게 이동 가능!
        float maxAllowedPan = Mathf.Lerp(0f, currentBowlRadius, zoomProgress);

        Vector2 clampedXY = Vector2.ClampMagnitude(new Vector2(targetPosition.x, targetPosition.y), maxAllowedPan);
        targetPosition.x = clampedXY.x;
        targetPosition.y = clampedXY.y;
        targetPosition.z = -10f;
    }
}