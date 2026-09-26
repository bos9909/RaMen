using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClearSceneController : MonoBehaviour
{
    [Header("버튼 컴포넌트 연결")]
    // [컴포넌트] 처음(1스테이지)부터 다시 플레이하기 버튼
    [SerializeField] private Button replayButton;

    // [컴포넌트] 타이틀 화면으로 돌아가기 버튼
    [SerializeField] private Button titleButton;

    [Header("씬 이름 설정")]
    // [변수] 게임 씬 이름
    [SerializeField] private string gameSceneName = "GameScene";

    // [변수] 타이틀 씬 이름
    [SerializeField] private string titleSceneName = "TitleScene";

    private void Awake()
    {
        if (replayButton)
            replayButton.onClick.AddListener(PlayAgain);

        if (titleButton)
            titleButton.onClick.AddListener(GoToTitle);
    }

    /// <summary>
    /// [함수] 1스테이지부터 다시 도전하는 함수
    /// </summary>
    public void PlayAgain()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// [함수] 타이틀 화면으로 복귀하는 함수
    /// </summary>
    public void GoToTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}