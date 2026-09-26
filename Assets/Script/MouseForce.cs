using UnityEngine;
using UnityEngine.InputSystem;
using MoreMountains.Feedbacks;

public enum DropInteractionMode
{
    None,   // 기본 밀기
    Merge,  // 왼클릭: 완전 융합
    Stick   // 오른클릭: 접착 결합
}

public class MouseForce : MonoBehaviour
{
    public static DropInteractionMode CurrentMode { get; private set; } = DropInteractionMode.None;

    [Header("밀어내기 설정")]
    [Tooltip("마우스 영향 반경 (기존 2.0 -> 3.0 ~ 3.5 추천)")]
    public float influenceRadius = 3.5f; 
    public float repulsionForce = 15.0f;
    public float swipeForceMultiplier = 0.5f;

    [Header("마우스 휠 크기 조절")]
    [Tooltip("정밀 조작용 최소 크기")]
    [SerializeField] private float minInfluenceRadius = 0.5f;

    [Tooltip("대량 이동용 최대 크기")]
    [SerializeField] private float maxInfluenceRadius = 4.5f;

    [Tooltip("휠 1칸 돌릴 때 커지고 작아지는 단계 크기")]
    [SerializeField] private float wheelStep = 0.25f;
    
    [Header("거리 감쇄 완화 설정")]
    [Tooltip("가장자리 테두리 끝에 닿았을 때도 유지할 최소 힘 비율 (0.3 = 끝에서도 30% 힘 보장)")]
    [Range(0f, 0.8f)] public float minEdgeStrength = 0.35f;

    [Tooltip("거리 감쇄 곡선 (1 = 직선 감쇄, 0.5 = 외곽까지 힘이 세게 유지되다가 끝에서 깎임)")]
    [Range(0.2f, 1.5f)] public float falloffPower = 0.5f;
    
    [Header("레이어 설정")]
    public LayerMask dropLayer;

    [Header("테두리 시각화 설정 (LineRenderer)")]
    [SerializeField] private float ringLineWidth = 0.04f;     // 테두리 선 두께
    [SerializeField] private int ringSegments = 60;          // 원의 부드러운 정도
    
    [Header("모드별 테두리 색상")]
    [SerializeField] private Color normalColor = new Color(0.3f, 0.8f, 1f, 0.5f);  // 기본: 은은한 하늘색
    [SerializeField] private Color mergeColor = new Color(0.2f, 1f, 0.4f, 0.8f);   // 왼클릭: 선명한 초록색
    [SerializeField] private Color stickColor = new Color(1f, 0.6f, 0.1f, 0.8f);   // 오른클릭: 선명한 주황색

    [Header("Feel 피드백 (밀기 사운드)")]
    [SerializeField] private MMF_Player pushFeedback;
    [SerializeField] private float pushSoundCooldown = 0.18f;
    private float lastPushSoundTime = -1f;

    private Vector2 previousMousePosition;
    private Camera mainCamera;
    private LineRenderer ringRenderer;

    private void Awake()
    {
        mainCamera = Camera.main;
        SetupRingVisual();
    }

    private void Start()
    {
        previousMousePosition = GetMouseWorldPosition();
    }

