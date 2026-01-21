using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// Workflow
    /// </summary>
    public class Workflow
    {
        /// <summary>
        /// Workflow ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Workflow name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Workflow description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Node list
        /// </summary>
        public List<WorkflowNode> Nodes { get; set; }

        /// <summary>
        /// Node connections (source node ID -> target node ID list)
        /// </summary>
        public Dictionary<string, List<string>> Connections { get; set; }

        /// <summary>
        /// Logger
        /// </summary>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Set logger (internal use for deserialization)
        /// </summary>
        internal void SetLogger(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Workflow(string id, string name, ILogger logger)
        {
            Id = id;
            Name = name;
            Logger = logger;
            Nodes = new List<WorkflowNode>();
            Connections = new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Add node
        /// </summary>
        public void AddNode(WorkflowNode node)
        {
            Nodes.Add(node);
            Logger.LogInfo($"Workflow {Name} added node: {node.Name}");
        }

        /// <summary>
        /// Remove node
        /// </summary>
        public void RemoveNode(string nodeId)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                Nodes.Remove(node);
                Connections.Remove(nodeId);

                // Remove connections from other nodes to this node
                foreach (var kvp in Connections)
                {
                    kvp.Value.Remove(nodeId);
                }

                Logger.LogInfo($"Workflow {Name} removed node: {node.Name}");
            }
        }

        /// <summary>
        /// Connect nodes
        /// </summary>
        public void ConnectNodes(string sourceNodeId, string targetNodeId)
        {
            if (!Connections.ContainsKey(sourceNodeId))
            {
                Connections[sourceNodeId] = new List<string>();
            }

            if (!Connections[sourceNodeId].Contains(targetNodeId))
            {
                Connections[sourceNodeId].Add(targetNodeId);
                Logger.LogInfo($"Workflow {Name} connected nodes: {sourceNodeId} -> {targetNodeId}");
            }
        }

        /// <summary>
        /// Disconnect nodes
        /// </summary>
        public void DisconnectNodes(string sourceNodeId, string targetNodeId)
        {
            if (Connections.ContainsKey(sourceNodeId))
            {
                Connections[sourceNodeId].Remove(targetNodeId);
                Logger.LogInfo($"Workflow {Name} disconnected: {sourceNodeId} -> {targetNodeId}");
            }
        }

        /// <summary>
        /// Execute workflow
        /// </summary>
        public List<AlgorithmResult> Execute(Mat inputImage)
        {
            var results = new List<AlgorithmResult>();
            var nodeResults = new Dictionary<string, Mat>();
            var executedNodes = new HashSet<string>();

            Logger.LogInfo($"Starting workflow execution: {Name}");

            // Find all nodes without input connections as start nodes
            var startNodes = Nodes.Where(n => !Connections.Values.Any(v => v.Contains(n.Id))).ToList();

            foreach (var startNode in startNodes)
            {
                ExecuteNodeRecursive(startNode, inputImage, nodeResults, executedNodes, results);
            }

            Logger.LogInfo($"Workflow {Name} execution completed, executed {executedNodes.Count} nodes");

            return results;
        }

        private void ExecuteNodeRecursive(WorkflowNode node, Mat inputImage,
            Dictionary<string, Mat> nodeResults, HashSet<string> executedNodes,
            List<AlgorithmResult> results)
        {
            if (executedNodes.Contains(node.Id) || !node.IsEnabled)
            {
                return;
            }

            // Execute subsequent nodes
            var input = inputImage;

            if (Connections.Any(kvp => kvp.Value.Contains(node.Id)))
            {
                // Get input from parent nodes
                var parentIds = Connections.Where(kvp => kvp.Value.Contains(node.Id)).Select(kvp => kvp.Key);
                var parentResults = parentIds.Where(id => nodeResults.ContainsKey(id)).Select(id => nodeResults[id]).ToList();

                if (parentResults.Any())
                {
                    input = parentResults.First();
                }
            }

            try
            {
                Logger.LogInfo($"Executing node: {node.Name} (ID: {node.Id})");

                var algorithm = node.CreateInstance();
                var resultImage = algorithm.Process(input) as Mat;

                nodeResults[node.Id] = resultImage ?? input;

                var result = new AlgorithmResult
                {
                    AlgorithmName = node.AlgorithmType,
                    Success = true,
                    ResultImage = resultImage,
                    ExecutionTime = DateTime.Now
                };

                results.Add(result);
                executedNodes.Add(node.Id);

                // Execute child nodes
                if (Connections.ContainsKey(node.Id))
                {
                    foreach (var childId in Connections[node.Id])
                    {
                        var childNode = Nodes.FirstOrDefault(n => n.Id == childId);
                        if (childNode != null)
                        {
                            ExecuteNodeRecursive(childNode, inputImage, nodeResults, executedNodes, results);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to execute node {node.Name}: {ex.Message}", ex);

                results.Add(new AlgorithmResult
                {
                    AlgorithmName = node.AlgorithmType,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Get workflow information
        /// </summary>
        public string GetInfo()
        {
            var info = $"Workflow: {Name} (ID: {Id})\n";
            info += $"Description: {Description}\n";
            info += $"Node count: {Nodes.Count}\n";
            info += "Node list:\n";

            foreach (var node in Nodes)
            {
                info += $"  - {node.Name} (ID: {node.Id}, {node.AlgorithmType}) - {(node.IsEnabled ? "Enabled" : "Disabled")}\n";
            }

            return info;
        }
    }
}
