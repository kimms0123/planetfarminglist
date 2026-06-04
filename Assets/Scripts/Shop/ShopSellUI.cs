using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// 상점 판매 UI
/// 
/// [개정] FCM 4 클러스터 + RBFN 7차원 입력 전달
/// 판매 1건마다: FCM 분류 → RBFN 입력 구성 → 추론 → 가격 보정 → 학습
/// </summary>
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

    [Header("NPC 친밀도 (현재 상점)")]
    [Tooltip("이 상점 NPC와의 친밀도 (0~1)")]
    [SerializeField] private float affinityWithNpc = 0.5f;

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

    // ─────────────────────────────────────────────
    public void OpenShop()
    {
        IsShopOpen = true;
        selectedSlotIndex = -1;
        shopPanel.SetActive(true);
        PlayerController.IsInputLocked = true;

        UpdateNpcDialogue();
        RefreshUI();
    }

    public void CloseShop()
    {
        IsShopOpen = false;
        shopPanel.SetActive(false);
        PlayerController.IsInputLocked = false;
    }

    /// <summary>
    /// NPC 대사 갱신 - FCM 클러스터 + RBFN 톤
    /// </summary>
    void UpdateNpcDialogue()
    {
        if (npcDialogueText == null) return;

        var cluster = FCMSalesAnalyzer.Instance?.DominantCluster ?? FCMSalesAnalyzer.ClusterType.None;
        var tone = RBFNetwork.Instance?.GetToneType() ?? RBFNetwork.DialogueToneType.Neutral;
        int count = FCMSalesAnalyzer.Instance?.TradeCount ?? 0;

        npcDialogueText.text = NPCDialogueGenerator.Generate(cluster, tone, count);
    }

    public void RefreshUI()
    {
        if (!IsShopOpen) return;
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < slotUIs.Length; i++)
        {
            var slot = InventoryManager.Instance.GetSlot(i);
            slotUIs[i].Refresh(slot, i == selectedSlotIndex);
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
        if (slot == null || slot.IsEmpty()) return;

        bool canSell = InventoryManager.Instance.CanSellSlot(selectedSlotIndex);

        if (itemNameText != null)
            itemNameText.text = slot.itemData.itemName;

        // RBFN 가격 보정 미리보기
        float multiplier = RBFNetwork.Instance?.PriceMultiplier ?? 1f;
        int adjustedPrice = Mathf.RoundToInt(slot.itemData.sellPrice * multiplier);

        if (itemPriceText != null)
            itemPriceText.text = canSell
                ? $"판매가: {adjustedPrice} G  (×{multiplier:F2})"
                : "판매 불가";
    }

    public void SelectSlot(int index)
    {
        selectedSlotIndex = index;
        RefreshUI();
    }

    // ─────────────────────────────────────────────
    // 판매 - FCM 기록 + RBFN 추론 + RBFN 학습
    // ─────────────────────────────────────────────
    public void QuickSell(int index)
    {
        ProcessSale(index, 1);
    }

    public void QuickSellAll(int index)
    {
        var slot = InventoryManager.Instance?.GetSlot(index);
        if (slot == null || slot.IsEmpty()) return;
        ProcessSale(index, slot.quantity);
    }

    /// <summary>
    /// 판매 1회의 전체 흐름
    /// 1) 판매 전 데이터 캡처
    /// 2) FCM 기록 (행동 벡터)
    /// 3) RBFN 입력 구성 (7차원) + 추론
    /// 4) 보정된 가격으로 판매
    /// 5) RBFN 학습 (거래 결과 피드백)
    /// </summary>
    private void ProcessSale(int index, int quantity)
    {
        var slot = InventoryManager.Instance?.GetSlot(index);
        if (slot == null || slot.IsEmpty())
        {
            ShowMessage("판매할 수 없는 아이템입니다.");
            return;
        }

        int itemPrice = slot.itemData.sellPrice;
        int totalQty = slot.quantity;
        int sellQty = Mathf.Min(quantity, totalQty);

        // (1) FCM에 거래 데이터 기록 → 4 클러스터 소속도 갱신
        FCMSalesAnalyzer.Instance?.RecordTrade(itemPrice, sellQty, totalQty);

        // (2) RBFN 입력 7차원 구성
        float[] fcmMembership = FCMSalesAnalyzer.Instance?.LastMembership ?? new float[4];
        float normalizedQty = Mathf.Clamp01((float)sellQty / 20f);  // 20개 기준
        float normalizedPrice = Mathf.Clamp01((float)itemPrice / 100f);
        float[] rbfnInput = RBFNetwork.BuildInput(fcmMembership, normalizedQty, normalizedPrice, affinityWithNpc);

        // (3) RBFN 추론 → 가격 보정 계수 등 획득
        RBFNetwork.Instance?.Predict(rbfnInput);
        float multiplier = RBFNetwork.Instance?.PriceMultiplier ?? 1f;

        // (4) 실제 판매 처리 (보정된 가격)
        int basePrice = (quantity == 1)
            ? InventoryManager.Instance.SellOneFromSlot(index)
            : InventoryManager.Instance.SellAllFromSlot(index);

        if (basePrice <= 0)
        {
            ShowMessage("판매할 수 없는 아이템입니다.");
            return;
        }

        int finalPrice = Mathf.RoundToInt(basePrice * multiplier);
        MoneyManager.Instance.AddMoney(finalPrice);

        // (5) RBFN 학습 - 거래 결과 피드백
        TrainRBFN(rbfnInput, sellQty, totalQty, itemPrice, multiplier);

        // (6) 친밀도 누적
        float deltaAffinity = RBFNetwork.Instance?.AffinityDelta ?? 0f;
        affinityWithNpc = Mathf.Clamp01(affinityWithNpc + deltaAffinity);

        ShowMessage($"판매 완료! +{finalPrice} G (×{multiplier:F2})");
        UpdateNpcDialogue();
        RefreshUI();
    }

    /// <summary>
    /// 거래 결과로 RBFN 학습
    /// 휴리스틱 타깃값 산출:
    ///   - 좋은 거래(클러스터 일치 잘 됨) → 친밀도 +, 가격 +
    ///   - 부정적 행동(과도한 대량) → 톤 ↓
    /// </summary>
    private void TrainRBFN(float[] input, int sellQty, int totalQty, int itemPrice, float currentMultiplier)
    {
        // 타깃 휴리스틱 — 우세 클러스터에 맞는 응대를 학습 목표로
        var cluster = FCMSalesAnalyzer.Instance?.DominantCluster ?? FCMSalesAnalyzer.ClusterType.None;
        float[] targets = new float[RBFNetwork.OUTPUT_DIM];

        // 기본값
        targets[0] = 1.0f;   // PriceMultiplier
        targets[1] = 0.01f;  // AffinityDelta (소폭 증가)
        targets[2] = 0.5f;   // DealAcceptRate
        targets[3] = 0.5f;   // DialogueTone
        targets[4] = 0.0f;   // RepeatVisitBonus

        // 클러스터별 보상 조정
        switch (cluster)
        {
            case FCMSalesAnalyzer.ClusterType.Direct:
                targets[0] = 1.08f;  // 고급 거래 우대
                targets[3] = 0.7f;
                break;
            case FCMSalesAnalyzer.ClusterType.Relational:
                targets[0] = 1.05f;
                targets[1] = 0.03f;  // 친밀도 크게 증가
                targets[3] = 0.75f;
                targets[4] = 0.05f;  // 단골 보너스
                break;
            case FCMSalesAnalyzer.ClusterType.Wholesale:
                targets[0] = 0.95f;  // 대량 할인
                targets[2] = 0.7f;   // 흥정 잘 받아줌
                break;
            case FCMSalesAnalyzer.ClusterType.Balanced:
                targets[0] = 1.0f;
                break;
        }

        RBFNetwork.Instance?.Train(input, targets);
    }

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