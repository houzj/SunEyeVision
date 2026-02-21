using System.Collections.Generic;


namespace SunEyeVision.Plugin.Infrastructure.Base
{
    /// <summary>
    /// 参数快照 - 占位实现
    /// TODO: 根据实际需求完善实现
    /// </summary>
    public class ParameterSnapshot
    {
        public string Name { get; set; } = "";
        public System.DateTime SnapshotTime { get; set; }
        public System.DateTime Timestamp { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public ParameterSnapshot()
        {
            Timestamp = System.DateTime.Now;
            SnapshotTime = Timestamp;
        }

        public static ParameterSnapshot Create(Dictionary<string, object?> parameters, string name)
        {
            return new ParameterSnapshot
            {
                Name = name,
                Parameters = new Dictionary<string, object>(),
                Timestamp = System.DateTime.Now,
                SnapshotTime = System.DateTime.Now
            };
        }
    }
}
