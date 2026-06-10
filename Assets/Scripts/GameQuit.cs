using UnityEngine;
using UnityEngine.InputSystem;

// ESC로 게임 종료 (빌드용).

public class GameQuit : MonoBehaviour
{
    public static GameQuit Instance;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 상점/인벤 등이 열려 있으면 종료하지 않음 (그쪽 ESC가 닫기 처리)
            if (IsAnyUIOpen()) return;

            QuitGame();
        }
    }

    bool IsAnyUIOpen()
    {
        if (ShopSellUI.Instance != null && ShopSellUI.Instance.IsShopOpen) return true;
        if (InventoryWindowUI.Instance != null && InventoryWindowUI.Instance.IsOpen) return true;
        return false;
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료!");

#if UNITY_EDITOR
        // 에디터에서는 플레이 모드만 중지 (에디터가 안 꺼지게)
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}