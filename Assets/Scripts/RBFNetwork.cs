using UnityEngine;


public class RBFNetwork : MonoBehaviour
{
    public static RBFNetwork Instance;

    public const int INPUT_DIM = 5;  // 수량, 단가, 품질, 친밀도, 계절적합
    public const int HIDDEN_DIM = 5;  // 은닉 노드 수 (설계 자유 → 5로 확정)
    public const int OUTPUT_DIM = 2;  // PriceMultiplier, AffinityDelta

    [Header("RBFN 하이퍼파라미터")]
    [SerializeField] private float sigma = 0.5f;
    [SerializeField] private float learningRate = 0.05f;
    [SerializeField] private int randomSeed = 42;

    [Header("출력 클램프 범위")]
    [SerializeField] private float priceMin = 0.85f;
    [SerializeField] private float priceMax = 1.15f;
    [SerializeField] private float affinityDeltaMin = -0.1f;
    [SerializeField] private float affinityDeltaMax = 0.1f;

    [Header("디버그")]
    [SerializeField] private bool logDebug = true;

    // 은닉 중심 [HIDDEN_DIM][INPUT_DIM] — FCM과 독립, 무작위 초기화
    private float[][] hiddenCenters;
    // 출력 가중치 [OUTPUT_DIM, HIDDEN_DIM] = 2×5
    private float[,] weights;
    private float[] bias; // [OUTPUT_DIM]

    public float[] LastActivations { get; private set; } = new float[HIDDEN_DIM];
    public float[] LastOutput { get; private set; } = new float[OUTPUT_DIM];

    public float PriceMultiplier => Mathf.Clamp(LastOutput[0], priceMin, priceMax);
    public float AffinityDelta => Mathf.Clamp(LastOutput[1], affinityDeltaMin, affinityDeltaMax);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        InitializeNetwork();
    }

    private void InitializeNetwork()
    {
        Random.InitState(randomSeed);

        hiddenCenters = new float[HIDDEN_DIM][];
        for (int j = 0; j < HIDDEN_DIM; j++)
        {
            hiddenCenters[j] = new float[INPUT_DIM];
            for (int i = 0; i < INPUT_DIM; i++)
                hiddenCenters[j][i] = Random.Range(0.2f, 0.8f); // 입력 공간 내부
        }

        weights = new float[OUTPUT_DIM, HIDDEN_DIM];
        bias = new float[OUTPUT_DIM];
        bias[0] = 1.0f;  // PriceMultiplier 기본 1.0 (보정 없음)
        bias[1] = 0.0f;  // AffinityDelta 기본 0

        for (int k = 0; k < OUTPUT_DIM; k++)
            for (int j = 0; j < HIDDEN_DIM; j++)
                weights[k, j] = Random.Range(-0.1f, 0.1f);

        if (logDebug)
            Debug.Log("[RBFN] 초기화 완료 (입력 5, 은닉 5, 출력 2) — 은닉 중심은 FCM과 독립");
    }


    public void Predict(float[] input)
    {
        if (input.Length != INPUT_DIM)
        {
            Debug.LogError($"[RBFN] 입력 차원 오류: {input.Length} != {INPUT_DIM}");
            return;
        }

        // 1) 은닉층 가우시안 활성화
        for (int j = 0; j < HIDDEN_DIM; j++)
        {
            float distSq = SquaredDistance(input, hiddenCenters[j]);
            LastActivations[j] = Mathf.Exp(-distSq / (2f * sigma * sigma));
        }

        // 2) 출력층 선형 결합
        for (int k = 0; k < OUTPUT_DIM; k++)
        {
            float sum = bias[k];
            for (int j = 0; j < HIDDEN_DIM; j++)
                sum += weights[k, j] * LastActivations[j];
            LastOutput[k] = sum;
        }

        if (logDebug)
            Debug.Log($"[RBFN] 예측: 가격×{PriceMultiplier:F3} | 친밀±{AffinityDelta:+0.00;-0.00}");
    }


    public void Train(float[] input, float[] targets)
    {
        if (targets.Length != OUTPUT_DIM)
        {
            Debug.LogError($"[RBFN] 타깃 차원 오류: {targets.Length} != {OUTPUT_DIM}");
            return;
        }

        Predict(input);

        for (int k = 0; k < OUTPUT_DIM; k++)
        {
            float error = targets[k] - LastOutput[k];
            for (int j = 0; j < HIDDEN_DIM; j++)
                weights[k, j] += learningRate * error * LastActivations[j];
            bias[k] += learningRate * error;
        }

        if (logDebug)
            Debug.Log($"[RBFN 학습] 타깃=[가격 {targets[0]:F2}, 친밀 {targets[1]:F3}]");
    }

    public static float[] BuildInput(float qty, float price, float quality, float affinityNpc, float seasonFit)
    {
        return new float[]
        {
            Mathf.Clamp01(qty),
            Mathf.Clamp01(price),
            Mathf.Clamp01(quality),
            Mathf.Clamp01(affinityNpc),
            Mathf.Clamp01(seasonFit)
        };
    }

    private float SquaredDistance(float[] a, float[] b)
    {
        float sum = 0f;
        for (int i = 0; i < a.Length; i++)
            sum += (a[i] - b[i]) * (a[i] - b[i]);
        return sum;
    }

    public string GetDebugInfo()
        => $"P×{PriceMultiplier:F3} | A{AffinityDelta:+0.00;-0.00}";
}