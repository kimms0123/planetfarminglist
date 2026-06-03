using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

public enum HarvestRhythmResult { Best, Normal, Trash }

public enum PestEventResult
{
    None, Success, Neutral, Fail
}

public class RhythmGameManager : MonoBehaviour
{
    public static RhythmGameManager Instance;

    [Header("Inspector 조절값")]
    public int noteCount = 10;
    public float totalTime = 8f;
    public float resultDelay = 1.5f;

    [Header("등급 기준 (남은 시간 비율)")]
    public float bestThreshold = 0.5f;
    public float normalThreshold = 0.2f;

    [Header("수확 애니메이션 속도")]
    [Tooltip("각 sprite 프레임 표시 시간 (초)")]
    public float harvestFrameDuration = 0.08f;

    [Tooltip("결과 시퀀스 프레임 표시 시간 (초) - 조금 더 천천히")]
    public float resultFrameDuration = 0.15f;

    [Header("UI")]
    public GameObject rhythmGamePanel;
    public Transform noteContainer;
    public TextMeshProUGUI judgmentText;
    public Image progressBarFill;
    public TextMeshProUGUI resultText;
    public GameObject resultPanel;

    [Header("노트 프리팹")]
    public GameObject notePrefab;

    [Header("★ 노트 입력 중 모션 (화면 가운데 큰 Image)")]
    [Tooltip("노트 누르는 동안 작물이 살짝 올라오는 모션 표시")]
    public Image harvestDisplayImage;

    [Header("★ 결과창 모션 Image (ResultPanel 안)")]
    [Tooltip("결과 화면에서 뽑힘/박힘 모션을 보여줄 Image")]
    public Image resultDisplayImage;

    private List<NoteDirection> notes = new List<NoteDirection>();
    private List<RhythmNoteUI> noteUIs = new List<RhythmNoteUI>();
    private int currentNoteIndex = 0;
    private float gameTimer = 0f;
    private bool isPlaying = false;

    private FarmTile currentFarmTile;
    private Vector3Int currentCellPos;

    private Coroutine currentHarvestAnimation;
    private bool gameFinished = false;

    void Awake()
    {
        Instance = this;
    }

    public void StartRhythmGame(FarmTile farmTile, Vector3Int cellPos)
    {
        if (isPlaying) return;
        currentFarmTile = farmTile;
        currentCellPos = cellPos;
        StartGame();
    }

    void StartGame()
    {
        isPlaying = true;
        gameFinished = false;
        gameTimer = 0f;
        currentNoteIndex = 0;

        notes.Clear();
        noteUIs.Clear();

        PlayerController.IsInputLocked = true;

        for (int i = 0; i < noteCount; i++)
            notes.Add((NoteDirection)UnityEngine.Random.Range(0, 4));

        rhythmGamePanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (harvestDisplayImage != null) harvestDisplayImage.enabled = true;
        if (judgmentText != null) judgmentText.text = "";

        GenerateNoteUIs();

        SetIdleSprite();

        Debug.Log("리듬게임 시작!");
        StartCoroutine(GameLoop());
    }

    void GenerateNoteUIs()
    {
        foreach (Transform child in noteContainer)
            Destroy(child.gameObject);
        noteUIs.Clear();

        for (int i = 0; i < notes.Count; i++)
        {
            GameObject noteObj = Instantiate(notePrefab, noteContainer);
            RhythmNoteUI noteUI = noteObj.GetComponent<RhythmNoteUI>();
            if (noteUI == null)
                noteUI = noteObj.AddComponent<RhythmNoteUI>();

            noteUI.Setup(notes[i]);
            noteUIs.Add(noteUI);
        }

        if (noteUIs.Count > 0)
            noteUIs[0].SetState("current");
    }

    IEnumerator GameLoop()
    {
        while (isPlaying)
        {
            gameTimer += Time.deltaTime;

            if (progressBarFill != null)
                progressBarFill.fillAmount = 1f - (gameTimer / totalTime);

            if (gameTimer >= totalTime)
            {
                Debug.Log("시간 초과!");
                HandleTimeout();
                yield break;
            }

            if (currentNoteIndex >= notes.Count)
            {
                // 마지막 노트 처리는 ProcessInput에서 진행됨
                yield break;
            }

            yield return null;
        }
    }

