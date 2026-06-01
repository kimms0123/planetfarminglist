using UnityEngine;
using TMPro;

// 화살표가 한자리에 고정된 채 "방향만" 반원으로 회전하는 버전 (해시계 방식).
//  아침(startHour) -> 왼쪽을 가리킴
//  정오쯤          -> 위를 가리킴
//  밤(endHour)     -> 오른쪽을 가리킴
// 클래스 이름 그대로(TimeUI)라 기존 파일을 덮어쓰면 됩니다.
public class TimeUI : MonoBehaviour
{
    [Header("텍스트")]
    public TextMeshProUGUI timeText;   // "오전 6:00"
    public TextMeshProUGUI dateText;   // "봄 1일차"

    [Header("시간 화살표 (제자리에서 방향만 회전)")]
    public RectTransform indicator;    // 화살표. 꼬리가 회전축이 되도록 pivot을 아래쪽에 둘 것.

    [Tooltip("하루 시작 때 화살표 각도(도). 90=왼쪽을 가리킴")]
    public float startAngle = 90f;
    [Tooltip("하루 끝 때 화살표 각도(도). -90=오른쪽을 가리킴")]
    public float endAngle = -90f;
    // ※ 위→오른쪽으로만 돌리고 싶다 같은 변형은 이 두 값만 바꾸면 됨.
    //   기본값(90 -> -90)이면 왼→위→오른쪽으로 180도(반원) 회전.

    [Tooltip("분 사이도 보간해 매끄럽게 회전")]
    public bool smoothIndicator = true;

    void Update()
    {
        var tm = TimeManager.Instance;
        if (tm == null) return;

        if (timeText != null) timeText.text = tm.GetTimeString();
        if (dateText != null) dateText.text = tm.GetDateString();

        UpdateIndicator(tm);
    }

    void UpdateIndicator(TimeManager tm)
    {
        if (indicator == null) return;

        float totalHours = tm.endHour - tm.startHour;   // 예: 26-6 = 20
        if (totalHours <= 0f) return;

        // 하루 진행도 0~1
        float minuteFrac = smoothIndicator ? tm.MinuteProgress : 0f;
        float elapsed = (tm.hour - tm.startHour) + (tm.minute + minuteFrac) / 60f;
        float t = Mathf.Clamp01(elapsed / totalHours);

        // 각도만 보간: t=0 -> startAngle, t=1 -> endAngle
        float angle = Mathf.Lerp(startAngle, endAngle, t);
        indicator.localEulerAngles = new Vector3(0f, 0f, angle);
    }
}