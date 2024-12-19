using System;
using UnityEngine;

namespace Utility.Timers
{
    //Could potentially make the timers cached, if it proves to be needed.
    public sealed class Timer : ITimer
    {
        public static Timer StartNew(TimeSpan duration)
        {
            return new Timer(duration, true);
        }

        public static Timer StartNew(TimeSpan duration, Action onCompleted)
        {
            var timer = new Timer(duration, false);
            timer.OnCompletion = onCompleted;
            timer.Start();
            return timer;
        }

        public bool ShouldLoop { get; set; } = false;

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
            Start();
        }

        public void Reset()
        {
            IsCompleted = false;
            Time = _initialTime.TotalSeconds;
        }

        public void Start()
        {
            Reset();

            this.RegisterTimer();
            OnStart?.Invoke();
        }

        public void Stop()
        {
            OnCompletion?.Invoke();
            IsCompleted = true;

            if (this.ShouldLoop)
            {
                Restart();
                return;
            }
            
            this.UnregisterTimer();
        }

        public void OnTick(double deltaTime)
        {
            Debug.Assert(!IsCompleted, "Timer was still ticked while being marked as Completed");

            Time -= deltaTime;

            if (Time > 0.0)
                return;

            Stop();
        }
    }
}