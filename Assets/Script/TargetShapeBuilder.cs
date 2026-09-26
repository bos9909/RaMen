using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(CompositeCollider2D))]
public class TargetShapeBuilder : MonoBehaviour
{
    [Header("스탬프 원형 스프라이트")]
    public Sprite circleSprite;

    [Header("기준 물방울 설정")]
    [Tooltip("기본 물방울 1개의 반지름 (기본 0.5)")]
    public float baseRadius = 0.5f;

    [Header("도달 가능한 부피 단계 (0.25배 ~ 9배)")]
    public static readonly float[] VolumeSteps = new float[] 
    { 
        0.25f, // 1/4 조각 (r = 0.25)
        0.5f,  // 1/2 반쪽 (r = 0.354)
        1.0f,  // 기본 1개 (r = 0.5)
        2.0f,  // 2개 합체 (r = 0.707)
        3.0f,  // 3개 합체 (r = 0.866)
        4.0f,  // 4개 합체 (r = 1.0)
        5.0f,  // 5개 합체 (r = 1.118)
        6.0f,  // 6개 합체 (r = 1.225)
        8.0f,  // 8개 합체 (r = 1.414)
        9.0f   // 9개 합체 (r = 1.5)
    };

    [HideInInspector] public int currentStepIndex = 2; // 기본값: 1.0배 (인덱스 2)
    [Range(0.1f, 1.0f)] public float stampAlpha = 0.1f;

    // 현재 선택된 부피와 반지름 계산 프로퍼티
    public float CurrentVolume => VolumeSteps[currentStepIndex];
    public float CurrentRadius => baseRadius * Mathf.Sqrt(CurrentVolume);

    public void PlaceCircle(Vector2 worldPos)
    {
        float radius = CurrentRadius;
        GameObject newCircle = new GameObject($"Circle_V{CurrentVolume}_{transform.childCount}");
        newCircle.transform.parent = transform;
        newCircle.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        newCircle.transform.localScale = Vector3.one * (radius * 2f);

        SpriteRenderer sr = newCircle.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = new Color(1f, 1f, 1f, stampAlpha);
        sr.sortingOrder = -5;

        CircleCollider2D col = newCircle.AddComponent<CircleCollider2D>();
        col.usedByComposite = true;

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(newCircle, "Place Stamp Circle");
#endif
    }
}

// ─── 유니티 에디터 스냅 및 UI ───
#if UNITY_EDITOR
[CustomEditor(typeof(TargetShapeBuilder))]
public class TargetShapeBuilderEditor : Editor
{
    private void OnSceneGUI()
    {
        TargetShapeBuilder builder = (TargetShapeBuilder)target;
        Event e = Event.current;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector2 mouseWorldPos = ray.origin;

        float currentRadius = builder.CurrentRadius;
        float currentVol = builder.CurrentVolume;

        // 1. 마우스 위치에 원형 가이드라인 표시
        Handles.color = new Color(0f, 1f, 0.4f, 0.8f);
        Handles.DrawWireDisc(mouseWorldPos, Vector3.forward, currentRadius);

        // 2. [핵심] 마우스 옆에 현재 부피 단계 라벨 표시
        GUIStyle labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.yellow;
        labelStyle.fontSize = 13;
        labelStyle.fontStyle = FontStyle.Bold;

        string volText = currentVol < 1f ? $"{currentVol}개분 (자르기)" : $"{currentVol}개분 (합치기)";
        Handles.Label(mouseWorldPos + new Vector2(currentRadius + 0.15f, 0.1f), $"부피: {volText}\n반지름: {currentRadius:F2}m", labelStyle);

        HandleUtility.Repaint();

        // 3. 마우스 좌클릭: 도장 찍기
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            builder.PlaceCircle(mouseWorldPos);
            e.Use();
        }

        // 4. Shift + 마우스 휠: 부피 단계(Step) 스냅 이동
        if (e.type == EventType.ScrollWheel && e.shift)
        {
            float delta = Mathf.Abs(e.delta.y) > 0.01f ? e.delta.y : e.delta.x;

            Undo.RecordObject(builder, "Change Volume Step");
            if (delta > 0) // 휠 아래: 축소
                builder.currentStepIndex = Mathf.Max(0, builder.currentStepIndex - 1);
            else // 휠 위: 확대
                builder.currentStepIndex = Mathf.Min(TargetShapeBuilder.VolumeSteps.Length - 1, builder.currentStepIndex + 1);

            EditorUtility.SetDirty(builder);
            e.Use();
        }

        // 5. [ 키 / ] 키로도 단계 스냅 조절
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.LeftBracket)
            {
                Undo.RecordObject(builder, "Decrease Step");
                builder.currentStepIndex = Mathf.Max(0, builder.currentStepIndex - 1);
                EditorUtility.SetDirty(builder);
                e.Use();
            }
            else if (e.keyCode == KeyCode.RightBracket)
            {
                Undo.RecordObject(builder, "Increase Step");
                builder.currentStepIndex = Mathf.Min(TargetShapeBuilder.VolumeSteps.Length - 1, builder.currentStepIndex + 1);
                EditorUtility.SetDirty(builder);
                e.Use();
            }
        }
    }
}
#endif