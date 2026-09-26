using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("스테이지 목록")]
    [SerializeField] private GameObject[] stagePrefabs;
    [SerializeField] private Transform stageSpawnPoint;

    [Header("하위 모듈 연결")]
    [SerializeField] private ShapeMatcher shapeMatcher;
    [SerializeField] private DropletSpawner dropletSpawner;
    [SerializeField] private StageTimer stageTimer;
    [SerializeField] private ClearHoldValidator clearValidator;
    [SerializeField] private StageIntroCountdown introCountdown;

    [Header("스테이지별 개별 제한 시간")]
    [SerializeField] private float[] stageCustomTimes;

    [Header("엔딩 씬 설정")]
    [SerializeField] private string clearSceneName = "ClearScene";

    [Header("진행 상태 (확인용)")]
    [SerializeField] private int currentStageIndex = 0;

    // [전역 상태] 현재 플레이어가 게임을 조작할 수 있는 상태인지 나타내는 플래그
    public static bool IsGameActive { get; private set; } = false;

    // [변수] 현재 스폰된 스테이지 도형 인스턴스
    private GameObject currentStageInstance;

    // [변수] 스테이지 전환(클리어/타임오버) 진행 중 플래그
    private bool isTransitioning = false;

    // [변수] [핵심 추가] 현재 재시작(리스타트) 처리가 진행 중인지 나타내는 플래그 (중복 방지)
    private bool isRestarting = false;

    private void Awake()
    {
        if (!Instance) Instance = this;
        else Destroy(gameObject);

        // 최신 규칙: TryGetComponent 사용
        if (!shapeMatcher) TryGetComponent<ShapeMatcher>(out shapeMatcher);
        if (!clearValidator) TryGetComponent<ClearHoldValidator>(out clearValidator);
        if (!introCountdown) TryGetComponent<StageIntroCountdown>(out introCountdown);
        if (!dropletSpawner) TryGetComponent<DropletSpawner>(out dropletSpawner);
        if (!stageTimer) TryGetComponent<StageTimer>(out stageTimer);

        if (!stageSpawnPoint) stageSpawnPoint = transform;
    }

    private void OnEnable()
    {
        if (clearValidator)
        {
            clearValidator.OnClearValidated += HandleStageClearValidated;
            clearValidator.OnHoldStateChanged += HandleHoldStateChanged;
        }
    }

    private void OnDisable()
    {
        if (clearValidator)
        {
            clearValidator.OnClearValidated -= HandleStageClearValidated;
            clearValidator.OnHoldStateChanged -= HandleHoldStateChanged;
        }
    }

    private void Start()
    {
        if (stagePrefabs.Length > 0)
        {
            LoadStage(currentStageIndex);
        }
    }

    private void HandleHoldStateChanged(bool isHolding)
    {
        if (stageTimer)
            stageTimer.SetPaused(isHolding);
    }

    private void HandleStageClearValidated()
    {
        if (!isTransitioning && !isRestarting)
        {
            StartCoroutine(StageClearRoutine());
        }
    }

    /// <summary>
    /// [함수] R키 또는 재시작 버튼을 눌렀을 때 현재 스테이지를 안전하고 즉각적으로 재시작하는 함수
    /// </summary>
    public void RestartCurrentStage()
    {
        // 이미 재시작 처리 중이거나 씬 전환 중이면 중복 실행 차단
        if (isRestarting || isTransitioning) return;

        // 1. [핵심] 코루틴 안이 아니라, 여기서 먼저 이전 잔여 코루틴들을 전부 강제 정지!
        StopAllCoroutines();

        // 2. 재시작 락 걸기
        isRestarting = true;
        isTransitioning = true;
        IsGameActive = false;

        // 3. 타이머 및 검증기 상태 즉시 중단 및 리셋
        if (stageTimer) stageTimer.StopTimer();
        if (clearValidator) clearValidator.ResetValidation();

        Debug.Log($"<color=orange>[StageManager] Stage {currentStageIndex + 1} 즉시 재시작 실행</color>");

        // 4. [핵심] 자기 자신을 죽이는 코루틴 없이, 즉시 깨끗하게 스테이지 다시 로드!
        LoadStage(currentStageIndex);
    }
    
    private void Update()
    {
        if (Keyboard.current == null) return;

        // [디버그 단축키 F1] 누르는 즉시 현재 스테이지 강제 클리어 치트키!
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            DebugClearStage();
        }
    }
    
    /// <summary>
    /// [디버그 함수] 테스트 및 포트폴리오 시연용 즉시 강제 클리어 함수 (F1 키 또는 UI 버튼으로 호출)
    /// </summary>
    public void DebugClearStage()
    {
        // 이미 씬 전환 중이거나 재시작 처리 중이면 중복 실행 방지
        if (isTransitioning || isRestarting) return;

        Debug.Log($"<color=cyan>[DEBUG] Stage {currentStageIndex + 1} 즉시 강제 클리어 실행!</color>");

        // 1. 검증기 카운트다운 정지 및 락 해제
        if (clearValidator)
        {
            clearValidator.ResetValidation();
        }

        // 2. 타이머 멈춤
        if (stageTimer)
        {
            stageTimer.StopTimer();
        }

        // 3. 즉시 클리어 코루틴 발동 (다음 스테이지 또는 엔딩 씬으로 이동)
        StartCoroutine(StageClearRoutine());
    }
    
    public void LoadStage(int index)
    {
        if (index < 0 || index >= stagePrefabs.Length)
        {
            Debug.Log("<color=cyan>[StageManager] 모든 스테이지 클리어! 엔딩 씬으로 이동합니다.</color>");
            if (stageTimer) stageTimer.StopTimer();
            IsGameActive = false;
            StartCoroutine(AllStagesClearRoutine());
            return;
        }

        IsGameActive = false;
        currentStageIndex = index;

        // 1. 이전 도형 제거 및 새 도형 소환
        if (currentStageInstance) Destroy(currentStageInstance);
        currentStageInstance = Instantiate(stagePrefabs[currentStageIndex], stageSpawnPoint.position, Quaternion.identity, stageSpawnPoint);

        // 2. ShapeMatcher에 콜라이더 전달
        Collider2D stageCollider = null;
        if (!currentStageInstance.TryGetComponent<Collider2D>(out stageCollider))
        {
            stageCollider = currentStageInstance.GetComponentInChildren<Collider2D>();
        }

        if (stageCollider && shapeMatcher)
            shapeMatcher.SetTargetCollider(stageCollider);

        // 3. 필드 물방울 청소 및 새 물방울 스폰
        ClearAllDropsInScene();

        if (dropletSpawner && shapeMatcher)
        {
            int requiredCount = shapeMatcher.CalculateRequiredDrops();
            dropletSpawner.SpawnDrops(requiredCount);
        }

        // 4. 검증기 리셋
        if (clearValidator) clearValidator.ResetValidation();

        // 5. 시작 카운트다운 루틴 실행
        float stageDuration = (stageCustomTimes != null && index < stageCustomTimes.Length) ? stageCustomTimes[index] : 60f;
        StartCoroutine(StartStageRoutine(stageDuration));
    }

    private IEnumerator StartStageRoutine(float duration)
    {
        yield return new WaitForSeconds(0.4f);

        if (introCountdown)
        {
            yield return introCountdown.PlayCountdownRoutine();
        }

        // [핵심] "START!"가 뜨고 게임이 정식으로 시작된 후에야 모든 락을 해제!
        IsGameActive = true;
        isRestarting = false;
        isTransitioning = false;

        if (stageTimer) stageTimer.StartTimer(duration);
    }

    private IEnumerator StageClearRoutine()
    {
        isTransitioning = true;
        if (stageTimer) stageTimer.StopTimer();

        Debug.Log($"<color=green>[StageManager] Stage {currentStageIndex + 1} 클리어!</color>");
        yield return new WaitForSeconds(1.5f);

        LoadStage(currentStageIndex + 1);
        isTransitioning = false;
    }

    private IEnumerator AllStagesClearRoutine()
    {
        isTransitioning = true;
        yield return new WaitForSeconds(1.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(clearSceneName);
    }

    public void OnStageTimeOut()
    {
        if (isTransitioning || isRestarting) return;
        StartCoroutine(StageFailRoutine());
    }

    private IEnumerator StageFailRoutine()
    {
        isTransitioning = true;
        yield return new WaitForSeconds(1.5f);
        LoadStage(currentStageIndex);
        isTransitioning = false;
    }

    private void ClearAllDropsInScene()
    {
        // 최신 표준: 정렬 모드 생략
        OilDrop[] activeDrops = FindObjectsByType<OilDrop>();
        foreach (var drop in activeDrops)
        {
            if (drop) Destroy(drop.gameObject);
        }
    }
}