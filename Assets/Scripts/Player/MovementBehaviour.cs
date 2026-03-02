using UnityEngine;
using UnityEngine.InputSystem;

public class MovementBehaviour : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;

    private Animator anim;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        movement = Vector2.zero;

        // PRIORIDAD VERTICAL (sin diagonales)
        if (Keyboard.current.wKey.isPressed) movement = Vector2.up;
        else if (Keyboard.current.sKey.isPressed) movement = Vector2.down;
        else if (Keyboard.current.aKey.isPressed) movement = Vector2.left;
        else if (Keyboard.current.dKey.isPressed) movement = Vector2.right;

        // --- ANIMATOR ---
        bool isMoving = movement != Vector2.zero;
        anim.SetBool("IsMoving", isMoving);

        // Reseteo siempre
        anim.SetBool("MoveUp", false);
        anim.SetBool("MoveDown", false);
        anim.SetBool("MoveLeft", false);
        anim.SetBool("MoveRight", false);

        // Solo marco dirección si me estoy moviendo
        if (isMoving)
        {
            if (movement == Vector2.up) anim.SetBool("MoveUp", true);
            else if (movement == Vector2.down) anim.SetBool("MoveDown", true);
            else if (movement == Vector2.left) anim.SetBool("MoveLeft", true);
            else if (movement == Vector2.right) anim.SetBool("MoveRight", true);
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
