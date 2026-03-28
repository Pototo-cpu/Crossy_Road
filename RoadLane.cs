using UnityEngine;

public class RoadLane : Lane
{
    public GameObject obstaclePrefab;
    public float obstacleSpeed = 10f;
    public float spawnInterval = 2f;
    
    public Vector3 obstacleSpawnOffset = new Vector3(0, 0, 0); // 초기값은 0으로 설정, SpawnObstacle에서 동적 계산

    public Vector3 obstacleMoveDirection = Vector3.left;

    public override LaneType GetLaneType()
    {
        return LaneType.Road;
    }

    public override void SpawnEntities()
    {
        InvokeRepeating("SpawnObstacle", 0.5f, spawnInterval);
    }

    void SpawnObstacle()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            CancelInvoke("SpawnObstacle");
            return;
        }

        float currentOffset_X;
        if (obstacleMoveDirection == Vector3.left) {
            currentOffset_X = (transform.localScale.x * 10f / 2f) + 5f;
        } else if (obstacleMoveDirection == Vector3.right) {
            currentOffset_X = -((transform.localScale.x * 10f / 2f) + 5f);
        } else {
            currentOffset_X = obstacleSpawnOffset.x; // 기본값 유지 (혹시 다른 방향이라면)
        }

        Vector3 spawnPos = new Vector3(transform.position.x + currentOffset_X, transform.position.y + obstacleSpawnOffset.y, transform.position.z + obstacleSpawnOffset.z);

        GameObject obstacleObj = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
        MovingObstacle movingObstacle = obstacleObj.GetComponent<MovingObstacle>();
        if (movingObstacle != null)
        {
            movingObstacle.Initialize(obstacleMoveDirection, obstacleSpeed);
        }
    }

    void OnDisable()
    {
        CancelInvoke("SpawnObstacle");
    }
}