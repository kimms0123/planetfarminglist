using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// FCM (Fuzzy C-Means) 기반 플레이어 판매 행동 분석기
/// 
/// [개정] 교수님 피드백 반영 - 4 클러스터로 확장
/// - 3차원 행동 벡터: [평균가격, 판매속도, 대량판매비율]
/// - 4개 클러스터: 직판형 / 관계형 / 납품형 / 균형형
/// </summary>
public class FCMSalesAnalyzer : MonoBehaviour
{
    public static FCMSalesAnalyzer Instance;

    public enum ClusterType
    {
        None,         // 거래 데이터 부족
        Direct,       // 직판형 - 비싼 거 소량씩
        Relational,   // 관계형 - 천천히, 같은 NPC 반복
        Wholesale,    // 납품형 - 대량 즉시 판매
        Balanced      // 균형형 - 다각 경영
    }

    [Header("클러스터 중심 (3차원 행동벡터)")]
    [SerializeField] private float[] directCenter = { 0.7f, 0.6f, 0.2f };
    [SerializeField] private float[] relationalCenter = { 0.5f, 0.3f, 0.5f };
    [SerializeField] private float[] wholesaleCenter = { 0.4f, 0.9f, 0.9f };
    [SerializeField] private float[] balancedCenter = { 0.5f, 0.5f, 0.5f };

    [Header("FCM 파라미터")]
    [SerializeField] private float fuzziness = 2f;
    [SerializeField] private int minTradesForAnalysis = 3;

    [Header("정규화 기준")]
    [SerializeField] private float maxItemPrice = 100f;
    [SerializeField] private float maxHoldTime = 300f;

    private List<Vector3> tradeHistory = new List<Vector3>();

    // 4차원 소속도 (개정: 3 → 4)
    public float[] LastMembership { get; private set; } = new float[4];
    public ClusterType DominantCluster { get; private set; } = ClusterType.None;
    public int TradeCount => tradeHistory.Count;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    public void RecordTrade(int itemPrice, int quantitySold, int totalQuantityInSlot, float holdTimeSeconds = 0f)
    {
        float avgPrice = Mathf.Clamp01((float)itemPrice / maxItemPrice);
        float sellSpeed = Mathf.Clamp01(1f - (holdTimeSeconds / maxHoldTime));
        float bulkRatio = totalQuantityInSlot > 0
            ? Mathf.Clamp01((float)quantitySold / totalQuantityInSlot)
            : 0f;

        Vector3 behaviorVector = new Vector3(avgPrice, sellSpeed, bulkRatio);
        tradeHistory.Add(behaviorVector);

        Debug.Log($"[FCM] 거래 기록: 가격={avgPrice:F2}, 속도={sellSpeed:F2}, 대량={bulkRatio:F2} (총 {tradeHistory.Count}건)");

        if (tradeHistory.Count >= minTradesForAnalysis)
            AnalyzeBehavior();
    }

    private void AnalyzeBehavior()
    {
        // 최근 5건 평균
        int sampleCount = Mathf.Min(5, tradeHistory.Count);
        Vector3 avgVector = Vector3.zero;
        for (int i = tradeHistory.Count - sampleCount; i < tradeHistory.Count; i++)
            avgVector += tradeHistory[i];
        avgVector /= sampleCount;

        float[] vec = { avgVector.x, avgVector.y, avgVector.z };

        float d1 = Mathf.Max(EuclideanDistance(vec, directCenter), 0.0001f);
        float d2 = Mathf.Max(EuclideanDistance(vec, relationalCenter), 0.0001f);
        float d3 = Mathf.Max(EuclideanDistance(vec, wholesaleCenter), 0.0001f);
        float d4 = Mathf.Max(EuclideanDistance(vec, balancedCenter), 0.0001f);

        float exponent = 2f / (fuzziness - 1f);

        float u1 = 1f / (Mathf.Pow(d1 / d1, exponent) + Mathf.Pow(d1 / d2, exponent)
                       + Mathf.Pow(d1 / d3, exponent) + Mathf.Pow(d1 / d4, exponent));
        float u2 = 1f / (Mathf.Pow(d2 / d1, exponent) + Mathf.Pow(d2 / d2, exponent)
                       + Mathf.Pow(d2 / d3, exponent) + Mathf.Pow(d2 / d4, exponent));
        float u3 = 1f / (Mathf.Pow(d3 / d1, exponent) + Mathf.Pow(d3 / d2, exponent)
                       + Mathf.Pow(d3 / d3, exponent) + Mathf.Pow(d3 / d4, exponent));
        float u4 = 1f / (Mathf.Pow(d4 / d1, exponent) + Mathf.Pow(d4 / d2, exponent)
                       + Mathf.Pow(d4 / d3, exponent) + Mathf.Pow(d4 / d4, exponent));

        LastMembership[0] = u1;
        LastMembership[1] = u2;
        LastMembership[2] = u3;
        LastMembership[3] = u4;

        float maxU = Mathf.Max(u1, Mathf.Max(u2, Mathf.Max(u3, u4)));
        if (maxU == u1) DominantCluster = ClusterType.Direct;
        else if (maxU == u2) DominantCluster = ClusterType.Relational;
        else if (maxU == u3) DominantCluster = ClusterType.Wholesale;
        else DominantCluster = ClusterType.Balanced;

        Debug.Log($"[FCM] 분석: 직판={u1:P0}, 관계={u2:P0}, 납품={u3:P0}, 균형={u4:P0} → {DominantCluster}");
    }

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

        return $"거래 {TradeCount}건 | 직판 {LastMembership[0]:P0} | 관계 {LastMembership[1]:P0} | " +
               $"납품 {LastMembership[2]:P0} | 균형 {LastMembership[3]:P0}";
    }
}