using NaughtyAttributes;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UpdateManager;

#if UNITY_EDITOR
public class UnityEventUtil : EventBehaviour, IUpdatable
{
    [SerializeField] private UnityEvent unityEvent;

    [SerializeField] private bool triggerUponEvent;
    [SerializeField, ShowIf(nameof(triggerUponEvent))] private Events.EventType eventType;

    [SerializeField] private bool activateEachFrame;

    [Button, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Execute ()
    {
        unityEvent?.Invoke();
    }

    protected override void OnEnable ()
    {
        base.OnEnable();
        if ( !triggerUponEvent )
            return;

        this.Subscribe(eventType, Execute);
    }

    public void OnUpdate ()
    {
        if ( !activateEachFrame )
        {
            this.UnRegister();
            return;
        }
        Execute();
    }
}
#endif