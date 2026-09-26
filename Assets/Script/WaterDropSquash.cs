using UnityEngine;

public class WaterDropSquash : MonoBehaviour
{
    [Header("타겟 설정")]
    [Tooltip("찌그러트릴 겉모습(자식 오브젝트)의 Transform")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private Rigidbody2D rb;

    [Header("물방울 변형 강도")]
    [Range(0.01f, 0.2f)]
    [SerializeField] private float stretchIntensity = 0.05f; // 속도에 비례해 늘어나는 정도
    [SerializeField] private float maxStretch = 1.6f;        // 최대로 늘어날 수 있는 한계치
    [SerializeField] private float smoothSpeed = 15f;        // 원래대로 돌아오는 탄성 속도

    private Vector3 originalScale;

    void Start()
    {
        if (visualTransform == null)
            visualTransform = transform.GetChild(0);

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        // 원래 기본 크기(보통 1, 1, 1) 기억
        originalScale = visualTransform.localScale;
    }

    void LateUpdate()
    {
        // 1. 현재 이동 속도와 방향 구하기
        Vector2 velocity = rb.linearVelocity; // Unity 최신 버전 기준 (구버전은 rb.velocity)
        float speed = velocity.magnitude;

        if (speed > 0.05f)
        {
            // 2. 이동 방향을 바라보도록 자식 오브젝트 회전
            // (스프라이트의 위쪽이 앞을 바라본다고 가정할 때 -90도 보정)
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
            visualTransform.rotation = Quaternion.Euler(0, 0, angle);

            // 3. 부피 보존 스쿼시 & 스트레치 수식 계산
            // 이동 방향(Y축)으로 늘어나는 비율
            float stretchFactor = 1.0f + (speed * stretchIntensity);
            stretchFactor = Mathf.Min(stretchFactor, maxStretch);

            // 면적(부피)을 보존하기 위해 수직 방향(X축)은 반비례로 줄어듦 (X * Y = 1)
            float squashFactor = 1.0f / stretchFactor;

            Vector3 targetScale = new Vector3(
                originalScale.x * squashFactor,
                originalScale.y * stretchFactor,
                originalScale.z
            );

            // 4. 부드럽게 변형 적용
            visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, targetScale, Time.deltaTime * smoothSpeed);
        }
        else
        {
            // 멈췄을 때는 원래 둥근 원형 크기로 서서히 복원
            visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, originalScale, Time.deltaTime * smoothSpeed);
        }
    }
    
    
    // 충돌 시 납작해지는 충격 연출
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 충돌 속도가 일정 이상일 때만 반응
        if (collision.relativeVelocity.magnitude > 1f)
        {
            // 순간적으로 가로로 펑퍼짐하게(Squash) 찌그러뜨림
            visualTransform.localScale = new Vector3(
                originalScale.x * 1.5f,
                originalScale.y * 0.6f,
                originalScale.z
            );
        }
    }
}

