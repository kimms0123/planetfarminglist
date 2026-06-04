using UnityEngine;

/// <summary>
/// NPC 대사 생성기
/// 
/// [개정] 4 클러스터 × 4 톤 = 16종 대사 매트릭스
/// FCM 결과 + RBFN의 DialogueTone을 결합해 대사 선택
/// </summary>
public static class NPCDialogueGenerator
{
    /// <summary>
    /// 클러스터 + 톤 기반 대사 생성
    /// </summary>
    public static string Generate(
        FCMSalesAnalyzer.ClusterType cluster,
        RBFNetwork.DialogueToneType tone,
        int tradeCount)
    {
        // 콜드스타트 - 거래 데이터 부족 시
        if (cluster == FCMSalesAnalyzer.ClusterType.None || tradeCount < 3)
            return GetColdStartLine();

        // 16종 매트릭스
        return cluster switch
        {
            FCMSalesAnalyzer.ClusterType.Direct => GetDirectLine(tone),
            FCMSalesAnalyzer.ClusterType.Relational => GetRelationalLine(tone),
            FCMSalesAnalyzer.ClusterType.Wholesale => GetWholesaleLine(tone),
            FCMSalesAnalyzer.ClusterType.Balanced => GetBalancedLine(tone),
            _ => "어서 와!"
        };
    }

    // ─────────────────────────────────────────────
    // 콜드스타트 (거래 0~2건)
    // ─────────────────────────────────────────────
    private static string GetColdStartLine()
    {
        string[] lines = {
            "어서 와! 뭐 팔 거라도 있어?",
            "수확물은 여기서 판매할 수 있어.",
            "좋은 물건이면 값을 더 쳐주겠다고.",
            "처음 보는 얼굴이군. 자, 거래해보자고!"
        };
        return lines[Random.Range(0, lines.Length)];
    }

    // ─────────────────────────────────────────────
    // 직판형 (Direct) - 비싼 거 소량씩 거래
    // ─────────────────────────────────────────────
    private static string GetDirectLine(RBFNetwork.DialogueToneType tone)
    {
        return tone switch
        {
            RBFNetwork.DialogueToneType.Cold => "고급 물건이군. 값은 정직하게 쳐주지.",
            RBFNetwork.DialogueToneType.Neutral => "품질을 보는 눈이 있군. 거래해보자고.",
            RBFNetwork.DialogueToneType.Friendly => "오, 또 좋은 물건을 가져왔네. 단골 대접해 줄게.",
            RBFNetwork.DialogueToneType.VeryFriendly => "역시 자네는 명품만 가져와! 최고가로 쳐주지!",
            _ => "거래해보자고."
        };
    }

    // ─────────────────────────────────────────────
    // 관계형 (Relational) - 천천히 같은 NPC 반복
    // ─────────────────────────────────────────────
    private static string GetRelationalLine(RBFNetwork.DialogueToneType tone)
    {
        return tone switch
        {
            RBFNetwork.DialogueToneType.Cold => "또 왔군. 뭐, 거래는 거래지.",
            RBFNetwork.DialogueToneType.Neutral => "꾸준히 오는군. 좋아, 거래해보자.",
            RBFNetwork.DialogueToneType.Friendly => "어, 단골이네! 오늘은 뭐 가져왔나?",
            RBFNetwork.DialogueToneType.VeryFriendly => "이야, 우리 단골! 자네 덕에 가게가 잘 돌아가.",
            _ => "거래해보자."
        };
    }

    // ─────────────────────────────────────────────
    // 납품형 (Wholesale) - 대량 즉시 판매
    // ─────────────────────────────────────────────
    private static string GetWholesaleLine(RBFNetwork.DialogueToneType tone)
    {
        return tone switch
        {
            RBFNetwork.DialogueToneType.Cold => "물량 많군. 단가는 좀 깎이지만 받아주지.",
            RBFNetwork.DialogueToneType.Neutral => "이만큼이면 도매가로 처리하자고.",
            RBFNetwork.DialogueToneType.Friendly => "대량 거래는 자네가 최고야. 시원시원하게 가자고!",
            RBFNetwork.DialogueToneType.VeryFriendly => "납품의 달인! 자네 같은 거래처는 정말 귀하네.",
            _ => "한 번에 다 받아주지."
        };
    }

    // ─────────────────────────────────────────────
    // 균형형 (Balanced) - 다각 경영
    // ─────────────────────────────────────────────
    private static string GetBalancedLine(RBFNetwork.DialogueToneType tone)
    {
        return tone switch
        {
            RBFNetwork.DialogueToneType.Cold => "이것저것 가져왔군. 보자.",
            RBFNetwork.DialogueToneType.Neutral => "다양하게 가져오는군. 좋아.",
            RBFNetwork.DialogueToneType.Friendly => "균형 잡힌 거래야. 보기 좋군!",
            RBFNetwork.DialogueToneType.VeryFriendly => "자네 같은 다재다능한 농부는 처음일세!",
            _ => "거래해보자."
        };
    }
}