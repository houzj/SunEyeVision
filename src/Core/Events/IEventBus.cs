using System;
using System.Collections.Generic;
using System.Linq;

namespace SunEyeVision.Core.Events
{
    /// <summary>
    /// Event handler delegate
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="eventData">Event data</param>
    public delegate void EventHandler<TEvent>(TEvent eventData) where TEvent : IEvent;

    /// <summary>
    /// Event bus interface
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Subscribe to an event
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <param name="handler">Event handler</param>
        void Subscribe<TEvent>(EventHandler<TEvent> handler) where TEvent : IEvent;

        /// <summary>
        /// Unsubscribe from an event
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <param name="handler">Event handler</param>
        void Unsubscribe<TEvent>(EventHandler<TEvent> handler) where TEvent : IEvent;

        /// <summary>
        /// Publish an event
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <param name="eventData">Event data</param>
        void Publish<TEvent>(TEvent eventData) where TEvent : IEvent;

    /// <summary>
    /// Clear all subscriptions
    /// </summary>
    void Clear();

    /// <summary>
    /// Get statistics about event bus usage
    /// </summary>
    string GetStatistics();
    }
}
