using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 lastMoveDir = Vector2.down;

    public static bool IsInputLocked = false;

    // 씬 전환에도 플레이어를 유지하기 위한 싱글톤
    public static PlayerController Instance;

    void Awake()
    {
        // 중복 방지: 이미 플레이어가 있으면 새로 로드된 씬의 플레이어는 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnMove(InputValue value)
    {
        if (IsInputLocked)
        {
            moveInput = Vector2.zero;
            return;
        }
        moveInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        if (IsInputLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        rb.linearVelocity = moveInput * moveSpeed;
    }

    void Update()
    {
        // 들고 있는지(도구 제외 아이템 선택) 매 프레임 갱신
        UpdateHoldingState();

        if (IsInputLocked)
        {
            animator.SetBool("IsMoving", false);
            return;
        }

        if (moveInput != Vector2.zero)
        {
            // 대각선일 때 가로/세로 중 더 큰 쪽 하나만 애니메이션에 반영 (모션 충돌 방지)
            Vector2 animDir = GetDominantDir(moveInput);

            lastMoveDir = animDir;
            animator.SetFloat("MoveX", animDir.x);
            animator.SetFloat("MoveY", animDir.y);
            animator.SetBool("IsMoving", true);

            // 좌우 뒤집기는 실제 입력의 x 부호로 (이동 자체는 대각선 그대로 됨)
            if (moveInput.x < 0)
                spriteRenderer.flipX = false;   // 왼쪽
            else if (moveInput.x > 0)
                spriteRenderer.flipX = true;    // 오른쪽
        }
        else
        {
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
            animator.SetBool("IsMoving", false);
        }
    }

    void OnToggleInventory()
    {
        Debug.Log("OnToggleInventory 호출됨!");
        InventoryWindowUI.Instance?.ToggleInventory();
    }

    // 도구가 아닌 아이템(씨앗/작물 등)을 들고 있으면 Animator의 IsHolding = true
    void UpdateHoldingState()
    {
        bool holding = false;

        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            ItemData selected = inv.SelectedItem;
            // 선택된 아이템이 있고, 그게 도구가 아니면 들고 있는 것으로 판정
            if (selected != null && selected.itemType != ItemType.Tool)
                holding = true;
        }

        animator.SetBool("IsHolding", holding);
    }

    // 대각선 입력에서 가로/세로 중 더 큰 축만 남긴 방향 반환 (한 방향 모션만 나오게)
    Vector2 GetDominantDir(Vector2 input)
    {
        if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
            return new Vector2(Mathf.Sign(input.x), 0f);   // 가로 우선
        else
            return new Vector2(0f, Mathf.Sign(input.y));   // 세로 우선
    }

    // ─────────────────────────────────────────────
    // 도구 동작(괭이질/물주기 등) 재생.
    //  - trigger: Animator의 Trigger 이름 (예: "DoHoe", "DoWater")
    //  - duration: 동작 애니메이션 길이(초). 이 시간 동안 IsBusy=true 로 Idle/Walk 전이를 막음
    // 사용: PlayerController.Instance.PlayAction("DoHoe", 0.5f);
    // ─────────────────────────────────────────────
    private Coroutine busyRoutine;

    public void PlayAction(string trigger, float duration)
    {
        if (animator == null) return;

        // 이전 동작 코루틴이 돌고 있으면 멈춤 (IsBusy 갇힘 방지)
        if (busyRoutine != null) StopCoroutine(busyRoutine);

        animator.SetBool("IsBusy", true);
        animator.SetTrigger(trigger);
        busyRoutine = StartCoroutine(EndBusyAfter(duration));
    }

    IEnumerator EndBusyAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (animator != null) animator.SetBool("IsBusy", false);
        busyRoutine = null;
    }

    void OnInteract()
    {
        Debug.Log("OnInteract 호출됨!");
    }
}