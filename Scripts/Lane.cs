using UnityEngine;

// 레인의 기본적인 동작을 정의하는 추상 클래스
public abstract class Lane : MonoBehaviour, ILane
{
    public float laneWidth = 1f; // 각 레인의 Z축 너비 (MapGenerator에서 설정될 값)
    public float laneHeight = 0f; // 레인의 Y축 높이 (MapGenerator에서 설정될 값)

    // 레인의 종류를 반환하는 추상 메서드 (하위 클래스에서 반드시 구현)
    public abstract LaneType GetLaneType();
    // 레인에 엔티티(장애물, 코인 등)를 생성하는 추상 메서드 (하위 클래스에서 반드시 구현)
    public abstract void SpawnEntities();

    // 레인의 속성(높이, 너비)을 설정하는 가상 메서드
    public virtual void SetLaneProperties(float height, float width)
    {
        laneHeight = height;
        laneWidth = width;
    }

    // 레인 초기화 (엔티티 생성 호출)
    public virtual void InitializeLane()
    {
        SpawnEntities();
    }
}