using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class ScreenBoundary : MonoBehaviour
{
    [Header("카메라 설정")]
    [SerializeField] private Camera targetCamera;

    [Header("벽 안쪽 여백")]
    [Tooltip("물방울 반지름만큼 여백을 주면 물방울이 화면 밖으로 전혀 삐져나가지 않습니다.")]
    [SerializeField] private float padding = 0.5f;

    private EdgeCollider2D edgeCollider;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        edgeCollider = GetComponent<EdgeCollider2D>();
        UpdateBoundaries();
    }

    // 에디터에서 값을 바꿀 때도 즉시 반응하도록 추가
    private void OnValidate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (edgeCollider == null) edgeCollider = GetComponent<EdgeCollider2D>();
        UpdateBoundaries();
    }

    public void UpdateBoundaries()
    {
        if (targetCamera == null || edgeCollider == null) return;

        // 1. 카메라 화면의 네 모서리 월드 좌표 구하기
        Vector2 bottomLeftWorld = targetCamera.ViewportToWorldPoint(new Vector3(0, 0, targetCamera.nearClipPlane));
        Vector2 topRightWorld = targetCamera.ViewportToWorldPoint(new Vector3(1, 1, targetCamera.nearClipPlane));

        // 2. 패딩 적용
        float left = bottomLeftWorld.x + padding;
        float right = topRightWorld.x - padding;
        float bottom = bottomLeftWorld.y + padding;
        float top = topRightWorld.y - padding;

        // 3. [핵심] 월드 좌표를 EdgeCollider가 이해할 수 있는 로컬 좌표로 변환!
        Vector2 p1 = transform.InverseTransformPoint(new Vector2(left, bottom));
        Vector2 p2 = transform.InverseTransformPoint(new Vector2(left, top));
        Vector2 p3 = transform.InverseTransformPoint(new Vector2(right, top));
        Vector2 p4 = transform.InverseTransformPoint(new Vector2(right, bottom));

        // 사각 루프 연결
        edgeCollider.points = new Vector2[5] { p1, p2, p3, p4, p1 };
    }

    // 실제 콜라이더 선 위치를 그대로 기즈모로 그려서 오차 0% 보장
    private void OnDrawGizmos()
    {
        if (edgeCollider == null || edgeCollider.points == null || edgeCollider.points.Length < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < edgeCollider.points.Length - 1; i++)
        {
            Vector3 start = transform.TransformPoint(edgeCollider.points[i]);
            Vector3 end = transform.TransformPoint(edgeCollider.points[i + 1]);
            Gizmos.DrawLine(start, end);
        }
    }
}