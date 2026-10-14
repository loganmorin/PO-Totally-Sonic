using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    //Contrôles du joueur
    PlayerActions controls;

    [Header("Vitesse et physique")]
    [SerializeField] float MAX_SPEED = 15f;
    [SerializeField] float ACCEL1 = 5f;     // accélération jusqu'à MAX_SPEED
    [SerializeField] float FRICTION = 10f;   // ralentissement sans input
    [SerializeField] float BRAKE = 25f;      // freinage quand on inverse la direction
    [SerializeField] float JUMP_STRENGTH = 8.5f;

    [Header("Rayon au sol")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float groundRayLength = 0.1f;

    [Header("Comportement avec les pentes")]
    [SerializeField] float SLOPE_FORCE = 20f;
    [SerializeField] float CEILING_LIMIT = 0.3f;       // normal.y en dessous = mur raide / plafond
    [SerializeField] float MIN_CEILING_SPEED = 8f;     // vitesse minimale pour rester collé
    
    [Header("Spindash")]
    [SerializeField] float SPINDASH_CHARGE = 2f;     // gain par appui sur Jump
    [SerializeField] float SPINDASH_DECAY = 1f;      // perte de charge par seconde

    Vector2 lastRayDir = Vector2.down;
    RaycastHit2D groundHit;
    Vector2 groundNormal = Vector2.up;
    float jumpLockTimer;
    float defaultGravity;
    float velocity;  // < 0 = gauche, > 0 = droite
    float spindashCharge;
    public int dir;
    int lastDir;
    int facingDir = 1;   // 1 = droite, -1 = gauche
    bool isSpindashing = false; 
    bool canInput = true;
    public bool isDead = false;
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
        defaultGravity = rb.gravityScale;
    }

    void OnEnable()
    {
        controls.Sonic.Enable();
    }

    void OnDisable()
    {
        controls.Sonic.Disable();
    }

    void Update()
    {
        if (canInput)
        {
            ManageJump();
            ManageCrounch();

            dir = ReadDirection();
            UpdateVelocity(dir);
            ApplySlope();
        }
    }

    void FixedUpdate()
    {
        CheckGround();
        AlignToGround();
        if (canInput)
            Move();
    }

    void ManageJump()
    {
        if (controls.Sonic.Jump.WasPressedThisFrame() && anim.GetBool("isGrounded"))
        {
            if (!anim.GetBool("isCrounching"))
            {
                Vector2 rotatedVector = Quaternion.AngleAxis(
                    transform.eulerAngles.z, 
                    Vector3.forward) * Vector2.up;

                rb.AddForce(rotatedVector * JUMP_STRENGTH, 
                    ForceMode2D.Impulse);

                anim.SetBool("isJumping", true);
                anim.SetBool("isGrounded", false);
                jumpLockTimer = 0.1f;
            }
            else
            {
                // Accroupi + Jump : on démarre / on charge le spindash
                if (!isSpindashing)
                {
                    StartSpindash();
                    spindashCharge = Mathf.Min(spindashCharge + SPINDASH_CHARGE, MAX_SPEED - JUMP_STRENGTH);
                }
            }
        }
        else if (anim.GetBool("isGrounded"))
        {
            anim.SetBool("isJumping", false);
        }

        // Saut variable (désactivé pendant le spindash)
        if (controls.Sonic.Jump.WasReleasedThisFrame() && 
            !isSpindashing && 
            rb.linearVelocityY > 0)
        {
            rb.linearVelocityY *= 0.5f;
        }

        // La charge retombe doucement
        if (isSpindashing)
            spindashCharge = Mathf.MoveTowards(spindashCharge, 0f, SPINDASH_DECAY * Time.deltaTime);
    }

    void ManageCrounch()
    {
        if (controls.Sonic.Crounch.WasPressedThisFrame() && anim.GetBool("isGrounded"))
        {
            anim.SetBool("isCrounching", true);
        }
        else if (controls.Sonic.Crounch.WasReleasedThisFrame())
        {
            anim.SetBool("isCrounching", false);
            if (isSpindashing) 
                ReleaseSpindash();
        }
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
        if (isSpindashing)
            return;

        // Friction
        if (dir == 0 || anim.GetBool("isCrounching"))
        {
            velocity = Mathf.MoveTowards(velocity, 0f, FRICTION * Time.deltaTime);
        }

        // Animation basée sur la vitesse
        anim.SetFloat("Horizontal Speed", Mathf.Abs(velocity));

        if (velocity < 0)
            sr.flipX = true;
        else if (velocity > 0)
            sr.flipX = false;

        // Direction mémorisée (pour le spindash)
        if (velocity != 0) facingDir = velocity > 0 ? 1 : -1;
        else if (dir != 0) facingDir = dir;

        // Accroupi : pas d'accélération ni de freinage
        if (anim.GetBool("isCrounching"))
        {
            anim.SetBool("isBraking", false);
            return;
        }

        // Freinage / accélération
        if (velocity * dir < 0f)
        {
            velocity += dir * BRAKE * Time.deltaTime;
            anim.SetBool("isBraking", true);
            sr.flipX = !sr.flipX;
        }
        else
        {
            float accel = Mathf.Abs(velocity) <= MAX_SPEED ? ACCEL1 : 0;
            velocity += dir * accel * Time.deltaTime;
            anim.SetBool("isBraking", false);
        }
    }

    void ApplySlope()
    {
        if (!anim.GetBool("isGrounded") || isSpindashing) 
            return;

        if(velocity != 0)
            velocity += SLOPE_FORCE * groundNormal.x * Time.deltaTime;
    }

    void CheckGround()
    {
        if (jumpLockTimer > 0f) jumpLockTimer -= Time.fixedDeltaTime;

        lastRayDir = anim.GetBool("isGrounded") ? -(Vector2)transform.up : Vector2.down;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, lastRayDir, groundRayLength, groundLayer);
        bool grounded = hit.collider != null && jumpLockTimer <= 0f;
        
        if(grounded && hit.normal.y < CEILING_LIMIT && Mathf.Abs(velocity) < MIN_CEILING_SPEED)
        {
           Vector2 t = new Vector2(hit.normal.y, -hit.normal.x);
            rb.linearVelocity = t * velocity;
            velocity = rb.linearVelocity.x;
            jumpLockTimer = 0.2f;     // empêche de se re-coller immédiatement
            grounded = false; 
        }

        groundHit = hit;
        groundNormal = grounded ? hit.normal : Vector2.up;

        if (grounded != anim.GetBool("isGrounded"))
        {
            anim.SetBool("isGrounded", grounded);

            if (grounded)
            {
                Vector2 t = new Vector2(groundNormal.y, -groundNormal.x);
                velocity = Vector2.Dot(rb.linearVelocity, t);
            }else
                CancelSpindash();
        }
    }

    void AlignToGround()
    {
        float current = transform.eulerAngles.z;
        float target = Mathf.Atan2(groundNormal.y, groundNormal.x) * Mathf.Rad2Deg - 90f;
        //On oriente le sprite à partir de l'angle en cours jusqu'à l'angle ciblé à une vitesse de 720f
        float angle = Mathf.MoveTowardsAngle(current, target, 720f * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Move()
    {
        if (anim.GetBool("isGrounded"))
        {
            rb.gravityScale = 0f;
            Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x);
            rb.linearVelocity = tangent * velocity;

            float perpDist = groundHit.distance * Vector2.Dot(-lastRayDir, groundNormal);
        }
        else
        {
            rb.gravityScale = defaultGravity;
            rb.linearVelocityX += dir * ACCEL1 * Time.deltaTime;
        }
    }

    void StartSpindash()
    {
        isSpindashing = true;
        anim.SetBool("isSpindashing",true);
        spindashCharge = 0f;
        velocity = 0f;
        sr.flipX = facingDir < 0;
    }

    void ReleaseSpindash()
    {
        velocity = facingDir * (JUMP_STRENGTH + spindashCharge);
        isSpindashing = false;
        anim.SetBool("isSpindashing",false);
        spindashCharge = 0f;
    }

    void CancelSpindash()
    {
        isSpindashing = false;
        spindashCharge = 0f;
    }

    public void Die()
    {
        anim.Play("Hit");
        canInput = false;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(Vector2.up * JUMP_STRENGTH/1.5f, ForceMode2D.Impulse);
        Destroy(gameObject, 1f);
    }
}