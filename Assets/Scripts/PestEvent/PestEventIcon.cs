using UnityEngine;
using TMPro;

public class PestEventIcon : MonoBehaviour
{
    private Vector3Int cellPos;

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