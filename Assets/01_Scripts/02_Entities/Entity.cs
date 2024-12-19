using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UpdateManager;
using Utility;

public abstract class Entity : ManagedMonoBehaviour
{
    [SerializeField, Required] protected BaseStats baseStats;
    public EntityStats Stats { get; private set; }

    private void Awake()
    {
        Stats = new EntityStats(new Mediator<EntityStats.StatTypes>(), baseStats);
    }

    private void OnDestroy()
    {
        Stats?.Dispose();
    }

}