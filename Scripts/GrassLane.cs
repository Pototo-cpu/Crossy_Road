using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq 확장 메서드 사용을 위해 추가

// 잔디 레인에 대한 구체적인 구현
public class GrassLane : Lane
{
    public GameObject coinPrefab; // 코인 프리팹
    [Range(0f, 1f)]
    public float coinSpawnChance = 0.5f; // 코인 스폰 확률

    public GameObject treeObstaclePrefab; // 나무 장애물 프리팹
    [Range(0f, 1f)]
    public float treeSpawnChance = 0.3f; // 나무 스폰 확률
    public int maxTreesPerLane = 3; // 레인 당 최대 나무 수
    public int maxCoinsPerLane = 5; // 레인 당 최대 코인 수

    public float gridSize = 1f; // 오브젝트 스폰 그리드 간격 (1미터)
    public float spawnOffsetY = 0.5f; // 오브젝트가 레인 표면에서 뜨는 높이

    // 레인 타입 반환
    public override LaneType GetLaneType()
    {
        return LaneType.Grass;
    }

    // 레인에 엔티티(나무, 코인) 생성
    public override void SpawnEntities()
    {
        // 레인의 실제 X축 월드 폭 계산 (스케일과 기본 메시 크기 고려)
        float actualLaneXWorldWidth = transform.localScale.x * 10f; 
        
        List<float> availableXPositions = new List<float>(); // 스폰 가능한 X 그리드 위치 목록
        // 레인 폭을 기준으로 스폰 그리드 X 좌표들 추가
        for (float x = -(actualLaneXWorldWidth / 2f) + gridSize / 2f; x < (actualLaneXWorldWidth / 2f); x += gridSize)
        {
            availableXPositions.Add(x);
        }

        List<Vector3> occupiedGridPositions = new List<Vector3>(); // 이미 오브젝트가 차지한 그리드 위치
        LayerMask checkLayer = LayerMask.GetMask("Obstacle", "Coin"); 

        // 1. 나무 스폰 로직 (그리드 랜덤 배치 및 겹침 방지)
        if (treeObstaclePrefab != null)
        {
            List<float> treeSpawnXCandidates = new List<float>(availableXPositions); // 나무 스폰 X 후보 위치
            ShuffleList(treeSpawnXCandidates); // 후보 위치를 무작위로 섞음

            int treesSpawned = 0; // 스폰된 나무 수
            foreach (float spawnX in treeSpawnXCandidates)
            {
                if (treesSpawned >= maxTreesPerLane) break; // 최대 나무 수 도달 시 중단

                if (Random.value < treeSpawnChance) // 확률에 따라 스폰
                {
                    Vector3 proposedTreePos = new Vector3(spawnX, transform.position.y + spawnOffsetY, transform.position.z);
                    
                    if (occupiedGridPositions.Contains(proposedTreePos)) continue; // 이미 차지된 위치면 건너뛰기

                    GameObject tree = Instantiate(treeObstaclePrefab, proposedTreePos, Quaternion.identity); // 나무 생성
                    tree.tag = "Obstacle"; // 태그 설정

                    Collider treeCollider = tree.GetComponent<Collider>(); // 콜라이더 설정
                    if (treeCollider != null) { treeCollider.isTrigger = false; }
                    else { Debug.LogWarning($"[GrassLane] Tree Prefab {treeObstaclePrefab.name} is missing a Collider!"); }
                    
                    occupiedGridPositions.Add(proposedTreePos); // 차지된 위치 추가
                    treesSpawned++; // 스폰된 나무 수 증가
                }
            }
        }
        else
        {
            Debug.LogWarning("[GrassLane] Tree Obstacle Prefab is not assigned in the Inspector!");
        }

        // 2. 코인 스폰 로직 (그리드 랜덤 배치 및 겹침 방지)
        if (coinPrefab != null)
        {
            List<float> coinSpawnXCandidates = new List<float>(availableXPositions); // 코인 스폰 X 후보 위치
            ShuffleList(coinSpawnXCandidates); // 후보 위치를 무작위로 섞음

            int coinsSpawned = 0; // 스폰된 코인 수
            foreach (float spawnX in coinSpawnXCandidates)
            {
                if (coinsSpawned >= maxCoinsPerLane) break; // 최대 코인 수 도달 시 중단

                if (Random.value < coinSpawnChance) // 확률에 따라 스폰
                {
                    Vector3 proposedCoinPos = new Vector3(spawnX, transform.position.y + spawnOffsetY, transform.position.z);

                    if (occupiedGridPositions.Contains(proposedCoinPos)) continue; // 이미 차지된 위치면 건너뛰기

                    GameObject coin = Instantiate(coinPrefab, proposedCoinPos, Quaternion.identity); // 코인 생성
                    occupiedGridPositions.Add(proposedCoinPos); // 차지된 위치 추가
                    coinsSpawned++; // 스폰된 코인 수 증가
                }
            }
        }
        else
        {
            Debug.LogWarning("[GrassLane] Coin Prefab is not assigned in the Inspector!");
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
}