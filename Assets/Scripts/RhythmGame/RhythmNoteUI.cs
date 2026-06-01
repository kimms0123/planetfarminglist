using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RhythmNoteUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI arrowText;  // 호환성 위해 유지 (안 써도 됨)
    public Image background;

    [Header("★ 화살표 Image (텍스트 대신)")]
    [Tooltip("화살표를 표시할 Image (TextMeshPro 대신)")]
    public Image arrowImage;

    [Header("화살표 Sprite")]
    public Sprite leftArrow;   // ← (A)
    public Sprite downArrow;   // ↓ (S)
    public Sprite upArrow;     // ↑ (W)
    public Sprite rightArrow;  // → (D)

    [Header("색상")]
    public Color upcomingColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color currentColor = new Color(1f, 1f, 0f, 1f);
    public Color perfectColor = new Color(0f, 1f, 0.5f, 1f);
    public Color goodColor = new Color(0f, 0.8f, 1f, 1f);
    public Color missColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    public void Setup(NoteDirection dir)
    {
        // 화살표 Sprite 방식 (우선)
        if (arrowImage != null)
        {
            switch (dir)
            {
                case NoteDirection.Left: arrowImage.sprite = leftArrow; break;
                case NoteDirection.Down: arrowImage.sprite = downArrow; break;
                case NoteDirection.Up: arrowImage.sprite = upArrow; break;
                case NoteDirection.Right: arrowImage.sprite = rightArrow; break;
            }
        }

        // 텍스트 방식 (백업, arrowImage 없을 때)
        if (arrowText != null && arrowImage == null)
        {
            switch (dir)
            {
                case NoteDirection.Left: arrowText.text = "←"; break;
                case NoteDirection.Down: arrowText.text = "↓"; break;
                case NoteDirection.Up: arrowText.text = "↑"; break;
                case NoteDirection.Right: arrowText.text = "→"; break;
            }
        }

        SetState("upcoming");
    }

    public void SetState(string state)
    {
        Debug.Log($"SetState 호출: {state}");
        Color col = upcomingColor;
        Vector3 scale = Vector3.one;

        switch (state)
        {
            case "upcoming": col = upcomingColor; scale = Vector3.one; break;
            case "current": col = currentColor; scale = Vector3.one * 1.2f; break;
            case "perfect": col = perfectColor; scale = Vector3.one; break;
            case "good": col = goodColor; scale = Vector3.one; break;
            case "miss": col = missColor; scale = Vector3.one; break;
        }

        if (background != null) background.color = col;
        transform.localScale = scale;
    }
}