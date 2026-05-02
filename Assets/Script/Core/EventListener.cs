// ============================================================
//  EventListener.cs
//  Easy Express — Optional Helper Base Class
// ============================================================
//
//  PROBLEM:
//    Every subscriber needs OnEnable/OnDisable boilerplate:
//      void OnEnable()  { GameEventBus.Subscribe<X>(OnX); }
//      void OnDisable() { GameEventBus.Unsubscribe<X>(OnX); }
//    Forgetting OnDisable causes memory leaks and null refs.
//
//  SOLUTION:
//    Inherit from EventListener instead of MonoBehaviour.
//    Override RegisterEvents() and call Listen<T>(handler).
//    Unsubscription is automatic — you can't forget it.
//
//  USAGE:
//    public class PlayerWallet : EventListener
//    {
//        protected override void RegisterEvents()
//        {
//            Listen<JobCompletedEvent>(OnJobCompleted);
//            Listen<GoldAddedEvent>(OnGoldAdded);
//        }
//
//        void OnJobCompleted(JobCompletedEvent evt)
//        {
//            AddGold(evt.totalPay);
//        }
//    }
//
//  NOTE: This is OPTIONAL. You can always use GameEventBus
//  directly if you prefer, or if you can't change a class's
//  base class (e.g., it already inherits from something).
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class EventListener : MonoBehaviour
{
    // Track all subscriptions so we can auto-unsubscribe
    private readonly List<Action> _unsubscribeActions = new List<Action>();

    /// <summary>
    /// Override this to register all your event listeners.
    /// Call Listen&lt;T&gt;(handler) for each event you care about.
    /// </summary>
    protected abstract void RegisterEvents();

    /// <summary>
    /// Subscribe to an event. Unsubscription is automatic on OnDisable.
    /// </summary>
    protected void Listen<T>(Action<T> handler) where T : struct
    {
        GameEventBus.Subscribe(handler);
        _unsubscribeActions.Add(() => GameEventBus.Unsubscribe(handler));
    }

    protected virtual void OnEnable()
    {
        RegisterEvents();
    }

    protected virtual void OnDisable()
    {
        foreach (Action unsub in _unsubscribeActions)
            unsub();
        _unsubscribeActions.Clear();
    }
}