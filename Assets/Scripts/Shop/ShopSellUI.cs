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

    [Header("디버그 표시 (선택)")]
    public TextMeshProUGUI debugText;

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

        // 상점이 열려있는 동안 가격 정보 실시간 갱신 (공급량 회복 보이게)
        UpdateItemInfo();
    }

    // 열기 / 닫기
    public void OpenShop()
    {
        IsShopOpen = true;
        selectedSlotIndex = -1;
        shopPanel.SetActive(true);
        PlayerController.IsInputLocked = true;

        UpdateRBFNPrediction();
        UpdateNpcDialogue();

        RefreshUI();
        UpdateMoneyUI();
        UpdateItemInfo();

        Debug.Log("상점 오픈!");
        if (FCMSalesAnalyzer.Instance != null)
            Debug.Log($"[FCM] {FCMSalesAnalyzer.Instance.GetDebugInfo()}");
        if (RBFNetwork.Instance != null)
            Debug.Log($"[RBFN] {RBFNetwork.Instance.GetDebugInfo()}");
        if (MarketSupplyManager.Instance != null)
            Debug.Log($"[Market]\n{MarketSupplyManager.Instance.GetDebugInfo()}");
    }

    public void CloseShop()
    {
        IsShopOpen = false;
        shopPanel.SetActive(false);
        PlayerController.IsInputLocked = false;
    }

    // RBFN 예측 갱신
    void UpdateRBFNPrediction()
    {
        if (FCMSalesAnalyzer.Instance == null || RBFNetwork.Instance == null) return;
        RBFNetwork.Instance.Predict(FCMSalesAnalyzer.Instance.LastMembership);
    }

    // NPC 대사 갱신
    void UpdateNpcDialogue()
    {
        if (npcDialogueText == null) return;

        if (FCMSalesAnalyzer.Instance == null)
        {
            npcDialogueText.text = "어서 와! 뭐 팔 거라도 있어?";
            return;
        }

        var cluster = FCMSalesAnalyzer.Instance.DominantCluster;
        var tone = RBFNetwork.Instance != null
            ? RBFNetwork.Instance.GetDialogueTone()
            : RBFNetwork.DialogueTone.Neutral;
        int tradeCount = FCMSalesAnalyzer.Instance.TradeCount;

        npcDialogueText.text = NPCDialogueGenerator.Generate(cluster, tone, tradeCount);
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
        UpdateDebugInfo();
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
        {
            if (canSell)
            {
                int basePrice = slot.itemData.sellPrice;
                int adjustedPrice = GetAdjustedPrice(slot.itemData, basePrice);

                // 공급 정보 함께 표시
                float supplyMult = 1.0f;
                float supplyAmount = 0f;
                if (MarketSupplyManager.Instance != null)
                {
                    supplyMult = MarketSupplyManager.Instance.GetSupplyMultiplier(slot.itemData);
                    supplyAmount = MarketSupplyManager.Instance.GetSupplyAmount(slot.itemData);
                }

                if (supplyMult < 0.95f)
                {
                    // 공급 과잉 상태 - 경고 표시
                    itemPriceText.text = $"<color=#FFA500>판매가: {adjustedPrice} G</color>\n<size=70%>(공급 과잉 x{supplyMult:F2})</size>";
                }
                else if (adjustedPrice > basePrice)
                {
                    itemPriceText.text = $"<color=#90EE90>판매가: {adjustedPrice} G</color>";
                }
                else
                {
                    itemPriceText.text = $"판매가: {adjustedPrice} G";
                }
            }
            else
            {
                itemPriceText.text = "판매 불가";
            }
        }
    }

    void UpdateDebugInfo()
    {
        if (debugText == null) return;
        if (FCMSalesAnalyzer.Instance == null || RBFNetwork.Instance == null) return;

        var fcm = FCMSalesAnalyzer.Instance;
        var rbfn = RBFNetwork.Instance;

        string supplyInfo = MarketSupplyManager.Instance != null
            ? MarketSupplyManager.Instance.GetDebugInfo()
            : "";

        debugText.text =
            $"거래: {fcm.TradeCount}건 | {fcm.DominantCluster}\n" +
            $"RBFN: x{rbfn.PriceMultiplier:F2} | 친함 {rbfn.Affinity:F2}\n" +
            $"<size=80%>{supplyInfo}</size>";
    }

    // 최종 가격 계산: 기본가 × RBFN 보정 × 공급 페널티
    int GetAdjustedPrice(ItemData item, int basePrice)
    {
        float rbfnMult = RBFNetwork.Instance != null ? RBFNetwork.Instance.PriceMultiplier : 1f;
        float supplyMult = MarketSupplyManager.Instance != null
            ? MarketSupplyManager.Instance.GetSupplyMultiplier(item)
            : 1f;

        return Mathf.Max(1, Mathf.RoundToInt(basePrice * rbfnMult * supplyMult));
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

    // 판매 (1개)
    public void QuickSell(int index)
    {
        var slot = InventoryManager.Instance?.GetSlot(index);
        if (slot == null || slot.IsEmpty())
        {
            ShowMessage("판매할 수 없는 아이템입니다.");
            return;
        }

        ItemData item = slot.itemData;
        int basePrice = item.sellPrice;
        int totalQty = slot.quantity;
        int sellQty = 1;

        // 인벤토리에서 1개 차감 (raw 가격 무시, 우리가 직접 계산)
        bool removed = InventoryManager.Instance.RemoveItem(item, sellQty);
        if (!removed)
        {
            // 폴백: SellOneFromSlot 사용
            int rawPrice = InventoryManager.Instance.SellOneFromSlot(index);
            if (rawPrice <= 0)
            {
                ShowMessage("판매할 수 없는 아이템입니다.");
                return;
            }
        }

        // 공급량 등록 (페널티 계산 전에)
        if (MarketSupplyManager.Instance != null)
            MarketSupplyManager.Instance.RegisterSale(item, sellQty);

        // 최종 가격 계산 (RBFN 보정 + 공급 페널티)
        int finalPrice = GetAdjustedPrice(item, basePrice * sellQty);
        MoneyManager.Instance.AddMoney(finalPrice);

        // FCM 거래 기록
        if (FCMSalesAnalyzer.Instance != null)
            FCMSalesAnalyzer.Instance.RecordTrade(basePrice, sellQty, totalQty);

        // RBFN 학습
        TrainRBFN(basePrice, sellQty, totalQty);

        // 메시지
        ShowSaleMessage(basePrice * sellQty, finalPrice, item);

        RefreshUI();
    }

    // 판매 (전체)
    public void QuickSellAll(int index)
    {
        var slot = InventoryManager.Instance?.GetSlot(index);
        if (slot == null || slot.IsEmpty())
        {
            ShowMessage("판매할 수 없는 아이템입니다.");
            return;
        }

        ItemData item = slot.itemData;
        int basePrice = item.sellPrice;
        int totalQty = slot.quantity;

        // 전체 판매 - 한 개씩 가격 계산해서 합산 (공급 페널티가 누진 적용됨)
        int totalEarned = 0;
        for (int i = 0; i < totalQty; i++)
        {
            if (MarketSupplyManager.Instance != null)
                MarketSupplyManager.Instance.RegisterSale(item, 1);

            int unitPrice = GetAdjustedPrice(item, basePrice);
            totalEarned += unitPrice;
        }

        // 인벤토리에서 차감
        InventoryManager.Instance.SellAllFromSlot(index);
        MoneyManager.Instance.AddMoney(totalEarned);

        // FCM 거래 기록
        if (FCMSalesAnalyzer.Instance != null)
            FCMSalesAnalyzer.Instance.RecordTrade(basePrice, totalQty, totalQty);

        // RBFN 학습
        TrainRBFN(basePrice, totalQty, totalQty);

        int baseSum = basePrice * totalQty;
        ShowSaleMessage(baseSum, totalEarned, item, true);

        selectedSlotIndex = -1;
        RefreshUI();
    }

    // 판매 결과 메시지 생성
    void ShowSaleMessage(int baseSum, int finalSum, ItemData item, bool bulk = false)
    {
        string prefix = bulk ? "전체 판매" : "판매 완료";

        if (finalSum >= baseSum)
        {
            int bonus = finalSum - baseSum;
            if (bonus > 0)
                ShowMessage($"{prefix}! +{finalSum} G (보너스 +{bonus})");
            else
                ShowMessage($"{prefix}! +{finalSum} G");
        }
        else
        {
            int loss = baseSum - finalSum;
            float ratio = (float)finalSum / baseSum;
            if (ratio < 0.5f)
                ShowMessage($"<color=#FF6B6B>{prefix}... +{finalSum} G (공급 과잉으로 -{loss})</color>");
            else
                ShowMessage($"<color=#FFA500>{prefix}! +{finalSum} G (-{loss})</color>");
        }
    }

    // RBFN 온라인 학습
    void TrainRBFN(int itemPrice, int quantitySold, int totalQty)
    {
        if (FCMSalesAnalyzer.Instance == null || RBFNetwork.Instance == null) return;

        // 목표 가격 배율: 비싼 아이템 + 대량 판매 -> 단골 우대 업
        float priceTarget = 1.0f;
        float priceNorm = Mathf.Clamp01(itemPrice / 100f);
        float bulkNorm = totalQty > 0 ? (float)quantitySold / totalQty : 0f;
        priceTarget += (priceNorm * 0.05f) + (bulkNorm * 0.05f);

        // 목표 친함도: 거래 횟수가 늘수록 천천히 상승
        int tradeCount = FCMSalesAnalyzer.Instance.TradeCount;
        float affinityTarget = Mathf.Clamp01(0.4f + tradeCount * 0.03f);

        // LMS 1스텝 학습
        RBFNetwork.Instance.Train(
            FCMSalesAnalyzer.Instance.LastMembership,
            priceTarget,
            affinityTarget
        );

        UpdateRBFNPrediction();
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
        yield return new WaitForSeconds(2.5f);
        if (messageText != null) messageText.text = "";
    }
}