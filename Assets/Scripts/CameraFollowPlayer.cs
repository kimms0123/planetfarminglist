using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;   // 최신 Cinemachine(3.x). 구버전이면 아래 주석 참고

// 씬이 시작/로드될 때, Cinemachine 카메라의 Follow 타겟을
// "Player 태그"를 가진 오브젝트로 자동 연결한다.
//  - Player가 DontDestroyOnLoad라 씬마다 다시 찾아 붙여줘야 하므로 필요.
//  - 농장 씬 / 집 씬 양쪽의 CinemachineCamera 에 이 스크립트를 붙이면 됨.
//
// ※ Cinemachine 2.x(구버전)을 쓰면:
//    - using Unity.Cinemachine;  ->  using Cinemachine;
//    - CinemachineCamera          ->  CinemachineVirtualCamera
//   로 두 군데만 바꾸면 된다.
[RequireComponent(typeof(CinemachineCamera))]
public class CameraFollowPlayer : MonoBehaviour
{
    public string playerTag = "Player";

    private CinemachineCamera cam;

    void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        AssignTarget();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 새로 로드될 때마다 플레이어를 다시 찾아 연결.
        // 한 프레임 늦게 시도해서 DontDestroyOnLoad 플레이어가 준비될 시간을 준다.
        StartCoroutine(AssignNextFrame());
    }

    System.Collections.IEnumerator AssignNextFrame()
    {
        yield return null;   // 한 프레임 대기
        AssignTarget();
    }

    void AssignTarget()
    {
        if (cam == null) cam = GetComponent<CinemachineCamera>();

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null && cam != null)
        {
            cam.Follow = player.transform;
            // 2D 탑다운이면 LookAt은 보통 안 씀. 필요하면 아래 주석 해제
            // cam.LookAt = player.transform;
        }
        else
        {
            Debug.LogWarning("[CameraFollowPlayer] Player를 찾지 못했습니다. Player 태그를 확인하세요.");
        }
    }
}