using UnityEngine;

[CreateAssetMenu(fileName = "CropData", menuName = "Farm/CropData")]
public class CropData : ScriptableObject
{
    [Header("작물 정보")]
    public string cropName;
    public int growthDays;        // 성장에 필요한 날 수
    public Sprite[] growthSprites; // 성장 단계별 스프라이트

    [Header("수확")]
    public string harvestItemName;
    public int harvestAmount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
