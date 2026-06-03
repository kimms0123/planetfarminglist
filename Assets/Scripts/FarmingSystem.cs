using UnityEngine;
using UnityEngine.InputSystem;

public class FarmingSystem : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject cropPrefab;

    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 인벤토리 열려있으면 농사 입력 차단
        if (InventoryWindowUI.Instance != null && InventoryWindowUI.Instance.IsOpen) return;

        if (PlayerController.IsInputLocked) return;

        if (PlayerController.IsInputLocked) return; // 리듬게임/인벤토리 중 입력 차단

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            worldPos.z = 0;

            Vector3Int cellPos = TileManager.Instance.WorldToCell(worldPos);
            ItemData selectedItem = InventoryManager.Instance.SelectedItem;
            if (selectedItem == null) return;

            FarmTile farmTile = GetFarmTileAt(worldPos);

            switch (selectedItem.itemType)
            {
                case ItemType.Tool:
                    UseTool(selectedItem, farmTile, cellPos, worldPos);
                    break;
                case ItemType.Seed:
                    PlantSeed(selectedItem, farmTile, cellPos);
                    break;
            }
        }
    }

    FarmTile GetFarmTileAt(Vector3 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        foreach (var hit in hits)
        {
            FarmTile tile = hit.GetComponent<FarmTile>();
            if (tile != null) return tile;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        Collider2D[] results = new Collider2D[10];
        Physics2D.OverlapPoint(worldPos, filter, results);
        foreach (var hit in results)
        {
            if (hit == null) continue;
            FarmTile tile = hit.GetComponent<FarmTile>();
            if (tile != null) return tile;
        }
        return null;
    }

    void UseTool(ItemData item, FarmTile farmTile, Vector3Int cellPos, Vector3 worldPos)
    {
        switch (item.toolType)
        {
            case ToolType.Hoe:
                if (!TileManager.Instance.IsFarmable(cellPos))
                {
                    Debug.Log("농사 가능한 흙 땅에서만 괭이질할 수 있어요!");
                    return;
                }
                if (TileManager.Instance.IsTilled(cellPos))
                {
                    Debug.Log("이미 갈아엎은 땅이에요!");
                    return;
                }
                // 괭이질 동작 
                PlayerController.Instance?.PlayAction("DoHoe", 0.5f);

                TileManager.Instance.SetTilled(cellPos);

                TileManager.Instance.SetTilled(cellPos);
                Vector3 centerPos = TileManager.Instance.CellCenter(cellPos);

                GameObject tileObj = new GameObject("FarmTile");
                tileObj.transform.position = centerPos;

                SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 5;

                BoxCollider2D col = tileObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1f);
                col.isTrigger = true;

                FarmTile newTile = tileObj.AddComponent<FarmTile>();
                newTile.Till();

                Debug.Log("땅을 팠어요!");
                break;

            case ToolType.WateringCan:
                if (!TileManager.Instance.IsTilled(cellPos))
                {
                    Debug.Log("갈아엎은 땅에만 물을 줄 수 있어요!");
                    return;
                }
                if (farmTile != null && farmTile.Water())
                {
                    TileManager.Instance.RefreshTile(cellPos, farmTile.state);
                    Debug.Log("물을 줬어요!");
                }
                break;

            case ToolType.Harvester:
                if (farmTile == null)
                {
                    Debug.Log("수확할 작물이 없어요!");
                    return;
                }
                if (farmTile.state != FarmTile.TileState.Grown)
                {
                    Debug.Log("아직 다 자라지 않았어요!");
                    return;
                }
                RhythmGameManager.Instance.StartRhythmGame(farmTile, cellPos);
                break;

            case ToolType.Pickaxe:
                if (!TileManager.Instance.IsTilled(cellPos))
                {
                    Debug.Log("경작지가 아니에요!");
                    return;
                }
                if (farmTile != null)
                {
                    farmTile.Reset();
                    Destroy(farmTile.gameObject);
                }
                TileManager.Instance.SetNormal(cellPos);
                Debug.Log("땅을 원래대로 돌렸어요!");
                break;
        }
    }

    void PlantSeed(ItemData seedItem, FarmTile farmTile, Vector3Int cellPos)
    {
        if (!TileManager.Instance.IsTilled(cellPos))
        {
            Debug.Log("땅을 먼저 갈아엎어야 해요!");
            return;
        }
        if (farmTile == null)
        {
            Debug.Log("FarmTile이 없어요!");
            return;
        }
        if (farmTile.cropData != null)
        {
            Debug.Log("이미 작물이 있어요!");
            return;
        }
        if (InventoryManager.Instance.GetItemCount(seedItem) <= 0)
        {
            Debug.Log($"{seedItem.itemName}이 부족해요!");
            return;
        }
        if (farmTile.Plant(seedItem.cropData))
        {
            InventoryManager.Instance.RemoveItem(seedItem, 1);
            TileManager.Instance.RefreshTile(cellPos, farmTile.state);
            Debug.Log($"{seedItem.cropData.cropName} 씨앗을 심었어요!");
        }
    }
}