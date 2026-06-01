using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

// 침대: 플레이어가 가까이서 E키 → 확인창(예/아니오 버튼) → 페이드아웃 → Sleep(다음날) → 페이드인
//
// 버튼 연결:
//  - "예" 버튼 OnClick  → Bed.ConfirmSleep()
//  - "아니오" 버튼 OnClick → Bed.CancelSleep()
//
// 세팅:
//  1) 침대 오브젝트에 Collider 2D (Is Trigger = true)
//  2) 이 스크립트 추가
//  3) confirmUI(확인창 패널), fader(ScreenFader) 인스펙터 연결
[RequireComponent(typeof(Collider2D))]
public class Bed : MonoBehaviour
{
    [Header("확인창 UI (예/아니오 버튼 패널)")]
    public GameObject confirmUI;

    [Header("페이드 (선택)")]
    public ScreenFader fader;
    public float fadeDuration = 1f;

    private bool playerInRange = false;
    private bool isSleeping = false;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        if (isSleeping) return;

        // 확인창이 떠 있는 동안엔 E키 무시 (버튼으로만 처리)
        if (confirmUI != null && confirmUI.activeSelf) return;

        // 침대 근처에서 E키 → 확인창 띄우기
        if (playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (confirmUI != null)
                confirmUI.SetActive(true);
            else
                StartCoroutine(SleepRoutine()); // 확인창 없으면 바로 잠
        }
    }

    // ───────── 버튼이 호출하는 함수 ─────────

    // "예" 버튼 OnClick 에 연결
    public void ConfirmSleep()
    {
        if (confirmUI != null) confirmUI.SetActive(false);
        StartCoroutine(SleepRoutine());
    }

    // "아니오" 버튼 OnClick 에 연결
    public void CancelSleep()
    {
        if (confirmUI != null) confirmUI.SetActive(false);
    }

    // ───────── 실제 취침 처리 ─────────
    IEnumerator SleepRoutine()
    {
        if (isSleeping) yield break;
        isSleeping = true;

        PlayerController.IsInputLocked = true;   // 자는 동안 입력 잠금

        // 1) 페이드 아웃 (어두워짐)
        if (fader != null)
            yield return fader.FadeOut(fadeDuration);

        // 2) 하루 넘기기 (날짜/계절/시간은 TimeManager가 처리)
        if (TimeManager.Instance != null)
            TimeManager.Instance.Sleep();

        yield return new WaitForSeconds(0.4f);   // 검은 화면에서 잠깐

        // 3) 페이드 인 (밝아짐)
        if (fader != null)
            yield return fader.FadeIn(fadeDuration);

        PlayerController.IsInputLocked = false;
        isSleeping = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}