using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// Workflow Engine
    /// </summary>
    public class WorkflowEngine
    {
        /// <summary>
        /// Workflow list
        /// </summary>
        private Dictionary<string, Workflow> Workflows { get; set; }

        /// <summary>
        /// Current active workflow
        /// </summary>
        public Workflow CurrentWorkflow { get; private set; }

        /// <summary>
        /// Logger
        /// </summary>
        private ILogger Logger { get; set; }

        public WorkflowEngine(ILogger logger)
        {
            Logger = logger;
            Workflows = new Dictionary<string, Workflow>();
        }

        /// <summary>
        /// Create new workflow
        /// </summary>
        public Workflow CreateWorkflow(string id, string name, string description = "")
        {
            if (Workflows.ContainsKey(id))
            {
                throw new ArgumentException($"Workflow ID {id} already exists");
            }

            var workflow = new Workflow(id, name, Logger)
            {
                Description = description
            };

            Workflows[id] = workflow;
            Logger.LogInfo($"Created workflow {name} (ID: {id})");

            return workflow;
        }

        /// <summary>
        /// Delete workflow
        /// </summary>
        public bool DeleteWorkflow(string id)
        {
            if (Workflows.TryGetValue(id, out var workflow))
            {
                Workflows.Remove(id);
                
                if (CurrentWorkflow?.Id == id)
                {
                    CurrentWorkflow = null;
                }

                Logger.LogInfo($"Deleted workflow {workflow.Name} (ID: {id})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set current workflow
        /// </summary>
        public void SetCurrentWorkflow(string id)
        {
            if (!Workflows.ContainsKey(id))
            {
                throw new ArgumentException($"Workflow ID {id} does not exist");
            }

            CurrentWorkflow = Workflows[id];
            Logger.LogInfo($"Set current workflow: {CurrentWorkflow.Name}");
        }

        /// <summary>
        /// Get workflow
        /// </summary>
        public Workflow GetWorkflow(string id)
        {
            Workflows.TryGetValue(id, out var workflow);
            return workflow;
        }

        /// <summary>
        /// Get all workflows
        /// </summary>
        public List<Workflow> GetAllWorkflows()
        {
            return Workflows.Values.ToList();
        }

        /// <summary>
        /// Execute workflow
        /// </summary>
        public List<AlgorithmResult> ExecuteWorkflow(string workflowId, Mat inputImage)
        {
            if (!Workflows.ContainsKey(workflowId))
            {
                Logger.LogError($"Workflow does not exist: {workflowId}");
                return new List<AlgorithmResult>();
            }

            var workflow = Workflows[workflowId];
            Logger.LogInfo($"Executing workflow: {workflow.Name}");

            var results = workflow.Execute(inputImage);

            return results;
        }

        /// <summary>
        /// Execute current workflow
        /// </summary>
        public List<AlgorithmResult> ExecuteCurrentWorkflow(Mat inputImage)
        {
            if (CurrentWorkflow == null)
            {
                Logger.LogWarning("No active workflow");
                return new List<AlgorithmResult>();
            }

            return ExecuteWorkflow(CurrentWorkflow.Id, inputImage);
        }

        /// <summary>
        /// Save workflow to file
        /// </summary>
        public bool SaveWorkflow(string workflowId, string filePath)
        {
            if (!Workflows.ContainsKey(workflowId))
            {
                Logger.LogError($"Workflow does not exist: {workflowId}");
                return false;
            }

            try
            {
                var workflow = Workflows[workflowId];
                var json = JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
                Logger.LogInfo($"Workflow saved to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to save workflow: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Load workflow from file
        /// </summary>
        public bool LoadWorkflow(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Logger.LogError($"File does not exist: {filePath}");
                return false;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var workflow = JsonSerializer.Deserialize<Workflow>(json);
                
                if (workflow != null)
                {
                    // Re-assign logger since it's not serialized
                    var existingWorkflow = new Workflow(workflow.Id, workflow.Name, Logger);
                    existingWorkflow.Description = workflow.Description;
                    Workflows[workflow.Id] = existingWorkflow;
                    
                    Logger.LogInfo($"Workflow loaded from file: {workflow.Name}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load workflow: {ex.Message}", ex);
                return false;
            }
        }
    }
}
