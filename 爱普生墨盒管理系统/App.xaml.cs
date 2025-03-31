using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using 爱普生墨盒管理系统.Utils;

namespace 爱普生墨盒管理系统
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // 添加全局异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            this.Exit += App_Exit;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                // 捕获所有可能的异常，确保应用程序能够启动
                try
                {
                    // 尝试加载SQLite库
                    LoadSQLiteLibrary();
                }
                catch (Exception ex)
                {
                    // 记录错误但继续启动
                    System.Diagnostics.Debug.WriteLine($"加载SQLite库时出错: {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show($"加载SQLite库时出错: {ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                
                // 初始化多语言支持
                try
                {
                    string cultureName = System.Globalization.CultureInfo.CurrentCulture.Name;
                    宽幅面打印机墨盒管理系统.Utils.LocalizationHelperAlias.SetLanguage(cultureName);
                }
                catch (Exception ex)
                {
                    // 记录错误但继续启动
                    System.Diagnostics.Debug.WriteLine($"初始化多语言支持时出错: {ex.Message}\n{ex.StackTrace}");
                }
                
                // 数据库初始化放在主窗口中进行，避免启动过程中的阻塞
            }
            catch (Exception ex)
            {
                // 使用简单的错误消息，避免引用可能导致问题的本地化资源
                MessageBox.Show(
                    $"应用程序启动时发生严重错误:\n{ex.Message}\n\n要查看详细信息吗?",
                    "启动错误", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Error);
                    
                // 记录详细错误信息到日志
                System.Diagnostics.Debug.WriteLine($"应用程序启动错误: {ex.Message}\n{ex.StackTrace}");
                
                // 严重错误时退出应用程序
                Shutdown();
            }
        }
        
        private void LoadSQLiteLibrary()
        {
            try
            {
                // 尝试从不同位置加载SQLite.Interop.dll
                List<string> paths = new List<string>()
                {
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "x86"),
                    IntPtr.Size > 4 ? 
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "x64") : null,
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"packages\Stub.System.Data.SQLite.Core.NetFramework.1.0.119.0\lib\net46")
                };
                
                foreach(string path in paths.Where(p => !string.IsNullOrEmpty(p)))
                {
                    string sqliteDll = System.IO.Path.Combine(path, "SQLite.Interop.dll");
                    if (File.Exists(sqliteDll))
                    {
                        // 尝试加载库，只记录成功但不实际加载
                        System.Diagnostics.Debug.WriteLine($"找到SQLite库: {sqliteDll}");
                        return; // 成功找到
                    }
                }
                
                // 如果没有找到库文件，只记录信息
                System.Diagnostics.Debug.WriteLine("未找到SQLite.Interop.dll，将使用默认加载机制");
            }
            catch (Exception ex)
            {
                // 记录错误但不中断启动
                System.Diagnostics.Debug.WriteLine($"检查SQLite库时出错: {ex.Message}");
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                string errorMessage;
                if (e.ExceptionObject is Exception ex)
                {
                    errorMessage = $"发生未处理的异常: {ex.Message}\n\n{ex.StackTrace}";
                }
                else
                {
                    errorMessage = "发生未知的未处理异常";
                }

                System.Diagnostics.Debug.WriteLine(errorMessage);
                MessageBox.Show(errorMessage, "应用程序错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception msgEx)
            {
                // 如果在显示错误信息时又出错，记录并直接退出
                System.Diagnostics.Debug.WriteLine($"显示错误信息时发生二次异常: {msgEx.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {msgEx.StackTrace}");
                // 这种情况下无法显示UI，只能记录日志
            }
            finally
            {
                if (e.IsTerminating)
                {
                    // 如果异常导致应用程序终止，执行清理操作
                    try
                    {
                        // 可以在这里添加一些清理代码，例如保存用户数据
                        System.Diagnostics.Debug.WriteLine("执行应用终止前的清理工作");
                    }
                    catch (Exception cleanupEx)
                    {
                        // 记录清理过程中的错误但继续退出流程
                        System.Diagnostics.Debug.WriteLine($"清理过程中发生错误: {cleanupEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {cleanupEx.StackTrace}");
                    }
                }
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                // 处理UI线程上未捕获的异常
                string errorMessage = $"发生未处理的UI异常: {e.Exception.Message}\n\n{e.Exception.StackTrace}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                MessageBox.Show(errorMessage, "UI错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // 标记异常为已处理，防止应用崩溃
                e.Handled = true;
            }
            catch (Exception msgEx)
            {
                // 如果在显示错误信息时又出错，记录并让异常继续传播
                System.Diagnostics.Debug.WriteLine($"处理UI异常时发生二次异常: {msgEx.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {msgEx.StackTrace}");
                e.Handled = false;
            }
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                // 应用程序退出时的清理工作
                System.Diagnostics.Debug.WriteLine("应用程序正常退出");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用程序退出时发生错误: {ex.Message}");
            }
        }
    }
}
