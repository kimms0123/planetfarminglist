using UnityEngine;

// 여행 상인 방문 관리.
//  - 상인은 2~3일에 한 번 랜덤하게 방문, 온 날 하루만 머물고 다음날 사라짐.
//  - TimeManager의 OnDayPass(하루 지남)에 OnDayPass()를 연결해서 사용.
//
// 세팅:
//  1) 빈 오브젝트에 이 스크립트 추가 (또는 기존 ShopNpc 옆 매니저 오브젝트)
//  2) shopNpcObject 칸에 상인(ShopNPC가 붙은) 오브젝트를 연결
//  3) TimeManager 인스펙터의 OnDayPass 이벤트에 이 매니저의 OnDayPass() 연결
//     (계절 전환을 OnSeasonChange에 연결했던 것과 같은 방식)
public class ShopVisitManager : MonoBehaviour
{
    [Header("상인 오브젝트 (ShopNPC가 붙은 것)")]
    public GameObject shopNpcObject;

    [Header("방문 주기 (일)")]
    [Tooltip("최소 간격(포함)")]
    public int minDays = 2;
    [Tooltip("최대 간격(포함)")]
    public int maxDays = 3;

    [Header("시작 시 며칠 뒤 첫 방문")]
    public int firstVisitInDays = 2;

    [Header("디버그")]
    public bool logDebug = true;

    private int daysUntilVisit;
    private bool isVisitingToday = false;

    void Start()
    {
        daysUntilVisit = Mathf.Max(1, firstVisitInDays);
        SetNpcActive(false);   // 처음엔 상인 없음
        isVisitingToday = false;

        if (logDebug)
            Debug.Log($"[Shop] 첫 방문까지 {daysUntilVisit}일");
    }

    // ─────────────────────────────────────────────
    // 하루가 지날 때마다 호출 (TimeManager.OnDayPass 에 연결)
    // ─────────────────────────────────────────────
    public void OnDayPass()
    {
        // 1) 어제 상인이 와 있었다면, 오늘은 떠남
        if (isVisitingToday)
        {
            isVisitingToday = false;
            SetNpcActive(false);
            if (logDebug) Debug.Log("[Shop] 상인이 떠났습니다.");
        }

        // 2) 방문 카운트다운
        daysUntilVisit--;

        if (daysUntilVisit <= 0)
        {
            // 오늘 상인 방문!
            isVisitingToday = true;
            SetNpcActive(true);

            // 다음 방문일 = 2~3일 랜덤 (maxDays 포함되도록 +1)
            daysUntilVisit = Random.Range(minDays, maxDays + 1);

            if (logDebug)
                Debug.Log($"[Shop] 상인이 방문했습니다! 다음 방문까지 {daysUntilVisit}일");
        }
        else
        {
            if (logDebug)
                Debug.Log($"[Shop] 다음 방문까지 {daysUntilVisit}일");
        }
    }

    void SetNpcActive(bool active)
    {
        if (shopNpcObject != null)
            shopNpcObject.SetActive(active);
    }

    // 외부에서 상태 확인용 (선택)
    public bool IsMerchantHere => isVisitingToday;
}