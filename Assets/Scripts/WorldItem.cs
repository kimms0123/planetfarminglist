using UnityEngine;


public class WorldItem : MonoBehaviour
{
    [Header("µ’µ’ æ÷¥œ∏ﬁ¿Ãº«")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.15f;

    [Header("¡›±‚ µÙ∑π¿Ã")]
    public float pickupDelay = 0.5f;

    private ItemData itemData;
    private int quantity;
    private float spawnTime;
    private Vector3 startPos;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(ItemData item, int amount)
    {
        itemData = item;
        quantity = amount;
        spawnTime = Time.time;
        startPos = transform.position;

        if (sr != null && item?.itemSprite != null)
            sr.sprite = item.itemSprite;
    }

    void Update()
    {
        // µ’µ’ æ÷¥œ∏ﬁ¿Ãº«
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    public bool CanPickup() => Time.time >= spawnTime + pickupDelay;
    public ItemData GetItemData() => itemData;
    public int GetQuantity() => quantity;

    public void SetQuantity(int amount)
    {
        quantity = amount;
        if (quantity <= 0)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!CanPickup()) return;
        if (!other.CompareTag("Player")) return;

        int leftover = InventoryManager.Instance.AddItemAndReturnLeftover(itemData, quantity);
        if (leftover <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            // ¿œ∫Œ∏∏ ¡›±‚
            SetQuantity(leftover);
        }
    }
}