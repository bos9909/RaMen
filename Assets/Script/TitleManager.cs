using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("씬 전환 설정")]
    // [변수] 게임 시작 시 불러올 실제 퍼즐 게임 씬 이름
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("조작 설명 팝업")]
    // [컴포넌트] 조작 방법 팝업 패널 창
    [SerializeField] private GameObject howToPlayPopup;
    // [컴포넌트] 팝업 안의 조작 설명 텍스트
    [SerializeField] private TMP_Text popupGuideText;

    [Header("버튼 컴포넌트 연결")]
    // [컴포넌트] 게임 시작 버튼
    [SerializeField] private Button startButton;
    // [컴포넌트] 조작 방법 열기 버튼
    [SerializeField] private Button guideButton;
    // [컴포넌트] 조작 방법 닫기 버튼
    [SerializeField] private Button closeGuideButton;
    // [컴포넌트] 언어 변경 버튼
    [SerializeField] private Button languageButton;
    // [컴포넌트] 게임 종료 버튼
    [SerializeField] private Button quitButton;

    [Header("다국어 텍스트 UI")]
    // [컴포넌트] 게임 제목 텍스트
    [SerializeField] private TMP_Text titleText;
    // [컴포넌트] 시작 버튼 텍스트
    [SerializeField] private TMP_Text startBtnText;
    // [컴포넌트] 가이드 버튼 텍스트
    [SerializeField] private TMP_Text guideBtnText;
    // [컴포넌트] 언어 버튼 텍스트
    [SerializeField] private TMP_Text languageBtnText;
    // [컴포넌트] 종료 버튼 텍스트
    [SerializeField] private TMP_Text quitBtnText;

    // [변수] 현재 타이틀 화면 언어
    private GameLanguage currentLanguage = GameLanguage.Korean;

    private void Awake()
    {
        // 1. 버튼 리스너 자동 등록
        if (startButton)
            startButton.onClick.AddListener(StartGame);

        if (guideButton)
            guideButton.onClick.AddListener(OpenGuidePopup);

        if (closeGuideButton)
            closeGuideButton.onClick.AddListener(CloseGuidePopup);

        if (languageButton)
            languageButton.onClick.AddListener(ToggleLanguage);

        if (quitButton)
            quitButton.onClick.AddListener(QuitGame);

        // 2. 팝업 초기 비활성화
        if (howToPlayPopup)
            howToPlayPopup.SetActive(false);

        // 3. 언어 텍스트 초기화
        UpdateAllTexts();
    }

    /// <summary>
    /// [함수] 게임 시작 버튼을 눌렀을 때 인게임 씬으로 이동하는 함수
    /// </summary>
    public void StartGame()
    {
        Debug.Log($"[TitleManager] {gameSceneName} 씬으로 이동합니다.");
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// [함수] 조작 방법 팝업창을 여는 함수
    /// </summary>
    public void OpenGuidePopup()
    {
        if (howToPlayPopup)
            howToPlayPopup.SetActive(true);
    }

    /// <summary>
    /// [함수] 조작 방법 팝업창을 닫는 함수
    /// </summary>
    public void CloseGuidePopup()
    {
        if (howToPlayPopup)
            howToPlayPopup.SetActive(false);
    }

    /// <summary>
    /// [함수] 언어 전환 버튼을 눌렀을 때 한국어 <-> 일본어로 변경하는 함수
    /// </summary>
    public void ToggleLanguage()
    {
        currentLanguage = (currentLanguage == GameLanguage.Korean) ? GameLanguage.Japanese : GameLanguage.Korean;
        UpdateAllTexts();
    }

    /// <summary>
    /// [함수] 현재 선택된 언어에 맞춰 타이틀 UI 모든 텍스트를 일괄 갱신하는 함수
    /// </summary>
    private void UpdateAllTexts()
    {
        if (currentLanguage == GameLanguage.Korean)
        {
            if (titleText) titleText.text = "다 먹고난 라멘 국물 위의 기름으로\n<size=50%><color=#FFE082>~뭔가 모양을 만드는 게임 ~</color></size>";
            if (startBtnText) startBtnText.text = "게임 시작";
            if (guideBtnText) guideBtnText.text = "조작 방법";
            if (languageBtnText) languageBtnText.text = "言語: 日本語";
            if (quitBtnText) quitBtnText.text = "게임 종료";

            if (popupGuideText)
            {
                popupGuideText.text =
                    "<b>[ 조작 방법 안내 ]</b>\n\n" +
                    "• <b>마우스 이동</b> : 기름방울 밀기\n" +
                    "• <b>좌클릭</b> : 물방울 융합 (합치기)\n" +
                    "• <b>우클릭</b> : 물방울 접착 (붙이기)\n" +
                    "• <b>Space + 드래그</b> : 칼날 절단 (자르기)\n" +
                    "• <b>마우스 휠</b> : 줌인 / 줌아웃\n" +
                    "• <b>휠 클릭 드래그</b> : 화면 이동\n" +
                    "• <b>Ctrl + 휠</b> : 밀어내기 반경 조절\n" +
                    "• <b>Z</b> : 되돌리기  |  <b>R</b> : 재시작";
            }
        }
        else
        {
            if (titleText) titleText.text = "ラーメン汁の油で\n<size=50%><color=#FFE082>~ なんか形を作るゲーム ~</color></size>";
            if (startBtnText) startBtnText.text = "ゲーム開始";
            if (guideBtnText) guideBtnText.text = "あそびかた";
            if (languageBtnText) languageBtnText.text = "Language: 한국어";
            if (quitBtnText) quitBtnText.text = "ゲーム終了";

            if (popupGuideText)
            {
                popupGuideText.text =
                    "<b>【 あそびかた 】</b>\n\n" +
                    "• <b>マウス移動</b> : 油を押し動かす\n" +
                    "• <b>左クリック</b> : 完全融合 (合体)\n" +
                    "• <b>右クリック</b> : 吸着結合 (くっつける)\n" +
                    "• <b>Space + ドラッグ</b> : スライス切断 (分ける)\n" +
                    "• <b>ホイール回転</b> : ズームイン・アウト\n" +
                    "• <b>ホイール長押し</b> : カメラ視点移動\n" +
                    "• <b>Ctrl + ホイール</b> : 押し出し範囲の調整\n" +
                    "• <b>Z</b> : 元に戻す  |  <b>R</b> : やり直す";
            }
        }
    }

    /// <summary>
    /// [함수] 게임 종료 함수 (에디터 환경과 실제 빌드 환경 모두 대응)
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[TitleManager] 게임을 종료합니다.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}