using UnityEngine;
using TMPro;
using MoreMountains.Feedbacks;

public class StageTimer : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TMP_Text timerText;

    [Header("시간 및 연출 설정")]
    [Tooltip("기본 스테이지 제한 시간 (초)")]
    [SerializeField] private float defaultTime = 60f;

    [Tooltip("경고가 시작될 남은 시간 (초)")]
    [SerializeField] private float warningThreshold = 10f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.25f);

    [Header("Feel 피드백 (타임오버 사운드)")]
    [SerializeField] private MMF_Player timeOutFeedback;
    
    // 실패 텍스트 표시
    [SerializeField] private TMP_Text startCountdownText;
    
    private float currentTime;
    private bool isRunning = false;
    private bool isPaused = false; // [추가] 3-2-1 판정 중 일시정지 플래그

    private void Update()
    {
        // 멈춰있거나 일시정지 상태면 카운트다운 건너뜀
        if (!isRunning || isPaused) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning = false;
            UpdateTimerUI();
            HandleTimeOut();
        }
        else
        {
            UpdateTimerUI();
        }
    }

    public void StartTimer(float duration)
    {
        currentTime = (duration > 0f) ? duration : defaultTime;
        isRunning = true;
        isPaused = false;
        UpdateTimerUI();
    }

    public void StopTimer()
    {
        isRunning = false;
        isPaused = false;
    }

    /// <summary>
    /// [핵심 추가] 3-2-1 판정 시작 시 일시정지(true), 판정 취소 시 재개(false)
    /// </summary>
    public void SetPaused(bool pause)
    {
        isPaused = pause;
    }

    private void UpdateTimerUI()
    {
        if (!timerText) return;

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";

        if (currentTime <= warningThreshold)
            timerText.color = warningColor;
        else
            timerText.color = normalColor;
    }

    private void HandleTimeOut()
    {
        startCountdownText.text = " !-FAIL-! ";
        startCountdownText.color = Color.red;
        startCountdownText.gameObject.SetActive(true);
        
        if (timeOutFeedback)
            timeOutFeedback.FeedbacksList[3].Play(transform.position,1f);

        if (StageManager.Instance)
            StageManager.Instance.OnStageTimeOut();
    }
}