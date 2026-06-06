using UnityEngine;
using UnityEngine.Events;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("시간 설정")]
    public int startHour = 6;
    public int endHour = 26;            // 26 = 새벽 2시. 이 시각 되면 자동으로 Sleep()

    [Tooltip("게임 시간 1틱(10분)이 지나는 데 걸리는 실제 시간(초)")]
    public float secondsPerTick = 1f;   // 예: 1이면 현실 1초마다 게임 10분 경과

    [Tooltip("한 번에 흐르는 분 단위 (10이면 10분씩 점프)")]
    public int minutesPerTick = 10;

    [Header("계절 설정")]
    public int daysPerSeason = 35;      // 한 계절 = 35일

    [Header("현재 시간")]
    public int day = 1;                 // 계절 내 일수 (1 ~ daysPerSeason)
    public int hour = 6;
    public int minute = 0;

    [Header("계절")]
    public Season currentSeason = Season.Spring;

    [Header("이벤트")]
    public UnityEvent OnDayPass;
    public UnityEvent OnHourPass;
    public UnityEvent OnSeasonChange;   // 계절이 바뀌는 순간 호출

    private float timer = 0f;
    private bool isSleeping = false;

    // 한 틱 사이 진행도(0~1). UI 화살표 보간용.
    public float MinuteProgress
    {
        get
        {
            if (secondsPerTick <= 0f) return 0f;
            return Mathf.Clamp01(timer / secondsPerTick);
        }
    }

    void Awake()
    {
        // 씬 전환에도 유지 + 중복 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        if (timer >= secondsPerTick)
        {
            timer = 0f;
            AdvanceTick();
        }
    }

    // 10분(minutesPerTick)씩 시간을 진행
    void AdvanceTick()
    {
        minute += minutesPerTick;

        // 60분 이상 쌓이면 시간으로 올림 (10분 단위라 60에서 딱 떨어짐)
        while (minute >= 60)
        {
            minute -= 60;
            hour++;
            OnHourPass?.Invoke();

            if (hour >= endHour)   // 새벽 2시 넘으면 자동 취침
            {
                Sleep();
                return;
            }
        }
    }

    public void Sleep()
    {
        isSleeping = true;
        day++;

        // 계절 마지막 날을 넘기면 다음 계절 1일로
        if (day > daysPerSeason)
        {
            day = 1;
            AdvanceSeason();   // 계절 전환 (OnSeasonChange 호출)
        }

        hour = startHour;
        minute = 0;
        timer = 0f;

        OnDayPass?.Invoke();
        isSleeping = false;
        Debug.Log($"{GetSeasonString()} {day}일차 아침!");
    }

    // 봄 -> 여름 -> 가을 -> 겨울 -> 봄 ... 순환 (All(4)은 건너뜀)
    void AdvanceSeason()
    {
        currentSeason = (Season)(((int)currentSeason + 1) % 4);
        OnSeasonChange?.Invoke();
        Debug.Log($"계절 변경! → {GetSeasonString()}");
    }

    public string GetSeasonString()
    {
        switch (currentSeason)
        {
            case Season.Spring: return "봄";
            case Season.Summer: return "여름";
            case Season.Fall: return "가을";
            case Season.Winter: return "겨울";
            default: return "";
        }
    }

    // 24~26시 표시 버그 수정 버전
    public string GetTimeString()
    {
        int h = hour % 24;                 // 24->0, 25->1, 26->2
        string ampm = h < 12 ? "오전" : "오후";
        int displayHour = h % 12;
        if (displayHour == 0) displayHour = 12;
        return $"{ampm} {displayHour}:{minute:00}";
    }

    // 요일 (봄 1일 = 월요일 기준). 한 계절 35일은 7의 배수라 계절이 바뀌어도 요일이 자연스럽게 이어짐.
    public string GetDayOfWeekString()
    {
        string[] days = { "월", "화", "수", "목", "금", "토", "일" };
        int idx = (day - 1) % 7;
        return days[idx];
    }

    public string GetDateString()
    {
        return $"{GetSeasonString()} {day}일 {GetDayOfWeekString()}";
    }
}