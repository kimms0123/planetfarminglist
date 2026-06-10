using UnityEngine;

/// <summary>
/// NPC 판매 대사 생성기.
///
/// [동작]
/// 1) FCM 분류 전(거래 데이터 부족) → '낯선 손님' 응대
/// 2) FCM 분류 후 → 유형(직판/관계/납품) + 단골 여부에 맞는 응대
///
/// [깜빡임 방지 — 핵심]
/// 톤(tone)은 거래마다 출렁이므로 더 이상 대사 선택에 쓰지 않는다.
/// 대신 (분류 여부 · StableCluster · 단골 여부)로 '대사 키'를 만들고,
/// 키가 같으면 직전 대사를 그대로 돌려준다 → 1개 팔 때마다 대사가 바뀌는 문제 해결.
/// 상황이 실제로 바뀌었을 때(분류 시작, 유형 전환, 단골 등급 변화)만 새 대사를 고른다.
///
/// ※ 정적(static) 상태라 NPC 1명 기준. 상인이 여러 명이면 NPC 전환 시 Reset() 호출.
/// </summary>
public static class NPCDialogueGenerator
{
    [Tooltip("이 친밀도 이상이면 '단골' 대접")]
    private const float RegularThreshold = 0.6f;

    // 직전에 고른 대사 키 / 대사 (sticky 유지용)
    private static string lastKey = "";
    private static string currentLine = "";

    /// <summary>
    /// 현재 상황에 맞는 대사를 돌려준다. 같은 상황이면 직전 대사를 유지한다.
    /// </summary>
    /// <param name="fcm">FCM 분석기 (StableCluster / IsClassified 사용)</param>
    /// <param name="affinity">해당 NPC와의 친밀도 0~1 (단골 판정용)</param>
    public static string Generate(FCMSalesAnalyzer fcm, float affinity)
    {
        bool classified = fcm != null && fcm.IsClassified;
        var cluster = fcm != null ? fcm.StableCluster : FCMSalesAnalyzer.ClusterType.None;
        bool regular = affinity >= RegularThreshold;

        // 대사 키: 이 키가 같으면 상황이 안 바뀐 것 → 대사 유지
        string key = classified ? $"{cluster}_{(regular ? "단골" : "일반")}" : "stranger";
        if (key == lastKey)
            return currentLine;

        lastKey = key;
        currentLine = Pick(classified, cluster, regular);
        return currentLine;
    }

    /// <summary>NPC를 바꿀 때 호출 — 다음 거래에서 대사를 새로 뽑게 한다.</summary>
    public static void Reset()
    {
        lastKey = "";
        currentLine = "";
    }

    private static string Pick(bool classified, FCMSalesAnalyzer.ClusterType cluster, bool regular)
    {
        if (!classified)
            return RandomOf(coldStart);

        switch (cluster)
        {
            case FCMSalesAnalyzer.ClusterType.Direct:
                return RandomOf(regular ? directRegular : directNormal);
            case FCMSalesAnalyzer.ClusterType.Relational:
                return RandomOf(regular ? relationalRegular : relationalNormal);
            case FCMSalesAnalyzer.ClusterType.Wholesale:
                return RandomOf(regular ? wholesaleRegular : wholesaleNormal);
            default:
                return RandomOf(coldStart);
        }
    }

    private static string RandomOf(string[] pool) => pool[Random.Range(0, pool.Length)];

    // ───────── 대사 풀 (기존 대사 유지 + 일반/단골로 정리) ─────────

    // 분류 전 — 낯선 손님
    private static readonly string[] coldStart =
    {
        "어서 와! 뭐 팔 거라도 있어?",
        "수확물은 여기서 판매할 수 있어.",
        "좋은 물건이면 값을 더 쳐주겠다고.",
        "처음 보는 얼굴이군. 자, 거래해보자고!",
    };

    // 직판형 — 고급 물건 소량
    private static readonly string[] directNormal =
    {
        "고급 물건이군. 값은 정직하게 쳐주지.",
        "품질을 보는 눈이 있군. 거래해보자고.",
    };
    private static readonly string[] directRegular =
    {
        "오, 또 좋은 물건을 가져왔네. 단골 대접해 줄게.",
        "역시 자네는 명품만 가져와! 최고가로 쳐주지!",
    };

    // 관계형 — 꾸준한 단골 지향
    private static readonly string[] relationalNormal =
    {
        "또 왔군. 뭐, 거래는 거래지.",
        "꾸준히 오는군. 좋아, 거래해보자.",
    };
    private static readonly string[] relationalRegular =
    {
        "어, 단골이네! 오늘은 뭐 가져왔나?",
        "이야, 우리 단골! 자네 덕에 가게가 잘 돌아가.",
    };

    // 납품형 — 대량 즉시 처분
    private static readonly string[] wholesaleNormal =
    {
        "물량 많군. 단가는 좀 깎이지만 받아주지.",
        "이만큼이면 도매가로 처리하자고.",
    };
    private static readonly string[] wholesaleRegular =
    {
        "대량 거래는 자네가 최고야. 시원시원하게 가자고!",
        "납품의 달인! 자네 같은 거래처는 정말 귀하네.",
    };
}