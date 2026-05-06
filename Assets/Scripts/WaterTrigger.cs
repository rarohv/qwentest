using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
    [SerializeField] private float playerSpeedPenalty = 0.5f;

    private float originalWalkSpeed;
    private PlayerMovement playerMovement;
    private bool playerInWater;

    void Start()
    {
        EnsureTriggerCollider();
        AutoLinkPlayer();
    }

    void EnsureTriggerCollider()
    {
        Collider existing = GetComponent<Collider>();
        if (existing == null)
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true;
        }
        else
        {
            existing.isTrigger = true;
        }
    }

    void AutoLinkPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerMovement = playerObj.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                originalWalkSpeed = playerMovement.GetWalkSpeed();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GolfBall"))
        {
            GolfBall ball = other.GetComponent<GolfBall>();
            if (ball != null)
            {
                ball.TriggerWaterReset();
            }
        }

        if (other.CompareTag("Player") && !playerInWater)
        {
            playerInWater = true;
            if (playerMovement != null)
            {
                playerMovement.SetWalkSpeed(originalWalkSpeed * playerSpeedPenalty);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && playerInWater)
        {
            playerInWater = false;
            if (playerMovement != null)
            {
                playerMovement.SetWalkSpeed(originalWalkSpeed);
            }
        }
    }
}
