using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class ShopSellUI : MonoBehaviour
{
    public static ShopSellUI Instance;

    [Header("패널")]
    public GameObject shopPanel;

    [Header("슬롯")]
    public ShopSlotUI[] slotUIs;

    [Header("아이템 정보")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;

    [Header("돈 표시")]
    public TextMeshProUGUI playerMoneyText;

    [Header("메시지")]
    public TextMeshProUGUI messageText;

    [Header("버튼")]
    public Button closeButton;

    [Header("NPC 대사")]
    public TextMeshProUGUI npcDialogueText;

    public bool IsShopOpen { get; private set; } = false;

    private int selectedSlotIndex = -1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        shopPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].Setup(i);

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RefreshUI;
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
    }

    void Update()
    {
        if (!IsShopOpen) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseShop();
    }

    // 열기 / 닫기
    public void OpenShop()
    {
        Debug.Log($"OpenShop 호출! shopPanel: {shopPanel}");

        IsShopOpen = true;
        selectedSlotIndex = -1;
        shopPanel.SetActive(true);
        PlayerController.IsInputLocked = true;

        // FCM 기반 NPC 대사 출력
        if (npcDialogueText != null)
        {
            if (FCMSalesAnalyzer.Instance != null)
                npcDialogueText.text = FCMSalesAnalyzer.Instance.GetDialogue();
            else
                npcDialogueText.text = "어서 와! 뭐 팔 거라도 있어?";
        }

        RefreshUI();
        UpdateMoneyUI();
        UpdateItemInfo();

        Debug.Log("상점 오픈!");
        if (FCMSalesAnalyzer.Instance != null)
            Debug.Log($"[FCM] {FCMSalesAnalyzer.Instance.GetDebugInfo()}");
    }

    public void CloseShop()
    {
        IsShopOpen = false;
        shopPanel.SetActive(false);
        PlayerController.IsInputLocked = false;
        Debug.Log("상점 종료!");
    }

    // UI 갱신
    public void RefreshUI()
    {
        if (!IsShopOpen) return;
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < slotUIs.Length; i++)
        {
            var slot = InventoryManager.Instance.GetSlot(i);
            bool isSelected = (i == selectedSlotIndex);
            slotUIs[i].Refresh(slot, isSelected);
        }

        UpdateMoneyUI();
        UpdateItemInfo();
    }

    void UpdateMoneyUI()
    {
        if (playerMoneyText != null && MoneyManager.Instance != null)
            playerMoneyText.text = $"보유 금액: {MoneyManager.Instance.CurrentMoney:N0} G";
    }

    void UpdateItemInfo()
    {
        if (selectedSlotIndex < 0)
        {
            if (itemNameText != null) itemNameText.text = "";
            if (itemPriceText != null) itemPriceText.text = "";
            return;
        }

        var slot = InventoryManager.Instance?.GetSlot(selectedSlotIndex);
        if (slot == null || slot.IsEmpty())
        {
            if (itemNameText != null) itemNameText.text = "";
            if (itemPriceText != null) itemPriceText.text = "";
            return;
        }

        bool canSell = InventoryManager.Instance.CanSellSlot(selectedSlotIndex);

        if (itemNameText != null)
            itemNameText.text = slot.itemData.itemName;

        if (itemPriceText != null)
            itemPriceText.text = canSell
                ? $"판매가: {slot.itemData.sellPrice} G"
                : "판매 불가";
    }

    // 슬롯 선택
    public void SelectSlot(int index)
    {
        selectedSlotIndex = index;
        RefreshUI();

        var slot = InventoryManager.Instance?.GetSlot(index);
        if (slot == null || slot.IsEmpty()) return;

        bool canSell = InventoryManager.Instance.CanSellSlot(index);
        if (!canSell)
            ShowMessage("판매할 수 없는 아이템입니다.");
    }

    // 판매 (FCM 데이터 기록 추가)
    public void QuickSell(int index)
    {
        // 판매 전 데이터 캡처 (FCM 분석용)
        var slot = InventoryManager.Instance?.GetSlot(index);
        int itemPrice = 0;
        int totalQty = 0;
        if (slot != null && !slot.IsEmpty())
        {
            itemPrice = slot.itemData.sellPrice;
            totalQty = slot.quantity;
        }

        int price = InventoryManager.Instance.SellOneFromSlot(index);
        if (price <= 0)
        {
            ShowMessage("판매할 수 없는 아이템입니다.");
            return;
        }
        MoneyManager.Instance.AddMoney(price);

        // FCM에 거래 데이터 기록 (1개 판매)
        if (FCMSalesAnalyzer.Instance != null)
            FCMSalesAnalyzer.Instance.RecordTrade(itemPrice, 1, totalQty);

        ShowMessage($"판매 완료! +{price} G");
        RefreshUI();
    }

    public void QuickSellAll(int index)
    {
        // 판매 전 데이터 캡처
        var slot = InventoryManager.Instance?.GetSlot(index);
        int itemPrice = 0;
        int totalQty = 0;
        if (slot != null && !slot.IsEmpty())
        {
            itemPrice = slot.itemData.sellPrice;
            totalQty = slot.quantity;
        }

        int price = InventoryManager.Instance.SellAllFromSlot(index);
        if (price <= 0)
        {
            ShowMessage("판매할 수 없는 아이템입니다.");
            return;
        }
        MoneyManager.Instance.AddMoney(price);

        // FCM에 거래 데이터 기록 (전체 판매)
        if (FCMSalesAnalyzer.Instance != null)
            FCMSalesAnalyzer.Instance.RecordTrade(itemPrice, totalQty, totalQty);

        ShowMessage($"전체 판매 완료! +{price} G");
        selectedSlotIndex = -1;
        RefreshUI();
    }

    // 메시지
    public void ShowMessage(string msg)
    {
        if (messageText == null) return;
        messageText.text = msg;
        StopCoroutine("HideMessage");
        StartCoroutine("HideMessage");
    }

    IEnumerator HideMessage()
    {
        yield return new WaitForSeconds(2f);
        if (messageText != null) messageText.text = "";
    }
}