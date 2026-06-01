using UnityEngine;

// 씬에 놓는 "플레이어 등장 지점".
//  - 씬이 로드되면, SceneTransition.nextSpawnID 와 자기 spawnID 가 같은 지점으로
//    플레이어를 순간이동시킨다.
//  - 각 씬에 문 개수만큼 SpawnPoint를 두고 ID를 맞춰주면 된다.
//    예) 농장 씬: 집에서 나오는 지점 "FromHouseDoor"
//        집 씬:   농장에서 들어오는 지점 "FromFarmDoor"
public class SpawnPoint : MonoBehaviour
{
    [Header("이 지점의 ID (Door의 targetSpawnID와 일치시킬 것)")]
    public string spawnID = "FromFarmDoor";

    [Header("플레이어 태그")]
    public string playerTag = "Player";

    void Start()
    {
        // 이번에 도착해야 할 지점이 나라면 → 플레이어를 여기로 이동
        if (SceneTransition.nextSpawnID == spawnID)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                player.transform.position = transform.position;
            }
        }
    }
}