    void Update()
    {
        if (!isPlaying) return;

        if (Keyboard.current.aKey.wasPressedThisFrame) ProcessInput(NoteDirection.Left);
        if (Keyboard.current.sKey.wasPressedThisFrame) ProcessInput(NoteDirection.Down);
        if (Keyboard.current.wKey.wasPressedThisFrame) ProcessInput(NoteDirection.Up);
        if (Keyboard.current.dKey.wasPressedThisFrame) ProcessInput(NoteDirection.Right);
    }

    void ProcessInput(NoteDirection dir)
    {
        if (currentNoteIndex >= notes.Count) return;

        NoteDirection currentNote = notes[currentNoteIndex];

        if (dir == currentNote)
        {
            noteUIs[currentNoteIndex].SetState("perfect");
            ShowJudgment("✅", Color.green);
            currentNoteIndex++;

            if (currentNoteIndex < noteUIs.Count)
                noteUIs[currentNoteIndex].SetState("current");

            bool isLastNote = (currentNoteIndex >= notes.Count);

            if (isLastNote)
            {
                // 마지막 노트까지 다 누름 → 결과 화면으로 (모션은 결과창에서)
                isPlaying = false;
                FinishGame(JudgeResult());
            }
            else
            {
                PlayNormalHarvestAnimation();
            }

            Debug.Log($"정답! {currentNoteIndex}/{noteCount}");
        }
        else
        {
            ShowJudgment("❌", Color.red);
            Debug.Log($"오답! 눌린키:{dir} 정답:{currentNote}");
        }
    }

    // ─────────────────────────────────────────────
    // 일반 노트 애니메이션: 수확1→2→3→4→1
    // ─────────────────────────────────────────────
    void PlayNormalHarvestAnimation()
    {
        if (currentFarmTile == null) return;

        if (currentHarvestAnimation != null)
            StopCoroutine(currentHarvestAnimation);

        currentHarvestAnimation = StartCoroutine(NormalHarvestCoroutine());
    }

    IEnumerator NormalHarvestCoroutine()
    {
        if (currentFarmTile == null || currentFarmTile.cropData == null) yield break;

        CropData crop = currentFarmTile.cropData;

        // 수확 단계 1 → 2 → 3
        if (crop.harvestStageSprites != null)
        {
            for (int i = 0; i < crop.harvestStageSprites.Length; i++)
            {
                ShowMotion(crop.harvestStageSprites[i]);
                yield return new WaitForSeconds(harvestFrameDuration);
            }
        }

        // 수확4 (살짝 뽑혔다가)
        if (crop.harvestFailSprite != null)
        {
            ShowMotion(crop.harvestFailSprite);
            yield return new WaitForSeconds(harvestFrameDuration);
        }

        // 다시 수확1 (원위치)
        SetIdleSprite();

        currentHarvestAnimation = null;
    }

    // ─────────────────────────────────────────────
    // 결과 판정
    // ─────────────────────────────────────────────
    HarvestRhythmResult JudgeResult()
    {
        float remainRatio = 1f - (gameTimer / totalTime);
        if (remainRatio >= bestThreshold)
            return HarvestRhythmResult.Best;
        else if (remainRatio >= normalThreshold)
            return HarvestRhythmResult.Normal;
        else
            return HarvestRhythmResult.Trash;
    }

    void SetIdleSprite()
    {
        if (currentFarmTile == null || currentFarmTile.cropData == null) return;

        CropData crop = currentFarmTile.cropData;
        if (crop.harvestStageSprites != null && crop.harvestStageSprites.Length > 0)
        {
            ShowMotion(crop.harvestStageSprites[0]);
        }
    }

    // ★ 수확 모션을 화면 가운데 Image에 표시 (없으면 밭 스프라이트로 폴백)
    void ShowMotion(Sprite sprite)
    {
        if (sprite == null) return;

        if (harvestDisplayImage != null)
        {
            harvestDisplayImage.enabled = true;
            harvestDisplayImage.sprite = sprite;
            harvestDisplayImage.preserveAspect = true;
        }
        else if (currentFarmTile != null)
        {
            // 가운데 Image를 안 넣었으면 기존처럼 밭에 표시 (폴백)
            currentFarmTile.SetHarvestSprite(sprite);
        }
    }

    void ShowJudgment(string text, Color color)
    {
        if (judgmentText == null) return;
        judgmentText.text = text;
        judgmentText.color = color;
        StopCoroutine("HideJudgment");
        StartCoroutine("HideJudgment");
    }

