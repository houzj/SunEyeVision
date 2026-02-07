using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using SunEyeVision.PluginSystem.Base.Base;

namespace SunEyeVision.PluginSystem.Infrastructure.Base
{
    /// <summary>
    /// 参数仓库 - 占位实现
    /// TODO: 根据实际需求完善实现
    /// </summary>
    public class ParameterRepository
    {
        private Dictionary<string, object> _storage = new Dictionary<string, object>();

        public void Save(string key, object value)
        {
            _storage[key] = value;
        }

        public T Load<T>(string key)
        {
            if (_storage.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default(T);
        }

        public void Clear()
        {
            _storage.Clear();
        }

        public void SaveToFile(string filePath, Dictionary<string, object> parameters)
        {
            var json = JsonSerializer.Serialize(parameters);
            File.WriteAllText(filePath, json);
        }

        public Dictionary<string, object> LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, object>();
            }

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                   ?? new Dictionary<string, object>();
        }

        public List<KeyValuePair<string, object>> LoadItemsFromFile(string filePath, string? snapshotName = null)
        {
            return LoadFromFile(filePath).ToList();
        }

        /// <summary>
        /// 从文件加载参数到ParameterItem集合
        /// </summary>
        public void LoadItemsFromFile(string filePath, ObservableCollection<ParameterItem> parameterItems)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            var json = File.ReadAllText(filePath);
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (parameters != null)
            {
                foreach (var item in parameterItems)
                {
                    if (parameters.TryGetValue(item.Name, out var value))
                    {
                        item.Value = value;
                    }
                }
            }
        }
    }
}
