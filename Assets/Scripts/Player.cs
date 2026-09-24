using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] public float MAX_SPEED = 20f;
    [SerializeField] public float accel = 1f;
    public float speed;
    public Vector2 moveInput;

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveInput != Vector2.zero)
        {
            if(speed <= MAX_SPEED)
            {
                transform.Translate(moveDir * speed * Time.deltaTime, Space.World);
            }
        }
        
    }
}
