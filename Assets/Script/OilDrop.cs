using System;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks; 


[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class OilDrop : MonoBehaviour
{
    //씬에 살아있는 모든 물방울을 실시간으로 담아두는 공유 리스트
    public static readonly List<OilDrop> ActiveDrops = new List<OilDrop>();
    
    [Header("자식 비주얼 연결")]
    [SerializeField] private Transform visualTransform;

    [Header("물방울 탄성 설정")]
    [SerializeField] private float squishFactor = 0.3f;   // 눌리는 강도
    [SerializeField] private float restoreSpeed = 8f;     // 원래대로 돌아오는 속도
    
    [Header("Feel 피드백 (합치기 사운드)")]
    [SerializeField] private MMF_Player mergeFeedback;
    
    [Header("우클릭 접착 편의성 설정")]
    //우클릭 시 주변 물방울을 감지해 끌어당기는 표면장력 거리
    [SerializeField] private float surfaceTensionDistance = 0.4f;
    //우클릭 시 서로를 끌어당기는 자석 인력 세기
    [SerializeField] private float surfaceTensionForce = 12.0f;
    
    private Rigidbody2D rb;
    private CircleCollider2D col;
    private bool isDestroyed = false;

    // 현재 찌그러진 정도 (-0.5 ~ +0.5)
    private float currentSquish = 0f;
    private float squishVelocity = 0f;

    private List<OilDrop> connectedDrops = new List<OilDrop>();

    // 조인트가 있는지 여부 확인 프로퍼티
    public bool HasJoints => connectedDrops.Count > 0;

    private void OnEnable()
    {
        //생성될 시 등록
        ActiveDrops.Add(this);
    }

    private void OnDisable()
    {
        //비활성화시 해제
        ActiveDrops.Remove(this);
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        rb.linearDamping = 3f;
        rb.gravityScale = 0f;

        // 비주얼이 없으면 자식을 검색해서 할당
        if (visualTransform == null && transform.childCount > 0)
            visualTransform = transform.GetChild(0);

        // 자식 비주얼의 기본 스케일은 무조건 1, 1, 1로 정규화
        if (visualTransform != null)
            visualTransform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (!visualTransform) return;

        // 스프링 탄성 복원: currentSquish가 0으로 서서히 돌아옴
        currentSquish = Mathf.Lerp(currentSquish, 0f, Time.deltaTime * restoreSpeed);

        // 찌그러짐 수치에 따라 가로/세로 비율 조절 (부피 보존: X * Y = 1 근사치)
        float scaleY = 1.0f - currentSquish;
        float scaleX = 1.0f + currentSquish;

        visualTransform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private void FixedUpdate()
    {
        // 속도가 아주 낮아지면 미세하게 미끄러지지 않고 그 자리에 즉시 정지시킴
        if (rb.linearVelocity.sqrMagnitude < 0.05f)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        // 우클릭(접착 모드) 상태라면 주변 물방울을 표면장력으로 끌어당김
        if (MouseForce.CurrentMode == DropInteractionMode.Stick)
        {
            ApplySurfaceTensionPull();
        }
    }
    
    public void Push(Vector2 force, Vector2 pushDirection)
    {
        rb.AddForce(force, ForceMode2D.Force);

        if (!visualTransform || pushDirection.sqrMagnitude < 0.001f) return;

        // 밀린 방향으로 자식 회전
        float angle = Mathf.Atan2(pushDirection.y, pushDirection.x) * Mathf.Rad2Deg - 90f;
        visualTransform.rotation = Quaternion.Euler(0, 0, angle);

        // 지속적으로 덮어씌우는 게 아니라, 힘의 크기에 비례해서 최대 눌림 한도 내에서만 찌그러지도록 설정
        float targetImpact = Mathf.Clamp(force.magnitude * squishFactor * 0.05f, 0f, 0.4f);
        currentSquish = Mathf.Max(currentSquish, targetImpact);
    }

    // ─── 물방울 충돌 시 로직 ───
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollisionInteraction(collision);
    }

    //이미 맞닿아 있는 상태에서 뒤늦게 우클릭을 눌러도 결합
    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollisionInteraction(collision);
    }

    
    private void MergeWith(OilDrop other)
    {
        other.isDestroyed = true;

        //물방울 흡수 사운드 재생
        mergeFeedback?.PlayFeedbacks();
        
        // 부피 보존 반지름 계산
        float r1 = transform.localScale.x;
        float r2 = other.transform.localScale.x;
        float newRadius = Mathf.Sqrt(r1 * r1 + r2 * r2);

        Vector2 combinedVelocity = (rb.linearVelocity + other.rb.linearVelocity) * 0.5f;
        transform.position = (transform.position + other.transform.position) * 0.5f;

        // 부모의 실제 크기를 키움 (자식 비주얼은 그대로 1, 1, 1 기준 유지)
        transform.localScale = Vector3.one * newRadius;
        rb.linearVelocity = combinedVelocity;

        // 흡수되는 순간 찰랑거리는 탄성 부여
        currentSquish = -0.3f;

        Destroy(other.gameObject);
    }

    private void StickWith(OilDrop other, Vector2 contactPoint)
    {
        if (connectedDrops.Contains(other)) return;

        FixedJoint2D joint = gameObject.AddComponent<FixedJoint2D>();
        joint.connectedBody = other.rb;
        joint.anchor = transform.InverseTransformPoint(contactPoint);
        joint.enableCollision = false;

        connectedDrops.Add(other);
        other.connectedDrops.Add(this);

        currentSquish = 0.2f;
    }
    
    public void TriggerSquish(float amount)
    {
        currentSquish = amount;
    }

    /// <summary>
    /// 그어진 선(cutStart ~ cutEnd)과 연결선이 교차하는 조인트만 골라서 절단
    /// </summary>
    public void BreakJointsIntersectingLine(Vector2 cutStart, Vector2 cutEnd)
    {
        FixedJoint2D[] joints = GetComponents<FixedJoint2D>();
        List<OilDrop> dropsToDisconnect = new List<OilDrop>();

        foreach (var joint in joints)
        {
            if (joint.connectedBody && joint.connectedBody.TryGetComponent<OilDrop>(out var targetDrop))
            {
                Vector2 p1 = transform.position;
                Vector2 p2 = targetDrop.transform.position;

                // 칼날 선분(cutStart-cutEnd)과 두 물방울 중심 연결선(p1-p2)이 교차하는지 검사
                if (CheckLineIntersection(cutStart, cutEnd, p1, p2))
                {
                    dropsToDisconnect.Add(targetDrop);
                    Destroy(joint);
                }
            }
        }

        foreach (var target in dropsToDisconnect)
        {
            connectedDrops.Remove(target);
            target.connectedDrops.Remove(this);
            
            // 분리되는 순간 통 튀는 탄성 연출
            TriggerSquish(0.3f);
            target.TriggerSquish(0.3f);
        }
    }

    // 2D 두 선분 간의 교차 판정 공식 (CCW 알고리즘)
    private bool CheckLineIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float ccw1 = CCW(a, b, c) * CCW(a, b, d);
        float ccw2 = CCW(c, d, a) * CCW(c, d, b);
        return ccw1 <= 0 && ccw2 <= 0;
    }

    private float CCW(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    public void RestoreJoint(OilDrop target)
    {
        if (connectedDrops.Contains(target)) return;

        FixedJoint2D joint = gameObject.AddComponent<FixedJoint2D>();
        joint.connectedBody = target.rb;
        joint.anchor = Vector2.zero;
        joint.enableCollision = false;
        
        connectedDrops.Add(target);
        target.connectedDrops.Add(this);
    }

    /// <summary>
    /// 우클릭 접착 모드일 때 가까운 물방울들을 자석처럼 서로 끌어당겨 쉽게 붙게 만드는 함수
    /// </summary>
    private void ApplySurfaceTensionPull()
    {
        if (!col) return;

        float currentRadius = col.radius * transform.localScale.x;
        float searchRadius = currentRadius + surfaceTensionDistance;

        // 2D 오버랩 검사
        Collider2D[] nearbyHits = Physics2D.OverlapCircleAll(transform.position, searchRadius);

        foreach (var hit in nearbyHits)
        {
            if (hit.TryGetComponent<OilDrop>(out var targetDrop))
            {
                // 자기 자신이거나, 이미 붙어있거나, 파괴된 객체는 무시
                if (targetDrop == this || targetDrop.isDestroyed || connectedDrops.Contains(targetDrop))
                    continue;

                Vector2 toTarget = (Vector2)targetDrop.transform.position - (Vector2)transform.position;
                float distance = toTarget.magnitude;

                if (distance > 0.001f)
                {
                    // 서로를 마주 보고 끌어당기는 인력 적용
                    Vector2 pullDirection = toTarget.normalized;
                    rb.AddForce(pullDirection * surfaceTensionForce, ForceMode2D.Force);
                }
            }
        }
    }

    /// <summary>
    /// [함수] 충돌 접촉 시 마우스 모드(융합 vs 접착)에 맞춰 상호작용을 처리하는 공용 함수
    /// </summary>
    private void HandleCollisionInteraction(Collision2D collision)
    {
        if (UndoManager.IsUndoing || isDestroyed) return;

        // 최신 표준: TryGetComponent 사용
        if (collision.gameObject.TryGetComponent<OilDrop>(out var otherDrop))
        {
            if (otherDrop.isDestroyed) return;

            // [왼클릭 모드]: 완전 융합 (Merge)
            if (MouseForce.CurrentMode == DropInteractionMode.Merge)
            {
                bool isBigger = transform.localScale.x > otherDrop.transform.localScale.x;
                bool isTieBreaker = Mathf.Approximately(transform.localScale.x, otherDrop.transform.localScale.x)
                                    && (GetEntityId() < otherDrop.GetEntityId());

                if (isBigger || isTieBreaker)
                {
                    if (UndoManager.Instance)
                        UndoManager.Instance.RecordState();

                    MergeWith(otherDrop);
                }
            }
            // [오른클릭 모드]: 형태 유지 접착 (Stick)
            else if (MouseForce.CurrentMode == DropInteractionMode.Stick)
            {
                if (!connectedDrops.Contains(otherDrop))
                {
                    if (UndoManager.Instance)
                        UndoManager.Instance.RecordState();

                    Vector2 contactPoint = collision.contactCount > 0
                        ? collision.contacts[0].point
                        : (Vector2)transform.position;
                    StickWith(otherDrop, contactPoint);
                }
            }
        }
    }
}