using UnityEngine;

public class MovingObstacle : MonoBehaviour, IMoveable
{
    private float currentSpeed; // 현재 이동 속도
    private Vector3 moveDirection; // 이동 방향
    public float destroyOffset = 20f; // 플레이어로부터 이만큼 멀어지면 오브젝트 파괴

    // 장애물 초기화 (방향 및 속도 설정)
    public void Initialize(Vector3 direction, float speed)
    {
        moveDirection = direction;
        currentSpeed = speed;

        if (moveDirection != Vector3.zero)
        {
            transform.forward = moveDirection; // 이동 방향으로 오브젝트 회전
        }
    }

    // 매 프레임 호출
    void Update()
    {
        // 현재 속도와 방향으로 오브젝트 이동
        transform.Translate(moveDirection * currentSpeed * Time.deltaTime, Space.World);

        float playerZ = 0; // 플레이어의 Z 위치
        // GameManager와 Player가 존재하면 플레이어의 Z 위치 가져오기
        if (GameManager.Instance != null && GameManager.Instance.GetComponentInChildren<Player>() != null)
        {
            playerZ = GameManager.Instance.GetComponentInChildren<Player>().transform.position.z;
        }
        
        // 플레이어로부터 일정 거리 이상 멀어지면 오브젝트 파괴
        // (visibleLanes * laneWidth / 2f 는 맵의 절반 길이)
        if (Mathf.Abs(transform.position.z - playerZ) > MapGenerator.Instance.visibleLanes * MapGenerator.Instance.laneWidth / 2f + destroyOffset)
        {
            Destroy(gameObject);
        }
    }

    // IMoveable 인터페이스 구현 (이 스크립트에서는 주로 Update에서 자체 이동)
    public void Move(Vector3 direction)
    {
        // 이 스크립트는 자체적으로 이동하므로 필요 시 구현
    }
}