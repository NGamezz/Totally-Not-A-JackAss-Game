using System;
using System.Diagnostics;

namespace Utility.Timers
{
    public sealed record DisposableTimer : IDisposable
    {
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private readonly string _name;

        public DisposableTimer(string name)
        {
            _name = name;
        }

        public DisposableTimer()
        {
            _name = string.Empty;
        }

        public void Dispose()
        {
            UnityEngine.Debug.Log($"{_name} Elapsed in : {_sw.Elapsed}");
        }
    }
}