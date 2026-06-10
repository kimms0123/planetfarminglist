using UnityEngine;
using System.Collections.Generic;


public class FCMSalesAnalyzer : MonoBehaviour
{
    public static FCMSalesAnalyzer Instance;

    public enum ClusterType
    {
        None,         // 거래 데이터 부족
        Direct,       // 직판형 — 고급 물건을 소량씩 빠르게
        Relational,   // 관계형 — 중간가·느긋·단골 지향
        Wholesale     // 납품형 — 저단가라도 대량 즉시 처분
    }

    public const int CLUSTER_COUNT = 3;

    [Header("클러스터 중심 (3차원 행동벡터 [평균가, 판매속도, 대량비율])")]
    [SerializeField] private float[] directCenter = { 0.75f, 0.65f, 0.20f };
    [SerializeField] private float[] relationalCenter = { 0.50f, 0.30f, 0.50f };
    [SerializeField] private float[] wholesaleCenter = { 0.40f, 0.90f, 0.90f };

    [Header("FCM 파라미터")]
    [SerializeField] private float fuzziness = 2f;          // 퍼지 계수 m
    [SerializeField] private int minTradesForAnalysis = 3;
    [SerializeField] private int smoothingWindow = 5;     // 최근 N건 평균

    [Header("대사 안정화 (히스테리시스)")]
    [Tooltip("새 유형이 이 횟수만큼 연속 우세해야 대사 유형이 바뀐다")]
    [SerializeField] private int clusterConfirmCount = 2;
    [Tooltip("새 유형의 소속도가 이 값 이상이어야 전환 허용")]
    [SerializeField] private float clusterSwitchConfidence = 0.40f;
    [Tooltip("새 유형이 기존 유형 소속도를 이 차이 이상으로 앞서야 전환")]
    [SerializeField] private float clusterSwitchMargin = 0.08f;

    [Header("정규화 기준")]
    [SerializeField] private float maxItemPrice = 100f;
    [SerializeField] private float maxHoldTime = 300f;

    [Header("디버그")]
    [SerializeField] private bool logDebug = true;

    // 플레이 데이터 저장
    private readonly List<Vector3> tradeHistory = new List<Vector3>();

    // 3차원 소속도 (합 = 1)
    public float[] LastMembership { get; private set; } = new float[CLUSTER_COUNT];

    // 매 거래 갱신되는 원시 우세 유형 (수치 계산용)
    public ClusterType DominantCluster { get; private set; } = ClusterType.None;

    // 히스테리시스 적용된 표시용 유형 (대사는 이걸 사용)
    public ClusterType StableCluster { get; private set; } = ClusterType.None;
    // 이번 거래에서 StableCluster가 실제로 바뀌었는지 (대사 갱신 트리거용)
    public bool StableClusterChanged { get; private set; } = false;

    public int TradeCount => tradeHistory.Count;

    // FCM 분류가 시작되었는지 (분류 전이면 '낯선 손님' 응대를 쓴다)
    public bool IsClassified => tradeHistory.Count >= minTradesForAnalysis
                                && StableCluster != ClusterType.None;

    // 히스테리시스 내부 상태
    private ClusterType candidateCluster = ClusterType.None;
    private int candidateStreak = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    public void RecordTrade(int itemPrice, int quantitySold, int totalQuantityInSlot, float holdTimeSeconds = 0f)
    {
        float avgPrice = Mathf.Clamp01(itemPrice / maxItemPrice);
        float sellSpeed = Mathf.Clamp01(1f - (holdTimeSeconds / maxHoldTime));
        float bulkRatio = totalQuantityInSlot > 0
            ? Mathf.Clamp01((float)quantitySold / totalQuantityInSlot)
            : 0f;

        tradeHistory.Add(new Vector3(avgPrice, sellSpeed, bulkRatio));

        // 분석을 먼저 돌려 유형을 갱신한 뒤 로그를 찍는다.
        StableClusterChanged = false;
        if (tradeHistory.Count >= minTradesForAnalysis)
            AnalyzeBehavior();

        if (logDebug)
            Debug.Log($"[FCM] 기록: 가격={avgPrice:F2}, 속도={sellSpeed:F2}, 대량={bulkRatio:F2} " +
                      $"(총 {tradeHistory.Count}건) → 표시유형={ClusterLabel(StableCluster)}");
    }

