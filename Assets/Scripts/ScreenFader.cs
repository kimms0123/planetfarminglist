using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// 화면을 까맣게 덮었다 걷는 페이드 효과.
// 세팅:
//  1) Canvas 아래에 화면 전체를 덮는 Image 하나 (검은색, 처음엔 투명)
//  2) 그 Image에 이 스크립트 추가, fadeImage 칸에 자기 Image 연결
//  3) Canvas의 Render Mode = Screen Space - Overlay 권장 (씬 위에 항상 덮이게)
public class ScreenFader : MonoBehaviour
{
    [Header("화면을 덮는 검은 Image")]
    public Image fadeImage;

    void Awake()
    {
        if (fadeImage == null) fadeImage = GetComponent<Image>();
        // 시작은 투명
        SetAlpha(0f);
    }

    // 어두워짐 (alpha 0 → 1)
    public IEnumerator FadeOut(float duration)
    {
        yield return Fade(0f, 1f, duration);
    }

    // 밝아짐 (alpha 1 → 0)
    public IEnumerator FadeIn(float duration)
    {
        yield return Fade(1f, 0f, duration);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeImage == null) yield break;

        // 페이드 중 클릭이 뒤로 넘어가지 않게 막기
        fadeImage.raycastTarget = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(to);

        if (to == 0f) fadeImage.raycastTarget = false; // 다 밝아지면 클릭 통과
    }

    void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}