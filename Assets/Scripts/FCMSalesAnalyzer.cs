using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// FCM (Fuzzy C-Means) ±â¹Ý ÇÃ·¹ÀÌ¾î ÆÇ¸Å Çàµ¿ ºÐ¼®±â
/// - 3Â÷¿ø Çàµ¿ º¤ÅÍ: [Æò±Õ°¡°Ý, ÆÇ¸Å¼Óµµ, ´ë·®ÆÇ¸ÅºñÀ²]
/// - 3°³ Å¬·¯½ºÅÍ: Çö±Ý¿Õ / Ç°ÁúÀåÀÎ / ±ÕÇüÇü
/// </summary>
public class FCMSalesAnalyzer : MonoBehaviour
{
    public static FCMSalesAnalyzer Instance;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Å¬·¯½ºÅÍ Á¤ÀÇ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public enum ClusterType
    {
        None,        // °Å·¡ µ¥ÀÌÅÍ ºÎÁ·
        CashKing,    // Çö±Ý¿Õ - ºü¸£°Ô ¸¹ÀÌ
        QualityMaster, // Ç°ÁúÀåÀÎ - ºñ½Ñ °Å À§ÁÖ
        Balanced     // ±ÕÇüÇü
    }

    [Header("Å¬·¯½ºÅÍ Áß½É (º¸°í¼­ 4.2 Âü°í, 3Â÷¿ø ´Ü¼øÈ­ ¹öÀü)")]
    [SerializeField] private float[] cashKingCenter = { 0.3f, 0.9f, 0.8f };
    [SerializeField] private float[] qualityMasterCenter = { 0.8f, 0.3f, 0.4f };
    [SerializeField] private float[] balancedCenter = { 0.5f, 0.5f, 0.5f };

    [Header("FCM ÆÄ¶ó¹ÌÅÍ")]
    [SerializeField] private float fuzziness = 2f;        // ÆÛÁö °è¼ö m
    [SerializeField] private int minTradesForAnalysis = 3; // ºÐ¼® ½ÃÀÛ ÃÖ¼Ò °Å·¡ ¼ö

    [Header("Á¤±ÔÈ­ ±âÁØ")]
    [SerializeField] private float maxItemPrice = 100f;   // °¡°Ý Á¤±ÔÈ­ ±âÁØ
    [SerializeField] private float maxHoldTime = 300f;    // º¸À¯ ½Ã°£ Á¤±ÔÈ­ ±âÁØ (ÃÊ)

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // °Å·¡ µ¥ÀÌÅÍ ´©Àû (3Â÷¿ø Çàµ¿ º¤ÅÍ)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private List<Vector3> tradeHistory = new List<Vector3>();

