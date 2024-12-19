using System;
using System.Linq;
using UnityEngine.LowLevel;

public static class PlayerLoopHelpers
{
    public static void InsertLoop<T>(ref PlayerLoopSystem[] loopSystems, in PlayerLoopSystem system)
    {
        var index = FindLoopSystemIndex(loopSystems, typeof(T));
        loopSystems[index].subSystemList = InsertSystem<T>(ref loopSystems[index], system, system.type);
    }

    public static void InsertLoop<T>(ref PlayerLoopSystem[] loopSystems, Type playerLoopType, ActionQueue uq)
        where T : struct
    {
        var newSystem = new PlayerLoopSystem()
        {
            type = playerLoopType,
            updateDelegate = uq.Run,
        };

        var index = FindLoopSystemIndex(loopSystems, typeof(T));
        loopSystems[index].subSystemList = InsertSystem<T>(ref loopSystems[index], newSystem, playerLoopType);
    }

    public static PlayerLoopSystem[] RemoveSystem(ref PlayerLoopSystem loop, Type typeToRemove)
    {
        return loop.subSystemList
            .WhereType(typeToRemove, static (x, type) => x.type != type).ToArray();
    }

    public static PlayerLoopSystem[] InsertSystem<T>(ref PlayerLoopSystem loop, in PlayerLoopSystem systemToInsert,
        Type type)
    {
        var src = RemoveSystem(ref loop, type);
        var dest = new PlayerLoopSystem[src.Length + 1];

        Array.Copy(src, 0, dest, 0, src.Length);
        dest[^1] = systemToInsert;
        return dest;
    }

    public static int FindLoopSystemIndex(PlayerLoopSystem[] playerLoopList, Type systemType)
    {
        for (var i = 0; i < playerLoopList.Length; i++)
        {
            if (playerLoopList[i].type == systemType)
            {
                return i;
            }
        }

        throw new Exception("Target PlayerLoopSystem not found. Type:" + systemType.FullName);
    }
}