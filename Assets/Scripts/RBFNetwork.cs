using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// RBFN (Radial Basis Function Network) - ÆÇ¸Å Çàµ¿ ¿¹Ãø ½Å°æ¸Á
/// 
/// ±¸Á¶:
///   ÀÔ·ÂÃþ (3): FCM ¼Ò¼Óµµ [u_Çö±Ý¿Õ, u_Ç°ÁúÀåÀÎ, u_±ÕÇüÇü]
///   Àº´ÐÃþ (3): °¡¿ì½Ã¾È RBF (°¢ Å¬·¯½ºÅÍº°)
///   Ãâ·ÂÃþ (2): [°¡°Ýº¸Á¤°è¼ö, Ä£ÇÔµµ]
/// 
/// ÇÐ½À: LMS (Least Mean Squares) ¿Â¶óÀÎ ÇÐ½À
/// º¸°í¼­ 3.3, 3.4, 4.4, 5Àý Âü°í
/// </summary>
public class RBFNetwork : MonoBehaviour
{
    public static RBFNetwork Instance;

    [Header("RBFN ÇÏÀÌÆÛÆÄ¶ó¹ÌÅÍ")]
    [SerializeField] private float sigma = 0.5f;           // °¡¿ì½Ã¾È Æø
    [SerializeField] private float learningRate = 0.05f;   // LMS ÇÐ½À·ü ¥ç
    [SerializeField] private int updateThreshold = 3;       // ÇÐ½À ½ÃÀÛ °Å·¡ ¼ö

    [Header("Ãâ·Â Å¬·¥ÇÁ ¹üÀ§")]
    [SerializeField] private float priceMultiplierMin = 0.9f;  // -10%
    [SerializeField] private float priceMultiplierMax = 1.10f; // +10%
    [SerializeField] private float affinityMin = 0f;
    [SerializeField] private float affinityMax = 1f;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // RBF Áß½É (FCM Å¬·¯½ºÅÍ Áß½É¿¡ ´ëÀÀ)
    // ÀÔ·Â °ø°£ÀÌ FCM ¼Ò¼ÓµµÀÌ¹Ç·Î, Áß½ÉÀº "±× Å¬·¯½ºÅÍ¿¡¸¸ 100% ¼Ò¼Ó" º¤ÅÍ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private readonly float[][] rbfCenters = new float[][]
    {
        new float[] { 1.0f, 0.0f, 0.0f },  // Çö±Ý¿Õ Áß½É (Çö±Ý¿Õ¿¡ 100% ¼Ò¼Ó)
        new float[] { 0.0f, 1.0f, 0.0f },  // Ç°ÁúÀåÀÎ Áß½É
        new float[] { 0.0f, 0.0f, 1.0f },  // ±ÕÇüÇü Áß½É
    };

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ãâ·ÂÃþ °¡ÁßÄ¡ W[Ãâ·Âk][Àº´Ðj] - ÈÞ¸®½ºÆ½ ÃÊ±âÈ­
    // weights[0] = °¡°Ý º¸Á¤ °è¼ö
    // weights[1] = Ä£ÇÔµµ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private float[,] weights = new float[2, 3]
    {
        { 1.02f, 1.08f, 1.00f },  // °¡°Ýº¸Á¤: Çö±Ý¿Õ +2%, Ç°ÁúÀåÀÎ +8%, ±ÕÇüÇü 0%
        { 0.70f, 0.60f, 0.50f },  // Ä£ÇÔµµ: Çö±Ý¿Õ 0.7, Ç°ÁúÀåÀÎ 0.6, ±ÕÇüÇü 0.5
    };

