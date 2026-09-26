using System;
using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;
using Random = UnityEngine.Random;

public class DropletSpawner : MonoBehaviour
{
    [Header("프리팹 연결")]
    [SerializeField] private GameObject oilDropPrefab;
    [SerializeField] private BowlBoundary bowlBoundary;

    [Header("스폰 연출 설정")]
    [Tooltip("그릇 반지름의 몇 % 위치에 배치할 것인가 (0.75 = 그릇 외곽 75% 지점)")]
    [Range(0.4f, 0.85f)]
    [SerializeField] private float spawnRadiusRatio = 0.72f;

    [Tooltip("물방울이 연속으로 소환되는 간격 시간")]
    [SerializeField] private float spawnInterval = 0.08f;

    [Header("Feel 피드백 (스폰 사운드)")]
    [SerializeField] private MMF_Player spawnFeedback;
    private MMF_MMSoundManagerSound spawnSound;
    private Coroutine spawnCoroutine;

    private void Awake()
    {
        spawnSound = spawnFeedback.GetFeedbackOfType<MMF_MMSoundManagerSound>("Spawn_Sound");
    }

    /// <summary>
    /// 외부(StageManager)에서 필요한 개수를 넘겨받아 스폰 시작
    /// </summary>
    public void SpawnDrops(int count)
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnRoutine(count));
    }

    private IEnumerator SpawnRoutine(int count)
    {
        if (!oilDropPrefab)
        {
            Debug.LogError("[DropletSpawner] OilDropPrefab이 연결되지 않았습니다!");
            yield break;
        }

        // 그릇의 중심 위치와 반지름 가져오기
        Vector3 bowlCenter = (bowlBoundary) ? bowlBoundary.transform.position : Vector3.zero;
        float bowlRadius = (bowlBoundary) ? bowlBoundary.BowlRadius : 4.0f;
        float actualSpawnRadius = bowlRadius * spawnRadiusRatio;

        float startAngle = 140f * Mathf.Deg2Rad;
        float endAngle = 40f * Mathf.Deg2Rad;
        float angleStep = (count > 1) ? (endAngle - startAngle) / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = (count > 1) ? startAngle + (angleStep * i) : 90f * Mathf.Deg2Rad;
            float jitterRadius = actualSpawnRadius + Random.Range(-0.1f, 0.1f);

            // [수정] 그릇의 중심 위치(bowlCenter)를 더해 정확한 월드 좌표에 배치
            Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * jitterRadius;
            Vector3 spawnPos = bowlCenter + spawnOffset;
            spawnPos.z = 0f; // 카메라 z=-10에서 완벽히 보이도록 z=0 고정

            GameObject dropObj = Instantiate(oilDropPrefab, spawnPos, Quaternion.identity);

            // 프리팹 원본 크기 확실히 보장
            dropObj.transform.localScale = oilDropPrefab.transform.localScale;

            if (dropObj.TryGetComponent<OilDrop>(out var drop))
            {
                drop.TriggerSquish(-0.3f);
            }

            spawnSound.Play(transform.position, 1f);

            if (spawnInterval > 0f)
                yield return new WaitForSeconds(spawnInterval);
        }

        yield return new WaitForFixedUpdate();
        UndoManager.Instance?.ResetStageInitialState();
    }
    
}