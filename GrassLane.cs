using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq 확장 메서드 사용을 위해 추가

public class GrassLane : Lane
{
    public GameObject coinPrefab;
    [Range(0f, 1f)]
    public float coinSpawnChance = 0.5f;

    public GameObject treeObstaclePrefab;
    [Range(0f, 1f)]
    public float treeSpawnChance = 0.3f;
    public int maxTreesPerLane = 3;
    public int maxCoinsPerLane = 5;

    public float gridSize = 1f; // 그리드 간격 (1미터)
    public float spawnOffsetY = 0.5f; // 오브젝트가 레인 표면에서 뜨는 높이

    public override LaneType GetLaneType()
    {
        return LaneType.Grass;
    }

    public override void SpawnEntities()
    {
        // Debug.Log($"[GrassLane] Spawning entities for lane at Z: {transform.position.z}");

        // --- 수정된 부분: 레인의 실제 스케일.x를 사용하여 폭 계산 ---
        // Plane의 기본 X 스케일 10을 곱하여 실제 월드 폭으로 변환
        // (Plane Mesh의 기본 크기가 1x1이 아니라 10x10 유닛이기 때문)
        float actualLaneXWorldWidth = transform.localScale.x * 10f; 
        // -------------------------------------------------------------
        
        // 스폰 가능한 X 그리드 위치 계산
        List<float> availableXPositions = new List<float>();
        // actualLaneXWorldWidth를 기준으로 X 좌표들을 계산
        for (float x = -(actualLaneXWorldWidth / 2f) + gridSize / 2f; x < (actualLaneXWorldWidth / 2f); x += gridSize)
        {
            availableXPositions.Add(x);
        }

        List<Vector3> occupiedGridPositions = new List<Vector3>(); 
        LayerMask checkLayer = LayerMask.GetMask("Obstacle", "Coin"); 
        if (checkLayer == 0) Debug.LogWarning("[GrassLane] Obstacle or Coin layer not found. CheckLayer may fail.");

        // 1. 나무 스폰 로직 (그리드 랜덤 배치 및 겹침 방지)
        if (treeObstaclePrefab != null)
        {
            List<float> treeSpawnXCandidates = new List<float>(availableXPositions); 
            ShuffleList(treeSpawnXCandidates); 

            int treesSpawned = 0;
            foreach (float spawnX in treeSpawnXCandidates)
            {
                if (treesSpawned >= maxTreesPerLane) break;

                if (Random.value < treeSpawnChance)
                {
                    Vector3 proposedTreePos = new Vector3(spawnX, transform.position.y + spawnOffsetY, transform.position.z);
                    
                    if (occupiedGridPositions.Contains(proposedTreePos)) continue;

                    GameObject tree = Instantiate(treeObstaclePrefab, proposedTreePos, Quaternion.identity);
                    tree.tag = "Obstacle"; 

                    Collider treeCollider = tree.GetComponent<Collider>();
                    if (treeCollider != null) { treeCollider.isTrigger = false; }
                    else { Debug.LogWarning($"[GrassLane] Tree Prefab {treeObstaclePrefab.name} is missing a Collider!"); }
                    
                    occupiedGridPositions.Add(proposedTreePos); 
                    treesSpawned++;
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
            List<float> coinSpawnXCandidates = new List<float>(availableXPositions); 
            ShuffleList(coinSpawnXCandidates); 

            int coinsSpawned = 0;
            foreach (float spawnX in coinSpawnXCandidates)
            {
                if (coinsSpawned >= maxCoinsPerLane) break;

                if (Random.value < coinSpawnChance)
                {
                    Vector3 proposedCoinPos = new Vector3(spawnX, transform.position.y + spawnOffsetY, transform.position.z);

                    if (occupiedGridPositions.Contains(proposedCoinPos)) continue;

                    GameObject coin = Instantiate(coinPrefab, proposedCoinPos, Quaternion.identity); 
                    occupiedGridPositions.Add(proposedCoinPos); 
                    coinsSpawned++;
                }
            }
        }
        else
        {
            Debug.LogWarning("[GrassLane] Coin Prefab is not assigned in the Inspector!");
        }
    }

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