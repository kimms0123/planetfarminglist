using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 전환을 담당하는 매니저 (씬이 바뀌어도 안 사라지는 싱글톤).
//  - 어느 문으로 들어왔는지(spawnPointID)를 기억했다가,
//    새 씬에서 그 ID에 맞는 위치로 플레이어를 놓는다.
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;

    // 다음 씬에서 플레이어가 등장할 지점의 ID (예: "FromFarmDoor")
    public static string nextSpawnID = "";

    void Awake()
    {
        // 씬이 바뀌어도 유지되는 싱글톤 1개만
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 문에서 호출: 지정한 씬으로 이동하면서, 도착 지점 ID를 넘긴다.
    public void GoToScene(string sceneName, string spawnID)
    {
        nextSpawnID = spawnID;
        SceneManager.LoadScene(sceneName);
    }
}