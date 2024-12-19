//Broker Chain Pattern. Needs better handling for other stat types, such as floats or doubles, and needs more efficient Handle Management.

using System;
using System.Collections.Generic;
using Utility.Timers;

public sealed class Mediator<TStatType> : IDisposable
{
    private readonly IList<Modifier<TStatType>> _modifiers = new List<Modifier<TStatType>>();
    public delegate void RefAction<T>(ref T value);

    public event RefAction<Query<TStatType>> Queries;
    
    public void PerformQuery(Query<TStatType> query) => Queries?.Invoke(ref query);

    public void AddModifier(Modifier<TStatType> modifier)
    {
        
        _modifiers.Add(modifier);
        Queries += modifier.Handle;

        modifier.OnDispose += m =>
        {
            _modifiers.Remove(m);
            Queries -= m.Handle;
        };
    }

    public void Dispose()
    {
        foreach (var t in _modifiers)
        {
            t.Dispose();
        }

        Queries = null;
    }
}

public class StatModifier<TType> : Modifier<TType> where TType : Enum
{
    public readonly TType Type;

    public delegate void Func(ref ValueType value);

    public readonly Func Operator;

    public StatModifier(float duration, Func op, TType type) : base(duration)
    {
        Type = type;
        Operator = op;
    }

    public override void Handle(ref Query<TType> query)
    {
        if (!query.Type.Equals(Type)) return;

        Operator(ref query.Value);
    }
}

public abstract class Modifier<TStatType> : IDisposable
{
    private readonly Timer _timer;

    public event Action<Modifier<TStatType>> OnDispose;

    public Modifier(float duration)
    {
        if (duration <= 0)
            return;

        _timer = new Timer(TimeSpan.FromSeconds(duration), true);
        _timer.OnCompletion += Dispose;
    }

    public abstract void Handle(ref Query<TStatType> query);

    public void Dispose()
    {
        if (!_timer.IsCompleted)
            _timer.Stop();

        if (OnDispose == null) return;

        OnDispose(this);
        OnDispose = null;
    }
}

public struct Query<TStatType>
{
    public readonly TStatType Type;
    public ValueType Value;

    public Query(TStatType type, ValueType value)
    {
        Type = type;
        Value = value;
    }
}