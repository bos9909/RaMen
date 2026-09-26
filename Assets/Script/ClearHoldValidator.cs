using System;
using UnityEngine;
using TMPro;
using MoreMountains.Feedbacks;

public class ClearHoldValidator : MonoBehaviour
{
    public event Action OnClearValidated;

    // 카운트다운 진행 여부(true=판정시작/일시정지, false=판정취소/타이머재개)를 알리는 이벤트
    public event Action<bool> OnHoldStateChanged;

    [Header("검증 대상")]
    [SerializeField] private ShapeMatcher shapeMatcher;

    [Header("카운트다운 설정")]
    [SerializeField] private float holdDuration = 3.0f;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private MMF_Player countdownTickFeedback;
    private MMF_MMSoundManagerSound countdownTickSound;
    private MMF_MMSoundManagerSound clearSound;
    
    [Header("투명도(Alpha) 연출 설정")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float startAlpha = 0.2f;
    [Range(0.8f, 1.0f)]
    [SerializeField] private float endAlpha = 1.0f;

    private float currentHoldTimer = 0f;
    private bool isHoldingSuccess = false;
    private int lastReportedSecond = -1;
    private bool isLocked = false;
    private Color baseColor = Color.yellow;

    private void Awake()
    {
        if (!shapeMatcher)
            shapeMatcher = GetComponent<ShapeMatcher>();

        if (countdownText)
            baseColor = countdownText.color;

        countdownTickSound = countdownTickFeedback.GetFeedbackOfType<MMF_MMSoundManagerSound>("Tick_Sound");
        clearSound = countdownTickFeedback.GetFeedbackOfType<MMF_MMSoundManagerSound>("Clear_Sound");
    }

    private void Update()
    {
        if (isLocked || !shapeMatcher) return;

        if (shapeMatcher.isClear)
        {
            if (!isHoldingSuccess)
            {
                isHoldingSuccess = true;
                currentHoldTimer = holdDuration;
                lastReportedSecond = -1;

                if (countdownText)
                    countdownText.gameObject.SetActive(true);

                // [핵심] 판정 시작 -> 타이머 멈추라고 알림!
                OnHoldStateChanged?.Invoke(true);
            }

            currentHoldTimer -= Time.deltaTime;

            float progress = 1f - Mathf.Clamp01(currentHoldTimer / holdDuration);

            if (countdownText)
            {
                Color c = baseColor;
                c.a = Mathf.Lerp(startAlpha, endAlpha, progress);
                countdownText.color = c;
            }

            int displaySecond = Mathf.CeilToInt(currentHoldTimer);
            if (displaySecond != lastReportedSecond && displaySecond > 0)
            {
                lastReportedSecond = displaySecond;

                if (countdownText)
                    countdownText.text = displaySecond.ToString();

                countdownTickSound.Play(transform.position, 1f);
            }

            if (currentHoldTimer <= 0f)
            {
                isLocked = true;

                if (countdownText)
                {
                    Color c = Color.orange;
                    c.a = 1.0f;
                    countdownText.color = c;
                    countdownText.text = "<color=green>NICE OIL</color>";
                    clearSound.Play(transform.position,1f);
                }

                OnClearValidated?.Invoke();
            }
        }
        else
        {
            if (isHoldingSuccess)
            {
                ResetValidation();
            }
        }
    }

    public void ResetValidation()
    {
        if (isHoldingSuccess)
        {
            // 판정 취소됨 -> 멈췄던 타이머 다시 가동하라고 알림!
            OnHoldStateChanged?.Invoke(false);
        }

        isHoldingSuccess = false;
        isLocked = false;
        currentHoldTimer = holdDuration;
        lastReportedSecond = -1;

        if (countdownText)
        {
            Color c = baseColor;
            c.a = startAlpha;
            countdownText.color = c;
            countdownText.gameObject.SetActive(false);
        }
    }
}