    // ÆíÇâ (b)
    private float[] bias = new float[2] { 0f, 0f };

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¸¶Áö¸· ¿¹Ãø °á°ú (¿ÜºÎ Á¶È¸¿ë)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public float PriceMultiplier { get; private set; } = 1.0f;
    public float Affinity { get; private set; } = 0.5f;
    public float[] LastActivations { get; private set; } = new float[3];

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¼øÀüÆÄ - FCM ¼Ò¼Óµµ ¡æ °¡°Ýº¸Á¤, Ä£ÇÔµµ ¿¹Ãø
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void Predict(float[] fcmMembership)
    {
        // 1. RBF Àº´ÐÃþ È°¼ºÈ­
        float[] phi = new float[3];
        for (int j = 0; j < 3; j++)
        {
            float distSq = SquaredDistance(fcmMembership, rbfCenters[j]);
            phi[j] = Mathf.Exp(-distSq / (2f * sigma * sigma));
        }
        LastActivations = phi;

        // 2. Ãâ·ÂÃþ (¼±Çü °áÇÕ + ÆíÇâ)
        float priceOutput = bias[0];
        float affinityOutput = bias[1];
        for (int j = 0; j < 3; j++)
        {
            priceOutput += weights[0, j] * phi[j];
            affinityOutput += weights[1, j] * phi[j];
        }

        // 3. Å¬·¥ÇÁ
        PriceMultiplier = Mathf.Clamp(priceOutput, priceMultiplierMin, priceMultiplierMax);
        Affinity = Mathf.Clamp(affinityOutput, affinityMin, affinityMax);

        Debug.Log($"[RBFN] ¿¹Ãø: °¡°Ý¹èÀ²={PriceMultiplier:F3}, Ä£ÇÔµµ={Affinity:F2} | RBFÈ°¼º={phi[0]:F2}/{phi[1]:F2}/{phi[2]:F2}");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // LMS ¿Â¶óÀÎ ÇÐ½À - º¸»ó ½ÅÈ£·Î °¡ÁßÄ¡ °»½Å
    // °Å·¡ 1°Ç = ÇÐ½À 1½ºÅÜ (º¸°í¼­ 5Àý)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    /// <summary>
    /// °Å·¡ °á°ú·Î ÇÐ½À
    /// </summary>
    /// <param name="fcmMembership">ÇöÀç FCM ¼Ò¼Óµµ</param>
    /// <param name="targetPriceMultiplier">¸ñÇ¥ °¡°Ý ¹èÀ² (ÀÌ¹ø °Å·¡ ¼º°ú ±â¹Ý)</param>
    /// <param name="targetAffinity">¸ñÇ¥ Ä£ÇÔµµ (ÀÌ¹ø °Å·¡ ¸¸Á·µµ ±â¹Ý)</param>
    public void Train(float[] fcmMembership, float targetPriceMultiplier, float targetAffinity)
    {
        // 1. ¼øÀüÆÄ·Î ÇöÀç ¿¹Ãø°ª °è»ê
        Predict(fcmMembership);

        // ÇöÀç ¿¹Ãø°ª
        float currentPrice = bias[0];
        float currentAffinity = bias[1];
        for (int j = 0; j < 3; j++)
        {
            currentPrice += weights[0, j] * LastActivations[j];
            currentAffinity += weights[1, j] * LastActivations[j];
        }

        // 2. ¿ÀÂ÷ °è»ê: ¿ÀÂ÷ = ¸ñÇ¥ - ¿¹Ãø
        float errorPrice = targetPriceMultiplier - currentPrice;
        float errorAffinity = targetAffinity - currentAffinity;

        // 3. LMS °¡ÁßÄ¡ °»½Å: w_kj ¡ç w_kj + ¥ç ¡¤ (t_k - y_k) ¡¤ ¥õ_j
        for (int j = 0; j < 3; j++)
        {
            weights[0, j] += learningRate * errorPrice * LastActivations[j];
            weights[1, j] += learningRate * errorAffinity * LastActivations[j];
        }

        // 4. ÆíÇâ °»½Å: b_k ¡ç b_k + ¥ç ¡¤ (t_k - y_k)
        bias[0] += learningRate * errorPrice;
        bias[1] += learningRate * errorAffinity;

        Debug.Log($"[RBFN ÇÐ½À] ¸ñÇ¥=°¡°Ý{targetPriceMultiplier:F3}/Ä£ÇÔ{targetAffinity:F2}, " +
                  $"¿ÀÂ÷=°¡°Ý{errorPrice:+0.000;-0.000}/Ä£ÇÔ{errorAffinity:+0.00;-0.00}");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // À¯Æ¿¸®Æ¼
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private float SquaredDistance(float[] a, float[] b)
    {
        float sum = 0f;
        for (int i = 0; i < a.Length; i++)
            sum += (a[i] - b[i]) * (a[i] - b[i]);
        return sum;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ä£ÇÔµµ ¡æ ´ë»ç Åæ ¸ÅÇÎ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public enum DialogueTone
    {
        Cold,       // 0.0 ~ 0.3
        Neutral,    // 0.3 ~ 0.6
        Friendly,   // 0.6 ~ 0.85
        VeryFriendly // 0.85 ~ 1.0
    }

    public DialogueTone GetDialogueTone()
    {
        if (Affinity < 0.3f) return DialogueTone.Cold;
        if (Affinity < 0.6f) return DialogueTone.Neutral;
        if (Affinity < 0.85f) return DialogueTone.Friendly;
        return DialogueTone.VeryFriendly;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // µð¹ö±×¿ë
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public string GetDebugInfo()
    {
        return $"°¡°Ý¹èÀ² {PriceMultiplier:F3} | Ä£ÇÔµµ {Affinity:F2} ({GetDialogueTone()}) | " +
               $"w_price=[{weights[0, 0]:F2},{weights[0, 1]:F2},{weights[0, 2]:F2}] | " +
               $"w_aff=[{weights[1, 0]:F2},{weights[1, 1]:F2},{weights[1, 2]:F2}]";
    }
}