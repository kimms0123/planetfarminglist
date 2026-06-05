using UnityEngine;

/// <summary>
/// FCM 멤버십 가중 응대 결합 (보고서 6장)
///
/// RBFN은 '이번 거래'(미시)를, FCM은 '플레이어 유형'(거시)을 본다.
/// 최종 NPC 응대 = RBFN 출력을 FCM 멤버십으로 가중 결합한 결과.
///
///   price_final    = Σ_c u_c·(1 + priceBias_c)  +  (RBF_PriceMult − 1.0)   → clamp[0.85,1.15]
///   affinity_final = RBF_AffinityDelta · (1 + u_관계)
///   tone           = Σ_c u_c·toneBase_c  +  0.2·affinityLevel
///
/// 멤버십 인덱스: 0 = 직판, 1 = 관계, 2 = 납품  (FCMSalesAnalyzer와 동일 순서)
/// </summary>
public static class ResponseCombiner
{
    // 클러스터 거시 성향 (직판, 관계, 납품)
    private static readonly float[] priceBias = { 0.08f, 0.05f, -0.05f };
    private static readonly float[] toneBase = { 0.70f, 0.75f, 0.50f };

    private const float PRICE_MIN = 0.85f;
    private const float PRICE_MAX = 1.15f;

    public struct Response
    {
        public float price;     // 최종 가격 보정 계수
        public float affinity;  // 최종 친밀도 변화량
        public float tone;      // 대사 톤 점수 (0~1+)
    }

    public enum Tone { Cold, Neutral, Friendly, VeryFriendly }

    /// <summary>RBFN 출력 + FCM 멤버십 → 최종 응대값.</summary>
    public static Response Combine(float[] u, float rbfPrice, float rbfAffinity, float affinityLevel)
    {
        float[] m = Normalize(u);

        float macro = 0f;
        for (int c = 0; c < 3; c++) macro += m[c] * (1f + priceBias[c]);
        float price = Mathf.Clamp(macro + (rbfPrice - 1f), PRICE_MIN, PRICE_MAX);

        // 관계형(인덱스 1)일수록 친밀도 적립 가속
        float affinity = rbfAffinity * (1f + m[1]);

        float tone = ComputeTone(m, affinityLevel);

        return new Response { price = price, affinity = affinity, tone = tone };
    }

    /// <summary>RBFN 없이 톤만 필요할 때(상점 첫 진입 등).</summary>
    public static float ComputeTone(float[] u, float affinityLevel)
    {
        float[] m = Normalize(u);
        float tone = 0f;
        for (int c = 0; c < 3; c++) tone += m[c] * toneBase[c];
        tone += 0.2f * Mathf.Clamp01(affinityLevel);
        return tone;
    }

    public static Tone ToTone(float t)
    {
        if (t < 0.30f) return Tone.Cold;
        if (t < 0.60f) return Tone.Neutral;
        if (t < 0.85f) return Tone.Friendly;
        return Tone.VeryFriendly;
    }

    // 멤버십 합이 0이거나 비정상일 때 안전 처리
    private static float[] Normalize(float[] u)
    {
        float[] m = new float[3];
        float s = 0f;
        for (int c = 0; c < 3 && c < u.Length; c++) { m[c] = Mathf.Max(0f, u[c]); s += m[c]; }
        if (s <= 1e-5f) return new float[] { 0f, 0f, 0f };
        for (int c = 0; c < 3; c++) m[c] /= s;
        return m;
    }
}