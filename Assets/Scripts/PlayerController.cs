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
            lastMoveDir = moveInput;
            animator.SetFloat("MoveX", moveInput.x);
            animator.SetFloat("MoveY", moveInput.y);
            animator.SetBool("IsMoving", true);

            if (moveInput.x < 0)
                spriteRenderer.flipX = false;   // 왼쪽 = 원본 그대로 (새 아트가 왼쪽 향함)
            else if (moveInput.x > 0)
                spriteRenderer.flipX = true;    // 오른쪽 = 뒤집기
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

    // ─────────────────────────────────────────────
    // 도구 동작(괭이질/물주기 등) 재생.
    //  - trigger: Animator의 Trigger 이름 (예: "DoHoe", "DoWater")
    //  - duration: 동작 애니메이션 길이(초). 이 시간 동안 IsBusy=true 로 Idle/Walk 전이를 막음
    // 사용: PlayerController.Instance.PlayAction("DoHoe", 0.5f);
    // ─────────────────────────────────────────────
    public void PlayAction(string trigger, float duration)
    {
        if (animator == null) return;
        animator.SetBool("IsBusy", true);
        animator.SetTrigger(trigger);
        StopCoroutine(nameof(EndBusyAfter));
        StartCoroutine(EndBusyAfter(duration));
    }

    IEnumerator EndBusyAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (animator != null) animator.SetBool("IsBusy", false);
    }

    void OnInteract()
    {
        Debug.Log("OnInteract 호출됨!");
    }
}