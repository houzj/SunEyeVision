using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SunEyeVision.PluginSystem.Parameters
{
    /// <summary>
    /// 参数存储库，提供参数的保存、加载、导入、导出功能
    /// </summary>
    public class ParameterRepository
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public ParameterRepository()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// 保存参数到文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="parameters">参数字典</param>
        public void SaveToFile(string filePath, Dictionary<string, object?> parameters)
        {
            try
            {
                var data = new ParameterData
                {
                    Version = "1.0",
                    SavedAt = DateTime.Now,
                    Parameters = parameters
                };

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存参数失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从文件加载参数
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>参数字典</returns>
        public Dictionary<string, object?> LoadFromFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<ParameterData>(json, _jsonOptions);

                return data?.Parameters ?? new Dictionary<string, object?>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"加载参数失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存参数项集合到文件
        /// </summary>
        public void SaveItemsToFile(string filePath, IEnumerable<ParameterItem> items)
        {
            try
            {
                var data = new ParameterItemData
                {
                    Version = "1.0",
                    SavedAt = DateTime.Now,
                    Items = new List<ParameterItemData.ItemInfo>()
                };

                foreach (var item in items)
                {
                    data.Items.Add(new ParameterItemData.ItemInfo
                    {
                        Name = item.Name,
                        Value = item.Value,
                        DataType = item.DataType.FullName
                    });
                }

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存参数项失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从文件加载参数并应用到参数项集合
        /// </summary>
        public void LoadItemsFromFile(string filePath, IList<ParameterItem> items)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<ParameterItemData>(json, _jsonOptions);

                if (data?.Items == null)
                    return;

                foreach (var itemInfo in data.Items)
                {
                    var item = items.FirstOrDefault(i => i.Name == itemInfo.Name);
                    if (item != null)
                    {
                        try
                        {
                            var convertedValue = ConvertValue(itemInfo.Value, item.DataType);
                            item.Value = convertedValue;
                        }
                        catch
                        {
                            // 忽略转换错误
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"加载参数项失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 导出参数到JSON字符串
        /// </summary>
        public string ExportToJson(Dictionary<string, object?> parameters)
        {
            try
            {
                var data = new ParameterData
                {
                    Version = "1.0",
                    SavedAt = DateTime.Now,
                    Parameters = parameters
                };

                return JsonSerializer.Serialize(data, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"导出参数失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从JSON字符串导入参数
        /// </summary>
        public Dictionary<string, object?> ImportFromJson(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<ParameterData>(json, _jsonOptions);
                return data?.Parameters ?? new Dictionary<string, object?>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"导入参数失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建参数快照
        /// </summary>
        public ParameterSnapshot CreateSnapshot(Dictionary<string, object?> parameters)
        {
            return new ParameterSnapshot
            {
                SnapshotTime = DateTime.Now,
                Parameters = new Dictionary<string, object?>(parameters)
            };
        }

        /// <summary>
        /// 从快照恢复参数
        /// </summary>
        public void RestoreFromSnapshot(Dictionary<string, object?> target, ParameterSnapshot snapshot)
        {
            if (snapshot?.Parameters == null)
                return;

            target.Clear();
            foreach (var (key, value) in snapshot.Parameters)
            {
                target[key] = value;
            }
        }

        /// <summary>
        /// 值类型转换
        /// </summary>
        private object? ConvertValue(object? value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType.IsInstanceOfType(value))
                return value;

            try
            {
                var stringValue = value.ToString();
                if (string.IsNullOrEmpty(stringValue))
                    return null;

                if (targetType == typeof(string))
                    return stringValue;

                if (targetType == typeof(int))
                    return int.Parse(stringValue);

                if (targetType == typeof(double))
                    return double.Parse(stringValue);

                if (targetType == typeof(float))
                    return float.Parse(stringValue);

                if (targetType == typeof(bool))
                    return bool.Parse(stringValue);

                if (targetType == typeof(decimal))
                    return decimal.Parse(stringValue);

                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 参数数据结构（用于序列化）
        /// </summary>
        private class ParameterData
        {
            public string Version { get; set; } = "1.0";
            public DateTime SavedAt { get; set; }
            public Dictionary<string, object?> Parameters { get; set; } = new();
        }

        /// <summary>
        /// 参数项数据结构（用于序列化）
        /// </summary>
        private class ParameterItemData
        {
            public string Version { get; set; } = "1.0";
            public DateTime SavedAt { get; set; }
            public List<ItemInfo> Items { get; set; } = new();

            public class ItemInfo
            {
                public string Name { get; set; } = "";
                public object? Value { get; set; }
                public string? DataType { get; set; }
            }
        }
    }

    /// <summary>
    /// 参数快照，用于保存参数的某个时间点的状态
    /// </summary>
    public class ParameterSnapshot
    {
        /// <summary>
        /// 快照时间
        /// </summary>
        public DateTime SnapshotTime { get; set; }

        /// <summary>
        /// 参数值字典
        /// </summary>
        public Dictionary<string, object?> Parameters { get; set; } = new();

        /// <summary>
        /// 快照描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 创建快照
        /// </summary>
        public static ParameterSnapshot Create(Dictionary<string, object?> parameters, string? description = null)
        {
            return new ParameterSnapshot
            {
                SnapshotTime = DateTime.Now,
                Parameters = new Dictionary<string, object?>(parameters),
                Description = description
            };
        }

        /// <summary>
        /// 克隆快照
        /// </summary>
        public ParameterSnapshot Clone()
        {
            return new ParameterSnapshot
            {
                SnapshotTime = SnapshotTime,
                Parameters = new Dictionary<string, object?>(Parameters),
                Description = Description
            };
        }
    }
}
