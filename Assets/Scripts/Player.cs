using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    PlayerActions controls;

    [SerializeField] float MAX_SPEED = 20f;
    [SerializeField] float ACCEL1 = 3f;     // accélération jusqu'à MAX_SPEED
    [SerializeField] float ACCEL2 = 0.6f;   // accélération au-delà de MAX_SPEED
    [SerializeField] float FRICTION = 6f;   // ralentissement sans input
    [SerializeField] float BRAKE = 15f;     // freinage quand on inverse la direction
    [SerializeField] float JUMP_STRENGTH = 5f;

    float velocity;  // < 0 = gauche, > 0 = droite
    int lastDir;
    bool isJumping;
    bool isGrounded;
    private Rigidbody2D rb;            // dernière touche pressée (-1 ou 1)

    void Awake()
    {
        controls = new PlayerActions();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        controls.Sonic.Enable();
    }

    void OnDisable()
    {
        controls.Sonic.Disable();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }


    void Update()
    {
        int dir = ReadDirection();

        if (controls.Sonic.Jump.WasPressedThisFrame() && isGrounded)
        {
            rb.AddForce(new Vector2(0,JUMP_STRENGTH));
        }

        UpdateVelocity(dir);
        transform.Translate(Vector3.right * velocity * Time.deltaTime, Space.World);
    }

    // Retourne -1, 0 ou 1. Si les deux touches sont tenues, la dernière pressée gagne.
    int ReadDirection()
    {
        var left = controls.Sonic.Left;
        var right = controls.Sonic.Right;

        if (left.WasPressedThisFrame())  lastDir = -1;
        if (right.WasPressedThisFrame()) lastDir = 1;

        bool l = left.IsPressed();
        bool r = right.IsPressed();

        if (l && r) return lastDir;
        if (l) return -1;
        if (r) return 1;
        return 0;
    }

    void UpdateVelocity(int dir)
    {
        float dt = Time.deltaTime;

        if (dir == 0)
        {
            // Pas d'input : friction jusqu'à l'arrêt complet
            velocity = Mathf.MoveTowards(velocity, 0f, FRICTION * dt);
            return;
        }

        bool reversing = velocity * dir < 0f;

        if (reversing)
        {
            // Direction opposée : on freine d'abord, puis on repart naturellement
            velocity += dir * BRAKE * dt;
        }
        else
        {
            float accel = Mathf.Abs(velocity) <= MAX_SPEED ? ACCEL1 : ACCEL2;
            velocity += dir * accel * dt;
        }
    }
}