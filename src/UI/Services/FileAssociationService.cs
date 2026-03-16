using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace SunEyeVision.UI.Services;

/// <summary>
/// 文件关联服务
/// 负责注册解决方案文件的自定义图标和文件类型关联
/// </summary>
public class FileAssociationService
{
    private const string FileExtension = ".solution";
    private const string ProgId = "SunEyeVision.SolutionFile";
    private const string FriendlyName = "SunEyeVision 解决方案文件";
    private const string ContentType = "application/vnd.suneyvision.solution";

    /// <summary>
    /// 注册文件关联（无需管理员权限，仅对当前用户）
    /// </summary>
    public void RegisterFileAssociation()
    {
        try
        {
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(executablePath))
                return;

            var iconPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(executablePath) ?? "",
                "Icons",
                "solution.ico"
            );

            // 检查自定义图标是否存在
            if (!File.Exists(iconPath))
            {
                // 如果自定义图标不存在，使用可执行文件图标作为占位
                iconPath = $"\"{executablePath}\",0";
            }
            else
            {
                iconPath = $"\"{iconPath}\",0";
            }

            // 注册文件扩展名 -> ProgID
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{FileExtension}"))
            {
                key?.SetValue("", ProgId);
                key?.SetValue("Content Type", ContentType);
                key?.SetValue("PerceivedType", "text");
            }

            // 注册 ProgID
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
            {
                key?.SetValue("", FriendlyName);
                key?.SetValue("EditFlags", 65536);
            }

            // 注册图标
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\DefaultIcon"))
            {
                key?.SetValue("", iconPath);
            }

            // 注册打开命令（双击打开）
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command"))
            {
                key?.SetValue("", $"\"{executablePath}\" \"%1\"");
            }

            // 注册编辑命令
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\edit\command"))
            {
                key?.SetValue("", $"\"{executablePath}\" \"%1\"");
            }

            // 注册文件关联
            // LogInfo("正在注册文件关联...");  // 可选：如果需要记录日志
        }
        catch (Exception ex)
        {
            // 静默失败，不影响程序运行
        }
    }

    /// <summary>
    /// 检查是否已注册
    /// </summary>
    public bool IsRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{FileExtension}");
            return key?.GetValue("")?.ToString() == ProgId;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 注销文件关联
    /// </summary>
    public void UnregisterFileAssociation()
    {
        try
        {
            // 删除 ProgID
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgId}", false);

            // 删除文件扩展名关联
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{FileExtension}", false);
        }
        catch (Exception ex)
        {
            // 静默失败
        }
    }
}
