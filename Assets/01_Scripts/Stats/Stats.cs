using System;

public class EntityStats : IDisposable
{
    public enum StatTypes
    {
        Health,
    }

    private readonly Mediator<StatTypes> _mediator;
    private readonly BaseStats _baseStats;

    public float Health 
    {
        get
        {
            //Could be optimised, since it uses Boxing currently.
            var query = new Query<StatTypes>(StatTypes.Health, _baseStats.health);
            _mediator.PerformQuery(query);
            return (float)query.Value;
        }
    }

    public EntityStats(Mediator<StatTypes> mediator, BaseStats baseStats)
    {
        _mediator = mediator;
        _baseStats = baseStats; 
    }

    public void Dispose()
    {
        _mediator?.Dispose();
    }
}