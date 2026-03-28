using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IMoveable, ICollidable
{
    // 이동 설정
    public float jumpDistance = 10f; // 한 칸 점프 거리
    public float moveDuration = 0.15f; // 이동 애니메이션 지속 시간
    public float jumpHeight = 1.0f; // 점프 높이

    // 레인 경계 설정
    public float laneHalfWidth = 100f; // 레인 폭이 200이면 절반은 100 (Inspector에서 설정)

    // 상태 변수
    private bool isMoving = false; // 이동 중 여부
    private Transform currentParent = null; // 현재 올라탄 오브젝트의 Transform
    private Coroutine moveCoroutine; // 현재 진행 중인 이동 코루틴 참조
    private Rigidbody rb; // Rigidbody 참조

    // 입력 시스템
    private PlayerInputActions playerInputActions; // Input System 액션 맵

    // 게임 오브젝트 초기화 시 호출
    void Awake()
    {
        playerInputActions = new PlayerInputActions(); // Input System 초기화
        playerInputActions.Player.Move.performed += OnMovePerformed; // 이동 액션 바인딩
        rb = GetComponent<Rigidbody>(); // Rigidbody 가져오기
        if (rb == null) Debug.LogError("Player Rigidbody not found!");
    }

    // 스크립트 활성화 시 호출
    void OnEnable() { playerInputActions.Enable(); } // 입력 활성화
    // 스크립트 비활성화 시 호출
    void OnDisable() { playerInputActions.Disable(); } // 입력 비활성화

    // 이동 액션이 수행되었을 때 호출
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        // 게임 오버 상태이거나 게임 플레이 중이 아니면 입력 무시
        if (GameManager.Instance == null || GameManager.Instance.isGameOver || GameManager.Instance.currentGameState != GameState.Playing) return;
        if (isMoving) return; // 이동 중이면 중복 이동 불가

        Vector2 inputVector = context.ReadValue<Vector2>(); // 입력 값 읽기
        Vector3 moveDirection = Vector3.zero; // 이동 방향 초기화

        if (inputVector.y > 0.5f) { moveDirection = Vector3.forward; } // 위
        else if (inputVector.y < -0.5f) { moveDirection = Vector3.back; } // 아래
        else if (inputVector.x < -0.5f) { moveDirection = Vector3.left; } // 왼쪽
        else if (inputVector.x > 0.5f) { moveDirection = Vector3.right; } // 오른쪽

        if (moveDirection != Vector3.zero) { Move(moveDirection); } // 유효한 방향이면 이동
    }

    // 플레이어 이동 시작
    public void Move(Vector3 direction)
    {
        Vector3 targetXZ = transform.position + direction * jumpDistance; // 목표 XZ 위치 계산
        targetXZ.y = transform.position.y; // Y는 현재 Y 유지

        if (moveCoroutine != null) // 이전에 시작된 이동 코루틴이 있다면 중지
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(SmoothJumpMove(targetXZ, direction)); // 점프 이동 코루틴 시작
    }

    // 살짝 점프하는 이동 코루틴
    IEnumerator SmoothJumpMove(Vector3 targetXZ, Vector3 direction)
    {
        isMoving = true; // 이동 중 상태로 설정
        transform.SetParent(null); // 이동 시작 시 부모 해제 (통나무 영향 방지)
        currentParent = null; // 부모 참조 초기화

        float elapsedTime = 0; // 경과 시간
        Vector3 startPosition = rb.position; // Rigidbody의 시작 위치

        if (direction != Vector3.zero) // 플레이어 모델의 앞 방향 회전 보정
        {
            transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0); 
        }
        
        while (elapsedTime < moveDuration)
        {
            if (rb != null) // 이동 중 물리 속도 초기화 (물리 간섭 방지)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            float t = elapsedTime / moveDuration; // 0에서 1까지의 진행률
            float jumpYOffset = jumpHeight * Mathf.Sin(t * Mathf.PI); // 사인 함수로 점프 곡선

            Vector3 currentPosition = Vector3.Lerp(startPosition, targetXZ, t); // XZ 평면 이동
            currentPosition.y = startPosition.y + jumpYOffset; // Y축 점프

            rb.MovePosition(currentPosition); // Rigidbody.MovePosition을 사용하여 이동
            
            elapsedTime += Time.deltaTime;
            yield return null; // 다음 프레임 대기
        }
        // 이동 완료 후 목표 XZ 위치의 원래 Y 레벨로 정확히 스냅
        rb.MovePosition(new Vector3(targetXZ.x, startPosition.y, targetXZ.z));

        isMoving = false; // 이동 완료
        moveCoroutine = null; // 코루틴 참조 해제

        // 이동 완료 후 상태 검사 (물, 레인 경계)
        CheckIfInWaterWithoutLog();
        CheckIfOutOfLaneBounds(); // X, Z축 레인 경계 이탈 검사

        // 앞으로 한 칸 이동 시 점수 및 새 레인 생성
        if (direction == Vector3.forward)
        {
            if (MapGenerator.Instance != null) { MapGenerator.Instance.GenerateNewLane(); } // 새 차선 생성
            if (GameManager.Instance != null) { GameManager.Instance.AddScore(1); } // 점수 추가
        }
        // 뒤로 이동 및 좌우 이동 시 점수 추가 안 함 (게임 오버 로직은 CheckIfOutOfLaneBounds()에서 처리)
    }

    // Trigger 콜라이더와의 상호작용 (Log, River, Coin 등)
    void OnTriggerEnter(Collider other)
    {
        HandleCollision(other); // 공통 충돌 처리 (Car, Coin)

        if (other.CompareTag("Log")) // 통나무에 진입 시
        {
            if (!isMoving) // 이동 중이 아닐 때만 부모 설정 (이동 완료 후 착지 시)
            {
                transform.SetParent(other.transform); // 통나무를 부모로 설정
                currentParent = other.transform; // 부모 참조 저장
            }
        }
    }

    // Trigger 콜라이더 내부에 머무를 때 호출
    void OnTriggerStay(Collider other)
    {
        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return; // 게임 오버 시 처리 안 함

        if (other.CompareTag("River")) // 강물에 머무를 때
        {
            if (!isMoving) { CheckIfInWaterWithoutLog(); } // 이동 중이 아닐 때만 물 확인
        }
        else if (other.CompareTag("Log")) // 통나무 위에 머무를 때
        {
            if (!isMoving) // 플레이어가 이동 중이 아닐 때만 부모를 설정/유지
            {
                if (currentParent == null || currentParent != other.transform) // 부모가 없거나 다른 통나무일 경우
                {
                    transform.SetParent(other.transform); // 해당 통나무를 부모로 설정
                    currentParent = other.transform; // 부모 참조 저장
                }
            }
        }
    }

    // Trigger 콜라이더에서 벗어날 때 호출
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
        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return; // 게임 오버 시 처리 안 함

        if (collision.gameObject.CompareTag("Obstacle")) // 나무와 같은 통과 불가능한 장애물에 부딪혔을 때
        {
            if (moveCoroutine != null) // 이동 중인 코루틴이 있다면 중지하여 이동을 멈춤
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
            isMoving = false; // 이동 상태 해제

            if (rb != null) // Rigidbody 속도 강제 초기화
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            // 현재 위치를 반올림하여 Grid에 스냅
            transform.position = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
        }
        else if (collision.gameObject.CompareTag("Car")) // 자동차와 충돌 시 (게임 오버)
        {
            if (moveCoroutine != null) // 이동 중인 코루틴이 있다면 중지
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
            isMoving = false;
            if (rb != null) // Rigidbody 속도 강제 초기화
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            transform.position = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
            GameManager.Instance.GameOver(); // 게임 오버 처리
        }
    }

    // 플레이어가 물에 빠졌는지 (통나무 위에 있지 않은 채 강물에 있는지) 확인
    private void CheckIfInWaterWithoutLog()
    {
        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return;

        BoxCollider playerBox = GetComponent<BoxCollider>(); // 플레이어의 BoxCollider 가져오기
        Vector3 playerFeetPosition; // 플레이어 발 위치 (OverlapBox의 중심점)
        Vector3 overlapBoxSize; // OverlapBox의 크기

        if (playerBox != null) // BoxCollider가 존재하면 정확한 발 위치 및 크기 계산
        {
            playerFeetPosition = transform.position + playerBox.center - Vector3.up * (playerBox.size.y / 2f - playerBox.size.z / 4f);
            overlapBoxSize = new Vector3(playerBox.size.x, playerBox.size.y / 2f, playerBox.size.z);
        }
        else // BoxCollider가 없으면 기본값 사용 (경고 메시지 출력)
        {
            playerFeetPosition = transform.position + Vector3.down * 0.9f; 
            overlapBoxSize = new Vector3(0.8f, 0.4f, 0.8f);
            Debug.LogWarning("[Player] Player does not have a BoxCollider. Using default feet position logic.");
        }
        
        int logLayer = LayerMask.GetMask("Log"); // 'Log' 레이어 마스크 가져오기
        if (logLayer == 0) { Debug.LogError("[Player] 'Log' Layer not found."); }
        
        int riverLayer = LayerMask.GetMask("River"); // 'River' 레이어 마스크 가져오기
        if (riverLayer == 0) { Debug.LogError("[Player] 'River' Layer not found."); }

        // 발 위치에서 통나무 콜라이더 오버랩 확인
        Collider[] logColliders = Physics.OverlapBox(playerFeetPosition, overlapBoxSize / 2f, Quaternion.identity, logLayer);
        bool currentlyOnLog = false;
        foreach (Collider col in logColliders) { if (col.CompareTag("Log")) { currentlyOnLog = true; break; } }

        // 발 위치에서 강물 콜라이더 오버랩 확인
        Collider[] riverColliders = Physics.OverlapBox(playerFeetPosition, overlapBoxSize / 2f, Quaternion.identity, riverLayer);
        bool currentlyInRiver = false;
        foreach(Collider col in riverColliders) { if(col.CompareTag("River")) { currentlyInRiver = true; break; } }
        
        if (currentlyInRiver && !currentlyOnLog) { GameManager.Instance.GameOver(); } // 물에 빠졌고 통나무 위에 없으면 게임 오버
    }
    
    // X축 및 Z축 레인 경계 이탈을 모두 검사하여 게임 오버 처리
    private void CheckIfOutOfLaneBounds()
    {
        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return;
        if (MapGenerator.Instance == null) {
            Debug.LogWarning("[Player] MapGenerator instance not found for boundary check.");
            return;
        }

        // 1. X축 경계 확인 (좌우 이탈)
        if (Mathf.Abs(transform.position.x) > laneHalfWidth)
        {
            Debug.Log("[Player] Player moved out of X-axis lane bounds! Game Over!");
            GameManager.Instance.GameOver();
            return; // 게임 오버되면 더 이상 검사할 필요 없음
        }

        // 2. Z축 경계 확인 (뒤로 이탈)
        float minAllowedZ = MapGenerator.Instance.GetMinAllowedZ(); // MapGenerator에게 현재 허용되는 최소 Z 위치 요청
        
        if (transform.position.z < minAllowedZ)
        {
            Debug.Log("[Player] Player moved too far backward, out of Z-axis lane bounds! Game Over!");
            GameManager.Instance.GameOver();
            return; // 게임 오버되면 더 이상 검사할 필요 없음
        }
    }

    // 자동차와 코인 충돌 처리
    public void HandleCollision(Collider other)
    {
        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return; // 게임 오버 시 처리 안 함

        if (other.CompareTag("Car")) // 자동차에 부딪히면 게임 오버
        { 
            GameManager.Instance.GameOver(); 
        }
        else if (other.CompareTag("Coin")) // 코인 획득
        {
            GameManager.Instance.AddScore(5); // 코인 획득 시 5점 추가
            Destroy(other.gameObject); // 코인 오브젝트 파괴
        }
    }
}