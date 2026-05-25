using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 lastMoveDir = Vector2.down;

    // ★ 리듬게임 중 이동 막기
    public static bool IsInputLocked = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnMove(InputValue value)
    {
        // ★ 입력 잠금 중이면 이동 입력 무시
        if (IsInputLocked)
        {
            moveInput = Vector2.zero;
            return;
        }
        moveInput = value.Get<Vector2>();
    }
    void OnOpenInventory()
    {
        InventoryWindowUI.Instance?.ToggleInventory();
    }

    void FixedUpdate()
    {
        // ★ 잠금 중이면 강제로 속도 0
        if (IsInputLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        rb.linearVelocity = moveInput * moveSpeed;
    }

    void Update()
    {
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
                spriteRenderer.flipX = true;
            else if (moveInput.x > 0)
                spriteRenderer.flipX = false;
        }
        else
        {
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
            animator.SetBool("IsMoving", false);
        }
    }
}