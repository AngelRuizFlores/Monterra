using UnityEngine;
using UnityEngine.InputSystem;

public class MovementBehaviour : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D ownRigidbody;
    private Animator animatorComponent;
    private Vector2 movement;

    private void Awake()
    {
        ownRigidbody = GetComponent<Rigidbody2D>();
        animatorComponent = GetComponent<Animator>();
    }

    private void Update()
    {
        movement = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
        {
            movement = Vector2.up;
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            movement = Vector2.down;
        }
        else if (Keyboard.current.aKey.isPressed)
        {
            movement = Vector2.left;
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            movement = Vector2.right;
        }

        bool isMoving = movement != Vector2.zero;

        animatorComponent.SetBool("IsMoving", isMoving);
        animatorComponent.SetBool("MoveUp", false);
        animatorComponent.SetBool("MoveDown", false);
        animatorComponent.SetBool("MoveLeft", false);
        animatorComponent.SetBool("MoveRight", false);

        if (isMoving)
        {
            if (movement == Vector2.up)
            {
                animatorComponent.SetBool("MoveUp", true);
            }
            else if (movement == Vector2.down)
            {
                animatorComponent.SetBool("MoveDown", true);
            }
            else if (movement == Vector2.left)
            {
                animatorComponent.SetBool("MoveLeft", true);
            }
            else if (movement == Vector2.right)
            {
                animatorComponent.SetBool("MoveRight", true);
            }
        }
    }

    private void FixedUpdate()
    {
        ownRigidbody.MovePosition(ownRigidbody.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}