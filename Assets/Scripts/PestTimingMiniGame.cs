using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class PestTimingMiniGame : MonoBehaviour
{
    public static PestTimingMiniGame Instance;

    [Header("Inspector 조절값")]
    public float roundDuration = 2.0f;
    public float successWindow = 0.06f;
    public float neutralWindow = 0.15f;
    public float targetCenterMin = 0.35f;
    public float targetCenterMax = 0.75f;
    public float resultDelay = 0.8f;

    [Header("UI")]
    public GameObject pestMiniGamePanel;
    public RectTransform marker;
    public RectTransform targetZone;
    public RectTransform timingBar;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI resultText;

    public bool IsPlaying { get; private set; } = false;

    private float elapsedTime = 0f;
    private float targetCenter = 0f;
    private bool inputReceived = false;
    private Vector3Int currentCellPos;

    void Awake()
    {
        Instance = this;
    }

    [Header("속도 랜덤 범위")]
    public float minDuration = 0.8f;  // 가장 빠름
    public float maxDuration = 2.0f;  // 가장 느림

    public void StartMiniGame(Vector3Int cellPos)
    {
        if (IsPlaying) return;

        currentCellPos = cellPos;
        IsPlaying = true;
        inputReceived = false;
        elapsedTime = 0f;

        // ★ 속도 랜덤 설정
        roundDuration = Random.Range(minDuration, maxDuration);
        Debug.Log($"이번 라운드 속도: {roundDuration:F2}초");

        // 플레이어 이동 잠금
        PlayerController.IsInputLocked = true;

        // 타겟 구간 랜덤 설정
        targetCenter = Random.Range(targetCenterMin, targetCenterMax);

        // UI 활성화
        pestMiniGamePanel.SetActive(true);
        if (resultText != null) resultText.text = "";

        UpdateTargetZoneUI();

        Debug.Log($"해충 박멸 미니게임 시작! 타겟:{targetCenter:F2}");

        StartCoroutine(MiniGameLoop());
    }

    void UpdateTargetZoneUI()
    {
        if (targetZone == null || timingBar == null) return;

        float barWidth = timingBar.rect.width;

        // 성공 구간 표시
        float zoneWidth = successWindow * 2f * barWidth;
        float zonePosX = (targetCenter - 0.5f) * barWidth;

        targetZone.sizeDelta = new Vector2(zoneWidth, targetZone.sizeDelta.y);
        targetZone.anchoredPosition = new Vector2(zonePosX, 0f);
    }

    IEnumerator MiniGameLoop()
    {
        while (IsPlaying)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / roundDuration;

            // 마커 이동
            UpdateMarker(t);

            // Space 입력 체크
            if (!inputReceived && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                inputReceived = true;
                Debug.Log($"Space 입력! 위치:{t:F2}");
                ProcessResult(t);
                yield break;
            }

            // 시간 초과
            if (elapsedTime >= roundDuration)
            {
                Debug.Log("시간 초과 → Fail");
                ProcessResult(-1f); // 시간 초과
                yield break;
            }

            yield return null;
        }
    }

    void UpdateMarker(float t)
    {
        if (marker == null || timingBar == null) return;
        float barWidth = timingBar.rect.width;
        float posX = (t - 0.5f) * barWidth;
        marker.anchoredPosition = new Vector2(posX, 0f);
    }

    void ProcessResult(float markerPos)
    {
        PestEventResult result;

        if (markerPos < 0)
        {
            // 시간 초과
            result = PestEventResult.Fail;
        }
        else
        {
            float diff = Mathf.Abs(markerPos - targetCenter);

            if (diff <= successWindow)
            {
                result = PestEventResult.Success;
                if (resultText != null) resultText.text = "성공!";
                Debug.Log("Success!");
            }
            else if (diff <= neutralWindow)
            {
                result = PestEventResult.Neutral;
                if (resultText != null) resultText.text = "보통";
                Debug.Log("Neutral!");
            }
            else
            {
                result = PestEventResult.Fail;
                if (resultText != null) resultText.text = "실패...";
                Debug.Log("Fail!");
            }
        }

        StartCoroutine(ShowResultAndClose(result));
    }

    IEnumerator ShowResultAndClose(PestEventResult result)
    {
        yield return new WaitForSeconds(resultDelay);

        pestMiniGamePanel.SetActive(false);
        IsPlaying = false;

        // 플레이어 이동 잠금 해제
        PlayerController.IsInputLocked = false;

        // 결과 전달
        PestGrowthEventManager.Instance?.OnMiniGameResult(currentCellPos, result);
    }
}