using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public enum GameLanguage
{
    Korean,   // 한국어
    Japanese  // 일본어
}

[RequireComponent(typeof(CanvasGroup))]
public class KeyGuideHUD : MonoBehaviour
{
    // [컴포넌트] 화면에서 투명도를 켜고 끌 UI 캔버스 그룹
    private CanvasGroup canvasGroup;

    // [컴포넌트] 가이드 텍스트가 표시될 TextMeshPro
    [SerializeField] private TMP_Text guideText;

    // [설정] 현재 표시할 기본 언어 (인스펙터에서 선택 가능)
    [SerializeField] private GameLanguage currentLanguage = GameLanguage.Korean;

    // [변수] 안내창 표시 여부 플래그
    private bool isVisible = true;

    private void Awake()
    {
        // 최신 표준: TryGetComponent 사용
        if (TryGetComponent<CanvasGroup>(out canvasGroup))
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        UpdateGuideText();
    }

    private void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        // [단축키 Tab] 가이드 패널 숨기기 / 보이기 토글
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleGuide();
        }

        // [단축키 L] 한국어 <-> 일본어 실시간 언어 전환
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            ToggleLanguage();
        }

        // [단축키 R] 스테이지 즉시 재시작 실행
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (StageManager.Instance)
            {
                StageManager.Instance.RestartCurrentStage();
            }
        }
    }

    /// <summary>
    /// [함수] 현재 설정된 언어에 맞춰 텍스트 서식을 갱신하는 함수
    /// </summary>
    private void UpdateGuideText()
    {
        if (!guideText) return;

        if (currentLanguage == GameLanguage.Korean)
        {
            // 한국어 조작 가이드
            guideText.text =
                "<color=#FFD54F><b>[조작 가이드]</b></color> <size=70%>(Tab: 토글 / L: 言語)</size>\n\n" +
                "<color=#81D4FA><b>마우스 이동</b></color>\n기름방울 밀기\n\n" +
                "<color=#A5D6A7><b>좌클릭 드래그</b></color>\n완전 융합 (합치기)\n\n" +
                "<color=#FFCC80><b>우클릭 드래그</b></color>\n접착 결합 (붙이기)\n\n" +
                "<color=#EF9A9A><b>Space + 드래그</b></color>\n칼날 절단 (자르기)\n\n" +
                "<color=#CE93D8><b>마우스 휠</b></color>\n카메라 줌인 / 줌아웃\n\n" +
                "<color=#B0BEC5><b>휠 클릭 드래그</b></color>\n화면 시점 이동\n\n" +
                "<color=#80CBC4><b>Ctrl + 휠</b></color>\n밀기 반경 조절\n\n" +
                "<color=#FFF59D><b>Z 키</b></color>\n되돌리기 (Undo)\n\n" +
                "<color=#FF8A80><b>R 키</b></color>\n스테이지 재시작";
        }
        else
        {
            // 일본어 조작 가이드 (日本語)
            guideText.text =
                "<color=#FFD54F><b>【操作ガイド】</b></color> <size=70%>(Tab: 切替 / L: 言語)</size>\n\n" +
                "<color=#81D4FA><b>マウス移動</b></color>\n油を押し動かす\n\n" +
                "<color=#A5D6A7><b>左クリック</b></color>\n完全融合 (合体)\n\n" +
                "<color=#FFCC80><b>右クリック</b></color>\n吸着結合 (くっつける)\n\n" +
                "<color=#EF9A9A><b>Space + ドラッグ</b></color>\nスライス切断 (分ける)\n\n" +
                "<color=#CE93D8><b>ホイール回転</b></color>\nズームイン・アウト\n\n" +
                "<color=#B0BEC5><b>ホイール長押し</b></color>\nカメラ視点移動\n\n" +
                "<color=#80CBC4><b>Ctrl + ホイール</b></color>\n押し出し範囲の調整\n\n" +
                "<color=#FFF59D><b>Z キー</b></color>\n元に戻す (Undo)\n\n" +
                "<color=#FF8A80><b>R キー</b></color>\nステージをやり直す";
        }
    }

    /// <summary>
    /// [함수] L 키를 눌렀을 때 한국어와 일본어를 서로 교체하는 함수
    /// </summary>
    public void ToggleLanguage()
    {
        currentLanguage = (currentLanguage == GameLanguage.Korean) ? GameLanguage.Japanese : GameLanguage.Korean;
        UpdateGuideText();
    }

    /// <summary>
    /// [함수] CanvasGroup의 투명도를 토글하여 가이드창을 숨기거나 표시하는 함수
    /// </summary>
    public void ToggleGuide()
    {
        isVisible = !isVisible;

        if (canvasGroup)
        {
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.blocksRaycasts = isVisible;
        }
    }
}