    private void AnalyzeBehavior()
    {
        // 평활화: 최근 smoothingWindow건 평균
        int n = Mathf.Min(smoothingWindow, tradeHistory.Count);
        Vector3 avg = Vector3.zero;
        for (int i = tradeHistory.Count - n; i < tradeHistory.Count; i++)
            avg += tradeHistory[i];
        avg /= n;

        float[] x = { avg.x, avg.y, avg.z };

        float d1 = Mathf.Max(EuclideanDistance(x, directCenter), 1e-4f);
        float d2 = Mathf.Max(EuclideanDistance(x, relationalCenter), 1e-4f);
        float d3 = Mathf.Max(EuclideanDistance(x, wholesaleCenter), 1e-4f);

        float e = 2f / (fuzziness - 1f);   // m=2 → 2

        // u_ij = 1 / Σ_k (d_ij / d_ik)^(2/(m-1))
        float u1 = 1f / (1f + Mathf.Pow(d1 / d2, e) + Mathf.Pow(d1 / d3, e));
        float u2 = 1f / (Mathf.Pow(d2 / d1, e) + 1f + Mathf.Pow(d2 / d3, e));
        float u3 = 1f / (Mathf.Pow(d3 / d1, e) + Mathf.Pow(d3 / d2, e) + 1f);

        LastMembership[0] = u1;
        LastMembership[1] = u2;
        LastMembership[2] = u3;

        // 원시 우세 유형 (매 거래 갱신, 가격 가중 등 수치 계산은 LastMembership을 그대로 사용)
        float maxU = Mathf.Max(u1, Mathf.Max(u2, u3));
        DominantCluster = maxU == u1 ? ClusterType.Direct
                        : maxU == u2 ? ClusterType.Relational
                        : ClusterType.Wholesale;

        // 표시용 유형 갱신 (히스테리시스)
        UpdateStableCluster();

        if (logDebug)
            Debug.Log($"[FCM] 직판={u1:P0}, 관계={u2:P0}, 납품={u3:P0} " +
                      $"| 원시={ClusterLabel(DominantCluster)} → 표시={ClusterLabel(StableCluster)}");
    }

    private void UpdateStableCluster()
    {
        // 첫 분석: 바로 채택
        if (StableCluster == ClusterType.None)
        {
            StableCluster = DominantCluster;
            StableClusterChanged = true;
            candidateCluster = DominantCluster;
            candidateStreak = 0;
            return;
        }

        // 원시 우세가 현재 표시 유형과 같으면 안정 상태 — 후보 초기화
        if (DominantCluster == StableCluster)
        {
            candidateCluster = StableCluster;
            candidateStreak = 0;
            return;
        }

        // 원시 우세가 표시 유형과 다르다 → 전환 후보로 누적
        if (DominantCluster == candidateCluster) candidateStreak++;
        else { candidateCluster = DominantCluster; candidateStreak = 1; }

        float stableU = MembershipOf(StableCluster);
        float candU = MembershipOf(candidateCluster);

        bool confirmed = candidateStreak >= clusterConfirmCount;
        bool confident = candU >= clusterSwitchConfidence;
        bool clearLead = (candU - stableU) >= clusterSwitchMargin;

        if (confirmed && confident && clearLead)
        {
            StableCluster = candidateCluster;
            StableClusterChanged = true;
            candidateStreak = 0;
        }
    }

    // 보고서 4장: 유클리드 거리 채택
    private float EuclideanDistance(float[] a, float[] b)
    {
        float sum = 0f;
        for (int i = 0; i < a.Length; i++)
            sum += (a[i] - b[i]) * (a[i] - b[i]);
        return Mathf.Sqrt(sum);
    }

    private float MembershipOf(ClusterType c)
    {
        switch (c)
        {
            case ClusterType.Direct: return LastMembership[0];
            case ClusterType.Relational: return LastMembership[1];
            case ClusterType.Wholesale: return LastMembership[2];
            default: return 0f;
        }
    }

    // ClusterType을 한글 라벨로 변환 (로그/UI/대사 선택용)
    private string ClusterLabel(ClusterType c)
    {
        switch (c)
        {
            case ClusterType.Direct: return "직판형";
            case ClusterType.Relational: return "관계형";
            case ClusterType.Wholesale: return "납품형";
            default: return "분석 전";   // 거래 minTradesForAnalysis건 미만
        }
    }

    public string GetDebugInfo()
    {
        if (TradeCount < minTradesForAnalysis)
            return $"거래 {TradeCount}건 (분석까지 {minTradesForAnalysis - TradeCount}건 남음)";
        return $"거래 {TradeCount}건 | 표시유형 {ClusterLabel(StableCluster)} | " +
               $"직판 {LastMembership[0]:P0} | 관계 {LastMembership[1]:P0} | 납품 {LastMembership[2]:P0}";
    }
}