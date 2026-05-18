using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RhythmNoteUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI arrowText;
    public Image background;

    [Header("색상")]
    public Color upcomingColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color currentColor = new Color(1f, 1f, 0f, 1f);
    public Color perfectColor = new Color(0f, 1f, 0.5f, 1f);
    public Color goodColor = new Color(0f, 0.8f, 1f, 1f);
    public Color missColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    public void Setup(NoteDirection dir)
    {
        if (arrowText != null)
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
        Debug.Log($"SetState 호출: {state}"); // ← 추가
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