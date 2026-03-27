using System.Windows.Input;

namespace SunEyeVision.UI.Commands
{
    /// <summary>
    /// 可撤销命令接口
    /// </summary>
    public interface IUndoableCommand : ICommand
    {
        /// <summary>
        /// 撤销命令执行
        /// </summary>
        void Undo();
    }
}
