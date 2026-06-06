using UnityEngine;
using TMPro;

// 파란 시스템 창 스타일용 시계 UI.
//  - 화살표(해시계) 제거, 숫자 텍스트만 표시
//  - 날짜: "봄 3일 수" (요일 포함), 시간: "오전 12:30"
//
// 세팅:
//  - 파란 창 배경 Image 위에 timeText / dateText 배치
//  - 이 스크립트의 두 칸에 각 TextMeshProUGUI 연결
public class TimeUI : MonoBehaviour
{
    [Header("텍스트")]
    public TextMeshProUGUI timeText;   // "오전 12:30"
    public TextMeshProUGUI dateText;   // "봄 3일 수"

    void Update()
    {
        var tm = TimeManager.Instance;
        if (tm == null) return;

        if (timeText != null) timeText.text = tm.GetTimeString();
        if (dateText != null) dateText.text = tm.GetDateString();
    }
}