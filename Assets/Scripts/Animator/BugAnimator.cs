using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 미니게임 패널 안의 개별 해충 1마리의 애니메이션 컨트롤러
/// - 평소: move 1~2번 sprite 반복
/// - 죽음: die 1~5번 sprite 재생
/// </summary>
public class BugAnimator : MonoBehaviour
{
    public enum BugType
    {
        Front,  // 정면 보는 해충 (moveF, dieF)
        Left    // 좌측 보는 해충 (moveL, dieL)
    }

    [Header("타입")]
    public BugType bugType = BugType.Front;

    [Header("Sprite 배열")]
    [Tooltip("이동 sprite (2장)")]
    public Sprite[] moveSprites;

    [Tooltip("죽는 sprite (5장)")]
    public Sprite[] dieSprites;

    [Header("애니메이션 속도")]
    public float moveFrameDuration = 0.3f;
    public float dieFrameDuration = 0.1f;

    private Image image;
    private Coroutine currentAnim;
    private bool isDying = false;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    void OnEnable()
    {
        isDying = false;
        StartMoveAnimation();
    }

    void OnDisable()
    {
        if (currentAnim != null)
        {
            StopCoroutine(currentAnim);
            currentAnim = null;
        }
    }

    public void StartMoveAnimation()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(MoveLoop());
    }

    IEnumerator MoveLoop()
    {
        if (moveSprites == null || moveSprites.Length == 0) yield break;

        int index = 0;
        while (!isDying)
        {
            if (image != null && moveSprites[index] != null)
                image.sprite = moveSprites[index];

            index = (index + 1) % moveSprites.Length;
            yield return new WaitForSeconds(moveFrameDuration);
        }
    }

    public void PlayDieAnimation()
    {
        if (isDying) return;
        isDying = true;

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(DieSequence());
    }

    IEnumerator DieSequence()
    {
        if (dieSprites == null || dieSprites.Length == 0) yield break;

        for (int i = 0; i < dieSprites.Length; i++)
        {
            if (image != null && dieSprites[i] != null)
                image.sprite = dieSprites[i];

            yield return new WaitForSeconds(dieFrameDuration);
        }
    }
}