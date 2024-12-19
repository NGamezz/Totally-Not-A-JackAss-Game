using KBCore.Refs;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UpdateManager;

[RequireComponent(typeof(Rigidbody))]
public sealed class DriftController : ManagedMonoBehaviour, IFixedUpdatable
{
    [SerializeField, Self] private Rigidbody rb;

    [Header("Driving Parameters")] [SerializeField]
    private float moveSpeed = 50;

    [SerializeField] private SerializableReactiveProperty<float> drag = new(0.4f);
    [SerializeField] private SerializableReactiveProperty<float> maxSpeed = new(15.0f);

    [SerializeField] private float traction = 1;
    [SerializeField] private float steerAngle;

    private Vector2 _moveInput;

    public void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
    private void OnValidate() => this.ValidateRefs(false);

    private void Start()
    {
        rb.maxLinearVelocity = maxSpeed.Value;
        rb.linearDamping = drag.Value;

        maxSpeed.Subscribe(x => rb.maxLinearVelocity = x).AddTo(this);
        drag.Subscribe(x => rb.linearDamping = x).AddTo(this);
    }

    private void HandleMovement()
    {
        var dt = Time.fixedDeltaTime;

        rb.AddForce(_moveInput.y * moveSpeed * transform.forward, ForceMode.Acceleration);

        var magnitude = rb.linearVelocity.magnitude;

        if (_moveInput.x != 0f)
        {
            var deltaRotation = Quaternion.Euler(0f, _moveInput.x * steerAngle * dt * magnitude, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity.normalized, transform.forward, traction * dt) * magnitude;
    }

    public void OnFixedUpdate()
    {
        HandleMovement();
    }
}