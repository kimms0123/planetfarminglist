using UnityEngine;
using UnityEngine.Tilemaps;

// 계절별 Ground(바닥) / Cliff(절벽) / Objects(나무 등) 타일맵을
// 같은 Grid 위에 겹쳐두고, 현재 계절의 것만 보이게(TilemapRenderer.enabled) 한다.
//  → 타일 데이터/좌표 계산은 그대로 살아있어 안전함.
//
// 사용법:
//  1) Grid 아래에 Ground/Cliff/Object 각각 봄~겨울 4개씩 칠해 둔다.
//  2) 아래 배열에 [봄, 여름, 가을, 겨울] 순서로 넣는다.
//  3) TimeManager.OnSeasonChange 이벤트에 ApplySeason() 을 연결한다.
public class SeasonMapManager : MonoBehaviour
{
    [Header("계절 바닥 (순서: 봄, 여름, 가을, 겨울)")]
    public Tilemap[] groundTilemaps = new Tilemap[4];

    [Header("계절 절벽 (순서: 봄, 여름, 가을, 겨울)")]
    public Tilemap[] cliffTilemaps = new Tilemap[4];

    [Header("계절 나무/오브젝트 (순서: 봄, 여름, 가을, 겨울)")]
    public Tilemap[] objectTilemaps = new Tilemap[4];

    void Start()
    {
        ApplySeason();   // 시작 시 현재 계절만 켜기
    }

    // TimeManager.OnSeasonChange 에 연결 (인자 없이 현재 계절을 읽음)
    public void ApplySeason()
    {
        if (TimeManager.Instance == null) return;
        ShowSeason(TimeManager.Instance.currentSeason);
    }

    public void ShowSeason(Season season)
    {
        int active = (int)season;   // Spring0 Summer1 Fall2 Winter3
        SetActiveOnly(groundTilemaps, active);
        SetActiveOnly(cliffTilemaps, active);
        SetActiveOnly(objectTilemaps, active);
    }

    // 배열 중 active 인덱스만 렌더러를 켜고 나머지는 끔
    void SetActiveOnly(Tilemap[] maps, int active)
    {
        if (maps == null) return;
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i] == null) continue;
            var renderer = maps[i].GetComponent<TilemapRenderer>();
            if (renderer != null)
                renderer.enabled = (i == active);
        }
    }
}