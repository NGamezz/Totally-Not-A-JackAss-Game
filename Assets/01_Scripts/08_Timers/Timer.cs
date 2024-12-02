using System;
using UpdateManager;

public sealed class Timer : IUpdatable
{
    public static Timer StartNew(TimeSpan duration)
    {
        return new Timer(duration, true);
    }

    public bool IsCompleted { get; private set; } = true;
    public double Time { get; private set; } = 0.0;

    public event Action OnCompletion;
    public event Action OnStart;

    public double Progress => Time / _initialTime.TotalSeconds;
    private readonly TimeSpan _initialTime;

    public Timer(TimeSpan time, bool start = false)
    {
        _initialTime = time;

        if (start)
        {
            Start();
        }
    }

    public void Restart()
    {
        Reset();
        Start();
    }

    public void Reset()
    {
        IsCompleted = false;
        Time = _initialTime.TotalSeconds;
    }

    public void Start()
    {
        if (!IsCompleted) return;

        Time = _initialTime.TotalSeconds;
        IsCompleted = false;

        this.Register();
        OnStart?.Invoke();
    }

    public void Stop()
    {
        IsCompleted = true;

        this.UnRegister();
        OnCompletion?.Invoke();
    }

    public void OnUpdate()
    {
        if (IsCompleted)
            return;

        Time -= UnityEngine.Time.deltaTime;

        if (Time > 0.0)
            return;

        Stop();
    }
}