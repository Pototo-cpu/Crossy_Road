using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; } // 싱글톤 인스턴스

    public GameObject[] lanePrefabs; // 사용할 레인 프리팹 배열
    public int initialLanes = 15;    // 게임 시작 시 생성할 초기 레인 수
    public int visibleLanes = 25;    // 화면에 보여질 최대 레인 수
    public float laneHeight = 0f;    // 레인 생성 Y 위치
    public float laneWidth = 10f;    // 각 레인의 Z축 너비

    private List<ILane> activeLanes = new List<ILane>(); // 현재 활성화된 레인 리스트
    private float nextSpawnZ = 0f; // 다음 레인 생성 Z 위치
    private Transform playerTransform; // 플레이어 Transform

    // 게임 오브젝트 초기화 시 호출
    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // 게임 시작 시 호출
    void Start()
    {
        Player player = FindObjectOfType<Player>();
        if (player != null) playerTransform = player.transform;
        else { Debug.LogError("[MapGenerator] Player object not found!"); enabled = false; return; }

        for (int i = 0; i < visibleLanes; i++)
        {
            GenerateNewLane(i < 5); 
        }
    }

    // 매 프레임 호출
    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        if (playerTransform == null) return;

        // 플레이어가 일정 거리 이상 전진하면 새 레인 생성
        if (playerTransform.position.z + (visibleLanes / 2f * laneWidth) > nextSpawnZ - (laneWidth * 2f))
        {
            GenerateNewLane(false);
        }

        // 플레이어가 지나간 오래된 레인 제거
        if (activeLanes.Count > 0)
        {
            MonoBehaviour oldestLaneMono = activeLanes[0] as MonoBehaviour;
            if (oldestLaneMono != null)
            {
                float destructionThresholdZ = oldestLaneMono.transform.position.z + laneWidth * 5; 
                if (playerTransform.position.z > destructionThresholdZ)
                {
                    RemoveOldestLane();
                }
            }
        }
    }

    // 새 레인 생성 (기본)
    public void GenerateNewLane()
    {
        GenerateNewLane(false);
    }

    // 새 레인 생성 (강제 풀밭 레인 옵션)
    public void GenerateNewLane(bool forceGrassLane)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        GameObject selectedLanePrefab = null;

        if (forceGrassLane)
        {
            selectedLanePrefab = lanePrefabs.FirstOrDefault(prefab => 
                prefab.TryGetComponent<Lane>(out Lane laneScript) && laneScript.GetLaneType() == LaneType.Grass);

            if (selectedLanePrefab == null)
            {
                Debug.LogWarning("GrassLane_Prefab이 lanePrefabs 배열에 없습니다.");
                selectedLanePrefab = lanePrefabs[Random.Range(0, lanePrefabs.Length)];
            }
        }
        else
        {
            selectedLanePrefab = lanePrefabs[Random.Range(0, lanePrefabs.Length)];
        }
        
        Vector3 spawnPosition = new Vector3(0, laneHeight, nextSpawnZ);
        GameObject newLaneObj = Instantiate(selectedLanePrefab, spawnPosition, Quaternion.identity, this.transform); 

        ILane newLane = newLaneObj.GetComponent<ILane>();
        if (newLane != null)
        {
            activeLanes.Add(newLane);
            if (newLaneObj.TryGetComponent<Lane>(out Lane laneScript))
            {
                laneScript.SetLaneProperties(laneHeight, laneWidth);
                laneScript.InitializeLane(); 
            }
        }
        else
        {
            Debug.LogWarning($"프리팹 {selectedLanePrefab.name}에는 ILane을 구현하는 컴포넌트가 없습니다.");
        }

        nextSpawnZ += laneWidth;
    }

    // 가장 오래된 레인 제거
    private void RemoveOldestLane()
    {
        if (activeLanes.Count > 0)
        {
            ILane oldestLane = activeLanes[0];
            activeLanes.RemoveAt(0);

            if (oldestLane is MonoBehaviour monoBehaviourLane)
            {
                Destroy(monoBehaviourLane.gameObject);
            }
            else
            {
                Debug.LogWarning("[MapGenerator] Oldest lane is not a MonoBehaviour.");
            }
        }
    }

    // 플레이어가 이동할 수 있는 가장 뒤쪽 레인의 Z 위치 반환
    public float GetMinAllowedZ()
    {
        if (activeLanes.Count > 0)
        {
            MonoBehaviour oldestLaneMono = activeLanes[0] as MonoBehaviour;
            if (oldestLaneMono != null)
            {
                // 플레이어 뒤에 유지할 레인 수를 2칸으로 가정
                return oldestLaneMono.transform.position.z - (laneWidth * 2f); 
            }
        }
        // 안전 장치: activeLanes가 비어있을 경우
        return playerTransform != null ? playerTransform.position.z - (laneWidth * 10f) : -Mathf.Infinity; 
    }
}