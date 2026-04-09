using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SunEyeVision.Core.Services.CameraDiscovery
{
    /// <summary>
    /// 相机发现服务接口
    /// </summary>
    public interface ICameraDiscoveryService
    {
        /// <summary>
        /// 相机类型
        /// </summary>
        CameraType CameraType { get; }
        
        /// <summary>
        /// 异步发现相机
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>发现的相机列表</returns>
        Task<List<DiscoveredCamera>> DiscoverAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// 停止发现
        /// </summary>
        void StopDiscovery();
    }
}
