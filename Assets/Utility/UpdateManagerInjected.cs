using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UpdateManager;
using IFixedUpdatable = UpdateManager.IFixedUpdatable;
using ILateUpdatable = UpdateManager.ILateUpdatable;
using IUpdatable = UpdateManager.IUpdatable;
using PlayerLoopType = UnityEngine.PlayerLoop;

public enum UpdateType
{
    Update = 0,
    LateUpdate = 1,
    FixedUpdate = 2,
}

public static class NewUpdateManager
{
    public static class UpdateManagerLoopRunners
    {
        public struct UpdateLoop
        {
        };

        public struct FixedUpdateLoop
        {
        };

        public struct LateUpdateLoop
        {
        };
    }

    //HashList is a custom collection that allows for fast addition and removal.
    private static readonly Utility.Collections.HashList<IUpdatable> Updatables = new Utility.Collections.HashList<IUpdatable>();
    private static readonly Utility.Collections.HashList<IFixedUpdatable> FixedUpdatables = new Utility.Collections.HashList<IFixedUpdatable>();
    private static readonly Utility.Collections.HashList<ILateUpdatable> LateUpdatables = new Utility.Collections.HashList<ILateUpdatable>();

    public static void Clear()
    {
        Updatables.Clear();
    }

    public static void RegisterUpdatable(in IManagedObject obj)
    {
        if (obj is IUpdatable updatable)
        {
            Updatables.Add(updatable);
        }

        if (obj is IFixedUpdatable fixedUpdatable)
        {
            FixedUpdatables.Add(fixedUpdatable);
        }

        if (obj is ILateUpdatable lateUpdatable)
        {
            LateUpdatables.Add(lateUpdatable);
        }
    }

    public static void UnRegisterUpdatable(in IManagedObject obj)
    {
        if (obj is IUpdatable updatable)
        {
            Updatables.Remove(updatable);
        }

        if (obj is IFixedUpdatable fixedUpdatable)
        {
            FixedUpdatables.Remove(fixedUpdatable);
        }

        if (obj is ILateUpdatable lateUpdatable)
        {
            LateUpdatables.Remove(lateUpdatable);
        }
    }

    public static void UpdateLoop()
    {
        foreach (var updatable in Updatables)
        {
            updatable.OnUpdate();
        }
    }

    public static void FixedUpdateLoop()
    {
        foreach (var updatable in FixedUpdatables)
        {
            updatable.OnFixedUpdate();
        }
    }

    public static void LateUpdateLoop()
    {
        foreach (var updatable in LateUpdatables)
        {
            updatable.OnLateUpdate();
        }
    }
}

public static class UpdateManagerInjector
{
    private static PlayerLoopSystem[] _updateLoopSystems;
    private static ActionQueue[] _updateRunners;

    private static class UpdateLoopRunners
    {
        public struct UpdateManagerUpdateLoop
        {
        };

        public struct UpdateManagerLateUpdateLoop
        {
        };

        public struct UpdateManagerFixedUpdateLoop
        {
        };
    }

    public static void Enqueue(Action action, UpdateType type = UpdateType.Update)
    {
        _updateRunners[(int)type].Enqueue(action);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void Initialize()
    {
        var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

        var copy = (PlayerLoopSystem[])playerLoop.subSystemList.Clone();

        var updateTypeCount = Enum.GetValues(typeof(UpdateType)).Length;
        _updateRunners = new ActionQueue[updateTypeCount];

        //Inject the loops.
        PlayerLoopHelpers.InsertLoop<PlayerLoopType.Update>(ref copy, typeof(UpdateLoopRunners.UpdateManagerUpdateLoop),
            _updateRunners[(int)UpdateType.Update] = new ActionQueue());

        PlayerLoopHelpers.InsertLoop<PlayerLoopType.FixedUpdate>(ref copy,
            typeof(UpdateLoopRunners.UpdateManagerFixedUpdateLoop),
            _updateRunners[(int)UpdateType.FixedUpdate] = new ActionQueue());

        PlayerLoopHelpers.InsertLoop<PlayerLoopType.PostLateUpdate>(ref copy,
            typeof(UpdateLoopRunners.UpdateManagerLateUpdateLoop),
            _updateRunners[(int)UpdateType.LateUpdate] = new ActionQueue());

        RegisterUpdateManager(ref copy);

        playerLoop.subSystemList = copy;
        PlayerLoop.SetPlayerLoop(playerLoop);
    }

    private static void RegisterUpdateManager(ref PlayerLoopSystem[] playerLoopSystems)
    {
        var updateSystem = new PlayerLoopSystem()
        {
            type = typeof(NewUpdateManager.UpdateManagerLoopRunners.UpdateLoop),
            updateDelegate = NewUpdateManager.UpdateLoop,
        };

        PlayerLoopHelpers.InsertLoop<PlayerLoopType.Update>(ref playerLoopSystems, updateSystem);

        var fixedUpdateSystem = new PlayerLoopSystem()
        {
            type = typeof(NewUpdateManager.UpdateManagerLoopRunners.FixedUpdateLoop),
            updateDelegate = NewUpdateManager.FixedUpdateLoop,
        };

        PlayerLoopHelpers.InsertLoop<PlayerLoopType.FixedUpdate>(ref playerLoopSystems, fixedUpdateSystem);

        var lateUpdateSystem = new PlayerLoopSystem()
        {
            type = typeof(NewUpdateManager.UpdateManagerLoopRunners.LateUpdateLoop),
            updateDelegate = NewUpdateManager.LateUpdateLoop,
        };

        PlayerLoopHelpers.InsertLoop<PlayerLoopType.PostLateUpdate>(ref playerLoopSystems, lateUpdateSystem);
    }

    private static void OnPlayModeChange(PlayModeStateChange obj)
    {
        if (obj != PlayModeStateChange.ExitingEditMode) return;

        var loop = PlayerLoop.GetCurrentPlayerLoop();
        for (var i = 0; i < _updateRunners.Length; ++i)
        {
            // PlayerLoopHelpers.RemoveSystem(ref loop, _updateRunners[i]);
        }

        PlayerLoop.SetPlayerLoop(loop);

        NewUpdateManager.Clear();
    }
}