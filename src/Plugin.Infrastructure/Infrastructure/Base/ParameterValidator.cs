using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SunEyeVision.Plugin.Infrastructure.Base;

namespace SunEyeVision.Plugin.Infrastructure.Base
{
    /// <summary>
    /// 参数验证器 - 占位实现
    /// TODO: 根据实际需求完善实现
    /// </summary>
    public class ParameterValidator
    {
        public List<string> Validate(object parameters)
        {
            var errors = new List<string>();
            // TODO: 实现参数验证逻辑
            return errors;
        }

        public bool IsValid(object parameters)
        {
            return Validate(parameters).Count == 0;
        }

        public List<(string Name, bool IsValid, string ErrorMessage)> ValidateItems(Dictionary<string, object> parameters)
        {
            var results = new List<(string, bool, string)>();
            foreach (var kvp in parameters)
            {
                results.Add((kvp.Key, true, ""));
            }
            return results;
        }

        /// <summary>
        /// 验证ParameterItem集合
        /// </summary>
        public List<(string Name, bool IsValid, string ErrorMessage)> ValidateItems(ObservableCollection<ParameterItem> parameterItems)
        {
            var results = new List<(string, bool, string)>();
            foreach (var item in parameterItems)
            {
                results.Add((item.Name, !item.HasError, item.ValidationError ?? ""));
            }
            return results;
        }
    }
}
