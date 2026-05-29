using UnityEngine;

/// <summary>
/// NPC 대사 생성기 - FCM 클러스터 × RBFN 친함도 조합으로 대사 선택
/// 보고서 6.4 절 (NPC 대사 반영) 참고
/// </summary>
public static class NPCDialogueGenerator
{
    /// <summary>
    /// FCM + RBFN 결과로 NPC 대사 생성
    /// </summary>
    public static string Generate(FCMSalesAnalyzer.ClusterType cluster, RBFNetwork.DialogueTone tone, int tradeCount)
    {
        // 거래 부족 - 콜드 스타트
        if (tradeCount < 3 || cluster == FCMSalesAnalyzer.ClusterType.None)
        {
            return PickRandom(new string[] {
                "어서 와! 뭐 팔 거라도 있어?",
                "수확물은 여기서 판매할 수 있어.",
                "좋은 물건이면 값을 더 쳐주겠다고.",
                "처음 보는 얼굴이군. 자, 거래해보자고!"
            });
        }

        // 클러스터 × 친함도 매트릭스로 대사 선택
        switch (cluster)
        {
            case FCMSalesAnalyzer.ClusterType.CashKing:
                return GetCashKingDialogue(tone);
            case FCMSalesAnalyzer.ClusterType.QualityMaster:
                return GetQualityMasterDialogue(tone);
            case FCMSalesAnalyzer.ClusterType.Balanced:
                return GetBalancedDialogue(tone);
            default:
                return "어서 와!";
        }
    }

    // ─────────────────────────────────────────────
    // 현금왕 × 친함도
    // ─────────────────────────────────────────────
    private static string GetCashKingDialogue(RBFNetwork.DialogueTone tone)
    {
        switch (tone)
        {
            case RBFNetwork.DialogueTone.Cold:
                return PickRandom(new string[] {
                    "또 한 방에 다 팔러 왔나.",
                    "거래량이 많긴 한데... 좀 신중하게 가는 게 어때?",
                    "현금만 챙기면 다인가? 흠."
                });
            case RBFNetwork.DialogueTone.Neutral:
                return PickRandom(new string[] {
                    "오, 또 왔군. 거래 시작하지.",
                    "이번에도 한 방에 다 팔 건가?",
                    "거래량이 많아서 좋아. 빨리 시작하자고."
                });
            case RBFNetwork.DialogueTone.Friendly:
                return PickRandom(new string[] {
                    "오, 단골 양반! 자네 일 처리가 빠른 게 마음에 들어.",
                    "이번엔 또 뭘 한 방에 팔아치울 건가?",
                    "자네 같은 손님이 있어야 가게가 돌아가지."
                });
            case RBFNetwork.DialogueTone.VeryFriendly:
                return PickRandom(new string[] {
                    "이야~ 우리 베스트 고객 오셨네! 오늘은 뭐 가져왔어?",
                    "자네 덕에 우리 가게 매출이 안정적이야. 고마워!",
                    "현금왕! 늘 자네가 최고야. 값 후하게 쳐줄게."
                });
            default:
                return "어서 와!";
        }
    }

    // ─────────────────────────────────────────────
    // 품질장인 × 친함도
    // ─────────────────────────────────────────────
    private static string GetQualityMasterDialogue(RBFNetwork.DialogueTone tone)
    {
        switch (tone)
        {
            case RBFNetwork.DialogueTone.Cold:
                return PickRandom(new string[] {
                    "비싼 것만 골라 오는군. 그래서 뭘 보여줄 건가?",
                    "고급품 하나씩 들고 오니 거래가 더디네.",
                    "품질은 좋다만... 자주 좀 와줬으면."
                });
            case RBFNetwork.DialogueTone.Neutral:
                return PickRandom(new string[] {
                    "역시 보는 눈이 있군. 어디 보자.",
                    "이런 고급 물건은 흔치 않지. 가져왔어?",
                    "장인의 손길이 느껴지는군. 거래하지."
                });
            case RBFNetwork.DialogueTone.Friendly:
                return PickRandom(new string[] {
                    "아, 우리 안목 있는 손님! 오늘도 명품 가져왔나?",
                    "자네가 가져오는 물건은 항상 기대가 돼.",
                    "이런 품질의 물건은 자네한테서만 볼 수 있지."
                });
            case RBFNetwork.DialogueTone.VeryFriendly:
                return PickRandom(new string[] {
                    "장인! 오늘도 최상품 가지고 왔나? 후한 값 쳐줄게.",
                    "자네 작물은 내 가게 명품 코너에 들어가지. 늘 감사해.",
                    "어서 와요, 우리 마스터! 오늘은 어떤 보물을 가져왔나?"
                });
            default:
                return "어서 와!";
        }
    }

    // ─────────────────────────────────────────────
    // 균형형 × 친함도
    // ─────────────────────────────────────────────
    private static string GetBalancedDialogue(RBFNetwork.DialogueTone tone)
    {
        switch (tone)
        {
            case RBFNetwork.DialogueTone.Cold:
                return PickRandom(new string[] {
                    "이것저것 골고루 가져왔구만.",
                    "특별할 것 없는 거래로군. 시작하지.",
                    "평범한 손님이군. 어디 보자."
                });
            case RBFNetwork.DialogueTone.Neutral:
                return PickRandom(new string[] {
                    "안정적인 거래 스타일이군. 좋아.",
                    "오늘은 뭘 가져왔나?",
                    "균형 잡힌 거래야. 시작하자고."
                });
            case RBFNetwork.DialogueTone.Friendly:
                return PickRandom(new string[] {
                    "꾸준한 우리 손님! 오늘은 뭘 보여줄 건가?",
                    "자네는 늘 안정적이라 거래하기 편해.",
                    "이번엔 또 다양한 거 가져왔나? 어디 보자."
                });
            case RBFNetwork.DialogueTone.VeryFriendly:
                return PickRandom(new string[] {
                    "어서 와요, 친구! 오늘은 뭘 가져왔어?",
                    "자네는 우리 가게 단골이지. 늘 환영이야.",
                    "꾸준함이 최고지! 오늘도 좋은 거래 해보자고."
                });
            default:
                return "어서 와!";
        }
    }

    private static string PickRandom(string[] arr)
    {
        return arr[Random.Range(0, arr.Length)];
    }
}