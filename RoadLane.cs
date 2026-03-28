using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 도로 레인에 대한 구체적인 구현 (장애물과 코인 스폰)
public class RoadLane : Lane
{
    public GameObject obstaclePrefab; // 도로에서 스폰할 장애물(예: 자동차) 프리팹
    public GameObject coinPrefab;     // 코인 프리팹

    public float obstacleSpeed = 10f; // 스폰될 장애물의 이동 속도
    public float spawnInterval = 2f; // 장애물 스폰 간격
    public Vector3 obstacleSpawnOffset = Vector3.zero; // 장애물 스폰 위치 오프셋 (기본값)
    public Vector3 obstacleMoveDirection = Vector3.left; // 장애물 이동 방향

    [Range(0f, 1f)]
    public float coinSpawnChance = 0.5f; // 코인 스폰 확률
    public int maxCoinsPerLane = 5;      // 레인 당 최대 코인 수

    public float gridSize = 1f;          // 오브젝트 스폰 그리드 간격
    public float spawnOffsetY = 0.5f;    // 오브젝트가 레인 표면에서 뜨는 높이

    // 레인 타입 반환
    public override LaneType GetLaneType()
    {
        return LaneType.Road;
    }

    // 레인에 엔티티(장애물, 코인) 생성 및 스폰 주기 설정
    public override void SpawnEntities()
    {
        // 일정 시간 간격으로 SpawnObstacle 메서드 반복 호출 시작
        InvokeRepeating("SpawnObstacle", 0.5f, spawnInterval); 
        SpawnCoins(); // 코인 스폰
    }

    // 장애물(자동차) 스폰
    void SpawnObstacle()
    {
        // 게임 오버 상태이면 스폰 중단
        if (GameManager.Instance == null || GameManager.Instance.isGameOver)
        {
            CancelInvoke("SpawnObstacle"); // 반복 호출 취소
            return;
        }

        float currentOffset_X; // 장애물 스폰의 X축 오프셋
        float laneWorldWidth = transform.localScale.x * 10f; // 레인의 실제 X축 월드 폭 계산

        // 장애물 이동 방향에 따라 초기 스폰 위치 설정 (레인 밖에서 진입)
        if (obstacleMoveDirection == Vector3.left) {
            currentOffset_X = (laneWorldWidth / 2f) + 5f; // 오른쪽 밖에서 스폰
        } else if (obstacleMoveDirection == Vector3.right) {
            currentOffset_X = -((laneWorldWidth / 2f) + 5f); // 왼쪽 밖에서 스폰
        } else {
            currentOffset_X = obstacleSpawnOffset.x; // 기본 오프셋 사용
        }

        // 장애물 스폰 위치 계산
        Vector3 spawnPos = new Vector3(transform.position.x + currentOffset_X, 
                                       transform.position.y + obstacleSpawnOffset.y, 
                                       transform.position.z + obstacleSpawnOffset.z);

        GameObject obstacleObj = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity); // 장애물 생성
        //obstacleObj.transform.SetParent(this.transform); // (선택 사항: 레인 자식으로 설정)

        MovingObstacle movingObstacle = obstacleObj.GetComponent<MovingObstacle>(); // MovingObstacle 컴포넌트 가져오기
        if (movingObstacle != null)
        {
            movingObstacle.Initialize(obstacleMoveDirection, obstacleSpeed); // 장애물 이동 초기화
        }
    }

    // 코인 스폰
    void SpawnCoins()
    {
        if (coinPrefab == null) return; // 코인 프리팹 없으면 중단

        float actualLaneXWorldWidth = transform.localScale.x * 10f; // 레인의 실제 X축 월드 폭 계산
        
        List<float> availableXPositions = new List<float>(); // 스폰 가능한 X 그리드 위치 목록
        // 레인 폭을 기준으로 스폰 그리드 X 좌표들 추가
        for (float x = -(actualLaneXWorldWidth / 2f) + gridSize / 2f; x < (actualLaneXWorldWidth / 2f); x += gridSize)
        {
            availableXPositions.Add(x);
        }

        List<Vector3> occupiedGridPositions = new List<Vector3>(); // 이미 오브젝트가 차지한 그리드 위치
        ShuffleList(availableXPositions); // 후보 위치를 무작위로 섞음

        int coinsSpawned = 0; // 스폰된 코인 수
        foreach (float spawnX in availableXPositions)
        {
            if (coinsSpawned >= maxCoinsPerLane) break; // 최대 코인 수 도달 시 중단

            if (Random.value < coinSpawnChance) // 확률에 따라 스폰
            {
                Vector3 proposedCoinPos = new Vector3(spawnX, transform.position.y + spawnOffsetY, transform.position.z);

                if (occupiedGridPositions.Contains(proposedCoinPos)) continue; // 이미 차지된 위치면 건너뛰기

                GameObject coin = Instantiate(coinPrefab, proposedCoinPos, Quaternion.identity); // 코인 생성
                coin.transform.SetParent(this.transform); // 코인을 현재 레인의 자식으로 설정
                occupiedGridPositions.Add(proposedCoinPos); // 차지된 위치 추가
                coinsSpawned++; // 스폰된 코인 수 증가
            }
        }
    }

    // 리스트 요소를 무작위로 섞는 유틸리티 메서드
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // 오브젝트가 비활성화될 때 호출 (씬이 로드되거나 오브젝트 파괴 시)
    void OnDisable()
    {
        CancelInvoke("SpawnObstacle"); // 반복 호출 중단
    }
}