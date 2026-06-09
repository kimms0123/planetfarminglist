using UnityEngine;
using TMPro;

public class PestEventIcon : MonoBehaviour
{
    private Vector3Int cellPos;

    [Header("정렬 순서 (작물/타일 위에 보이게)")]
    public int sortingOrder = 30;

    // TODO: 나중에 스프라이트로 교체
    private TextMeshPro tmp;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = "!";
            tmp.color = Color.red;
            tmp.fontSize = 5;
            tmp.alignment = TextAlignmentOptions.Center;

            // 작물/타일에 가려지지 않게 렌더 순서 올리기
            tmp.sortingOrder = sortingOrder;

            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Default";
                mr.sortingOrder = sortingOrder;
            }
        }
        else
        {
            Debug.LogWarning("PestEventIcon: TextMeshPro 컴포넌트가 없어요! (프리팹 확인)");
        }
    }

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