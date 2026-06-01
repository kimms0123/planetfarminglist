using UnityEngine;
using System;

public class RhythmNote : MonoBehaviour
{
    public NoteDirection direction;
    public Action OnMissed;

    private RectTransform rectTransform;
    private RectTransform judgeLineRect;
    private float missRange;
    private float speed = 300f;
    private bool isJudged = false;

    public void Init(NoteDirection dir, RectTransform judgeLine, float missRange)
    {
        this.direction = dir;
        this.judgeLineRect = judgeLine;
        this.missRange = missRange;
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (isJudged) return;

        // 노트 내려오기
        rectTransform.anchoredPosition += Vector2.down * speed * Time.deltaTime;

        // 판정선 아래로 너무 내려가면 Miss
        float dist = GetDistanceToJudgeLine();
        if (rectTransform.anchoredPosition.y < judgeLineRect.anchoredPosition.y - missRange)
        {
            isJudged = true;
            OnMissed?.Invoke();
            Destroy(gameObject);
        }
    }

    public float GetDistanceToJudgeLine()
    {
        if (rectTransform == null || judgeLineRect == null) return float.MaxValue;
        return Mathf.Abs(rectTransform.anchoredPosition.y - judgeLineRect.anchoredPosition.y);
    }
}