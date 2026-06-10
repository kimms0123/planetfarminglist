using UnityEngine;

// 2D 탑다운 Y정렬: 화면 아래쪽(Y가 낮을수록) 앞에 그린다.
// 플레이어, 작물, 나무 등 "가려지고 가리는" 오브젝트에 붙인다.
// 모두 같은 Sorting Layer(Entities)에 있어야 서로 정렬된다.
[RequireComponent(typeof(SpriteRenderer))]
public class YSorting : MonoBehaviour
{
    [Tooltip("정렬에 쓸 기준점. 비우면 자기 transform 사용 (보통 발밑/밑동 위치)")]
    public Transform sortPoint;

    [Tooltip("미세 조정용 오프셋")]
    public int offset = 0;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sortingLayerName = "Entities";
    }

    void LateUpdate()
    {
        float y = (sortPoint != null ? sortPoint.position.y : transform.position.y);
        sr.sortingOrder = Mathf.RoundToInt(-y * 100f) + offset;
    }
}