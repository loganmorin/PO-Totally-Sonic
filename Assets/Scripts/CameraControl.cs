using Unity.VisualScripting;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private float maxXOffset = 5f;
    [SerializeField] private float xAccel = 0.1f;
    [SerializeField] private float yOffset = 0.5f;
    [SerializeField] private float MIN_X = 4f;
    private GameObject leftborder;
    private GameObject player;
    private Player playerScript;
    private Quaternion initialRotation;
    private float xOffset = 0f;
    private float gap_x;

    void Start()
    {
        leftborder = GameObject.FindWithTag("LeftBorder");
        player = GameObject.FindWithTag("Player");
        playerScript = player.GetComponent<Player>();
        initialRotation = transform.rotation;
    }

    void Update()
    {
        if(player == null) return;

        gap_x = player.transform.position.x + xOffset - leftborder.transform.position.x;
        FollowPlayer();
        CalculateOffset();
    }

    void FollowPlayer()
    {
        if(gap_x < MIN_X)
        {
            transform.position = new Vector3(
                leftborder.transform.position.x + MIN_X,
                player.transform.position.y + yOffset,
                -1
            );
        }else if (!playerScript.isDead)
        {
            transform.position = new Vector3(
                player.transform.position.x + xOffset,
                player.transform.position.y + yOffset,
                -1
            );
        }
    }

    void CalculateOffset()
    {
        if(gap_x < MIN_X) return;
        if (playerScript.dir == 0 || xOffset * playerScript.dir < 0){
            if (xOffset < 0)
                xOffset += xAccel;
            else
                xOffset -= xAccel;
        }
        
        if (xOffset < maxXOffset && xOffset > -maxXOffset)
        {
            xOffset += xAccel * playerScript.dir;
        }
    }

    void LateUpdate()
    {
        transform.rotation = initialRotation;
    }
}
