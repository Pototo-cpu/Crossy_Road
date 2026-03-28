// ICollidable.cs
using UnityEngine;

public interface ICollidable
{
    void HandleCollision(Collider other);
}