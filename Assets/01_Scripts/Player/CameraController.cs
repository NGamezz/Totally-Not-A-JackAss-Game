using Unity.Burst;
using UnityEngine;
using UpdateManager;

[BurstCompile]
public class CameraController : ManagedMonoBehaviour, IFixedUpdatable
{
    [SerializeField] private Transform target;

    [SerializeField] private float distanceToTarget;
    [SerializeField] private float positionSmoothing = 1.0f;
    [SerializeField] private float rotationSmoothing = 1.0f;

    private Transform _cachedTransform;

    public void OnFixedUpdate()
    {
        var dt = Time.fixedDeltaTime;
        
        _cachedTransform.position = Vector3.Lerp(_cachedTransform.position, target.position, positionSmoothing * dt);
        _cachedTransform.forward = Vector3.Lerp(_cachedTransform.forward, target.forward, rotationSmoothing * dt);
    }

    private void Start()
    {
        if (!target)
        {
            this.enabled = false;
            this.UnRegister();
            return;
        }

        _cachedTransform = transform;
    }
}