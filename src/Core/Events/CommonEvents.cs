namespace SunEyeVision.Core.Events
{
    /// <summary>
    /// Workflow executed event
    /// </summary>
    public class WorkflowExecutedEvent : EventBase
    {
        /// <summary>
        /// Workflow ID
        /// </summary>
        public string WorkflowId { get; set; }

        /// <summary>
        /// Workflow name
        /// </summary>
        public string WorkflowName { get; set; }

        /// <summary>
        /// Execution success
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Execution duration in milliseconds
        /// </summary>
        public long ExecutionDurationMs { get; set; }

        /// <summary>
        /// Number of nodes executed
        /// </summary>
        public int NodesExecuted { get; set; }

        /// <summary>
        /// Error message (if failed)
        /// </summary>
        public string ErrorMessage { get; set; }

        public WorkflowExecutedEvent(string source, string workflowId, string workflowName)
            : base(source)
        {
            WorkflowId = workflowId;
            WorkflowName = workflowName;
        }
    }

    /// <summary>
    /// Workflow node executed event
    /// </summary>
    public class WorkflowNodeExecutedEvent : EventBase
    {
        /// <summary>
        /// Workflow ID
        /// </summary>
        public string WorkflowId { get; set; }

        /// <summary>
        /// Node ID
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Node name
        /// </summary>
        public string NodeName { get; set; }

        /// <summary>
        /// Algorithm type
        /// </summary>
        public string AlgorithmType { get; set; }

        /// <summary>
        /// Execution success
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Execution duration in milliseconds
        /// </summary>
        public long ExecutionDurationMs { get; set; }

        /// <summary>
        /// Error message (if failed)
        /// </summary>
        public string ErrorMessage { get; set; }

        public WorkflowNodeExecutedEvent(string source, string workflowId, string nodeId, string nodeName)
            : base(source)
        {
            WorkflowId = workflowId;
            NodeId = nodeId;
            NodeName = nodeName;
        }
    }

    /// <summary>
    /// Algorithm execution event
    /// </summary>
    public class AlgorithmExecutionEvent : EventBase
    {
        /// <summary>
        /// Algorithm name
        /// </summary>
        public string AlgorithmName { get; set; }

        /// <summary>
        /// Execution success
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Execution duration in milliseconds
        /// </summary>
        public long ExecutionDurationMs { get; set; }

        /// <summary>
        /// Error message (if failed)
        /// </summary>
        public string ErrorMessage { get; set; }

        public AlgorithmExecutionEvent(string source, string algorithmName)
            : base(source)
        {
            AlgorithmName = algorithmName;
        }
    }

    /// <summary>
    /// Plugin loaded event
    /// </summary>
    public class PluginLoadedEvent : EventBase
    {
        /// <summary>
        /// Plugin ID
        /// </summary>
        public string PluginId { get; set; }

        /// <summary>
        /// Plugin name
        /// </summary>
        public string PluginName { get; set; }

        /// <summary>
        /// Plugin version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Load success
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message (if failed)
        /// </summary>
        public string ErrorMessage { get; set; }

        public PluginLoadedEvent(string source, string pluginId, string pluginName, string version)
            : base(source)
        {
            PluginId = pluginId;
            PluginName = pluginName;
            Version = version;
        }
    }

    /// <summary>
    /// Configuration changed event
    /// </summary>
    public class ConfigurationChangedEvent : EventBase
    {
        /// <summary>
        /// Configuration key that changed
        /// </summary>
        public string ConfigurationKey { get; set; }

        /// <summary>
        /// Old value
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// New value
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// Configuration section
        /// </summary>
        public string Section { get; set; }

        public ConfigurationChangedEvent(string source, string section, string key)
            : base(source)
        {
            Section = section;
            ConfigurationKey = key;
        }
    }

    /// <summary>
    /// Error event
    /// </summary>
    public class ErrorEvent : EventBase
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Error type
        /// </summary>
        public string ErrorType { get; set; }

        /// <summary>
        /// Stack trace
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// Severity level
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        public ErrorEvent(string source, string errorMessage, ErrorSeverity severity = ErrorSeverity.Error)
            : base(source)
        {
            ErrorMessage = errorMessage;
            Severity = severity;
            ErrorType = severity.ToString();
        }
    }

    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Warning level
        /// </summary>
        Warning,

        /// <summary>
        /// Error level
        /// </summary>
        Error,

        /// <summary>
        /// Critical level
        /// </summary>
        Critical
    }

    /// <summary>
    /// Log event
    /// </summary>
    public class LogEvent : EventBase
    {
        /// <summary>
        /// Log message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Log level
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// Category
        /// </summary>
        public string Category { get; set; }

        public LogEvent(string source, string message, LogLevel logLevel = LogLevel.Info, string category = null)
            : base(source)
        {
            Message = message;
            LogLevel = logLevel;
            Category = category ?? source;
        }
    }

    /// <summary>
    /// Log levels
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug level
        /// </summary>
        Debug,

        /// <summary>
        /// Info level
        /// </summary>
        Info,

        /// <summary>
        /// Warning level
        /// </summary>
        Warning,

        /// <summary>
        /// Error level
        /// </summary>
        Error
    }
}
