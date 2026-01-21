namespace SunEyeVision.Models
{
    /// <summary>
    /// 算法结果
    /// </summary>
    public class AlgorithmResult
    {
        /// <summary>
        /// 算法名称
        /// </summary>
        public string AlgorithmName { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 结果数据
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 结果图像
        /// </summary>
        public object? ResultImage { get; set; }

        /// <summary>
        /// 执行时间(毫秒)
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// 执行时间(DateTime类型，用于向后兼容)
        /// </summary>
        public System.DateTime ExecutionTime { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static AlgorithmResult CreateSuccess(object? data = null, long executionTimeMs = 0)
        {
            return new AlgorithmResult
            {
                Success = true,
                Data = data,
                ResultImage = data,
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static AlgorithmResult CreateError(string errorMessage, long executionTimeMs = 0)
        {
            return new AlgorithmResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ExecutionTimeMs = executionTimeMs
            };
        }
    }
}

