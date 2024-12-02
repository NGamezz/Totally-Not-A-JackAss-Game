using System;
using System.Diagnostics;

public sealed class DisposableTimer : IDisposable
{
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private string _name;

    public void SetName(string n)
    {
        _name = n;
    }

    public void Dispose()
    {
        UnityEngine.Debug.Log($"{_name} Elapsed in : {_sw.Elapsed}");
    }
}