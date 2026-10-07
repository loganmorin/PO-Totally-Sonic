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
    bool isMoving = false;
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
            anim.SetBool("isGrounded",true);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            anim.SetBool("isGrounded",false);
        }
    }


    void Update()
    {
        ManageJump();
        ManageCrounch();
        
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

        print(rb.linearVelocityY);
        if (controls.Sonic.Jump.WasReleasedThisFrame())
        {
            if(rb.linearVelocityY >=0)
                rb.linearVelocityY -= rb.linearVelocityY/2;
        }
    }

    void ManageCrounch()
    {
        if (controls.Sonic.Crounch.WasPressedThisFrame() && isGrounded)
        {
            anim.SetBool("isCrounching", true);
        }else if (controls.Sonic.Crounch.WasReleasedThisFrame())
        {
            anim.SetBool("isCrounching", false);
        }
    }

    void UpdateVelocity(int dir)
    {

        //Friction
        if (dir == 0 || anim.GetBool("isCrounching"))
        {
            velocity = Mathf.MoveTowards(velocity, 0f, FRICTION * Time.deltaTime);
        }

        //Animation basée sur la vitesse
        anim.SetFloat("Horizontal Speed", Mathf.Abs(velocity));

        if (velocity < 0)
            sr.flipX = true;
        else if (velocity > 0)
            sr.flipX = false;

        //Animation de freinage
        if (velocity * dir < 0f)
        {
            velocity += dir * BRAKE * Time.deltaTime;;
            anim.SetBool("isBraking", true);
            sr.flipX = !sr.flipX;
        }
        else
        {
            float accel = Mathf.Abs(velocity) <= MAX_SPEED ? ACCEL1 : ACCEL2;
            velocity += dir * accel * Time.deltaTime;;
            anim.SetBool("isBraking", false);
        }
    }
}