// FollowPlayer.cs
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform playerTransform; // 카메라가 따라갈 플레이어의 Transform

    [Header("Camera Offset")]
    // 이 오프셋이 핵심입니다.
    // X: 카메라의 X축 위치 (보통 0으로 중앙 유지)
    // Y: 카메라의 Y축 높이 (플레이어보다 훨씬 높게 설정)
    // Z: 카메라가 플레이어 뒤에서 얼마나 떨어져 있을지 (음수 값으로 설정)
    public Vector3 offset = new Vector3(0f, 10f, -5f); // <-- 길건너 친구들 시점의 기본 값

    [Header("Movement Settings")]
    // 카메라가 목표 위치로 얼마나 부드럽게 이동할지 결정합니다.
    // 값이 작을수록 부드러움 (느리게 따라감), 값이 클수록 빠릿하게 따라감 (거의 즉시)
    public float smoothSpeed = 0.125f; // <-- 부드러움 정도 조절

    private Vector3 targetPosition; // 카메라가 목표로 할 최종 위치

    void LateUpdate() // Update 대신 LateUpdate를 사용하여 플레이어 이동 후 카메라가 움직이도록 합니다.
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("FollowPlayer: Player Transform이 할당되지 않았습니다. Inspector에서 할당해주세요.");
            return;
        }

        // 플레이어의 현재 Z 위치에만 반응하도록 목표 위치를 계산합니다.
        // 플레이어의 X, Y 위치는 무시하고, 카메라의 X, Y 오프셋만 적용합니다.
        targetPosition = new Vector3(
            playerTransform.position.x + offset.x, // 플레이어의 X에 오프셋을 더하여 X를 따라가도록 (고정된 X 오프셋)
            playerTransform.position.y + offset.y, // 플레이어의 Y에 오프셋을 더하여 Y를 따라가도록 (고정된 Y 오프셋)
            playerTransform.position.z + offset.z  // 플레이어의 Z에 오프셋을 더하여 Z를 따라가도록 (가장 중요)
        );

        // 현재 카메라 위치에서 목표 위치까지 부드럽게 이동합니다.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // 카메라의 회전은 Update/LateUpdate에서 계속 LookAt을 호출하는 대신,
        // 에디터에서 Transform 컴포넌트의 Rotation 값을 직접 설정하는 것이 좋습니다.
        // 이는 카메라가 항상 특정 각도로 플레이어를 내려다보게 하기 위함입니다.
    }
}