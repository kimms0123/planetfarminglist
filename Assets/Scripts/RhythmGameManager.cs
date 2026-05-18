using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

public enum HarvestRhythmResult { Best, Normal, Trash }

public class RhythmGameManager : MonoBehaviour
{
    public static RhythmGameManager Instance;

    [Header("Inspector 조절값")]
    public int noteCount = 10;
    public float totalTime = 8f;     // 전체 제한 시간
    public float resultDelay = 1.5f;

    [Header("등급 기준 (남은 시간 비율)")]
    public float bestThreshold = 0.5f;    // 50% 이상 남으면 Best
    public float normalThreshold = 0.2f;  // 20% 이상 남으면 Normal

    [Header("UI")]
    public GameObject rhythmGamePanel;
    public Transform noteContainer;
    public TextMeshProUGUI judgmentText;
    public Image progressBarFill;
    public TextMeshProUGUI resultText;
    public GameObject resultPanel;

    [Header("노트 프리팹")]
    public GameObject notePrefab;

    // 내부 데이터
    private List<NoteDirection> notes = new List<NoteDirection>();
    private List<RhythmNoteUI> noteUIs = new List<RhythmNoteUI>();
    private int currentNoteIndex = 0;
    private float gameTimer = 0f;
    private bool isPlaying = false;

    // 수확 연결
    private FarmTile currentFarmTile;
    private Vector3Int currentCellPos;

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
        gameTimer = 0f;
        currentNoteIndex = 0;

        notes.Clear();
        noteUIs.Clear();

        // 노트 랜덤 생성
        for (int i = 0; i < noteCount; i++)
            notes.Add((NoteDirection)UnityEngine.Random.Range(0, 4));

        // UI 활성화
        rhythmGamePanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (judgmentText != null) judgmentText.text = "";

        // 노트 UI 생성
        GenerateNoteUIs();

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

        // 첫 번째 노트 강조
        if (noteUIs.Count > 0)
            noteUIs[0].SetState("current");
    }

    IEnumerator GameLoop()
    {
        while (isPlaying)
        {
            gameTimer += Time.deltaTime;

            // 프로그레스 바 갱신 (시간 줄어듦)
            if (progressBarFill != null)
                progressBarFill.fillAmount = 1f - (gameTimer / totalTime);

            // 시간 초과
            if (gameTimer >= totalTime)
            {
                Debug.Log("시간 초과!");
                EndGame(true);
                yield break;
            }

            // 모든 노트 완료
            if (currentNoteIndex >= notes.Count)
            {
                EndGame(false);
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
            // 정답!
            noteUIs[currentNoteIndex].SetState("perfect");
            ShowJudgment("✅", Color.green);
            currentNoteIndex++;

            // 다음 노트 강조
            if (currentNoteIndex < noteUIs.Count)
                noteUIs[currentNoteIndex].SetState("current");

            Debug.Log($"정답! {currentNoteIndex}/{noteCount}");
        }
        else
        {
            // 오답
            ShowJudgment("❌", Color.red);
            Debug.Log($"오답! 눌린키:{dir} 정답:{currentNote}");
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

    void EndGame(bool isTimeout)
    {
        isPlaying = false;
        StopAllCoroutines();

        HarvestRhythmResult result;

        if (isTimeout)
        {
            result = HarvestRhythmResult.Trash;
        }
        else
        {
            float remainRatio = 1f - (gameTimer / totalTime);
            if (remainRatio >= bestThreshold)
                result = HarvestRhythmResult.Best;
            else if (remainRatio >= normalThreshold)
                result = HarvestRhythmResult.Normal;
            else
                result = HarvestRhythmResult.Trash;
        }

        Debug.Log($"리듬게임 종료! 남은시간비율:{1f - gameTimer / totalTime:F2} → {result}");

        // 수확 처리
        CropQuality quality = result == HarvestRhythmResult.Best ? CropQuality.Best
            : result == HarvestRhythmResult.Normal ? CropQuality.Normal
            : CropQuality.Trash;

        if (currentFarmTile != null)
        {
            currentFarmTile.HarvestWithQuality(quality);
            TileManager.Instance.RefreshTile(currentCellPos, currentFarmTile.state);
            Destroy(currentFarmTile.gameObject);
        }

        StartCoroutine(ShowResultAndClose(result));
    }

    IEnumerator ShowResultAndClose(HarvestRhythmResult result)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null)
        {
            switch (result)
            {
                case HarvestRhythmResult.Best: resultText.text = "🌟 최상급 수확!"; break;
                case HarvestRhythmResult.Normal: resultText.text = "✅ 일반 수확"; break;
                case HarvestRhythmResult.Trash: resultText.text = "💀 하위 수확"; break;
            }
        }

        yield return new WaitForSeconds(resultDelay);
        rhythmGamePanel.SetActive(false);
    }
}