    // ¸¶Áö¸· ºÐ¼® °á°ú
    public float[] LastMembership { get; private set; } = new float[3]; // [Çö±Ý¿Õ, Ç°ÁúÀåÀÎ, ±ÕÇüÇü]
    public ClusterType DominantCluster { get; private set; } = ClusterType.None;
    public int TradeCount => tradeHistory.Count;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¿ÜºÎ È£Ãâ - ÆÇ¸Å µ¥ÀÌÅÍ ±â·Ï
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    /// <summary>
    /// ÆÇ¸Å 1°ÇÀÌ ¿Ï·áµÉ ¶§¸¶´Ù È£Ãâ
    /// </summary>
    /// <param name="itemPrice">¾ÆÀÌÅÛ 1°³´ç ÆÇ¸Å°¡</param>
    /// <param name="quantitySold">ÆÇ¸ÅÇÑ ¼ö·®</param>
    /// <param name="totalQuantityInSlot">½½·Ô¿¡ ¿ø·¡ ÀÖ´ø ÃÑ ¼ö·®</param>
    /// <param name="holdTimeSeconds">¾ÆÀÌÅÛ È¹µæ ÈÄ °æ°ú ½Ã°£ (ÃÊ), ¸ð¸£¸é 0</param>
    public void RecordTrade(int itemPrice, int quantitySold, int totalQuantityInSlot, float holdTimeSeconds = 0f)
    {
        // 3Â÷¿ø Çàµ¿ º¤ÅÍ °è»ê (0~1 Á¤±ÔÈ­)
        float avgPrice = Mathf.Clamp01((float)itemPrice / maxItemPrice);

        // sell_speed: º¸À¯ ½Ã°£ÀÌ ÂªÀ»¼ö·Ï 1¿¡ °¡±î¿ò (Áï½Ã ÆÇ¸Å = 1)
        float sellSpeed = Mathf.Clamp01(1f - (holdTimeSeconds / maxHoldTime));

        // bulk_ratio: ½½·Ô ÀüÃ¼ ´ëºñ ÆÇ¸Å ºñÀ²
        float bulkRatio = totalQuantityInSlot > 0
            ? Mathf.Clamp01((float)quantitySold / totalQuantityInSlot)
            : 0f;

        Vector3 behaviorVector = new Vector3(avgPrice, sellSpeed, bulkRatio);
        tradeHistory.Add(behaviorVector);

        Debug.Log($"[FCM] °Å·¡ ±â·Ï: °¡°Ý={avgPrice:F2}, ¼Óµµ={sellSpeed:F2}, ´ë·®={bulkRatio:F2} (ÃÑ {tradeHistory.Count}°Ç)");

        // ºÐ¼® °¡´ÉÇÑ µ¥ÀÌÅÍ°¡ ½×ÀÌ¸é ºÐ¼® ½ÇÇà
        if (tradeHistory.Count >= minTradesForAnalysis)
        {
            AnalyzeBehavior();
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // FCM ºÐ¼® - ÃÖ±Ù °Å·¡ µ¥ÀÌÅÍÀÇ Æò±Õ º¤ÅÍ·Î ¼Ò¼Óµµ °è»ê
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void AnalyzeBehavior()
    {
        // ÃÖ±Ù 5°ÇÀÇ Æò±Õ º¤ÅÍ »ç¿ë (ÀÌµ¿ Æò±Õ)
        int sampleCount = Mathf.Min(5, tradeHistory.Count);
        Vector3 avgVector = Vector3.zero;
        for (int i = tradeHistory.Count - sampleCount; i < tradeHistory.Count; i++)
            avgVector += tradeHistory[i];
        avgVector /= sampleCount;

        float[] vec = { avgVector.x, avgVector.y, avgVector.z };

        // °¢ Å¬·¯½ºÅÍ±îÁöÀÇ À¯Å¬¸®µå °Å¸®
        float d1 = EuclideanDistance(vec, cashKingCenter);
        float d2 = EuclideanDistance(vec, qualityMasterCenter);
        float d3 = EuclideanDistance(vec, balancedCenter);

        // °Å¸® 0 ¹æÁö (¼öÄ¡ ¾ÈÁ¤¼º)
        d1 = Mathf.Max(d1, 0.0001f);
        d2 = Mathf.Max(d2, 0.0001f);
        d3 = Mathf.Max(d3, 0.0001f);

        // FCM ¼Ò¼Óµµ °è»ê: u_ij = 1 / ¥Ò_k (d_ij / d_ik)^(2/(m-1))
        float exponent = 2f / (fuzziness - 1f); // m=2 ¡æ exponent=2

        float u1 = 1f / (Mathf.Pow(d1 / d1, exponent) + Mathf.Pow(d1 / d2, exponent) + Mathf.Pow(d1 / d3, exponent));
        float u2 = 1f / (Mathf.Pow(d2 / d1, exponent) + Mathf.Pow(d2 / d2, exponent) + Mathf.Pow(d2 / d3, exponent));
        float u3 = 1f / (Mathf.Pow(d3 / d1, exponent) + Mathf.Pow(d3 / d2, exponent) + Mathf.Pow(d3 / d3, exponent));

        LastMembership[0] = u1;
        LastMembership[1] = u2;
        LastMembership[2] = u3;

        // ¿ì¼¼ Å¬·¯½ºÅÍ °áÁ¤
        if (u1 >= u2 && u1 >= u3) DominantCluster = ClusterType.CashKing;
        else if (u2 >= u1 && u2 >= u3) DominantCluster = ClusterType.QualityMaster;
        else DominantCluster = ClusterType.Balanced;

        Debug.Log($"[FCM] ºÐ¼® °á°ú: Çö±Ý¿Õ={u1:P0}, Ç°ÁúÀåÀÎ={u2:P0}, ±ÕÇüÇü={u3:P0} ¡æ ¿ì¼¼: {DominantCluster}");
    }

    private float EuclideanDistance(float[] a, float[] b)
    {
        float sum = 0f;
        for (int i = 0; i < a.Length; i++)
            sum += (a[i] - b[i]) * (a[i] - b[i]);
        return Mathf.Sqrt(sum);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // NPC ´ë»ç »ý¼º - ¿ì¼¼ Å¬·¯½ºÅÍ ±â¹Ý
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public string GetDialogue()
    {
        // °Å·¡ µ¥ÀÌÅÍ ºÎÁ· - ±âº» ÀÎ»ç
        if (TradeCount < minTradesForAnalysis)
        {
            string[] defaultDialogues = {
                "¾î¼­ ¿Í! ¹¹ ÆÈ °Å¶óµµ ÀÖ¾î?",
                "¼öÈ®¹°Àº ¿©±â¼­ ÆÇ¸ÅÇÒ ¼ö ÀÖ¾î.",
                "ÁÁÀº ¹°°ÇÀÌ¸é °ªÀ» ´õ ÃÄÁÖ°Ú´Ù°í.",
                "Ã³À½ º¸´Â ¾ó±¼ÀÌ±º. ÀÚ, °Å·¡ÇØº¸ÀÚ°í!"
            };
            return defaultDialogues[Random.Range(0, defaultDialogues.Length)];
        }

        // Å¬·¯½ºÅÍº° ¸ÂÃã ´ë»ç
        switch (DominantCluster)
        {
            case ClusterType.CashKing:
                string[] cashKingLines = {
                    "¿À, ¶Ç ¿Ô³ª? ÀÚ³×´Â ÀÏ Ã³¸®°¡ ºü¸¥ °Ô ¸¶À½¿¡ µé¾î.",
                    "°Å·¡·®ÀÌ ¸¹¾Æ¼­ ÁÁ±º. ÀÚ, »¡¸® ÆÈ¾ÆÄ¡¿ìÀÚ°í.",
                    "Çö±ÝÀÌ ÃÖ°íÁö! ÀÚ³× °°Àº ¼Õ´ÔÀÌ ¶Ç ÇÊ¿äÇØ.",
                    "ÀÌ¹ø¿¡µµ ÇÑ ¹æ¿¡ ´Ù ÆÈ °Ç°¡? ÁÁ¾Æ, È¯¿µÀÌÁö."
                };
                return cashKingLines[Random.Range(0, cashKingLines.Length)];

            case ClusterType.QualityMaster:
                string[] qualityLines = {
                    "¿ª½Ã ÀÚ³×´Â º¸´Â ´«ÀÌ ÀÖ¾î. ÁÁÀº ¹°°Ç¸¸ °¡Á®¿À´Â±º.",
                    "ÀÌ·± °í±Þ ¹°°ÇÀº ÈçÄ¡ ¾ÊÁö. °ªÀ» ÈÄÇÏ°Ô ÃÄÁÖ°Ú¾î.",
                    "ÀåÀÎÀÇ ¼Õ±æÀÌ ´À²¸Áö´Â±º. °Å·¡´Â ¾ðÁ¦µç È¯¿µÀÌ¾ß.",
                    "Ç°ÁúÀÌ ´Ù¸£±º. ÀÚ³×¿ÍÀÇ °Å·¡´Â Áñ°Ì´Ù°í."
                };
                return qualityLines[Random.Range(0, qualityLines.Length)];

            case ClusterType.Balanced:
                string[] balancedLines = {
                    "¾ÈÁ¤ÀûÀÎ °Å·¡ ½ºÅ¸ÀÏÀÌ±º. ÁÁ¾Æ, ÁÁ¾Æ.",
                    "ÀÌ°ÍÀú°Í °ñ°í·ç °¡Á®¿Ô±¸¸¸. ¾îµð º¸ÀÚ°í.",
                    "²ÙÁØÇÑ ¼Õ´ÔÀÌÁö. ¿À´ÃÀº ¹» °¡Á®¿Ô³ª?",
                    "±ÕÇü ÀâÈù °Å·¡¾ß. ÀÚ, ½ÃÀÛÇØº¼±î?"
                };
                return balancedLines[Random.Range(0, balancedLines.Length)];

            default:
                return "¾î¼­ ¿Í!";
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // µð¹ö±×¿ë
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public string GetDebugInfo()
    {
        if (TradeCount < minTradesForAnalysis)
            return $"°Å·¡ {TradeCount}°Ç (ºÐ¼® ½ÃÀÛ±îÁö {minTradesForAnalysis - TradeCount}°Ç ³²À½)";

        return $"°Å·¡ {TradeCount}°Ç | Çö±Ý¿Õ {LastMembership[0]:P0} | Ç°ÁúÀåÀÎ {LastMembership[1]:P0} | ±ÕÇüÇü {LastMembership[2]:P0}";
    }
}