using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.UI;


public class ShapeMatcher : MonoBehaviour
{
    [Header("판정 설정")]
    [SerializeField] private LayerMask dropLayer;
    [Tooltip("샘플링 격자 간격 (작을수록 정밀하지만 연산량이 늘어남. 0.15~0.25 추천)")]
    [SerializeField] private float sampleGridSize = 0.2f;
    [Tooltip("외곽선 바로 바깥의 자연스러운 삐져나옴을 봐주는 완충 거리 (0.15 ~ 0.25 추천)")]
    [SerializeField] private float spillToleranceBuffer = 0.2f;
    [Tooltip("도형 밖으로 심하게 삐져나갔을 때 감점 배율 (기존 1.0 -> 0.4로 완화)")]
    [SerializeField] private float spillPenaltyWeight = 0.4f;
    [Tooltip("클리어 인정 기준 퍼센트")]
    [SerializeField] private float passThreshold = 72.0f;

    [Header("실시간 결과 (확인용)")]
    [Range(0, 100)] public float currentAccuracy = 0f;
    public bool isClear = false;

    [Header("물방울 개수 자동 계산")]
    [Tooltip("반지름을 자동으로 추출해 올 물방울 프리팹")]
    [SerializeField] private GameObject oilDropPrefab;

    [Tooltip("플레이어에게 줄 여유분 배율 (1.0 = 이론상 딱 맞음, 1.15 = 15% 여유 제공)")]
    [SerializeField] private float marginMultiplier = 1.15f;

    [Header("자동 계산 결과 (인스펙터 확인용)")]
    [SerializeField] private float detectedDropRadius; // 감지된 프리팹의 실제 반지름
    [SerializeField] private float targetShapeArea; // 도형 총면적
    [SerializeField] private int recommendedDropCount; // 추천 물방울 수
    
    private Collider2D targetCollider;
    private List<Vector2> insideTargetPoints = new List<Vector2>();
    private List<Vector2> outsideBorderPoints = new List<Vector2>();

    // 디버그 기즈모용 상태 저장
    private List<Vector2> coveredTargetPoints = new List<Vector2>();
    private List<Vector2> emptyTargetPoints = new List<Vector2>();
    private List<Vector2> spilledPoints = new List<Vector2>();

    //ui용 mm프로그래스 바
    [SerializeField] private MMProgressBar proBar;
    [SerializeField] private Image foregroundBar;
    
    private void Awake()
    {
        // 시작 시 클리어 상태 초기화
        isClear = false;
        currentAccuracy = 0f;
        
        // 씬 시작 시 자체 콜라이더가 있다면 잡고, 없다면 StageManager가 주입해 줄 때까지 대기
        if (!targetCollider)
            targetCollider = GetComponent<Collider2D>();

        if (targetCollider)
            targetCollider.isTrigger = true;
        
        proBar.Initialization();
    }

    private void Start()
    {
        //targetCollider가 주입되기 전까지는 절대 실행하지 않음
        if (targetCollider)
        {
            GenerateSampleGrid();
            CalculateRequiredDrops();// 시작할 때 면적과 필요 개수 자동 연산
        }
    }

    /// <summary>
    /// 스테이지가 바뀔 때 새 도형의 콜라이더를 넘겨받아 판정판을 즉시 재구성
    /// </summary>
    public void SetTargetCollider(Collider2D newCollider)
    {
        targetCollider = newCollider;
        isClear = false; // [추가] 새 도형이 들어오면 클리어 플래그 무조건 리셋!
        
        if (targetCollider)
        {
            targetCollider.isTrigger = true;
            GenerateSampleGrid();        // 새 도형 모양대로 감시 점 재배치
            CalculateRequiredDrops();    // 새 도형에 필요한 물방울 수 재계산
        }
    }
    
    /// <summary>
    /// 목표 콜라이더 주변으로 샘플링 포인트 격자 생성
    /// </summary>
    public void GenerateSampleGrid()
    {
        //콜라이더가 아직 없으면 에러를 내지 않고 리턴
        if (!targetCollider) return;
        
        insideTargetPoints.Clear();
        outsideBorderPoints.Clear();

        Bounds bounds = targetCollider.bounds;
        // 도형 바깥 삐져나감을 감지하기 위해 바운즈를 살짝 넓혀서 검사
        float margin = 1.2f;
        float minX = bounds.min.x - margin;
        float maxX = bounds.max.x + margin;
        float minY = bounds.min.y - margin;
        float maxY = bounds.max.y + margin;

        for (float x = minX; x <= maxX; x += sampleGridSize)
        {
            for (float y = minY; y <= maxY; y += sampleGridSize)
            {
                Vector2 point = new Vector2(x, y);

                // 목표 콜라이더 영역 내부인지 판정
                if (targetCollider.OverlapPoint(point))
                {
                    insideTargetPoints.Add(point);
                }
                else
                {
                    // 2. 목표 도형 외부 점 (유니티 표준 ClosestPoint 사용)
                    // 현재 점에서 콜라이더 외곽선 표면까지의 최단 지점을 구함
                    Vector2 closestEdgePoint = targetCollider.ClosestPoint(point);
                    float distanceToEdge = Vector2.Distance(point, closestEdgePoint);
                    
                    // 외곽선에서 완충 거리(spillToleranceBuffer)보다 더 멀리 벗어난 점만 '진짜 감점 점'으로 수집
                    if (distanceToEdge > spillToleranceBuffer && bounds.Contains(point))
                    {
                        outsideBorderPoints.Add(point);
                    }
                }
            }
        }
        
        //필요한 물방울 수 계산
        CalculateRequiredDrops();
    }

