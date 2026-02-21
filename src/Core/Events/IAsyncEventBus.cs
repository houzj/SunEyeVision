using System;
using System.Threading.Tasks;

namespace SunEyeVision.Core.Events
{
    /// <summary>
    /// Async event handler delegate
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="eventData">Event data</param>
    public delegate Task AsyncEventHandler<TEvent>(TEvent eventData) where TEvent : IEvent;

    /// <summary>
    /// Async event bus interface
    /// </summary>
    public interface IAsyncEventBus : IDisposable
    {
        /// <summary>
        /// Subscribe to an async event
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <param name="handler">Event handler</param>
        void Subscribe<TEvent>(AsyncEventHandler<TEvent> handler) where TEvent : IEvent;

        /// <summary>
        /// Unsubscribe from an async event
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <param name="handler">Event handler</param>
        void Unsubscribe<TEvent>(AsyncEventHandler<TEvent> handler) where TEvent : IEvent;

        /// <summary>
        /// Publish an event (fire-and-forget mode)
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <param name="eventData">Event data</param>
        Task PublishAsync<TEvent>(TEvent eventData) where TEvent : IEvent;

        /// <summary>
        /// Publish an event and wait for all handlers to complete
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <param name="eventData">Event data</param>
        Task PublishAndWaitAsync<TEvent>(TEvent eventData) where TEvent : IEvent;

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
