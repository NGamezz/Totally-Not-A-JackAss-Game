using KBCore.Refs;
using UnityEngine;
using UpdateManager;
using Utility;

public class Player : Entity, IUpdatable
{
    private void OnValidate() => this.ValidateRefs();

    public void OnUpdate()
    {
        using var tempBuffer = TempBuffer<Collider>.Create(100);
        Physics.OverlapSphereNonAlloc(Vector3.zero, 100.0f, tempBuffer);
    }
}