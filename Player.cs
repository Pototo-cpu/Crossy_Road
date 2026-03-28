using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // Input System 사용을 위해 필요

public class Player : MonoBehaviour, IMoveable, ICollidable
{
    public float moveSpeed = 8f; // 플레이어 이동 속도
    public float jumpDistance = 10f; // 한 칸 점프 거리
    public float moveDuration = 0.15f; // 이동 애니메이션 지속 시간

    private bool isMoving = false; // 이동 중 여부
    private Vector3 targetPosition; // 목표 위치
    private Transform currentParent = null; // 현재 올라탄 오브젝트의 Transform
    private Coroutine moveCoroutine; // 현재 진행 중인 이동 코루틴 참조

    private PlayerInputActions playerInputActions; // Input System 액션 맵

    void Awake()
    {
        playerInputActions = new PlayerInputActions(); // Input System 초기화
        playerInputActions.Player.Move.performed += OnMovePerformed; // 이동 액션 바인딩
    }

    void OnEnable() { playerInputActions.Enable(); } // 입력 활성화
    void OnDisable() { playerInputActions.Disable(); } // 입력 비활성화

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return; // 게임 오버 시 이동 불가
        if (isMoving) return; // 이동 중이면 중복 이동 불가

        Vector2 inputVector = context.ReadValue<Vector2>(); // 입력 값 읽기
        Vector3 moveDirection = Vector3.zero; // 이동 방향 초기화

        if (inputVector.y > 0.5f) { moveDirection = Vector3.forward; } // 위
        else if (inputVector.y < -0.5f) { moveDirection = Vector3.back; } // 아래
        else if (inputVector.x < -0.5f) { moveDirection = Vector3.left; } // 왼쪽
        else if (inputVector.x > 0.5f) { moveDirection = Vector3.right; } // 오른쪽

