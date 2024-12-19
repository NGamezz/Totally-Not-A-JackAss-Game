using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;
using Utility.Collections;
using PlayerLoopType = UnityEngine.PlayerLoop;

namespace Utility.Timers
{
    public interface ITimer
    {
        void OnTick(double deltaTime);
    }

    public static class TimerManager
    {
        private static readonly HashList<ITimer> ActiveTimers = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterTimerManager()
        {
            //Make sure the timers are cleared.
            ActiveTimers.Clear();
            
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            var copy = playerLoop.subSystemList.ToArray();

            //Create the update System.
            var updateSystem = new PlayerLoopSystem()
            {
                type = typeof(TimerManager),
                updateDelegate = UpdateTimers,
            };

            //Inject the TimerManager Update.
            PlayerLoopHelpers.InsertLoop<PlayerLoopType.Update>(ref copy, updateSystem);

            playerLoop.subSystemList = copy;
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        public static void RegisterTimer(this ITimer timer)
        {
            if (ActiveTimers.Contains(timer))
            {
                return;
            }

            ActiveTimers.Add(timer);
        }

        public static void UnregisterTimer(this ITimer timer)
        {
            if (!ActiveTimers.Contains(timer))
            {
                return;
            }

            ActiveTimers.Remove(timer);
        }

        private static void UpdateTimers()
        {
            var enumerator = ActiveTimers.GetEnumerator();

            try
            {
                while (enumerator.MoveNext())
                {
                    enumerator.Current?.OnTick(Time.deltaTime);
                }
            }
            finally
            {
                enumerator.Dispose();
            }
        }
    }
}