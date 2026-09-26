using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MoreMountains.Feedbacks;


[RequireComponent(typeof(LineRenderer))]
public class DropletCutter : MonoBehaviour
{
    [Header("절단 레이어")]
    [SerializeField] private LayerMask dropLayer;

    [Header("시각 효과 설정")]
    [SerializeField] private Color bladeColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private float separationImpulse = 2.0f; // 잘린 후 양옆으로 튕겨 나가는 힘

    [Header("Feel 피드백 연결")]
    [SerializeField] private MMF_Player sliceFeedback;
    
    private LineRenderer lineRenderer;
    private Camera mainCamera;
    private Vector2 cutStartWorldPos;
    private bool isCutting = false;

    private void Awake()
    {
        mainCamera = Camera.main;
        SetupLineRenderer();
    }

    //라인 렌더러 초기 설정
    private void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;

        // 기본 단색 머티리얼 적용
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = bladeColor;
        lineRenderer.endColor = bladeColor;
    }

    private void Update()
    {
        // 카운트다운 중에는 칼질 불가
        if (!StageManager.IsGameActive) return;
        
        if (Keyboard.current == null || Mouse.current == null) return;

        // Space 키를 누른 상태를 "자르기 모드"로 사용
        bool cutModifier = Keyboard.current.spaceKey.isPressed;

        // 1. 자르기 시작 (Space + 마우스 좌클릭)
        if (cutModifier && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isCutting = true;
            cutStartWorldPos = GetMouseWorldPosition();
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, cutStartWorldPos);
            lineRenderer.SetPosition(1, cutStartWorldPos);
        }

        // 2. 궤적 드래그 중
        if (isCutting && Mouse.current.leftButton.isPressed)
        {
            Vector2 currentPos = GetMouseWorldPosition();
            lineRenderer.SetPosition(0, cutStartWorldPos);
            lineRenderer.SetPosition(1, currentPos);
        }

        // 3. 손을 뗐을 때 절단 실행
        if (isCutting && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Vector2 cutEndWorldPos = GetMouseWorldPosition();
            lineRenderer.enabled = false;
            isCutting = false;

            // 너무 짧게 그었을 경우 오작동 방지 (최소 0.3m 이상)
            if (Vector2.Distance(cutStartWorldPos, cutEndWorldPos) > 0.3f)
            {
                ExecuteCut(cutStartWorldPos, cutEndWorldPos);
            }
        }

        // 도중에 Space를 뗐을 경우 자르기 취소
        if (isCutting && !cutModifier)
        {
            isCutting = false;
            lineRenderer.enabled = false;
        }
    }

    private void ExecuteCut(Vector2 start, Vector2 end)
    {
        // 칼날 선과 교차하는 모든 콜라이더 검출
        RaycastHit2D[] hits = Physics2D.LinecastAll(start, end, dropLayer);
        if (hits.Length == 0) return;

        //사운드 재생
        sliceFeedback.PlayFeedbacks();
        
        // 칼날에 걸린 물방울이 있을 때, 자르기 직전 상태 기록!
        UndoManager.Instance?.RecordState();
        
        HashSet<OilDrop> processedDrops = new HashSet<OilDrop>();

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<OilDrop>(out var drop))
            {
                if (processedDrops.Contains(drop)) continue;
                processedDrops.Add(drop);

                // 1순위: 오른클릭 접착(Joint) 분리 검사
                if (drop.HasJoints)
                { 
                    drop.BreakJointsIntersectingLine(start, end);
                }
                // 2순위: 단일 물방울 쪼개기 (최소 반지름 0.4 이상일 때만 분할 허용)
                else if (drop.transform.localScale.x >= 0.5f)
                {
                    TrySplitSingleDrop(drop, start, end);
                }
            }
        }
    }

    // 단일 물방울 기하학적 분할 로직
    private void TrySplitSingleDrop(OilDrop drop, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 center = drop.transform.position;
        float radius = drop.transform.localScale.x * 0.5f;

        // 원 중심에서 절단선까지의 수직 거리(d) 계산
        Vector2 lineDir = (lineEnd - lineStart).normalized;
        Vector2 toCenter = center - lineStart;
        float projection = Vector2.Dot(toCenter, lineDir);
        Vector2 closestPoint = lineStart + lineDir * projection;
        float distanceToLine = Vector2.Distance(center, closestPoint);

        // 절단선이 원을 완전히 가로지르지 못했으면 통과
        if (distanceToLine >= radius) return;

        // 면적 비율 계산 (원형 세그먼트 공식 근사)
        // h = 중심에서 현까지의 거리 비율 (0 = 정중앙 절단, 1 = 빗겨침)
        float h = Mathf.Clamp01(distanceToLine / radius);
        
        // 정중앙일 때 5:5, 가장자리로 갈수록 7:3, 8:2 등으로 비율 결정 (최소 20% 보장)
        float ratio = Mathf.Lerp(0.5f, 0.2f, h); 
        float ratioA = 1.0f - ratio;
        float ratioB = ratio;

        // 부피 보존 반지름: r_new = R * sqrt(ratio)
        float currentScale = drop.transform.localScale.x;
        float scaleA = currentScale * Mathf.Sqrt(ratioA);
        float scaleB = currentScale * Mathf.Sqrt(ratioB);

        // 절단선 수직 벡터 (양쪽으로 밀어낼 방향)
        Vector2 cutNormal = new Vector2(-lineDir.y, lineDir.x);
        if (Vector2.Dot(cutNormal, center - closestPoint) < 0)
            cutNormal = -cutNormal;

        // 기존 물방울 정보 복사 후 새 조각 2개 생성
        Vector2 posA = center + cutNormal * (scaleB * 0.2f);
        Vector2 posB = center - cutNormal * (scaleA * 0.2f);

        GameObject prefab = drop.gameObject;

        // 조각 A (본체 재활용)
        drop.transform.position = posA;
        drop.transform.localScale = Vector3.one * scaleA;
        drop.TriggerSquish(-0.3f);
        if (drop.TryGetComponent<Rigidbody2D>(out var rbA))
            rbA.AddForce(cutNormal * separationImpulse, ForceMode2D.Impulse);

        // 조각 B (신규 복제)
        GameObject newDropObj = Instantiate(prefab, posB, Quaternion.identity);
        newDropObj.transform.localScale = Vector3.one * scaleB;
        if (newDropObj.TryGetComponent<OilDrop>(out var newDrop))
        {
            newDrop.TriggerSquish(-0.3f);
        }
        if (newDropObj.TryGetComponent<Rigidbody2D>(out var rbB))
            rbB.AddForce(-cutNormal * separationImpulse, ForceMode2D.Impulse);
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        return (Vector2)worldPos;
    }
}