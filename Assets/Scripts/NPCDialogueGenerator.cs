using UnityEngine;


public static class NPCDialogueGenerator
{
    public static string Generate(
        FCMSalesAnalyzer.ClusterType cluster,
        ResponseCombiner.Tone tone,
        int tradeCount)
    {
        if (cluster == FCMSalesAnalyzer.ClusterType.None || tradeCount < 3)
            return GetColdStartLine();

        switch (cluster)
        {
            case FCMSalesAnalyzer.ClusterType.Direct: return GetDirectLine(tone);
            case FCMSalesAnalyzer.ClusterType.Relational: return GetRelationalLine(tone);
            case FCMSalesAnalyzer.ClusterType.Wholesale: return GetWholesaleLine(tone);
            default: return "어서 와!";
        }
    }

    private static string GetDirectLine(ResponseCombiner.Tone tone)
    {
        switch (tone)
        {
            case ResponseCombiner.Tone.Cold: return "고급 물건이군. 값은 정직하게 쳐주지.";
            case ResponseCombiner.Tone.Neutral: return "품질을 보는 눈이 있군. 거래해보자고.";
            case ResponseCombiner.Tone.Friendly: return "오, 또 좋은 물건을 가져왔네. 단골 대접해 줄게.";
            case ResponseCombiner.Tone.VeryFriendly: return "역시 자네는 명품만 가져와! 최고가로 쳐주지!";
            default: return "거래해보자고.";
        }
    }

    private static string GetRelationalLine(ResponseCombiner.Tone tone)
    {
        switch (tone)
        {
            case ResponseCombiner.Tone.Cold: return "또 왔군. 뭐, 거래는 거래지.";
            case ResponseCombiner.Tone.Neutral: return "꾸준히 오는군. 좋아, 거래해보자.";
            case ResponseCombiner.Tone.Friendly: return "어, 단골이네! 오늘은 뭐 가져왔나?";
            case ResponseCombiner.Tone.VeryFriendly: return "이야, 우리 단골! 자네 덕에 가게가 잘 돌아가.";
            default: return "거래해보자.";
        }
    }

    private static string GetWholesaleLine(ResponseCombiner.Tone tone)
    {
        switch (tone)
        {
            case ResponseCombiner.Tone.Cold: return "물량 많군. 단가는 좀 깎이지만 받아주지.";
            case ResponseCombiner.Tone.Neutral: return "이만큼이면 도매가로 처리하자고.";
            case ResponseCombiner.Tone.Friendly: return "대량 거래는 자네가 최고야. 시원시원하게 가자고!";
            case ResponseCombiner.Tone.VeryFriendly: return "납품의 달인! 자네 같은 거래처는 정말 귀하네.";
            default: return "한 번에 다 받아주지.";
        }
    }

    private static string GetColdStartLine()
    {
        string[] lines =
        {
            "어서 와! 뭐 팔 거라도 있어?",
            "수확물은 여기서 판매할 수 있어.",
            "좋은 물건이면 값을 더 쳐주겠다고.",
            "처음 보는 얼굴이군. 자, 거래해보자고!"
        };
        return lines[Random.Range(0, lines.Length)];
    }
}