    //밀기 영역 식별을 위한 그리기 함수
    private void SetupRingVisual()
    {
        // LineRenderer가 없으면 자동 부착
        ringRenderer = GetComponent<LineRenderer>();
        if (ringRenderer == null)
            ringRenderer = gameObject.AddComponent<LineRenderer>();

        ringRenderer.useWorldSpace = true;
        ringRenderer.startWidth = ringLineWidth;
        ringRenderer.endWidth = ringLineWidth;
        ringRenderer.positionCount = ringSegments + 1;
        ringRenderer.loop = true;

        // 투명도를 지원하는 기본 스프라이트 머티리얼 적용
        ringRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    private void Update()
    {
        //작 카운트다운 중이거나 게임 비활성 상태면 마우스 밀기 정지
        if (!StageManager.IsGameActive)
        {
            if (ringRenderer) ringRenderer.enabled = false; // 링 숨기기
            return;
        }
        
        //링 표시
        if (ringRenderer) ringRenderer.enabled = true;
        
        UpdateInputMode();

        //마우스 휠로 반경 크기 실시간 조절
        HandleWheelResize();
        
        Vector2 currentMousePosition = GetMouseWorldPosition();
        Vector2 mouseVelocity = (currentMousePosition - previousMousePosition) / Time.deltaTime;

        // 1. 플레이어에게 보일 원형 테두리 실시간 갱신
        DrawRadiusRing(currentMousePosition);

        // 2. 물리 밀기 검사
        Collider2D[] hits = Physics2D.OverlapCircleAll(currentMousePosition, influenceRadius, dropLayer);
        bool pushedAnyDrop = false;

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<OilDrop>(out var drop))
            {
                Vector2 dropPos = hit.transform.position;
                Vector2 awayVector = dropPos - currentMousePosition;
                float distance = awayVector.magnitude;

                if (distance < 0.001f) continue;

                // 1. 거리 정규화 (1 = 마우스 중심, 0 = 가장자리 끝)
                float t = 1f - Mathf.Clamp01(distance / influenceRadius);

                // 2. [핵심] 완만한 거듭제곱 곡선 적용 (외곽까지 힘이 덜 줄어듦)
                float curvedT = Mathf.Pow(t, falloffPower);

                // 3. [핵심] 테두리 끝에서도 최소 35%의 힘을 보장하여 굼뜸 방지
                float strength = Mathf.Lerp(minEdgeStrength, 1.0f, curvedT);
                
                Vector2 pushDirection = awayVector.normalized;
                Vector2 repulsion = pushDirection * (repulsionForce * strength);
                Vector2 swipe = mouseVelocity * (swipeForceMultiplier * strength);

                drop.Push(repulsion + swipe, pushDirection);
                pushedAnyDrop = true;
            }
        }

        // 사운드 재생
        if (pushedAnyDrop && Time.time >= lastPushSoundTime + pushSoundCooldown)
        {
            lastPushSoundTime = Time.time;
            pushFeedback?.PlayFeedbacks();
        }

        previousMousePosition = currentMousePosition;
    }

    // 마우스 좌표를 중심으로 둘레에 점들을 찍어 원형 링을 그림
    private void DrawRadiusRing(Vector2 center)
    {
        if (ringRenderer == null) return;

        float deltaTheta = (2f * Mathf.PI) / ringSegments;
        float theta = 0f;

        for (int i = 0; i <= ringSegments; i++)
        {
            float x = center.x + (influenceRadius * Mathf.Cos(theta));
            float y = center.y + (influenceRadius * Mathf.Sin(theta));
            ringRenderer.SetPosition(i, new Vector3(x, y, 0f));
            theta += deltaTheta;
        }

        // 모드에 따라 테두리 색상 즉시 변경 (시각적 피드백)
        Color currentColor = CurrentMode switch
        {
            DropInteractionMode.Merge => mergeColor,
            DropInteractionMode.Stick => stickColor,
            _ => normalColor
        };

        ringRenderer.startColor = currentColor;
        ringRenderer.endColor = currentColor;
    }

    private void UpdateInputMode()
    {
        if (Mouse.current == null) return;

        bool left = Mouse.current.leftButton.isPressed;
        bool right = Mouse.current.rightButton.isPressed;

        if (left)
            CurrentMode = DropInteractionMode.Merge;
        else if (right)
            CurrentMode = DropInteractionMode.Stick;
        else
            CurrentMode = DropInteractionMode.None;
    }

    private Vector2 GetMouseWorldPosition()
    {
        if (Mouse.current == null) return Vector2.zero;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        return (Vector2)worldPos;
    }
    
    /// <summary>
    /// 마우스 휠을 굴려 밀어내기 반경을 실시간으로 확대/축소
    /// </summary>
    private void HandleWheelResize()
    {
        if (Mouse.current == null || Keyboard.current == null) return;
        
        // Ctrl 키를 누르고 있을 때만 밀어내기 링 크기 조절
        if (!Keyboard.current.ctrlKey.isPressed) return;
        
        // New Input System의 휠 스크롤 값 읽기
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            // 휠을 위로 굴리면 확대(+), 아래로 굴리면 축소(-)
            influenceRadius += Mathf.Sign(scroll) * wheelStep;

            // 최소~최대 범위 제한
            influenceRadius = Mathf.Clamp(influenceRadius, minInfluenceRadius, maxInfluenceRadius);
        }
    }
}