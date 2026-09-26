using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UndoManager : MonoBehaviour
{
    public static UndoManager Instance { get; private set; }

    // 복원 중에는 물방울끼리 재충돌/재합성되는 것을 방지하는 안전 플래그
    public static bool IsUndoing { get; private set; } = false;

    [Header("물방울 프리팹 (비어있어도 자동 대처)")]
    [SerializeField] private GameObject oilDropPrefab;

    [Header("설정")]
    [SerializeField] private int maxUndoSteps = 20;

    private class DropData
    {
        public int id;
        public Vector2 position;
        public Vector3 localScale;
        public Vector2 linearVelocity;
        public List<int> connectedIds = new List<int>();
    }

    private class Snapshot
    {
        public List<DropData> drops = new List<DropData>();
    }

    private Stack<Snapshot> undoHistory = new Stack<Snapshot>();
    private Snapshot initialSnapshot; // 게임 시작 시점의 절대적인 원본 상태
    private int lastRecordedFrame = -1;

    private void Awake()
    {
        // 씬에 이미 다른 UndoManager가 살아있을 때만 나를 파괴
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[UndoManager] 중복된 언두 매니저가 있어 제거합니다.");
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        // 씬의 모든 오브젝트가 물리 배치를 끝마칠 때까지 1프레임 대기 후 시작 상태 캡처
        yield return new WaitForEndOfFrame();
        initialSnapshot = CaptureCurrentState();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // [수정] Ctrl 없이 그냥 'Z' 키만 눌러도 즉시 언두 발동!
        // (Ctrl + Z 도 여전히 작동하도록 둘 다 허용)
        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            Undo();
        }
    }

    public void RecordState()
    {
        // 언두 복원 중이거나, 같은 프레임에서 중복 호출되면 무시
        if (IsUndoing || Time.frameCount == lastRecordedFrame) return;
        lastRecordedFrame = Time.frameCount;

        Snapshot snapshot = CaptureCurrentState();
        if (snapshot.drops.Count == 0) return;

        undoHistory.Push(snapshot);

        if (undoHistory.Count > maxUndoSteps)
        {
            // 최대 보관 개수를 넘어가면 오래된 기록 정리
        }
    }

    private Snapshot CaptureCurrentState()
    {
        List<OilDrop> activeDrops = OilDrop.ActiveDrops;
        Snapshot snapshot = new Snapshot();
        Dictionary<OilDrop, int> dropToIdMap = new Dictionary<OilDrop, int>();

        for (int i = 0; i < activeDrops.Count; i++)
        {
            if (!activeDrops[i]) continue;
            
            dropToIdMap[activeDrops[i]] = i;
            var rb = activeDrops[i].GetComponent<Rigidbody2D>();

            snapshot.drops.Add(new DropData
            {
                id = i,
                position = activeDrops[i].transform.position,
                localScale = activeDrops[i].transform.localScale,
                linearVelocity = rb ? rb.linearVelocity : Vector2.zero
            });
        }

        for (int i = 0; i < activeDrops.Count; i++)
        {
            var joints = activeDrops[i].GetComponents<FixedJoint2D>();
            foreach (var j in joints)
            {
                if (j.connectedBody && j.connectedBody.TryGetComponent<OilDrop>(out var targetDrop))
                {
                    if (dropToIdMap.TryGetValue(targetDrop, out int targetId))
                    {
                        snapshot.drops[i].connectedIds.Add(targetId);
                    }
                }
            }
        }

        return snapshot;
    }

    public void Undo()
    {
        // 1. 되돌릴 히스토리가 없으면 최초 시작 상태(initialSnapshot)로 복원
        Snapshot targetSnapshot = null;

        if (undoHistory.Count > 0)
        {
            targetSnapshot = undoHistory.Pop();
        }
        else if (initialSnapshot != null && initialSnapshot.drops.Count > 0)
        {
            // 이미 초기 상태와 똑같다면 아무것도 하지 않음 (더 이상 안 내려감)
            OilDrop[] current = FindObjectsByType<OilDrop>();
            if (current.Length == initialSnapshot.drops.Count)
            {
                Debug.Log("[Undo] 이미 게임 시작 초기 상태입니다.");
                return;
            }
            targetSnapshot = initialSnapshot;
        }
        else
        {
            return;
        }

        StartCoroutine(RestoreRoutine(targetSnapshot));
    }

    private IEnumerator RestoreRoutine(Snapshot snapshot)
    {
        IsUndoing = true; // 충돌 방지 락 걸기

        OilDrop[] currentDrops = FindObjectsByType<OilDrop>();

        // 프리팹이 비어있을 경우 현재 씬에 있는 물방울 중 하나를 템플릿으로 임시 사용
        GameObject spawnTemplate = oilDropPrefab;
        if (!spawnTemplate && currentDrops.Length > 0)
        {
            spawnTemplate = currentDrops[0].gameObject;
        }

        if (!spawnTemplate)
        {
            Debug.LogError("[UndoManager] 복제할 물방울 프리팹이나 템플릿이 없습니다!");
            IsUndoing = false;
            yield break;
        }

        // 기존 물방울 제거
        foreach (var drop in currentDrops)
        {
            Destroy(drop.gameObject);
        }

        // 삭제 처리가 완전히 끝날 때까지 1프레임 대기 (충돌 버그 방지)
        yield return new WaitForFixedUpdate();

        Dictionary<int, OilDrop> restoredDrops = new Dictionary<int, OilDrop>();

        // 스냅샷 데이터 기반 재생성
        foreach (var data in snapshot.drops)
        {
            GameObject newObj = Instantiate(spawnTemplate, data.position, Quaternion.identity);
            newObj.transform.localScale = data.localScale;

            if (newObj.TryGetComponent<OilDrop>(out var drop))
            {
                if (newObj.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.linearVelocity = data.linearVelocity;
                }
                restoredDrops[data.id] = drop;
            }
        }

        // 조인트 재연결
        foreach (var data in snapshot.drops)
        {
            if (!restoredDrops.TryGetValue(data.id, out var sourceDrop)) continue;

            foreach (int targetId in data.connectedIds)
            {
                if (data.id < targetId && restoredDrops.TryGetValue(targetId, out var targetDrop))
                {
                    sourceDrop.RestoreJoint(targetDrop);
                }
            }
        }

        yield return new WaitForFixedUpdate();
        IsUndoing = false; // 안전 락 해제
    }
    
    private void OnDestroy()
    {
        // 내가 파괴될 때 인스턴스 참조를 깨끗하게 비워줌 (에디터 메모리 잔여 버그 방지)
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    /// <summary>
    /// 스테이지가 바뀔 때 이전 기록을 지우고 현재 필드의 물방울들을 새 시작 상태로 영구 보존
    /// </summary>
    public void ResetStageInitialState()
    {
        undoHistory.Clear();
        initialSnapshot = CaptureCurrentState();
        Debug.Log("[UndoManager] 새 스테이지 초기 상태 캡처 완료.");
    }
    
}