using System.Collections;
using UnityEngine;
using TMPro;
using MoreMountains.Feedbacks;

public class StageIntroCountdown : MonoBehaviour
{
    [Header("UI 연결")]
    // [컴포넌트] 시작 카운트다운을 표시할 커다란 텍스트 (화면 중앙)
    [SerializeField] private TMP_Text startCountdownText;

    [Header("Feel 사운드 피드백")]
    [SerializeField] private MMF_Player mmf_Player;
    // [컴포넌트] 3, 2, 1 잴 때의 삑 소리
    private MMF_MMSoundManagerSound tickFeedback;
    // [컴포넌트] START! 뜰 때의 경쾌한 시작 휘슬/차임벨 소리
    private MMF_MMSoundManagerSound startFeedback;

    private void Awake()
    {
        if (startCountdownText)
            startCountdownText.gameObject.SetActive(false);

        startFeedback = mmf_Player.GetFeedbackOfType<MMF_MMSoundManagerSound>("Start_Sound");
        tickFeedback = mmf_Player.GetFeedbackOfType<MMF_MMSoundManagerSound>("Tick_Sound");
    }

    /// <summary>
    /// [코루틴] 3, 2, 1, START! 카운트다운을 순차적으로 진행하는 코루틴
    /// </summary>
    public IEnumerator PlayCountdownRoutine()
    {
        if (!startCountdownText)
        {
            yield break;
        }

        // 혹시 이전 텍스트가 남아있을 수 있으므로 강제 초기화
        startCountdownText.text = "";
        startCountdownText.gameObject.SetActive(true);

        // 3 -> 2 -> 1 순차 카운트다운
        for (int i = 3; i >= 1; i--)
        {
            startCountdownText.text = $"<color=#FFD54F>{i}</color>";

            tickFeedback.Play(transform.position,1f);
            
            // 1초 대기
            yield return new WaitForSeconds(1.0f);
        }

        // START! 연출
        startCountdownText.text = "<color=#81D4FA>MOVE THE OIL</color>";
        startFeedback.Play(transform.position, 1f);


        // START 글자를 0.5초 동안 보여줌
        yield return new WaitForSeconds(0.5f);

        startCountdownText.gameObject.SetActive(false);
    }

}