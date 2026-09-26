using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CompositeCollider2D))]
public class ShapeOutlineRenderer : MonoBehaviour
{
    // [컴포넌트] 외곽선 좌표를 추출할 복합 콜라이더
    private CompositeCollider2D compositeCollider;

    [Header("외곽선 스타일 설정")]
    // [설정] 외곽선 두께 (0.04 ~ 0.06 추천)
    [SerializeField] private float outlineWidth = 0.05f;

    // [설정] 외곽선 색상 (국물과 어울리는 따뜻한 미색/크림색)
    [SerializeField] private Color outlineColor = new Color(1.0f, 0.92f, 0.75f, 0.85f);

    // [설정] 렌더링 레이어 순서 (물방울보다 살짝 뒤, 가이드라인보다 앞)
    [SerializeField] private int sortingOrder = -1;

    // [변수] 생성된 외곽선 렌더러 오브젝트 목록
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    private void Awake()
    {
        
        if (!compositeCollider)
        {
            TryGetComponent<CompositeCollider2D>(out compositeCollider);
        }

        GenerateOutline();
    }

    /// <summary>
    /// [함수] CompositeCollider2D의 외곽 경로를 읽어 닫힌 선(루프)으로 외곽선을 생성하는 함수
    /// </summary>
    public void GenerateOutline()
    {
        if (!compositeCollider) return;

        // 기존에 그려진 라인이 있다면 정리
        ClearExistingLines();

        // 복합 콜라이더가 계산한 외곽선 패스 개수
        int pathCount = compositeCollider.pathCount;
        List<Vector2> pathPoints = new List<Vector2>();

        for (int i = 0; i < pathCount; i++)
        {
            pathPoints.Clear();
            int pointCount = compositeCollider.GetPath(i, pathPoints);

            // 선을 잇기 위해 최소 점 3개 이상 필요
            if (pointCount < 3) continue;

            // 라인을 그릴 자식 오브젝트 생성
            GameObject lineObj = new GameObject($"Outline_Path_{i}");
            lineObj.transform.SetParent(transform, false);

            // 최신 규칙: 컴포넌트 추가 및 null 체크
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true; // [핵심] 시작점과 끝점을 닫아 완전한 폐곡선 형성
            lr.startWidth = outlineWidth;
            lr.endWidth = outlineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = outlineColor;
            lr.endColor = outlineColor;
            lr.sortingOrder = sortingOrder;

            // 좌표 배열 전달
            Vector3[] positions = new Vector3[pointCount];
            for (int p = 0; p < pointCount; p++)
            {
                positions[p] = new Vector3(pathPoints[p].x, pathPoints[p].y, 0f);
            }

            lr.positionCount = pointCount;
            lr.SetPositions(positions);

            lineRenderers.Add(lr);
        }
    }

    /// <summary>
    /// [함수] 기존에 생성된 외곽선 라인 오브젝트들을 메모리에서 정리하는 함수
    /// </summary>
    private void ClearExistingLines()
    {
        foreach (var lr in lineRenderers)
        {
            if (lr) Destroy(lr.gameObject);
        }
        lineRenderers.Clear();
    }
}