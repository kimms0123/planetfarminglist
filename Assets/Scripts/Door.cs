using UnityEngine;
using UnityEngine.InputSystem;

// 문/입구에 붙이는 스크립트.
//  - 플레이어가 트리거 안에 있을 때 E키를 누르면 targetScene 으로 이동한다.
//  - 도착 씬에서 어느 지점(spawnID)에 설 지도 함께 넘긴다.
//
// 세팅:
//  1) 문 오브젝트에 Collider 2D (Is Trigger = true) 추가
//  2) 이 스크립트 추가 후 Target Scene / Spawn ID 입력
//  3) 플레이어 오브젝트의 Tag 가 "Player" 인지 확인
[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    [Header("이동할 씬 이름 (Build Settings에 등록돼 있어야 함)")]
    public string targetScene = "HouseScene";

    [Header("도착 씬에서 설 지점의 ID")]
    public string targetSpawnID = "FromFarmDoor";

    [Header("상호작용 안내(선택)")]
    public GameObject promptUI;   // "E키로 들어가기" 같은 표시. 없으면 비워둠

    private bool playerInRange = false;

    void Reset()
    {
        // 콜라이더를 자동으로 트리거로
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        if (!playerInRange) return;

        // 새 Input System 기준 E키
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            EnterDoor();
        }
    }

    void EnterDoor()
    {
        if (SceneTransition.Instance == null)
        {
            Debug.LogError("[Door] SceneTransition이 씬에 없습니다. 빈 오브젝트에 SceneTransition을 붙여주세요.");
            return;
        }
        SceneTransition.Instance.GoToScene(targetScene, targetSpawnID);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (promptUI != null) promptUI.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }
}