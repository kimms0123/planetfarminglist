using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class SprayAnimator : MonoBehaviour
{
    [Header("Sprite 배열 (1번~4번)")]
    [Tooltip("스프레이 1, 2, 3, 4번 sprite 차례로")]
    public Sprite[] spraySprites;

    [Header("애니메이션 속도")]
    [Tooltip("각 sprite 프레임 표시 시간 (초)")]
    public float frameDuration = 0.1f;

    private Image image;
    private Coroutine currentAnim;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    void OnEnable()
    {
        // 패널 켜질 때 1번 sprite로 초기화
        SetIdleSprite();
    }

    void OnDisable()
    {
        if (currentAnim != null)
        {
            StopCoroutine(currentAnim);
            currentAnim = null;
        }
    }

    // ─────────────────────────────────────────
    // 기본 상태 (1번 sprite)
    // ─────────────────────────────────────────
    public void SetIdleSprite()
    {
        if (currentAnim != null)
        {
            StopCoroutine(currentAnim);
            currentAnim = null;
        }

        if (image != null && spraySprites != null && spraySprites.Length > 0)
            image.sprite = spraySprites[0];
    }

    // ─────────────────────────────────────────
    // 뿌리기 애니메이션 (1→2→3→4)
    // ─────────────────────────────────────────
    public void PlaySprayAnimation()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SpraySequence());
    }

    IEnumerator SpraySequence()
    {
        if (spraySprites == null || spraySprites.Length == 0) yield break;

        for (int i = 0; i < spraySprites.Length; i++)
        {
            if (image != null && spraySprites[i] != null)
                image.sprite = spraySprites[i];

            yield return new WaitForSeconds(frameDuration);
        }

        // 마지막 sprite (4번 = 1번과 같음) 유지
        currentAnim = null;
    }
}