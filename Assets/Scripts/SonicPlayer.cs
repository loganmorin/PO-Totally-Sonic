using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class SonicPlayer : MonoBehaviour
{
    [Header("Au sol")]
    [SerializeField] private float acceleration = 10f;   // accélération quand on pousse dans le sens du mouvement
    [SerializeField] private float deceleration = 60f;   // freinage quand on pousse dans le sens contraire
    [SerializeField] private float friction = 10f;       // ralentissement sans input
    [SerializeField] private float topSpeed = 12f;       // vitesse max atteinte "par soi-même"
    [SerializeField] private float slopeFactor = 15f;    // influence des pentes sur la vitesse

    [Header("En l'air")]
    [SerializeField] private float airAcceleration = 20f;
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float maxFallSpeed = 20f;
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float jumpCutVelocity = 5f; // saut plus court si on relâche le bouton

    [Header("Détection du sol")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.6f; // un peu plus que la demi-hauteur du perso
    [SerializeField] private float stickToGroundForce = 2f;    // colle le perso au sol dans les courbes

    private Rigidbody2D rb;

    private Vector2 moveInput;
    private bool jumpQueued;
    private bool jumpReleased;

    private bool grounded;
    private Vector2 groundNormal = Vector2.up;
    private float groundSpeed;   // vitesse le long de la pente (le cœur de la physique Sonic)
    private Vector2 velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;        // on gère la gravité nous-mêmes
        rb.freezeRotation = true;
    }

    // --- Input System (PlayerInput -> Behavior: Invoke Unity Events) ---
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started) jumpQueued = true;
        if (context.canceled) jumpReleased = true;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        CheckGround();

        if (grounded) GroundMovement(dt);
        else AirMovement(dt);

        rb.linearVelocity = velocity; // Unity 6. Avant Unity 6 : rb.velocity
        AlignToGround();

        jumpQueued = false;
        jumpReleased = false;
    }

    private void CheckGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(rb.position, Vector2.down, groundCheckDistance, groundLayer);

        // On ne considère pas qu'on est au sol si on s'éloigne de la surface (ex : juste après un saut)
        bool movingAway = hit && Vector2.Dot(velocity, hit.normal) > 0.1f;

        if (hit && !movingAway)
        {
            groundNormal = hit.normal;

            if (!grounded)
            {
                // Atterrissage : on convertit la vitesse en vitesse le long de la pente
                groundSpeed = Vector2.Dot(velocity, GetTangent());
            }
            grounded = true;
        }
        else
        {
            grounded = false;
            groundNormal = Vector2.up;
        }
    }

    // Direction "vers la droite" le long de la surface
    private Vector2 GetTangent()
    {
        return new Vector2(groundNormal.y, -groundNormal.x);
    }

    private void GroundMovement(float dt)
    {
        Vector2 tangent = GetTangent();
        float input = moveInput.x;

        if (Mathf.Abs(input) > 0.01f)
        {
            input = Mathf.Sign(input);

            if (groundSpeed != 0f && Mathf.Sign(groundSpeed) != input)
            {
                // On pousse à contre-sens : freinage
                groundSpeed += input * deceleration * dt;
            }
            else if (Mathf.Abs(groundSpeed) < topSpeed)
            {
                // Accélération seulement si on est sous la vitesse max
                // (les pentes peuvent nous faire dépasser topSpeed, on ne bride pas)
                groundSpeed = Mathf.Clamp(groundSpeed + input * acceleration * dt, -topSpeed, topSpeed);
            }
        }
        else
        {
            groundSpeed = Mathf.MoveTowards(groundSpeed, 0f, friction * dt);
        }

        // Effet des pentes : monter ralentit, descendre accélère
        groundSpeed -= slopeFactor * tangent.y * dt;

        // Saut : on part perpendiculairement à la surface
        if (jumpQueued)
        {
            velocity = tangent * groundSpeed + groundNormal * jumpForce;
            grounded = false;
            return;
        }

        velocity = tangent * groundSpeed - groundNormal * stickToGroundForce;
    }

    private void AirMovement(float dt)
    {
        float input = moveInput.x;

        if (Mathf.Abs(input) > 0.01f)
        {
            input = Mathf.Sign(input);
            bool underTop = Mathf.Abs(velocity.x) < topSpeed || Mathf.Sign(velocity.x) != input;
            if (underTop) velocity.x += input * airAcceleration * dt;
        }

        // Saut variable : relâcher le bouton coupe la montée
        if (jumpReleased && velocity.y > jumpCutVelocity)
            velocity.y = jumpCutVelocity;

        velocity.y = Mathf.Max(velocity.y - gravity * dt, -maxFallSpeed);
    }

    private void AlignToGround()
    {
        float angle = Mathf.Atan2(-groundNormal.x, groundNormal.y) * Mathf.Rad2Deg;
        rb.MoveRotation(angle);
    }
}