using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Unity.Mathematics;

public static class EventHandler
{
    // Using ConcurrentDictionary for thread-safety without explicit locks
    private static readonly ConcurrentDictionary<Events.EventType, SubscriberList> _events = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventSubscription Subscribe(Events.EventType type, Action observer)
    {
        var subscribers = _events.GetOrAdd(type, _ => new SubscriberList());
        subscribers.Add(observer);
        return new EventSubscription(type, observer, subscribers);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeEvent(Events.EventType msg)
    {
        if (_events.TryGetValue(msg, out var subscribers))
        {
            subscribers.Invoke();
        }
    }

    public static void Clear()
    {
        _events.Clear();
    }
}

// Custom list optimized for storing and invoking actions
public sealed class SubscriberList
{
    private Action[] _actions = Array.Empty<Action>();
    private volatile int _count;
    private readonly object _lock = new();

    public void Add(Action action)
    {
        lock (_lock)
        {
            if (_count == _actions.Length)
            {
                Array.Resize(ref _actions, Math.Max(4, _actions.Length * 2));
            }
            _actions[_count++] = action;
        }
    }

    public bool Remove(Action action)
    {
        lock (_lock)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_actions[i] == action)
                {
                    _count--;
                    if (i < _count)
                    {
                        Array.Copy(_actions, i + 1, _actions, i, _count - i);
                    }
                    _actions[_count] = null;
                    return true;
                }
            }
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invoke()
    {
        Action[] localActions;
        lock (_lock)
        {
            localActions = _actions;
        }

        var localCount = _count;

        for (int i = 0; i < localCount; i++)
        {
            localActions[i]?.Invoke();
        }
    }
}

public readonly struct EventSubscription : IDisposable
{
    public readonly Events.EventType _eventType;
    private readonly Action _action;
    private readonly SubscriberList _subscribers;

    public EventSubscription(Events.EventType eventType, Action action, SubscriberList subscribers)
    {
        _eventType = eventType;
        _action = action;
        _subscribers = subscribers;
    }

    public void Dispose()
    {
        _subscribers.Remove(_action);
    }
}

public interface ISubscriber { }

public static class EventExtensions
{
    private static readonly ConditionalWeakTable<ISubscriber, SubscriptionCollection> _subscriptions = new();

    public static void Subscribe(this ISubscriber subscriber, Events.EventType type, Action action)
    {
        var subscription = EventHandler.Subscribe(type, action);
        var subscriptions = _subscriptions.GetOrCreateValue(subscriber);
        subscriptions.Add(subscription);
    }

    public static void UnsubscribeAll(this ISubscriber subscriber)
    {
        if (_subscriptions.TryGetValue(subscriber, out var subscriptions))
        {
            subscriptions.DisposeAll();
            _subscriptions.Remove(subscriber);
        }
    }

    public static void Unsubscribe(this ISubscriber subscriber, Events.EventType type)
    {
        if (_subscriptions.TryGetValue(subscriber, out var subscriptions))
        {
            subscriptions.DisposeEvent(type);
        }
    }
}

public sealed class SubscriptionCollection
{
    private readonly Dictionary<Events.EventType, List<EventSubscription>> _subscriptions = new();
    private readonly object _lock = new();

    public void Add(EventSubscription subscription)
    {
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(subscription._eventType, out var list))
            {
                list = new List<EventSubscription>();
                _subscriptions[subscription._eventType] = list;
            }
            list.Add(subscription);
        }
    }

    public void DisposeAll()
    {
        lock (_lock)
        {
            foreach (var subscriptionList in _subscriptions.Values)
            {
                foreach (var subscription in subscriptionList)
                {
                    subscription.Dispose();
                }
            }
            _subscriptions.Clear();
        }
    }

    public void DisposeEvent(Events.EventType eventType)
    {
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(eventType, out var subscriptionList))
            {
                foreach (var subscription in subscriptionList)
                {
                    subscription.Dispose();
                }
                _subscriptions.Remove(eventType);
            }
        }
    }
}