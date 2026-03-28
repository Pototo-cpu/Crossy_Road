using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필요
using TMPro; // UI 텍스트를 위해 TextMeshPro 사용 (설치 필요)

// 게임의 현재 상태를 정의하는 열거형
public enum GameState
{
    WaitingToStart, // 시작 대기 상태
    Playing,        // 게임 플레이 중
    GameOver        // 게임 오버 상태
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } // 싱글톤 인스턴스

    [Header("UI References")]
    public TextMeshProUGUI scoreText;             // 점수 표시 UI 텍스트
    public GameObject pressAnyKeyToStartPanel; // '아무 키나 눌러 시작' 패널
    public GameObject gameOverPanel;           // 게임 오버 시 보여줄 패널
    public TextMeshProUGUI finalScoreText;    // 게임 오버 패널에 최종 점수 표시

    private int score = 0; // 현재 게임 점수
    public int Score // 점수 속성 (점수 설정 시 UI 업데이트)
    {
        get { return score; }
        private set
        {
            score = value;
            if (scoreText != null)
            {
                scoreText.text = "Score: " + score;
            }
        }
    }

    public GameState currentGameState { get; private set; } = GameState.WaitingToStart; // 현재 게임 상태

    public bool isGameOver => currentGameState == GameState.GameOver; // 게임 오버 상태 여부 확인

    // 게임 오브젝트 초기화 시 호출
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

        // 씬 로드 시마다 점수 초기화
        Score = 0;
    }

    // 게임 시작 시 호출
    void Start()
    {
        InitializeGame();
    }

    // 매 프레임 호출
    void Update()
    {
        // '시작 대기' 상태에서 아무 키나 누르면 게임 시작
        if (currentGameState == GameState.WaitingToStart && Input.anyKeyDown)
        {
            StartGame();
        }
    }

    // 게임 초기화 (초기 UI 상태 설정)
    public void InitializeGame()
    {
        currentGameState = GameState.WaitingToStart;

        Score = 0; 
        if (scoreText != null) scoreText.gameObject.SetActive(false); 
        if (pressAnyKeyToStartPanel != null) pressAnyKeyToStartPanel.SetActive(true); 
        if (gameOverPanel != null) gameOverPanel.SetActive(false); 

        // 플레이어 위치 초기화 등 필요한 초기화 로직은 다른 스크립트에서 관리
    }

    // 게임 시작 (UI 상태 변경)
    public void StartGame()
    {
        currentGameState = GameState.Playing;
        if (pressAnyKeyToStartPanel != null) pressAnyKeyToStartPanel.SetActive(false); 
        if (scoreText != null) scoreText.gameObject.SetActive(true); 
        
        // 게임 시작 시 추가 로직 (예: 타이머 시작)
    }

    // 게임 오버 처리 (UI 표시 및 최종 점수 업데이트)
    public void GameOver()
    {
        if (currentGameState == GameState.GameOver) return; // 중복 호출 방지

        currentGameState = GameState.GameOver;
        if (scoreText != null) scoreText.gameObject.SetActive(false); 
        if (gameOverPanel != null) gameOverPanel.SetActive(true); 
        if (finalScoreText != null) finalScoreText.text = "Final Score: " + Score; 

        Debug.Log("Game Over! Final Score: " + Score);
    }

    // 점수 추가 (외부 스크립트에서 호출)
    public void AddScore(int amount)
    {
        if (currentGameState == GameState.Playing)
        {
            Score += amount;
        }
    }

    // 게임 재시작 (UI 버튼에 연결)
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // 현재 씬 다시 로드
    }
}