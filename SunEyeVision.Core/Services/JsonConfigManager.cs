using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SunEyeVision.Services
{
    /// <summary>
    /// JSON配置管理器实现
    /// </summary>
    public class JsonConfigManager : SunEyeVision.Interfaces.IConfigManager
    {
        private readonly Dictionary<string, string> _configs;
        private readonly string _configFile;
        private readonly object _lockObject = new object();

        public JsonConfigManager(string configFile = "config.json")
        {
            _configFile = configFile;
            _configs = new Dictionary<string, string>();
        }

        public void SaveConfig(string key, string value)
        {
            lock (_lockObject)
            {
                _configs[key] = value;
            }
        }

        public void SaveConfig<T>(string key, T value)
        {
            var jsonValue = JsonSerializer.Serialize(value);
            SaveConfig(key, jsonValue);
        }

        public string LoadConfig(string key)
        {
            lock (_lockObject)
            {
                if (_configs.TryGetValue(key, out var value))
                {
                    return value;
                }
                return null;
            }
        }

        public T LoadConfig<T>(string key)
        {
            var jsonValue = LoadConfig(key);
            if (string.IsNullOrEmpty(jsonValue))
            {
                return default(T);
            }

            try
            {
                return JsonSerializer.Deserialize<T>(jsonValue);
            }
            catch
            {
                return default(T);
            }
        }

        public Dictionary<string, string> LoadAllConfigs()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, string>(_configs);
            }
        }

        public async Task SaveToFileAsync(string filePath)
        {
            lock (_lockObject)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(_configs, options);
                File.WriteAllText(filePath, json);
            }

            await Task.CompletedTask;
        }

        public async Task LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"配置文件不存在: {filePath}");
            }

            var json = await File.ReadAllTextAsync(filePath);
            
            lock (_lockObject)
            {
                _configs.Clear();
                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (loaded != null)
                {
                    foreach (var kvp in loaded)
                    {
                        _configs[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
    }
}
