using System;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 宸ヤ綔娴佽妭鐐圭被鍨?
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        /// 绠楁硶鑺傜偣
        /// </summary>
        Algorithm,
        /// <summary>
        /// 杈撳叆鑺傜偣
        /// </summary>
        Input,
        /// <summary>
        /// 杈撳嚭鑺傜偣
        /// </summary>
        Output,
        /// <summary>
        /// 鏉′欢鑺傜偣
        /// </summary>
        Condition
    }

    /// <summary>
    /// 宸ヤ綔娴佽妭鐐?
    /// </summary>
    public class WorkflowNode
    {
        /// <summary>
        /// 鑺傜偣ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 鑺傜偣鍚嶇О
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 鑺傜偣绫诲瀷
        /// </summary>
        public NodeType Type { get; set; }

        /// <summary>
        /// 绠楁硶绫诲瀷鍚嶇О
        /// </summary>
        public string AlgorithmType { get; set; }

        /// <summary>
        /// 鑺傜偣鍙傛暟
        /// </summary>
        public AlgorithmParameters Parameters { get; set; }

        /// <summary>
        /// 鏄惁鍚敤
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 鎵ц鍓嶄簨浠?
        /// </summary>
        public event Action<WorkflowNode> BeforeExecute;

        /// <summary>
        /// 鎵ц鍚庝簨浠?
        /// </summary>
        public event Action<WorkflowNode, AlgorithmResult> AfterExecute;

        public WorkflowNode(string id, string name, NodeType type)
        {
            Id = id;
            Name = name;
            Type = type;
            Parameters = new AlgorithmParameters();
        }

        /// <summary>
        /// 瑙﹀彂鎵ц鍓嶄簨浠?
        /// </summary>
        protected virtual void OnBeforeExecute()
        {
            BeforeExecute?.Invoke(this);
        }

        /// <summary>
        /// 瑙﹀彂鎵ц鍚庝簨浠?
        /// </summary>
        protected virtual void OnAfterExecute(AlgorithmResult result)
        {
            AfterExecute?.Invoke(this, result);
        }

        /// <summary>
        /// 鍒涘缓绠楁硶瀹炰緥锛堢殑鍒濇澶勭悊鏂规硶锛?
        /// </summary>
        public virtual IImageProcessor CreateInstance()
        {
            // 婧虹珛鍙锋拴瀹炵幇锛岀敓瀹熷簨鏀硅起鏉ュ唴鐨勫姞杞借瘎娉?
            throw new NotImplementedException($"Algorithm type '{AlgorithmType}' is not implemented.");
        }
    }
}
