using UnityEngine;

// 강물 레인에 대한 구체적인 구현 (통나무 스폰)
public class RiverLane : Lane
{
    public GameObject obstaclePrefab; // 강물에서 스폰할 장애물(예: 통나무) 프리팹
    public float obstacleSpeed = 5f; // 스폰될 통나무의 이동 속도
    public float spawnInterval = 3f; // 통나무 스폰 간격
    
    public Vector3 obstacleSpawnOffset = new Vector3(0, 0, 0); // 통나무 스폰 위치 오프셋 (동적 계산됨)

    public Vector3 obstacleMoveDirection = Vector3.right; // 통나무 이동 방향

    // 레인 타입 반환
    public override LaneType GetLaneType()
    {
        return LaneType.River;
    }

    // 레인에 엔티티(통나무) 생성 및 스폰 주기 설정
    public override void SpawnEntities()
    {
        // 일정 시간 간격으로 SpawnObstacle 메서드 반복 호출 시작
        InvokeRepeating("SpawnObstacle", 0.5f, spawnInterval);
    }

    // 통나무 스폰
    // ReSharper disable Unity.PerformanceAnalysis (성능 분석 경고 무시)
    void SpawnObstacle()
    {
        // 게임 오버 상태이면 스폰 중단
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            CancelInvoke("SpawnObstacle"); // 반복 호출 취소
            return;
        }

        float currentOffset_X; // 통나무 스폰의 X축 오프셋
        // 레인의 실제 X축 월드 폭 계산
        if (obstacleMoveDirection == Vector3.left) {
            currentOffset_X = (transform.localScale.x * 10f / 2f) + 5f; // 오른쪽 밖에서 스폰
        } else if (obstacleMoveDirection == Vector3.right) {
            currentOffset_X = -((transform.localScale.x * 10f / 2f) + 5f); // 왼쪽 밖에서 스폰
        } else {
            currentOffset_X = obstacleSpawnOffset.x; // 기본 오프셋 사용
        }

        // 통나무 스폰 위치 계산
        Vector3 spawnPos = new Vector3(transform.position.x + currentOffset_X, transform.position.y + obstacleSpawnOffset.y, transform.position.z + obstacleSpawnOffset.z);
        
        GameObject obstacleObj = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity); // 통나무 생성
        
        MovingObstacle movingObstacle = obstacleObj.GetComponent<MovingObstacle>(); // MovingObstacle 컴포넌트 가져오기
        if (movingObstacle != null)
        {
            movingObstacle.Initialize(obstacleMoveDirection, obstacleSpeed); // 통나무 이동 초기화
        }
    }

    // 오브젝트가 비활성화될 때 호출 (씬이 로드되거나 오브젝트 파괴 시)
    void OnDisable()
    {
        CancelInvoke("SpawnObstacle"); // 반복 호출 중단
    }
}