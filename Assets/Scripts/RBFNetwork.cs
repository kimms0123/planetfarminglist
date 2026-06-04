using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// RBFN (Radial Basis Function Network) - NPC 응대 결정 신경망
/// 
/// [개정] 교수님 피드백 반영 - FCM과 완전 독립
/// 
/// 구조:
///   입력층 (7): [FCM 소속도 4개 + 거래 수량 + 거래 가격 + NPC 친밀도]
///   은닉층 (7): 가우시안 RBF (자체 무작위 초기화 후 학습)
///   출력층 (5): [PriceMultiplier, AffinityDelta, DealAcceptRate, DialogueTone, RepeatVisitBonus]
/// 
/// 핵심 변경:
///   - 기존: 은닉층 중심 = FCM 클러스터 중심 (잘못된 결합)
///   - 신규: 은닉층 중심을 무작위 초기화 → 데이터 분포에 맞게 자체 학습
/// </summary>
public class RBFNetwork : MonoBehaviour
{
    public static RBFNetwork Instance;

    // ─────────────────────────────────────────────
    // 네트워크 구조 상수
    // ─────────────────────────────────────────────
    public const int INPUT_DIM = 7;  // FCM 4 + qty + price + affinity
    public const int HIDDEN_DIM = 7;  // 입력 차원과 동일 (RBFN 휴리스틱)
    public const int OUTPUT_DIM = 5;  // Price, Affinity, Deal, Tone, Repeat

    [Header("RBFN 하이퍼파라미터")]
    [SerializeField] private float sigma = 0.5f;
    [SerializeField] private float learningRate = 0.05f;
    [SerializeField] private int randomSeed = 42;

    [Header("출력 클램프 범위")]
    [SerializeField] private float priceMin = 0.85f;
    [SerializeField] private float priceMax = 1.15f;
    [SerializeField] private float affinityDeltaMin = -0.1f;
    [SerializeField] private float affinityDeltaMax = 0.1f;

    // ─────────────────────────────────────────────
    // 은닉층 RBF 뉴런 중심 (자체 독립!)
    // 초기: 무작위 분포 → 학습으로 조정 (Phase 2)
    // ─────────────────────────────────────────────
    private float[][] hiddenCenters;  // [HIDDEN_DIM][INPUT_DIM]

    // 출력층 가중치 W[output][hidden]
    private float[,] weights;  // [OUTPUT_DIM, HIDDEN_DIM]
    private float[] bias;       // [OUTPUT_DIM]

    // 마지막 추론 결과
    public float[] LastActivations { get; private set; } = new float[HIDDEN_DIM];
    public float[] LastOutput { get; private set; } = new float[OUTPUT_DIM];

    // 외부 접근용 출력
    public float PriceMultiplier => Mathf.Clamp(LastOutput[0], priceMin, priceMax);
    public float AffinityDelta => Mathf.Clamp(LastOutput[1], affinityDeltaMin, affinityDeltaMax);
    public float DealAcceptRate => Mathf.Clamp01(LastOutput[2]);
    public float DialogueTone => Mathf.Clamp01(LastOutput[3]);
    public float RepeatVisitBonus => Mathf.Clamp(LastOutput[4], 0f, 0.1f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        InitializeNetwork();
    }

    /// <summary>
    /// 은닉층 중심 무작위 초기화 (FCM과 독립)
    /// 출력층 가중치도 작은 무작위값으로 초기화
    /// </summary>
    private void InitializeNetwork()
    {
        Random.InitState(randomSeed);

        // 은닉층 중심 무작위 (입력 공간 [0,1]^7에 분포)
        hiddenCenters = new float[HIDDEN_DIM][];
        for (int j = 0; j < HIDDEN_DIM; j++)
        {
            hiddenCenters[j] = new float[INPUT_DIM];
            for (int i = 0; i < INPUT_DIM; i++)
                hiddenCenters[j][i] = Random.Range(0.2f, 0.8f);  // 입력 공간 내부
        }

        // 출력층 가중치 작은 무작위값
        weights = new float[OUTPUT_DIM, HIDDEN_DIM];
        bias = new float[OUTPUT_DIM];

        for (int k = 0; k < OUTPUT_DIM; k++)
        {
            bias[k] = GetInitialBias(k);
            for (int j = 0; j < HIDDEN_DIM; j++)
                weights[k, j] = Random.Range(-0.1f, 0.1f);
        }

        Debug.Log("[RBFN] 네트워크 초기화 완료 (입력 7, 은닉 7, 출력 5)");
        Debug.Log($"[RBFN] 은닉 중심은 FCM과 독립, 무작위 분포");
    }

    private float GetInitialBias(int outputIdx)
    {
        // 출력별 초기 편향 (학습 시작점)
        return outputIdx switch
        {
            0 => 1.0f,   // PriceMultiplier: 1.0 (보정 없음)
            1 => 0.0f,   // AffinityDelta: 0
            2 => 0.5f,   // DealAcceptRate: 0.5
            3 => 0.5f,   // DialogueTone: Neutral
            4 => 0.0f,   // RepeatVisitBonus: 0
            _ => 0f
        };
    }

