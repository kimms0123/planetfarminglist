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

    [Header("해충 애니메이션")]
    [Tooltip("미니게임 패널 안의 해충들")]
    public BugAnimator[] bugAnimators;

    [Tooltip("성공/보통 시 작물 표시할 sprite (선택)")]
    public Image cropDisplayImage;

    [Header("★ 스프레이 애니메이션")]
    [Tooltip("살충제 스프레이")]
    public SprayAnimator sprayAnimator;

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
    public float minDuration = 0.8f;
    public float maxDuration = 2.0f;

    public void StartMiniGame(Vector3Int cellPos)
    {
        if (IsPlaying) return;

        currentCellPos = cellPos;
        IsPlaying = true;
        inputReceived = false;
        elapsedTime = 0f;

        roundDuration = Random.Range(minDuration, maxDuration);
        Debug.Log($"이번 라운드 속도: {roundDuration:F2}초");

        PlayerController.IsInputLocked = true;

        targetCenter = Random.Range(targetCenterMin, targetCenterMax);

        // 모든 해충 활성화 + 이동 애니메이션 시작
        ActivateBugs();

        // 스프레이 idle 상태로 (1번 sprite)
        if (sprayAnimator != null)
            sprayAnimator.SetIdleSprite();

        pestMiniGamePanel.SetActive(true);
        if (resultText != null) resultText.text = "";

        UpdateTargetZoneUI();

        Debug.Log($"해충 박멸 미니게임 시작! 타겟:{targetCenter:F2}");

        StartCoroutine(MiniGameLoop());
    }

    void ActivateBugs()
    {
        if (bugAnimators == null) return;

        foreach (var bug in bugAnimators)
        {
            if (bug == null) continue;
            bug.gameObject.SetActive(true);
        }
    }

    void UpdateTargetZoneUI()
    {
        if (targetZone == null || timingBar == null) return;

        float barWidth = timingBar.rect.width;
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

            UpdateMarker(t);

            if (!inputReceived && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                inputReceived = true;
                Debug.Log($"Space 입력! 위치:{t:F2}");
                ProcessResult(t);
                yield break;
            }

            if (elapsedTime >= roundDuration)
            {
                Debug.Log("시간 초과 → Fail");
                ProcessResult(-1f);
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
            result = PestEventResult.Fail;
            if (resultText != null) resultText.text = "시간 초과...";
        }
        else
        {
            float diff = Mathf.Abs(markerPos - targetCenter);

            if (diff <= successWindow)
            {
                result = PestEventResult.Success;
                if (resultText != null)
                {
                    resultText.text = "성공! 칙!";
                    resultText.color = Color.green;
                }
                Debug.Log("Success!");
            }
            else if (diff <= neutralWindow)
            {
                result = PestEventResult.Neutral;
                if (resultText != null)
                {
                    resultText.text = "보통";
                    resultText.color = Color.yellow;
                }
                Debug.Log("Neutral!");
            }
            else
            {
                result = PestEventResult.Fail;
                if (resultText != null)
                {
                    resultText.text = "실패...";
                    resultText.color = Color.red;
                }
                Debug.Log("Fail!");
            }
        }

        // 결과에 따라 해충 + 스프레이 애니메이션 처리
        StartCoroutine(PlayResultAnimation(result));
    }

    IEnumerator PlayResultAnimation(PestEventResult result)
    {
        if (result == PestEventResult.Success || result == PestEventResult.Neutral)
        {
            // 성공/보통 → 스프레이 뿌리기 + 해충 죽음
            if (sprayAnimator != null)
                sprayAnimator.PlaySprayAnimation();

            KillAllBugs();
        }
        // 실패 → 스프레이는 1번 그대로, 해충도 그대로 움직임

        yield return new WaitForSeconds(resultDelay);

        ClosePanel(result);
    }

    void KillAllBugs()
    {
        if (bugAnimators == null) return;

        foreach (var bug in bugAnimators)
        {
            if (bug == null) continue;
            bug.PlayDieAnimation();
        }

        Debug.Log("모든 해충 처치!");
    }

    void ClosePanel(PestEventResult result)
    {
        pestMiniGamePanel.SetActive(false);
        IsPlaying = false;

        PlayerController.IsInputLocked = false;

        PestGrowthEventManager.Instance?.OnMiniGameResult(currentCellPos, result);
    }
}