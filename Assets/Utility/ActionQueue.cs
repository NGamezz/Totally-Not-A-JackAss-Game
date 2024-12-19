using System;
using System.Threading;

public sealed class ActionQueue
{
    private SpinLock _spinLock = new(false);

    private bool _dequing;

    //One less than the max integer number.
    private const int MaxArraySize = 0X7FEFFFFF;
    private const int InitialSize = 16;

    private int _actionCount;
    private Action[] _actions = new Action[InitialSize];

    private int _lateActionCount;
    private Action[] _lateActions = new Action[InitialSize];

    public void Enqueue(Action action)
    {
        var lockTaken = false;

        //Regular lock is faster for Queueing. So might want to change to that.
        try
        {
            _spinLock.Enter(ref lockTaken);

            if (_dequing)
            {
                if (_lateActionCount == _lateActions.Length)
                {
                    var newLength = _lateActionCount * 2;

                    if ((uint)newLength > MaxArraySize) newLength = MaxArraySize;

                    var newActions = new Action[newLength];
                    Array.Copy(_lateActions, newActions, _lateActions.Length);

                    _lateActions = newActions;
                }

                _lateActions[_lateActionCount++] = action;
            }
            else
            {
                if (_actionCount == _actions.Length)
                {
                    var newLength = _actionCount * 2;

                    if ((uint)newLength > MaxArraySize) newLength = MaxArraySize;

                    var newActions = new Action[newLength];
                    Array.Copy(_actions, newActions, _actions.Length);
                    _actions = newActions;
                }

                _actions[_actionCount++] = action;
            }
        }
        finally
        {
            //Make sure we release the lock.
            if (lockTaken) _spinLock.Exit(false);
        }
    }

//Delegate Entry.
    public void Run()
    {
        RunCore();
    }

    public int Clear()
    {
        var lockTaken = false;

        try
        {
            _spinLock.Enter(ref lockTaken);

            var rest = _actionCount + _lateActionCount;

            _actionCount = 0;
            _actions = new Action[InitialSize];

            _lateActionCount = 0;
            _lateActions = new Action[InitialSize];

            return rest;
        }
        finally
        {
            if (lockTaken) _spinLock.Exit(false);
        }
    }

    [System.Diagnostics.DebuggerHidden]
    private void RunCore()
    {
        {
            var lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);

                if (_actionCount == 0) return;
                _dequing = true;
            }
            finally
            {
                if (lockTaken) _spinLock.Exit(false);
            }
        }

        for (var i = 0; i < _actionCount; i++)
        {
            var action = _actions[i];
            _actions[i] = null;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        {
            var lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);
                _dequing = false;

                var swapTempActionList = _actions;

                _actionCount = _lateActionCount;
                _actions = _lateActions;

                _lateActionCount = 0;
                _lateActions = swapTempActionList;
            }
            finally
            {
                if (lockTaken) _spinLock.Exit(false);
            }
        }
    }
}