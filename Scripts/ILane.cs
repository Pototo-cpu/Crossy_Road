using UnityEngine;

public interface ILane
{
    LaneType GetLaneType();
    void SpawnEntities();
    void SetLaneProperties(float height, float width);
    void InitializeLane();
}