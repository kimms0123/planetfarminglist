using UnityEngine;

// 시계 UI 같은 HUD Canvas를 씬 전환에도 유지시키는 스크립트.
//  - 씬이 바뀌어도 안 사라짐 (DontDestroyOnLoad)
//  - 다시 원래 씬으로 돌아왔을 때 중복 생성되면 새 것을 제거
//
// 사용법:
//  1) 시계 UI가 들어있는 Canvas(또는 그 UI를 감싸는 최상위 오브젝트)에 이 스크립트 추가
//  2) 그 Canvas는 "농장 씬"에만 두기 (집 씬엔 두지 않음 — 농장에서 따라옴)
public class PersistentUI : MonoBehaviour
{
    private static PersistentUI Instance;

    void Awake()
    {
        // 이미 유지 중인 UI가 있으면, 새로 로드된 씬의 중복 UI는 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}