    /// <summary>
    /// 순전파: 7차원 입력 → 5차원 출력
    /// </summary>
    /// <param name="input">7차원: [u_Direct, u_Relational, u_Wholesale, u_Balanced, qty, price, affinity_npc]</param>
    public void Predict(float[] input)
    {
        if (input.Length != INPUT_DIM)
        {
            Debug.LogError($"[RBFN] 입력 차원 오류: {input.Length} != {INPUT_DIM}");
            return;
        }

        // 1. 은닉층 가우시안 활성화
        for (int j = 0; j < HIDDEN_DIM; j++)
        {
            float distSq = SquaredDistance(input, hiddenCenters[j]);
            LastActivations[j] = Mathf.Exp(-distSq / (2f * sigma * sigma));
        }

        // 2. 출력층 (선형 결합)
        for (int k = 0; k < OUTPUT_DIM; k++)
        {
            float sum = bias[k];
            for (int j = 0; j < HIDDEN_DIM; j++)
                sum += weights[k, j] * LastActivations[j];
            LastOutput[k] = sum;
        }

        Debug.Log($"[RBFN] 예측: 가격×{PriceMultiplier:F3} | 친밀±{AffinityDelta:+0.00;-0.00} | " +
                  $"흥정 {DealAcceptRate:P0} | 톤 {DialogueTone:F2} | 단골+{RepeatVisitBonus:F3}");
    }

    /// <summary>
    /// LMS 학습 - 거래 결과 피드백으로 가중치 업데이트
    /// </summary>
    /// <param name="input">7차원 입력 (위와 동일)</param>
    /// <param name="targets">5차원 목표값</param>
    public void Train(float[] input, float[] targets)
    {
        if (targets.Length != OUTPUT_DIM)
        {
            Debug.LogError($"[RBFN] 타깃 차원 오류: {targets.Length} != {OUTPUT_DIM}");
            return;
        }

        // 1. 순전파로 현재 예측 계산
        Predict(input);

        // 2. 출력별 LMS 업데이트: w_kj ← w_kj + η · (t_k - y_k) · φ_j
        for (int k = 0; k < OUTPUT_DIM; k++)
        {
            float error = targets[k] - LastOutput[k];
            for (int j = 0; j < HIDDEN_DIM; j++)
                weights[k, j] += learningRate * error * LastActivations[j];
            bias[k] += learningRate * error;
        }

        Debug.Log($"[RBFN 학습] 타깃=[{targets[0]:F2}/{targets[1]:F2}/{targets[2]:F2}/{targets[3]:F2}/{targets[4]:F2}]");
    }

    // ─────────────────────────────────────────────
    // 입력 벡터 생성 헬퍼 - FCM 결과 + 거래 특성 결합
    // ─────────────────────────────────────────────
    /// <summary>
    /// RBFN 입력 벡터 생성
    /// </summary>
    public static float[] BuildInput(float[] fcmMembership, float qty, float price, float affinityNpc)
    {
        // fcmMembership은 4차원 [Direct, Relational, Wholesale, Balanced]
        float[] input = new float[INPUT_DIM];
        for (int i = 0; i < 4; i++) input[i] = fcmMembership[i];
        input[4] = Mathf.Clamp01(qty);
        input[5] = Mathf.Clamp01(price);
        input[6] = Mathf.Clamp01(affinityNpc);
        return input;
    }

    private float SquaredDistance(float[] a, float[] b)
    {
        float sum = 0f;
        for (int i = 0; i < a.Length; i++)
            sum += (a[i] - b[i]) * (a[i] - b[i]);
        return sum;
    }

    // ─────────────────────────────────────────────
    // 대사 톤 매핑 (DialogueTone 0~1 → enum 4단계)
    // ─────────────────────────────────────────────
    public enum DialogueToneType
    {
        Cold,         // 0.0 ~ 0.3
        Neutral,      // 0.3 ~ 0.6
        Friendly,     // 0.6 ~ 0.85
        VeryFriendly  // 0.85 ~ 1.0
    }

    public DialogueToneType GetToneType()
    {
        float t = DialogueTone;
        if (t < 0.3f) return DialogueToneType.Cold;
        if (t < 0.6f) return DialogueToneType.Neutral;
        if (t < 0.85f) return DialogueToneType.Friendly;
        return DialogueToneType.VeryFriendly;
    }

    public string GetDebugInfo()
    {
        return $"P×{PriceMultiplier:F3} | A{AffinityDelta:+0.00;-0.00} | " +
               $"Deal {DealAcceptRate:P0} | Tone {GetToneType()} | " +
               $"Repeat+{RepeatVisitBonus:F3}";
    }
}