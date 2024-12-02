using UpdateManager;

public class EventBehaviour : ManagedMonoBehaviour, ISubscriber
{
    protected override void OnDisable ()
    {
        base.OnDisable();
        // this.UnSubScribeAll();
    }
}