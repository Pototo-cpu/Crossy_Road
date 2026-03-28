using UnityEngine;

public class MovingObstacle : MonoBehaviour, IMoveable
{
    private float currentSpeed;
    private Vector3 moveDirection;
    public float destroyOffset = 20f; // <-- 필요 시 조절 (플레인 길이 10 기준)

    public void Initialize(Vector3 direction, float speed)
    {
        moveDirection = direction;
        currentSpeed = speed;

        if (moveDirection != Vector3.zero)
        {
            transform.forward = moveDirection;
        }
    }

    void Update()
    {
        transform.Translate(moveDirection * currentSpeed * Time.deltaTime, Space.World);

        float playerZ = 0;
        if (GameManager.Instance != null && GameManager.Instance.GetComponentInChildren<Player>() != null)
        {
            playerZ = GameManager.Instance.GetComponentInChildren<Player>().transform.position.z;
        }
        
        if (Mathf.Abs(transform.position.z - playerZ) > MapGenerator.Instance.visibleLanes * MapGenerator.Instance.laneWidth / 2f + destroyOffset)
        {
            Destroy(gameObject);
        }
    }

    public void Move(Vector3 direction)
    {
        // 이 스크립트는 자체적으로 이동하므로 필요 시 구현
    }
}