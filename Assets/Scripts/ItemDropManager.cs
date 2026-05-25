using UnityEngine;

public class ItemDropManager : MonoBehaviour
{
    public static ItemDropManager Instance;

    [Header("¼³Á¤")]
    public GameObject worldItemPrefab;
    public Transform playerTransform;
    public float dropDistance = 1.0f;
    public float scatterRadius = 0.5f;

    void Awake()
    {
        Instance = this;
    }

    public void DropItem(ItemData item, int quantity, Vector3 worldPosition)
    {
        if (worldItemPrefab == null || item == null || quantity <= 0) return;

        Vector3 scatter = new Vector3(
            Random.Range(-scatterRadius, scatterRadius),
            Random.Range(-scatterRadius, scatterRadius), 0);

        GameObject obj = Instantiate(worldItemPrefab, worldPosition + scatter, Quaternion.identity);
        WorldItem wi = obj.GetComponent<WorldItem>();
        wi?.Init(item, quantity);
    }

    public void DropItemNearPlayer(ItemData item, int quantity)
    {
        if (playerTransform == null) return;
        Vector3 pos = playerTransform.position + new Vector3(dropDistance, 0, 0);
        DropItem(item, quantity, pos);
    }

    public void DropItemFromHarvest(ItemData item, int quantity, Vector3 cropWorldPosition)
    {
        DropItem(item, quantity, cropWorldPosition);
    }
}