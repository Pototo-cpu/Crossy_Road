using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform playerTransform; // 카메라가 따라갈 플레이어의 Transform

    [Header("Camera Offset")]
    public Vector3 offset = new Vector3(0f, 10f, -5f); // 플레이어로부터의 카메라 오프셋 (X, Y, Z)

    [Header("Movement Settings")]
    public float smoothSpeed = 0.125f; // 카메라 이동의 부드러움 정도

    private Vector3 targetPosition; // 카메라의 최종 목표 위치

    // 모든 Update 함수가 호출된 후 호출 (플레이어 이동 후 카메라가 움직이도록)
    void LateUpdate() 
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("FollowPlayer: Player Transform이 할당되지 않았습니다. Inspector에서 할당해주세요.");
            return;
        }

        // 플레이어 위치와 오프셋을 기반으로 목표 위치 계산
        targetPosition = new Vector3(
            playerTransform.position.x + offset.x, 
            playerTransform.position.y + offset.y, 
            playerTransform.position.z + offset.z  
        );

        // 현재 카메라 위치에서 목표 위치까지 부드럽게 보간하여 이동
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // (카메라 회전은 에디터에서 수동으로 설정하는 것을 권장)
    }
}