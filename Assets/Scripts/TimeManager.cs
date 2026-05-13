using UnityEngine;
using UnityEngine.Events;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("시간 설정")]
    public int startHour = 6;
    public int endHour = 26;
    public float secondsPerMinute = 0.1f;

    [Header("현재 시간")]
    public int day = 1;
    public int hour = 6;
    public int minute = 0;

    [Header("계절")]
    // 기획서: 봄 1~14일, 여름 15~21일, 가을 22~35일, 겨울 추가
    public Season currentSeason = Season.Spring;

    [Header("이벤트")]
    public UnityEvent OnDayPass;
    public UnityEvent OnHourPass;

    private float timer = 0f;
    private bool isSleeping = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        hour = startHour;
        minute = 0;
        UpdateSeason();
    }

    void Update()
    {
        if (isSleeping) return;

        timer += Time.deltaTime;
        if (timer >= secondsPerMinute)
        {
            timer = 0f;
            AdvanceMinute();
        }
    }

    void AdvanceMinute()
    {
        minute++;
        if (minute >= 60)
        {
            minute = 0;
            hour++;
            OnHourPass?.Invoke();

            if (hour >= endHour)
                Sleep();
        }
    }

    public void Sleep()
    {
        isSleeping = true;
        day++;

        // 35일 지나면 다시 1일로
        if (day > 35)
            day = 1;

        UpdateSeason();

        hour = startHour;
        minute = 0;
        timer = 0f;

        OnDayPass?.Invoke();
        isSleeping = false;

        Debug.Log($"{GetSeasonString()} {day}일차 아침!");
    }

    void UpdateSeason()
    {
        // 기획서 기준: 봄 1~14, 여름 15~21, 가을 22~35
        if (day >= 1 && day <= 14)
            currentSeason = Season.Spring;
        else if (day >= 15 && day <= 21)
            currentSeason = Season.Summer;
        else if (day >= 22 && day <= 35)
            currentSeason = Season.Fall;
    }

    public string GetSeasonString()
    {
        switch (currentSeason)
        {
            case Season.Spring: return "🌸 봄";
            case Season.Summer: return "☀️ 여름";
            case Season.Fall: return "🍂 가을";
            case Season.Winter: return "❄️ 겨울";
            default: return "";
        }
    }

    public string GetTimeString()
    {
        int displayHour = hour % 24;
        string ampm = hour < 12 ? "오전" : "오후";
        if (hour >= 12) displayHour = hour == 12 ? 12 : hour - 12;
        return $"{ampm} {displayHour}:{minute:00}";
    }

    public string GetDateString()
    {
        return $"{GetSeasonString()} {day}일차";
    }
}