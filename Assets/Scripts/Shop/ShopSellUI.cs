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

    [Header("NPC 친밀도 (현재 상점)")]
    [Tooltip("이 상점 NPC와의 친밀도 (0~1)")]
    [SerializeField] private float affinityWithNpc = 0.5f;

    [Header("RBFN 입력 정규화 기준")]
    [SerializeField] private float maxItemPrice = 100f; // 단가/품질 정규화 기준
    [SerializeField] private float maxQty = 20f;        // 수량 정규화 기준

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

        // 구매 영역도 같이 표시 (스듀식: 한 패널에서 판매+구매)
        ShopBuyUI.Instance?.BuildOnce();
    }

    public void CloseShop()
    {
        IsShopOpen = false;
        shopPanel.SetActive(false);
        PlayerController.IsInputLocked = false;
    }

    void UpdateNpcDialogue()
    {
        if (npcDialogueText == null) return;
        npcDialogueText.text = NPCDialogueGenerator.Generate(FCMSalesAnalyzer.Instance, affinityWithNpc);
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
        if (itemNameText != null) itemNameText.text = slot.itemData.itemName;

        // 최종(결합) 가격 보정 미리보기
        float multiplier = canSell ? PreviewMultiplier(slot, slot.quantity) : 1f;
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
    public void QuickSell(int index) => ProcessSale(index, 1);

    public void QuickSellAll(int index)
    {
        var slot = InventoryManager.Instance?.GetSlot(index);
        if (slot == null || slot.IsEmpty()) return;
        ProcessSale(index, slot.quantity);
    }

    // ─────────────────────────────────────────────
    // 공통: RBFN 입력 5차원 구성
    // ─────────────────────────────────────────────
    private float[] BuildRbfInput(InventorySlot slot, int sellQty)
    {
        float qtyN = sellQty / Mathf.Max(1f, maxQty);
        float priceN = slot.itemData.sellPrice / Mathf.Max(1f, maxItemPrice);
        float qualityN = GetQualityNorm(slot.itemData);
        float seasonFit = SeasonalDemand.Instance != null
            ? SeasonalDemand.Instance.GetDemandFit(slot.itemData)
            : 0.6f;

        return RBFNetwork.BuildInput(qtyN, priceN, qualityN, affinityWithNpc, seasonFit);
    }


    // 품질 정규화값(0~1).
    // TODO: ItemData 에 별도 품질/등급 필드가 있다면 이 한 줄만 교체하세요.
    //       (현재는 단가를 품질 대용 지표로 사용)

    private float GetQualityNorm(ItemData item)
        => Mathf.Clamp01(item.sellPrice / Mathf.Max(1f, maxItemPrice));

    // 판매 전 결합 가격 보정 미리보기 (side effect로 RBFN.Predict 호출)
    private float PreviewMultiplier(InventorySlot slot, int sellQty)
    {
        if (RBFNetwork.Instance == null) return 1f;

        float[] input = BuildRbfInput(slot, sellQty);
        RBFNetwork.Instance.Predict(input);

        float[] u = FCMSalesAnalyzer.Instance?.LastMembership ?? new float[3];
        var r = ResponseCombiner.Combine(
            u, RBFNetwork.Instance.PriceMultiplier, RBFNetwork.Instance.AffinityDelta, affinityWithNpc);
        return r.price;
    }

    // ─────────────────────────────────────────────
    // 판매 1회 전체 흐름
    // ─────────────────────────────────────────────
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

        // (1) FCM 기록 → 3 클러스터 소속도
        FCMSalesAnalyzer.Instance?.RecordTrade(itemPrice, sellQty, totalQty);
        float[] u = FCMSalesAnalyzer.Instance?.LastMembership ?? new float[3];

        // (2) RBFN 5차원 입력 + 추론 (FCM과 독립)
        float[] rbfInput = BuildRbfInput(slot, sellQty);
        RBFNetwork.Instance?.Predict(rbfInput);
        float rbfPrice = RBFNetwork.Instance?.PriceMultiplier ?? 1f;
        float rbfAff = RBFNetwork.Instance?.AffinityDelta ?? 0f;

        // (3) 결합: RBF 출력 × FCM 멤버십 가중
        var resp = ResponseCombiner.Combine(u, rbfPrice, rbfAff, affinityWithNpc);

        // (4) 보정 가격으로 판매
        int basePrice = (quantity == 1)
            ? InventoryManager.Instance.SellOneFromSlot(index)
            : InventoryManager.Instance.SellAllFromSlot(index);

        if (basePrice <= 0)
        {
            ShowMessage("판매할 수 없는 아이템입니다.");
            return;
        }

        int finalPrice = Mathf.RoundToInt(basePrice * resp.price);
        MoneyManager.Instance.AddMoney(finalPrice);

        // (5) 친밀도 갱신 + 대사
        //     친밀도를 먼저 갱신해, 이번 거래로 단골 문턱을 넘으면 즉시 단골 대사가 나오게 한다.
        affinityWithNpc = Mathf.Clamp01(affinityWithNpc + resp.affinity);
        if (npcDialogueText != null)
            npcDialogueText.text = NPCDialogueGenerator.Generate(FCMSalesAnalyzer.Instance, affinityWithNpc);

        // (6) RBFN LMS 학습 (거래 결과 휴리스틱 타깃 — 원시 분류 DominantCluster 사용)
        var cluster = FCMSalesAnalyzer.Instance?.DominantCluster ?? FCMSalesAnalyzer.ClusterType.None;
        TrainRBFN(rbfInput, cluster);

        ShowMessage($"판매 완료! +{finalPrice} G (×{resp.price:F2})");
        RefreshUI();
    }

    private void TrainRBFN(float[] input, FCMSalesAnalyzer.ClusterType cluster)
    {
        float[] targets = new float[RBFNetwork.OUTPUT_DIM];
        targets[0] = 1.0f;   // 기본 가격
        targets[1] = 0.01f;  // 기본 친밀도 소폭

        switch (cluster)
        {
            case FCMSalesAnalyzer.ClusterType.Direct:
                targets[0] = 1.08f; targets[1] = 0.02f; break;
            case FCMSalesAnalyzer.ClusterType.Relational:
                targets[0] = 1.05f; targets[1] = 0.04f; break;   // 친밀도 적립 큼
            case FCMSalesAnalyzer.ClusterType.Wholesale:
                targets[0] = 0.96f; targets[1] = 0.01f; break;   // 대량 할인
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