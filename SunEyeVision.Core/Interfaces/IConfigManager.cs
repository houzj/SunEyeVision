using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SunEyeVision.Interfaces
{
    /// <summary>
    /// 配置管理器接口
    /// </summary>
    public interface IConfigManager
    {
        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        void SaveConfig(string key, string value);

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        void SaveConfig<T>(string key, T value);

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>配置值</returns>
        string LoadConfig(string key);

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="key">配置键</param>
        /// <returns>配置值</returns>
        T LoadConfig<T>(string key);

        /// <summary>
        /// 加载所有配置
        /// </summary>
        /// <returns>所有配置</returns>
        Dictionary<string, string> LoadAllConfigs();

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        Task SaveToFileAsync(string filePath);

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        /// <param name="filePath">文件路径</param>
        Task LoadFromFileAsync(string filePath);
    }
}