        if (moveDirection != Vector3.zero) { Move(moveDirection); } // 유효한 방향이면 이동
    }

    public void Move(Vector3 direction)
    {
        targetPosition = transform.position + direction * jumpDistance; // 목표 위치 설정
        // 이전에 시작된 이동 코루틴이 있다면 중지
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(SmoothMove(targetPosition, direction)); // 부드러운 이동 코루틴 시작
    }

    IEnumerator SmoothMove(Vector3 target, Vector3 direction)
    {
        isMoving = true; // 이동 중 상태로 설정
        float elapsedTime = 0; // 경과 시간
        Vector3 startPosition = transform.position; // 시작 위치

        if (direction != Vector3.zero) { transform.forward = direction; } // 이동 방향으로 회전
        
        transform.SetParent(null); // 이동 시작 시 부모 해제
        currentParent = null; // 현재 부모 참조 제거

        // 목표 위치를 향해 보간 이동
        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(startPosition, target, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // 다음 프레임 대기
        }
        transform.position = target; // 최종 위치 설정

        isMoving = false; // 이동 완료
        moveCoroutine = null; // 코루틴 참조 해제

        CheckIfInWaterWithoutLog(); // 이동 완료 후 착지 지점 확인

        if (direction == Vector3.forward) // 앞으로 이동 시
        {
            if (MapGenerator.Instance != null) { MapGenerator.Instance.GenerateNewLane(); } // 새 차선 생성
            GameManager.Instance.AddScore(1); // 점수 추가
        }
    }

    // Trigger 콜라이더와의 상호작용 (Log, River, Coin 등)
    void OnTriggerEnter(Collider other)
    {
        HandleCollision(other); // 공통 충돌 처리 (Car, Coin)

        if (other.CompareTag("Log")) // 통나무에 진입 시
        {
            if (!isMoving) // 이동 중이 아닐 때만 부모 설정
            {
                transform.SetParent(other.transform); // 통나무를 부모로 설정
                currentParent = other.transform; // 부모 참조 저장
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return; // 게임 오버 시 처리 안 함

        if (other.CompareTag("River")) // 강물에 머무를 때
        {
            if (!isMoving) { CheckIfInWaterWithoutLog(); } // 이동 중이 아닐 때만 물 확인
        }
        else if (other.CompareTag("Log")) // 통나무 위에 머무를 때
        {
            if (currentParent == null || currentParent != other.transform) // 부모가 없거나 다른 통나무일 경우
            {
                transform.SetParent(other.transform); // 해당 통나무를 부모로 설정
                currentParent = other.transform; // 부모 참조 저장
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Log")) // 통나무에서 벗어날 때
        {
            if (currentParent == other.transform) // 현재 부모가 해당 통나무라면
            {
                transform.SetParent(null); // 부모 해제
                currentParent = null; // 부모 참조 제거
            }
        }
    }

    // Non-Trigger 콜라이더와의 물리적 충돌 (나무 등)
    void OnCollisionEnter(Collision collision)
    {
        // Debug.Log($"[Player] Collided with: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");

        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return; // 게임 오버 시 처리 안 함

        if (collision.gameObject.CompareTag("Obstacle")) // 나무와 같은 통과 불가능한 장애물에 부딪혔을 때
        {
            // Debug.Log($"[Player] Hit an Obstacle: {collision.gameObject.name}!");
            // 이동 중인 코루틴이 있다면 중지하여 이동을 멈춤
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                isMoving = false; // 이동 상태 해제
                moveCoroutine = null; // 코루틴 참조 해제

                // 플레이어를 충돌 직전의 Grid 위치로 되돌림
                // 현재 위치를 반올림하여 Grid에 스냅 (X, Z 좌표를 가장 가까운 정수로)
                transform.position = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
            }
            // 나무에 부딪혔다고 해서 게임 오버는 아님 (이동만 막음)
        }
    }

    // 플레이어가 물에 빠졌는지 (통나무 위에 있지 않은 채 강물에 있는지) 확인
    private void CheckIfInWaterWithoutLog()
    {
        BoxCollider playerBox = GetComponent<BoxCollider>(); // 플레이어의 BoxCollider 가져오기
        Vector3 playerFeetPosition; // 플레이어 발 위치 (OverlapBox의 중심점)
        Vector3 overlapBoxSize; // OverlapBox의 크기

        if (playerBox != null) // BoxCollider가 존재하면 정확한 발 위치 및 크기 계산
        {
            // BoxCollider의 하단 중앙을 발 위치로 사용
            playerFeetPosition = transform.position + playerBox.center - Vector3.up * (playerBox.size.y / 2f - playerBox.size.z / 4f);
            overlapBoxSize = new Vector3(playerBox.size.x, playerBox.size.y / 2f, playerBox.size.z);
        }
        else // BoxCollider가 없으면 기본값 사용 (경고 메시지 출력)
        {
            playerFeetPosition = transform.position + Vector3.down * 0.9f; 
            overlapBoxSize = new Vector3(0.8f, 0.4f, 0.8f);
            Debug.LogWarning("[Player] Player does not have a BoxCollider. Using default feet position logic.");
        }
        
        int logLayer = LayerMask.GetMask("Log");
        if (logLayer == 0) { Debug.LogError("[Player] 'Log' Layer not found."); }
        
        int riverLayer = LayerMask.GetMask("River");
        if (riverLayer == 0) { Debug.LogError("[Player] 'River' Layer not found."); }

        // OverlapBox의 halfExtents는 size / 2f
        Collider[] logColliders = Physics.OverlapBox(playerFeetPosition, overlapBoxSize / 2f, Quaternion.identity, logLayer);
        bool currentlyOnLog = false;
        foreach (Collider col in logColliders) { if (col.CompareTag("Log")) { currentlyOnLog = true; break; } }

        Collider[] riverColliders = Physics.OverlapBox(playerFeetPosition, overlapBoxSize / 2f, Quaternion.identity, riverLayer);
        bool currentlyInRiver = false;
        foreach(Collider col in riverColliders) { if(col.CompareTag("River")) { currentlyInRiver = true; break; } }
        
        if (currentlyInRiver && !currentlyOnLog) { GameManager.Instance.GameOver(); } // 물에 빠졌고 통나무 위에 없으면 게임 오버
    }

    // 자동차와 코인 충돌 처리는 HandleCollision에서 계속 처리
    public void HandleCollision(Collider other)
    {
        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return; // 게임 오버 시 처리 안 함

        if (other.CompareTag("Car")) // 자동차에 부딪히면 게임 오버
        { 
            GameManager.Instance.GameOver(); 
        }
        else if (other.CompareTag("Coin")) // 코인 획득
        {
            GameManager.Instance.AddScore(5); 
            Destroy(other.gameObject); 
        }
    }
}