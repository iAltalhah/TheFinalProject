using UnityEngine;

public class MovingPlatform : MonoBehaviour
{

    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private Transform player;
    [SerializeField] private float speed = 3f;

    private Transform targetPosition;
    private bool isPlayerOn;

    private void Start()
    {
        targetPosition = pointB;
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition.position,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition.position) < 0.1f)
        {
            if (targetPosition == pointA)
            {
                targetPosition = pointB;
            }
            else
            {
                targetPosition = pointA;
            }
        }

        if (isPlayerOn)
        {
            SnapPlayer();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerOn = true;
        Debug.Log("player is on");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerOn = false;
        Debug.Log("no the player left");

    }

    private void SnapPlayer()
    {
        player.position = transform.position;
    }
}