using UnityEngine;
using System.Collections.Generic;

// 판매 패널(ShopSellUI) 안에 얹히는 "구매 영역".
//  - 자체 패널/NPC 없음. ShopSellUI가 열고 닫을 때 같이 빌드/정리.
//  - 판매 패널 오른쪽 상단 등에 SlotContainer를 배치해 두면 됨.
//  - 구매 입력: 우클릭 = 1개, Shift+우클릭 = 10개 (슬롯에서 처리)
public class ShopBuyUI : MonoBehaviour
{
    public static ShopBuyUI Instance;

    [System.Serializable]
    public class BuyEntry
    {
        public ItemData item;       // 판매할 씨앗
        public int price = 50;      // 1개당 구매 가격
    }

    [Header("판매 품목 (Inspector에서 등록)")]
    public List<BuyEntry> buyItems = new List<BuyEntry>();

    [Header("UI")]
    [Tooltip("구매 슬롯들이 생성될 부모 (판매 패널 안 오른쪽 상단 등)")]
    public Transform slotContainer;
    public GameObject buySlotPrefab;    // ShopBuySlotUI가 붙은 프리팹

    private List<ShopBuySlotUI> spawnedSlots = new List<ShopBuySlotUI>();
    private bool built = false;

    void Awake()
    {
        Instance = this;
    }

    // ShopSellUI.OpenShop()에서 호출
    public void BuildOnce()
    {
        if (built) { RefreshAll(); return; }
        if (slotContainer == null || buySlotPrefab == null)
        {
            Debug.LogWarning("ShopBuyUI: slotContainer 또는 buySlotPrefab이 비었어요!");
            return;
        }
        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);
        spawnedSlots.Clear();
        foreach (var entry in buyItems)
        {
            if (entry == null || entry.item == null) continue;
            GameObject obj = Instantiate(buySlotPrefab, slotContainer);

            // ▼▼▼ 여기 3줄이 바뀐 부분 ▼▼▼
            ShopBuySlotUI slot = obj.GetComponentInChildren<ShopBuySlotUI>();
            if (slot == null) { Destroy(obj); continue; }
            // ▲▲▲ 여기까지 ▲▲▲

            slot.Setup(entry.item, entry.price, this);
            spawnedSlots.Add(slot);
        }
        built = true;
    }

    void RefreshAll()
    {
        foreach (var s in spawnedSlots)
            if (s != null) s.RefreshAffordable();
    }

    // 슬롯에서 호출: amount개 구매 시도
    public void TryBuy(ItemData item, int pricePer, int amount)
    {
        if (item == null || amount <= 0) return;

        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("MoneyManager가 없어요!");
            return;
        }

        // 살 수 있는 만큼만 (돈 기준)
        int affordable = pricePer > 0 ? MoneyManager.Instance.CurrentMoney / pricePer : 0;
        int buyCount = Mathf.Min(amount, affordable);

        if (buyCount <= 0)
        {
            ShopSellUI.Instance?.ShowMessage("돈이 부족해요!");
            return;
        }

        // 인벤토리에 들어가는 만큼만 (1개씩 넣어보며 실제 들어간 수 카운트)
        int bought = 0;
        for (int i = 0; i < buyCount; i++)
        {
            bool added = InventoryManager.Instance.AddItem(item, 1);
            if (!added) break;          // 인벤 꽉 참
            bought++;
        }

        if (bought <= 0)
        {
            ShopSellUI.Instance?.ShowMessage("인벤토리가 가득 찼어요!");
            return;
        }

        int cost = bought * pricePer;
        MoneyManager.Instance.SpendMoney(cost);

        ShopSellUI.Instance?.ShowMessage($"{item.itemName} x{bought} 구매! -{cost} G");
        ShopSellUI.Instance?.RefreshUI();   // 판매창 돈 표시도 갱신
        RefreshAll();
    }
}