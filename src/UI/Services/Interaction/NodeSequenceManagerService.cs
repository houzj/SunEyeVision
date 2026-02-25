using System;
using System.Collections.Generic;
using System.Threading;
using SunEyeVision.UI.Services.Canvas;
using SunEyeVision.UI.Services.Interaction;
using SunEyeVision.UI.Views.Controls.Canvas;

namespace SunEyeVision.UI.Services.Interaction
{
    /// <summary>
    /// 节点序号管理器实例
    /// </summary>
    public class NodeSequenceManager : INodeSequenceManager
    {
        private readonly Dictionary<string, Dictionary<string, int>> _localSequences = new Dictionary<string, Dictionary<string, int>>();
        private int _globalIndex = 0;
        private readonly object _lockObject = new object();

        public int GetNextGlobalIndex()
        {
            lock (_lockObject)
            {
                return Interlocked.Increment(ref _globalIndex);
            }
        }

        public int GetNextLocalIndex(string workflowId, string algorithmType)
        {
            lock (_lockObject)
            {
                if (!_localSequences.TryGetValue(workflowId, out var workflowSequences))
                {
                    workflowSequences = new Dictionary<string, int>();
                    _localSequences[workflowId] = workflowSequences;
                }

                if (!workflowSequences.TryGetValue(algorithmType, out var localIndex))
                {
                    localIndex = 0;
                    workflowSequences[algorithmType] = localIndex;
                }

                // 递增并返回新的序号
                int newIndex = localIndex + 1;
                workflowSequences[algorithmType] = newIndex;
                return newIndex;
            }
        }

        public void Reset()
        {
            lock (_lockObject)
            {
                _globalIndex = 0;
                _localSequences.Clear();
            }
        }

        public void ResetWorkflow(string workflowId)
        {
            lock (_lockObject)
            {
                _localSequences.Remove(workflowId);
            }
        }
    }
}
