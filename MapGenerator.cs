using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; }

    public GameObject[] lanePrefabs;
    public int initialLanes = 15;
    public int visibleLanes = 25;
    public float laneHeight = 0f;
    public float laneWidth = 10f;

    private List<ILane> activeLanes = new List<ILane>();
    private float nextSpawnZ = 0f;
    private Transform playerTransform;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        playerTransform = FindObjectOfType<Player>().transform;

        for (int i = 0; i < initialLanes; i++)
        {
            GenerateNewLane(i < 5);
        }
    }

    void Update()
    {
        if (playerTransform != null && playerTransform.position.z + (visibleLanes / 2 * laneWidth) > nextSpawnZ - (laneWidth * 2))
        {
            GenerateNewLane(false);
        }
    }

    public void GenerateNewLane()
    {
        GenerateNewLane(false);
    }

    public void GenerateNewLane(bool forceGrassLane)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        GameObject selectedLanePrefab;
        if (forceGrassLane)
        {
            selectedLanePrefab = null;
            foreach (GameObject prefab in lanePrefabs)
            {
                if (prefab.TryGetComponent<Lane>(out Lane laneScript) && laneScript.GetLaneType() == LaneType.Grass)
                {
                    selectedLanePrefab = prefab;
                    break;
                }
            }

            if (selectedLanePrefab == null)
            {
                Debug.LogWarning("GrassLane_Prefab이 lanePrefabs 배열에 없습니다. 대신 초기 차선에 무작위 차선을 생성합니다.");
                selectedLanePrefab = lanePrefabs[Random.Range(0, lanePrefabs.Length)];
            }
        }
        else
        {
            selectedLanePrefab = lanePrefabs[Random.Range(0, lanePrefabs.Length)];
        }
        
        Vector3 spawnPosition = new Vector3(0, laneHeight, nextSpawnZ);
        // 프리팹의 X 로테이션이 90도로 유지되므로, Instantiate 시 Quaternion.identity를 사용합니다.
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

        if (activeLanes.Count > visibleLanes)
        {
            RemoveOldestLane();
        }
    }

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
        }
    }
}