    IEnumerator HideJudgment()
    {
        yield return new WaitForSeconds(0.3f);
        if (judgmentText != null) judgmentText.text = "";
    }

    void HandleTimeout()
    {
        if (currentHarvestAnimation != null)
        {
            StopCoroutine(currentHarvestAnimation);
            currentHarvestAnimation = null;
        }

        // 시간 초과 - 수확1로
        SetIdleSprite();

        FinishGame(HarvestRhythmResult.Trash);
    }

    void FinishGame(HarvestRhythmResult result)
    {
        if (gameFinished) return;
        gameFinished = true;
        isPlaying = false;

        Debug.Log($"리듬게임 종료! 남은시간비율:{1f - gameTimer / totalTime:F2} → {result}");

        CropQuality quality = result == HarvestRhythmResult.Best ? CropQuality.Best
            : result == HarvestRhythmResult.Normal ? CropQuality.Normal
            : CropQuality.Trash;

        // 결과창 모션용 cropData 먼저 확보 (수확하면 타일이 비워질 수 있으므로)
        CropData cropForMotion = currentFarmTile != null ? currentFarmTile.cropData : null;

        if (currentFarmTile != null)
        {
            currentFarmTile.HarvestWithQuality(quality, currentCellPos);
            TileManager.Instance.RefreshTile(currentCellPos, currentFarmTile.state);
        }

        StartCoroutine(ShowResultAndClose(result, cropForMotion));
    }

    IEnumerator ShowResultAndClose(HarvestRhythmResult result, CropData crop)
    {
        if (resultPanel != null) resultPanel.SetActive(true);

        // 노트 입력 중 모션 Image는 숨기고, 결과창 모션 Image로 전환
        if (harvestDisplayImage != null) harvestDisplayImage.enabled = false;

        // ★ 결과 화면에서 뽑힘/박힘 모션 재생
        yield return StartCoroutine(PlayResultMotion(result, crop));

        // 글씨는 보조로 (원치 않으면 resultText 연결 안 하면 됨)
        if (resultText != null)
        {
            switch (result)
            {
                case HarvestRhythmResult.Best: resultText.text = "최상급 수확!"; break;
                case HarvestRhythmResult.Normal: resultText.text = "일반 수확"; break;
                case HarvestRhythmResult.Trash: resultText.text = "실패..."; break;
            }
        }

        yield return new WaitForSeconds(resultDelay);

        rhythmGamePanel.SetActive(false);

        if (InventoryWindowUI.Instance == null || !InventoryWindowUI.Instance.IsOpen)
            PlayerController.IsInputLocked = false;
    }

    // 결과창 모션: 성공이면 뽁 뽑힘, 실패면 박힌 채 멈춤
    IEnumerator PlayResultMotion(HarvestRhythmResult result, CropData crop)
    {
        if (resultDisplayImage == null || crop == null) yield break;

        resultDisplayImage.enabled = true;
        resultDisplayImage.preserveAspect = true;

        // 공통: 수확 단계 1→2→3 빠르게 (살짝 뽑으려는 동작)
        if (crop.harvestStageSprites != null)
        {
            for (int i = 0; i < crop.harvestStageSprites.Length; i++)
            {
                resultDisplayImage.sprite = crop.harvestStageSprites[i];
                yield return new WaitForSeconds(harvestFrameDuration);
            }
        }

        if (result == HarvestRhythmResult.Trash)
        {
            // 실패: 박힌 채 멈춤 (실패 스프라이트가 있으면 그걸로, 없으면 수확1로)
            if (crop.harvestFailSprite != null)
                resultDisplayImage.sprite = crop.harvestFailSprite;
            else if (crop.harvestStageSprites != null && crop.harvestStageSprites.Length > 0)
                resultDisplayImage.sprite = crop.harvestStageSprites[0];
        }
        else
        {
            // 성공: 결과 시퀀스 = 뽁! 하고 뽑힘
            if (crop.harvestResultSprites != null && crop.harvestResultSprites.Length > 0)
            {
                int endIndex = crop.GetResultEndIndex(result);
                for (int i = 0; i <= endIndex && i < crop.harvestResultSprites.Length; i++)
                {
                    resultDisplayImage.sprite = crop.harvestResultSprites[i];
                    yield return new WaitForSeconds(resultFrameDuration);
                }
            }
        }
    }
}