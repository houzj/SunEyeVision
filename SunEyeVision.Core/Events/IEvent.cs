using System;

namespace SunEyeVision.Events
{
    /// <summary>
    /// Event interface - all events must implement this interface
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Event timestamp
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Event source
        /// </summary>
        string Source { get; }
    }

    /// <summary>
    /// Base event class - provides common event functionality
    /// </summary>
    public abstract class EventBase : IEvent
    {
        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Event source
        /// </summary>
        public string Source { get; private set; }

        protected EventBase(string source)
        {
            Timestamp = DateTime.Now;
            Source = source;
        }
    }
}
