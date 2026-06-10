using UnityEngine;


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