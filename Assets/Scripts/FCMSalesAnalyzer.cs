using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// FCM (Fuzzy C-Means) 기반 플레이어 거래 패턴 분석기 — 거시(Macro) 분류
///
/// [보고서 v3 반영]
/// - 3차원 행동 벡터: [평균가격, 판매속도, 대량판매비율]  (모두 0~1 정규화)
/// - 3개 클러스터: 직판(Direct) / 관계(Relational) / 납품(Wholesale)
/// - 거리 척도: 유클리드 (보고서 4장 결정)
/// - 최근 5건 평균으로 평활화
/// - RBFN과 완전 독립: 소속도(LastMembership)는 RBFN 입력으로 넘기지 않고,
///   결합(ResponseCombiner) 단계에서 '가중치'로만 사용된다.
/// </summary>
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

    [Header("정규화 기준")]
    [SerializeField] private float maxItemPrice = 100f;
    [SerializeField] private float maxHoldTime = 300f;

    [Header("디버그")]
    [SerializeField] private bool logDebug = true;

    private readonly List<Vector3> tradeHistory = new List<Vector3>();

    // 3차원 소속도 (합 = 1)
    public float[] LastMembership { get; private set; } = new float[CLUSTER_COUNT];
    public ClusterType DominantCluster { get; private set; } = ClusterType.None;
    public int TradeCount => tradeHistory.Count;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>거래 1건을 행동 벡터로 변환해 기록하고 분석을 갱신한다.</summary>
    public void RecordTrade(int itemPrice, int quantitySold, int totalQuantityInSlot, float holdTimeSeconds = 0f)
    {
        float avgPrice = Mathf.Clamp01(itemPrice / maxItemPrice);
        float sellSpeed = Mathf.Clamp01(1f - (holdTimeSeconds / maxHoldTime));
        float bulkRatio = totalQuantityInSlot > 0
            ? Mathf.Clamp01((float)quantitySold / totalQuantityInSlot)
            : 0f;

        tradeHistory.Add(new Vector3(avgPrice, sellSpeed, bulkRatio));

        if (logDebug)
            Debug.Log($"[FCM] 기록: 가격={avgPrice:F2}, 속도={sellSpeed:F2}, 대량={bulkRatio:F2} (총 {tradeHistory.Count}건)");

        if (tradeHistory.Count >= minTradesForAnalysis)
            AnalyzeBehavior();
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

        float maxU = Mathf.Max(u1, Mathf.Max(u2, u3));
        DominantCluster = maxU == u1 ? ClusterType.Direct
                        : maxU == u2 ? ClusterType.Relational
                        : ClusterType.Wholesale;

        if (logDebug)
            Debug.Log($"[FCM] 직판={u1:P0}, 관계={u2:P0}, 납품={u3:P0} → {DominantCluster}");
    }

    // 보고서 4장: 유클리드 거리 채택
    private float EuclideanDistance(float[] a, float[] b)
    {
        float sum = 0f;
        for (int i = 0; i < a.Length; i++)
            sum += (a[i] - b[i]) * (a[i] - b[i]);
        return Mathf.Sqrt(sum);
    }

    public string GetDebugInfo()
    {
        if (TradeCount < minTradesForAnalysis)
            return $"거래 {TradeCount}건 (분석까지 {minTradesForAnalysis - TradeCount}건 남음)";
        return $"거래 {TradeCount}건 | 직판 {LastMembership[0]:P0} | 관계 {LastMembership[1]:P0} | 납품 {LastMembership[2]:P0}";
    }
}