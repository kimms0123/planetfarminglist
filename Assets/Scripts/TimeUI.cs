using UnityEngine;
using TMPro;

public class TimeUI : MonoBehaviour
{
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI dateText;

    void Update()
    {
        if (TimeManager.Instance == null) return;
        timeText.text = TimeManager.Instance.GetTimeString();
        if (dateText != null)
            dateText.text = TimeManager.Instance.GetDateString();
    }
}