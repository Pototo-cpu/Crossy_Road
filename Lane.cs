using UnityEngine;

public abstract class Lane : MonoBehaviour, ILane
{
    public float laneWidth = 1f; // 이 값은 MapGenerator에서 10으로 설정될 것임.
    public float laneHeight = 0f;

    public abstract LaneType GetLaneType();
    public abstract void SpawnEntities();

    public virtual void SetLaneProperties(float height, float width)
    {
        laneHeight = height;
        laneWidth = width;
    }

    public virtual void InitializeLane()
    {
        SpawnEntities();
    }
}