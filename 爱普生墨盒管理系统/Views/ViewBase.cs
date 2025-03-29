using System;
using System.Windows;
using System.Windows.Controls;
using 爱普生墨盒管理系统.Utils;

namespace 爱普生墨盒管理系统.Views
{
    /// <summary>
    /// 所有页面的基类
    /// </summary>
    public class ViewBase : Page
    {
        protected bool IsSQLiteAvailable
        {
            get
            {
                try
                {
                    // 尝试获取墨盒数据以测试SQLite是否可用
                    var cartridges = DatabaseHelper.GetAllCartridges();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 显示受限模式提示
        /// </summary>
        /// <param name="controlToDisable">需要禁用的控件</param>
        protected void ShowLimitedModeWarning(UIElement controlToDisable = null)
        {
            ShowWarning("SQLite库不可用，系统处于受限模式。部分功能不可用。\n请安装SQLite以获得完整功能。");
            
            // 如果提供了控件，则禁用它
            if (controlToDisable != null)
            {
                controlToDisable.IsEnabled = false;
            }
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">错误信息</param>
        /// <param name="exception">异常对象（可选）</param>
        protected void ShowError(string message, Exception exception = null)
        {
            string fullMessage = message;
            if (exception != null)
            {
                fullMessage += $"\n\n错误详情: {exception.Message}";
                if (exception.InnerException != null)
                {
                    fullMessage += $"\n{exception.InnerException.Message}";
                }
            }
            
            MessageBox.Show(fullMessage, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        /// <summary>
        /// 显示警告消息
        /// </summary>
        /// <param name="message">警告信息</param>
        protected void ShowWarning(string message)
        {
            MessageBox.Show(message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        
        /// <summary>
        /// 显示信息消息
        /// </summary>
        /// <param name="message">消息内容</param>
        protected void ShowInfo(string message)
        {
            MessageBox.Show(message, "信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认信息</param>
        /// <returns>是否确认</returns>
        protected bool Confirm(string message)
        {
            return MessageBox.Show(message, "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
} 