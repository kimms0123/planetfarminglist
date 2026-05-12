using UnityEngine;
using UnityEngine.Events;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("시간 설정")]
    public int startHour = 6;        // 시작 시간 (오전 6시)
    public int endHour = 26;         // 강제 수면 시간 (새벽 2시)
    public float secondsPerMinute = 0.1f; // 실제 몇 초가 게임 내 1분인지

    [Header("현재 시간")]
    public int day = 1;
    public int hour = 6;
    public int minute = 0;

    [Header("계절")]
    public string[] seasons = { "봄", "여름", "가을", "겨울" };
    public int currentSeason = 0;
    public int dayPerSeason = 28;

    // 이벤트
    public UnityEvent OnDayPass;      // 하루 지날 때
    public UnityEvent OnHourPass;     // 1시간 지날 때

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

            // 강제 수면
            if (hour >= endHour)
                Sleep();
        }
    }

    public void Sleep()
    {
        isSleeping = true;
        day++;

        // 계절 변경
        if (day > dayPerSeason)
        {
            day = 1;
            currentSeason = (currentSeason + 1) % 4;
        }

        hour = startHour;
        minute = 0;
        timer = 0f;

        OnDayPass?.Invoke();
        isSleeping = false;

        Debug.Log($"{seasons[currentSeason]} {day}일차 아침!");
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
        return $"{seasons[currentSeason]} {day}일차";
    }
}