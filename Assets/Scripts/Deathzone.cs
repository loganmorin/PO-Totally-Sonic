using Unity.VisualScripting;
using UnityEngine;

public class Deathzone : MonoBehaviour
{
    bool alreadyDied = false;
    void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject obj = collision.gameObject;
        if (obj.CompareTag("Player") && !alreadyDied)
        {
            Player player = collision.GetComponent<Player>();
            player.Die();
            alreadyDied = true;
        }
    }
}
