using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class BowlBoundary : MonoBehaviour
{
    [Header("그릇 크기 설정")]
    [Tooltip("라면 그릇의 반지름 (화면 크기에 맞게 3.5 ~ 4.5 추천)")]
    [SerializeField] private float bowlRadius = 15f;
    [Tooltip("원의 매끄러운 정도 (64개 추천)")]
    [SerializeField] private int segments = 64;

    [Header("그릇 테두리 외형 (선)")]
    [SerializeField] private bool showRimVisual = true;
    [SerializeField] private float rimWidth = 0.12f;
    [SerializeField] private Color rimColor = new Color(0.9f, 0.85f, 0.75f, 1.0f); // 도자기 그릇 느낌의 미색

    private EdgeCollider2D edgeCollider;
    private LineRenderer lineRenderer;
    
    public float BowlRadius => bowlRadius;

    private void Awake()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
        GenerateBowl();
    }

    // 인스펙터에서 반지름 값을 바꿀 때 즉시 갱신
    private void OnValidate()
    {
        if (edgeCollider == null) edgeCollider = GetComponent<EdgeCollider2D>();
        GenerateBowl();
    }

    public void GenerateBowl()
    {
        if (edgeCollider == null) return;

        // 1. 원형 둘레를 따라 점 배열 생성 (시작점과 끝점을 연결해 루프 완성)
        Vector2[] points = new Vector2[segments + 1];
        float angleStep = (2f * Mathf.PI) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * bowlRadius;
        }

        // 2. 물리 충돌체에 원형 루프 적용 (안쪽에 물방울이 갇힘)
        edgeCollider.points = points;

        // 3. 눈에 보이는 그릇 테두리 링(LineRenderer) 그리기
        if (showRimVisual)
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

            lineRenderer.useWorldSpace = false;
            lineRenderer.startWidth = rimWidth;
            lineRenderer.endWidth = rimWidth;
            lineRenderer.positionCount = segments + 1;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = rimColor;
            lineRenderer.endColor = rimColor;

            for (int i = 0; i <= segments; i++)
            {
                lineRenderer.SetPosition(i, points[i]);
            }
        }
    }

    // 에디터 씬 뷰에서 노란색 원으로 확인
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bowlRadius);
    }
}