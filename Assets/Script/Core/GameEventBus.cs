// ============================================================
//  GameEventBus.cs
//  Easy Express — System Decoupling Foundation
// ============================================================
//  Location: Assets/Script/Core/GameEventBus.cs
//
//  USAGE:
//    GameEventBus.Subscribe<MyEvent>(handler);     // OnEnable
//    GameEventBus.Unsubscribe<MyEvent>(handler);   // OnDisable
//    GameEventBus.Publish(new MyEvent { ... });    // fire event
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameEventBus
{
    private static readonly Dictionary<Type, Delegate> _handlers
        = new Dictionary<Type, Delegate>();

    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        Type key = typeof(T);
        if (_handlers.TryGetValue(key, out Delegate existing))
            _handlers[key] = Delegate.Combine(existing, handler);
        else
            _handlers[key] = handler;
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        Type key = typeof(T);
        if (_handlers.TryGetValue(key, out Delegate existing))
        {
            Delegate result = Delegate.Remove(existing, handler);
            if (result == null)
                _handlers.Remove(key);
            else
                _handlers[key] = result;
        }
    }

    public static void Publish<T>(T evt) where T : struct
    {
        Type key = typeof(T);
        if (_handlers.TryGetValue(key, out Delegate existing))
        {
            Action<T> action = existing as Action<T>;
            if (action == null) return;

            foreach (Delegate d in action.GetInvocationList())
            {
                try
                {
                    ((Action<T>)d).Invoke(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameEventBus] Handler for {typeof(T).Name} threw: {ex}");
                }
            }
        }
    }

    public static int ActiveEventTypeCount => _handlers.Count;
    public static void ClearAll() => _handlers.Clear();
}