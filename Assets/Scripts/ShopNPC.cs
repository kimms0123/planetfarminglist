using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ShopNPC : MonoBehaviour
{
    [Header("설정")]
    public string npcName = "상인";
    public string playerTag = "Player";

    [Header("UI")]
    public GameObject promptUI;
    public TextMeshProUGUI promptText;

    private bool playerInRange = false;

    void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange) return;
        if (ShopSellUI.Instance != null && ShopSellUI.Instance.IsShopOpen) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log($"E 키 눌림! ShopSellUI.Instance: {ShopSellUI.Instance}");
            ShopSellUI.Instance?.OpenShop();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = true;
        if (promptUI != null) promptUI.SetActive(true);
        if (promptText != null) promptText.text = "E: 상점 열기";
        Debug.Log("상점 NPC 범위 진입");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }
}