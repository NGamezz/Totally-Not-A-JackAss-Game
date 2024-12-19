using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public interface IAsyncState
{
    UniTask OnEnterAsync();
    UniTask OnUpdateAsync();
    UniTask OnExitAsync();
}

public class AsyncStateMachine<T> where T : Enum
{
    private T _state;
    private readonly Dictionary<T, IAsyncState> _states = new();
    private readonly Dictionary<T, Dictionary<T, Func<bool>>> _transitions = new();

    public async UniTask InitializeAsync(T initialState)
    {
        _state = initialState;
        if (_states.TryGetValue(_state, out var state))
        {
            await state.OnEnterAsync();
        }
    }

    public void AddState(T state, IAsyncState stateObject)
    {
        _states.Add(state, stateObject);
    }

    public void AddTransition(T from, T to, Func<bool> condition)
    {
        if (!_transitions.ContainsKey(from))
        {
            _transitions[from] = new Dictionary<T, Func<bool>>();
        }

        _transitions[from].Add(to, condition);
    }

    public async UniTask UpdateAsync()
    {
        if (!_states.TryGetValue(_state, out var state)) return;

        await state.OnUpdateAsync();
        await CheckTransitionsAsync();
    }

    public async UniTask TransitionToAsync(T to)
    {
        if (!_states.TryGetValue(to, out var newState)) return;

        await _states[_state].OnExitAsync();
        _state = to;
        await newState.OnEnterAsync();
    }

    private async UniTask CheckTransitionsAsync()
    {
        if (!_transitions.ContainsKey(_state)) return;

        var state = _transitions[_state].First(x => x.Value());
        await TransitionToAsync(state.Key);
    }
}

public interface IState
{
    public void OnEnter();
    public void OnExit();
    public void OnUpdate();
}

public class StateMachine<T> where T : Enum
{
    private T _state;
    private readonly Dictionary<T, IState> _states = new();
    private readonly Dictionary<T, Dictionary<T, Func<bool>>> _transitions = new();

    public void Initialize(T initialState)
    {
        _state = initialState;
        if (_states.TryGetValue(_state, out var state))
        {
            state?.OnEnter();
        }
    }

    public void AddState(T state, IState stateObject)
    {
        _states.Add(state, stateObject);
    }

    public void AddTransition(T from, T to, Func<bool> condition)
    {
        if (!_transitions.ContainsKey(from))
        {
            _transitions[from] = new Dictionary<T, Func<bool>>();
        }

        _transitions[from].Add(to, condition);
    }

    public void OnUpdate()
    {
        if (!_states.TryGetValue(_state, out var state)) return;

        state?.OnUpdate();
        UpdateTransitions();
    }

    public void TransitionTo(T to)
    {
        if (!_states.TryGetValue(to, out var newState))
            return;

        _states[_state].OnExit();
        _state = to;
        newState?.OnEnter();
    }

    public void UpdateTransitions()
    {
        if (!_transitions.TryGetValue(_state, out var transition))
            return;

        foreach (var state in transition)
        {
            if (!state.Value())
                continue;

            TransitionTo(state.Key);
            return;
        }
    }
}