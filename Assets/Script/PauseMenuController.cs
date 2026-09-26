using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI 패널 연결")]
    // [컴포넌트] 일시정지 팝업창 전체 패널
    [SerializeField] private GameObject pausePanel;

    [Header("버튼 컴포넌트 연결")]
    // [컴포넌트] 게임 계속하기 버튼
    [SerializeField] private Button resumeButton;

    // [컴포넌트] 현재 스테이지 처음부터 다시하기 버튼
    [SerializeField] private Button restartButton;

    // [컴포넌트] 타이틀 화면으로 나가기 버튼
    [SerializeField] private Button titleButton;

    [Header("씬 이름 설정")]
    // [변수] 돌아갈 타이틀 씬의 정확한 이름
    [SerializeField] private string titleSceneName = "TitleScene";

    // [전역 변수] 현재 게임이 일시정지 상태인지 나타내는 플래그
    public static bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        // 1. 버튼 클릭 리스너 자동 등록
        if (resumeButton) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton) restartButton.onClick.AddListener(RestartStage);
        if (titleButton) titleButton.onClick.AddListener(GoToTitle);

        // 2. 시작 시 일시정지 상태 초기화
        if (pausePanel) pausePanel.SetActive(false);
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // [ESC 키] 일시정지 창 켜기 / 끄기 토글
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    /// <summary>
    /// [함수] 게임을 멈추고 일시정지 팝업창을 띄우는 함수
    /// </summary>
    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f; // [핵심] 유니티 물리 및 타이머 완전 정지!

        if (pausePanel)
            pausePanel.SetActive(true);
    }

    /// <summary>
    /// [함수] 게임을 다시 진행시키고 팝업창을 닫는 함수
    /// </summary>
    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f; // [핵심] 물리 및 타이머 정상 속도 재개!

        if (pausePanel)
            pausePanel.SetActive(false);
    }

    /// <summary>
    /// [함수] 현재 스테이지를 즉시 리셋하고 재시작하는 함수
    /// </summary>
    public void RestartStage()
    {
        ResumeGame();

        if (StageManager.Instance)
        {
            StageManager.Instance.RestartCurrentStage();
        }
    }

    /// <summary>
    /// [함수] 시간을 정상화한 뒤 타이틀 씬으로 돌아가는 함수
    /// </summary>
    public void GoToTitle()
    {
        Time.timeScale = 1f; // 씬 전환 전 시간 복원 필수!
        IsPaused = false;
        SceneManager.LoadScene(titleSceneName);
    }

    private void OnDestroy()
    {
        // 씬이 닫힐 때 시간 스케일이 0으로 남아있는 버그 방지
        Time.timeScale = 1f;
        IsPaused = false;
    }
}