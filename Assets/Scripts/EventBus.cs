using System;
using System.Collections.Generic;

public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> events = new();

    public static void Subscribe<T>(Action<T> listener)
    {
        if (events.TryGetValue(typeof(T), out var existing))
            events[typeof(T)] = Delegate.Combine(existing, listener);
        else
            events[typeof(T)] = listener;
    }

    public static void Unsubscribe<T>(Action<T> listener)
    {
        if (!events.TryGetValue(typeof(T), out var existing)) return;

        var current = Delegate.Remove(existing, listener);

        if (current == null)
            events.Remove(typeof(T));
        else
            events[typeof(T)] = current;
    }

    public static void Publish<T>(T eventData)
    {
        if (events.TryGetValue(typeof(T), out var existing))
            ((Action<T>)existing)?.Invoke(eventData);
    }
}