    private void Update()
    {
        // 실시간으로 일치도 갱신
        EvaluateShape();
    }

    /// <summary>
    /// 현재 물방울들과 목표 도형의 일치율(%) 계산
    /// </summary>
    public void EvaluateShape()
    {
        if (insideTargetPoints.Count == 0) return;

        coveredTargetPoints.Clear();
        emptyTargetPoints.Clear();
        spilledPoints.Clear();

        // 1. 도형 내부 채움률 검사
        int insideCoveredCount = 0;
        foreach (var p in insideTargetPoints)
        {
            if (Physics2D.OverlapPoint(p, dropLayer))
            {
                insideCoveredCount++;
                coveredTargetPoints.Add(p);
            }
            else
            {
                emptyTargetPoints.Add(p);
            }
        }

        // 2. 도형 외부 삐져나감(Spill) 검사
        int outsideSpillCount = 0;
        foreach (var p in outsideBorderPoints)
        {
            if (Physics2D.OverlapPoint(p, dropLayer))
            {
                outsideSpillCount++;
                spilledPoints.Add(p);
            }
        }

        // 3. 최종 점수 계산
        float fillRatio = (float)insideCoveredCount / insideTargetPoints.Count;
        float spillRatio = (float)outsideSpillCount / insideTargetPoints.Count;

        float finalScore = (fillRatio - (spillRatio * spillPenaltyWeight)) * 100f;
        proBar.UpdateBar(finalScore,0,100);
        currentAccuracy = Mathf.Clamp(finalScore, 0f, 100f);

        isClear = currentAccuracy >= passThreshold;

        //달성도를 넘으면 초록색으로 변경
        foregroundBar.color = isClear ? Color.green : Color.white;
    }

    // 씬 뷰에서 실시간 일치 상태를 시각적으로 확인 (초록: 채움 / 빨강: 빔 / 노랑: 삐져나옴)
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 채워진 부분 (초록색)
        Gizmos.color = Color.green;
        foreach (var p in coveredTargetPoints)
            Gizmos.DrawSphere(p, sampleGridSize * 0.25f);

        // 아직 덜 채워진 부분 (빨간색)
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        foreach (var p in emptyTargetPoints)
            Gizmos.DrawSphere(p, sampleGridSize * 0.15f);

        // 밖으로 삐져나간 부분 (주황색/노란색)
        Gizmos.color = new Color(1, 0.5f, 0, 0.8f);
        foreach (var p in spilledPoints)
            Gizmos.DrawSphere(p, sampleGridSize * 0.25f);
    }
    
    /// <summary>
    /// 도형 면적과 필요한 물방울 수를 자동 계산
    /// </summary>
    public int CalculateRequiredDrops()
    {
        if (insideTargetPoints.Count == 0)
            GenerateSampleGrid();

        // 1. 프리팹으로부터 실제 물리 반지름 추출
        detectedDropRadius = GetDropPrefabRadius();

        // 2. 격자 기반 도형 면적 산출
        targetShapeArea = insideTargetPoints.Count * (sampleGridSize * sampleGridSize);

        // 3. 표준 물방울 1개의 면적 (PI * r^2)
        float singleDropArea = Mathf.PI * detectedDropRadius * detectedDropRadius;

        // 4. 필요 개수 계산 (올림 처리 + 여유분 배율)
        recommendedDropCount = Mathf.CeilToInt((targetShapeArea / singleDropArea) * marginMultiplier);

        return Mathf.Max(1, recommendedDropCount);
    }
    
    /// 프리팹의 CircleCollider2D와 Scale을 분석하여 실제 월드 반지름 반환
    /// </summary>
    public float GetDropPrefabRadius()
    {
        if (oilDropPrefab != null)
        {
            // 루트 또는 자식에 붙은 CircleCollider2D 탐색
            CircleCollider2D circleCol = oilDropPrefab.GetComponentInChildren<CircleCollider2D>();
            if (circleCol != null)
            {
                // 콜라이더 자체의 radius에 프리팹의 스케일을 곱해야 '실제 크기'가 나옵니다.
                return circleCol.radius * oilDropPrefab.transform.localScale.x;
            }
        }

        // 프리팹 연결을 깜빡했을 때의 안전 기본값
        return 0.5f;
    }
}