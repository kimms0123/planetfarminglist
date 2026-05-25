using UnityEngine;

public class PestEventIcon : MonoBehaviour
{
    private Vector3Int cellPos;

    // TODO: 나중에 해충 스프레이 아이템 검사 추가
    // public bool requireToolCheck = false;

    public void Init(Vector3Int pos)
    {
        cellPos = pos;
    }

    void OnMouseDown()
    {
        Debug.Log("해충 아이콘 클릭!");
        PestGrowthEventManager.Instance?.OnIconClicked(cellPos);
    }
}