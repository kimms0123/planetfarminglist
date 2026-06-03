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

    void OnInteract()
    {
        Debug.Log("OnInteract 호출됨!");
    }
}