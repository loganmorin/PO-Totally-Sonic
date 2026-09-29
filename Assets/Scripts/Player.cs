using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    PlayerActions controls;

    [SerializeField] float RUNNING_SPEED = 7f;
    [SerializeField] float SPRINTING_SPEED = 10;
    [SerializeField] float MAX_SPEED = 20f;
    [SerializeField] float ACCEL1 = 10f;     // accélération jusqu'à MAX_SPEED
    [SerializeField] float ACCEL2 = 1f;   // accélération au-delà de MAX_SPEED
    [SerializeField] float FRICTION = 10f;   // ralentissement sans input
    [SerializeField] float BRAKE = 15f;     // freinage quand on inverse la direction
    [SerializeField] float JUMP_STRENGTH = 5f;

    float velocity;  // < 0 = gauche, > 0 = droite
    int lastDir;
    bool isGrounded;
    private Rigidbody2D rb;    
    private Animator anim;
    private SpriteRenderer sr;

    void Awake()
    {
        controls = new PlayerActions();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
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
        ManageJump();
        
        int dir = ReadDirection();
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

    void ManageJump()
    {
        if (controls.Sonic.Jump.WasPressedThisFrame() && isGrounded)
        {
            rb.AddForce(new Vector2(0,JUMP_STRENGTH));
            anim.SetBool("isJumping", true);
            isGrounded = !isGrounded;
        }
        else if (isGrounded)
        {
            anim.SetBool("isJumping", false);
        }

        if (controls.Sonic.Jump.WasReleasedThisFrame())
        {
            rb.AddForce(new Vector2(0,-JUMP_STRENGTH * 2/3));
        }
    }

    void UpdateVelocity(int dir)
    {
        float dt = Time.deltaTime;

        //Friction
        if (dir == 0)
        {
            velocity = Mathf.MoveTowards(velocity, 0f, FRICTION * dt);
        }

        //Animation basée sur la vitesse
        SpeedAnimation();

        //Animation de freinage
        if (velocity * dir < 0f)
        {
            velocity += dir * BRAKE * dt;
            anim.SetBool("isBraking", true);
            sr.flipX = !sr.flipX;
        }
        else
        {
            float accel = Mathf.Abs(velocity) <= MAX_SPEED ? ACCEL1 : ACCEL2;
            velocity += dir * accel * dt;
            anim.SetBool("isBraking", false);
        }
    }


    void SpeedAnimation()
    {
        if (velocity == 0f)
        {
            anim.SetBool("isWalking",false);
        }else if (velocity > SPRINTING_SPEED || velocity < -SPRINTING_SPEED)
        {
            anim.SetBool("isSprinting", true);
        }else if (velocity > RUNNING_SPEED || velocity < -RUNNING_SPEED)
        {
            anim.SetBool("isSprinting", false);
            anim.SetBool("isRunning", true);
        }
        else
        {
            anim.SetBool("isWalking",true);
            anim.SetBool("isRunning", false);
        }

        if (velocity < 0)
            sr.flipX = true;
        else if (velocity > 0)
            sr.flipX = false;
    }
}