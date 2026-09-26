using UnityEngine;
using UnityEngine.InputSystem;

public class SoupFluidController : MonoBehaviour
{
    // [컴포넌트] 국물 스프라이트 렌더러
    private SpriteRenderer spriteRenderer;

    // [컴포넌트] 국물 머티리얼 인스턴스
    private Material soupMaterial;

    // [변수] 메인 카메라 캐싱
    private Camera mainCamera;

    // [변수] 이전 프레임 마우스 위치
    private Vector2 prevMouseWorldPos;

    // [변수] 파문 경과 시간 (초)
    private float rippleTimer = 100f;

    // [변수] 파문 최소 발생 쿨다운 및 속도 임계값
    private const float MIN_SWIPE_SPEED = 1.5f;

    // [셰이더 프로퍼티 ID 캐싱] 성능 최적화
    private static readonly int RippleCenterProp = Shader.PropertyToID("_RippleCenter");
    private static readonly int RippleTimeProp = Shader.PropertyToID("_RippleTime");

    private void Awake()
    {
        mainCamera = Camera.main;

        // 최신 표준: TryGetComponent 사용
        if (TryGetComponent<SpriteRenderer>(out spriteRenderer))
        {
            soupMaterial = spriteRenderer.material;
        }
    }

    private void Start()
    {
        if (mainCamera)
            prevMouseWorldPos = GetMouseWorldPos();
    }

    private void Update()
    {
        if (!soupMaterial || !mainCamera || Mouse.current == null) return;

        Vector2 currentMouseWorld = GetMouseWorldPos();
        Vector2 mouseDelta = currentMouseWorld - prevMouseWorldPos;
        float mouseSpeed = mouseDelta.magnitude / Time.deltaTime;

        // 1. 마우스가 일정 속도 이상으로 국물을 휙 가를 때 파문 유발
        if (mouseSpeed > MIN_SWIPE_SPEED)
        {
            // 국물 스프라이트의 로컬 UV 좌표(0~1)로 변환
            Vector2 localPos = transform.InverseTransformPoint(currentMouseWorld);
            Vector2 uvPos = (localPos / transform.localScale.x) + new Vector2(0.5f, 0.5f);

            // 국물 원 안쪽을 클릭/이동했을 때만 파문 트리거
            if (Vector2.Distance(uvPos, new Vector2(0.5f, 0.5f)) < 0.48f)
            {
                TriggerRippleAt(uvPos);
            }
        }

        // 2. 파문 시간 진행 업데이트
        rippleTimer += Time.deltaTime;
        soupMaterial.SetFloat(RippleTimeProp, rippleTimer);

        prevMouseWorldPos = currentMouseWorld;
    }

    /// <summary>
    /// 특정 UV 지점에 새로운 동심원 물결 파문을 발생시키는 함수
    /// </summary>
    public void TriggerRippleAt(Vector2 uv)
    {
        if (!soupMaterial) return;

        rippleTimer = 0f;
        soupMaterial.SetVector(RippleCenterProp, new Vector4(uv.x, uv.y, 0f, 0f));
    }

    /// <summary>
    /// 마우스 화면 좌표를 2D 월드 좌표로 변환하는 함수
    /// </summary>
    private Vector2 GetMouseWorldPos()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 world = mainCamera.ScreenToWorldPoint(mouseScreen);
        return new Vector2(world.x, world.y);
    }
}