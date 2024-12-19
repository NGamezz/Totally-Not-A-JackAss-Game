using UnityEngine;
using UpdateManager;

public class Testing : MonoBehaviour, IUpdatable
{
    private void OnEnable()
    {
        NewUpdateManager.RegisterUpdatable(this);
    }

    private void OnDisable()
    {
        NewUpdateManager.UnRegisterUpdatable(this);
    }

    public void OnUpdate()
    {
        //Using a static Lambda to make sure the delegate gets cached, and doesn't allocate a new one every time.
        // Action test = static () => Test();
        // var t = Enumerable.Range(0, 100).Where(static i => (i % 2) == 0).ToArray();
    }
}