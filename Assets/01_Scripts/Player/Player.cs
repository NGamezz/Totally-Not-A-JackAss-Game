using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using KBCore.Refs;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UpdateManager;
using Utility;
using Debug = UnityEngine.Debug;

public class IdleState : IState
{
    public void OnEnter()
    {
        Debug.Log("Idle");
    }

    public void OnExit()
    {
    }

    public void OnUpdate()
    {
    }
}

public record State : IState
{
    public void OnEnter()
    {
        throw new NotImplementedException();
    }

    public void OnExit()
    {
        throw new NotImplementedException();
    }

    public void OnUpdate()
    {
        throw new NotImplementedException();
    }
}

public class DrivingState : IState
{
    public void OnEnter()
    {
        Debug.Log("Driving");
    }

    public void OnExit()
    {
    }

    public void OnUpdate()
    {
    }
}

public class Player : Entity, IUpdatable
{
    // private void OnValidate() => this.ValidateRefs();

    private enum PlayerStates
    {
        Idle,
        Driving,
    };

    private StateMachine<PlayerStates> _stateMachine;

    private void Start()
    {
        _stateMachine = new StateMachine<PlayerStates>();

        _stateMachine.AddState(PlayerStates.Idle, new IdleState());
        _stateMachine.AddState(PlayerStates.Driving, new DrivingState());

        _stateMachine.AddTransition(PlayerStates.Driving, PlayerStates.Idle, () => true);
        _stateMachine.AddTransition(PlayerStates.Idle, PlayerStates.Driving, () => false);

        _stateMachine.Initialize(PlayerStates.Idle);
    }

    public void OnUpdate()
    {
        using var tempBuffer = TempBuffer<Collider>.Create(100);
        // Physics.OverlapSphereNonAlloc(Vector3.zero, 100.0f, tempBuffer);
        //
        // using var buffer = UnsafeBuffer<Collider>.Create(100);
        //
        // var a = Enumerable.Range(0, 100).Select(x => new TEstA { a = x }).ToArray();
        // var b = UnsafeUtility.As<TEstA[], TestB[]>(ref a);
        //
        // foreach (var tB in b)
        // {
        // }
        //
        // _stateMachine?.OnUpdate();
    }
}