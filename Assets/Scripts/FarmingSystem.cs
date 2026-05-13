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
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            worldPos.z = 0;

            Vector3Int cellPos = TileManager.Instance.WorldToCell(worldPos);

            ItemData selectedItem = Inventory.Instance.SelectedItem;
            if (selectedItem == null) return;

            // 해당 위치 작물 오브젝트 확인
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            FarmTile farmTile = hit != null ? hit.GetComponent<FarmTile>() : null;

            switch (selectedItem.itemType)
            {
                case ItemType.Tool:
                    UseTool(selectedItem.toolType, farmTile, cellPos);
                    break;
                case ItemType.Seed:
                    PlantSeed(selectedItem, farmTile, cellPos);
                    break;
            }
        }
    }

    void UseTool(ToolType toolType, FarmTile farmTile, Vector3Int cellPos)
    {
        switch (toolType)
        {
            case ToolType.Hoe:
                // 일반 땅에만 괭이질 가능
                if (!TileManager.Instance.IsGround(cellPos))
                {
                    Debug.Log("땅이 아니에요!");
                    return;
                }
                if (TileManager.Instance.IsTilled(cellPos))
                {
                    Debug.Log("이미 갈아엎은 땅이에요!");
                    return;
                }
                TileManager.Instance.SetTilled(cellPos);
                // 빈 FarmTile 오브젝트 생성
                GameObject tileObj = new GameObject("FarmTile");
                tileObj.transform.position = TileManager.Instance.CellCenter(cellPos);
                FarmTile newFarmTile = tileObj.AddComponent<FarmTile>();
                newFarmTile.Till();
                Debug.Log("땅을 팠어요!");
                break;

            case ToolType.WateringCan:
                // 경작지에만 효과 있음
                if (!TileManager.Instance.IsTilled(cellPos))
                {
                    Debug.Log("경작지에만 물을 줄 수 있어요!");
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
                if (farmTile.Harvest())
                {
                    TileManager.Instance.RefreshTile(cellPos, farmTile.state);
                    Debug.Log("수확했어요!");
                }
                else
                {
                    Debug.Log("아직 다 자라지 않았어요!");
                }
                break;

            case ToolType.Pickaxe:
                // 경작지 초기화
                if (!TileManager.Instance.IsTilled(cellPos))
                {
                    Debug.Log("경작지가 아니에요!");
                    return;
                }
                if (farmTile != null)
                {
                    farmTile.Reset();
                    if (farmTile.state == FarmTile.TileState.Normal)
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
        if (farmTile != null && farmTile.cropData != null)
        {
            Debug.Log("이미 작물이 있어요!");
            return;
        }
        if (farmTile != null && farmTile.Plant(seedItem.cropData))
        {
            TileManager.Instance.RefreshTile(cellPos, farmTile.state);
            Debug.Log("씨앗을 심었어요!");
        }
    }
}