using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 爱普生墨盒管理系统.Utils;
using 爱普生墨盒管理系统.Views;
using 爱普生墨盒管理系统.Interfaces;
using System.Reflection;
using System.Diagnostics;
using MahApps.Metro.Controls;

namespace 爱普生墨盒管理系统
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // 注册数据库初始化完成事件
            DatabaseHelper.DatabaseInitialized += DatabaseHelper_DatabaseInitialized;
            
            SetupMainWindow();
        }

        // 数据库初始化完成事件处理
        private void DatabaseHelper_DatabaseInitialized(object sender, EventArgs e)
        {
            // 分离到一个单独的任务中执行，以避免阻塞其他UI操作
            System.Threading.Tasks.Task.Run(() => 
            {
                // 让UI线程有时间完成其他操作
                System.Threading.Thread.Sleep(200);
                
                // 在UI线程上执行，因为事件可能来自非UI线程
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        Console.WriteLine("数据库初始化完成事件触发，刷新当前页面");
                        
                        // 更新状态文本
                        txtStatus.Text = "数据库初始化完成，正在加载数据...";
                        
                        // 获取当前显示的页面
                        var currentPage = MainFrame.Content;
                        
                        if (currentPage != null)
                        {
                            // 使用IRefreshable接口进行页面刷新
                            if (currentPage is IRefreshable refreshable)
                            {
                                // 使用一个短暂的延迟以确保UI组件都已加载完成
                                Dispatcher.BeginInvoke(new Action(() => 
                                {
                                    try
                                    {
                                        refreshable.Refresh();
                                        Console.WriteLine("页面数据已刷新");
                                        txtStatus.Text = "数据库初始化完成，数据已加载";
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"刷新页面时出错: {ex.Message}");
                                        txtStatus.Text = "数据加载出错，请尝试刷新";
                                    }
                                }), System.Windows.Threading.DispatcherPriority.Background);
                            }
                            else
                            {
                                // 如果当前页面不支持刷新，则导航到仪表盘页面
                                MainFrame.Navigate(new Uri("/Views/DashboardPage.xaml", UriKind.Relative));
                                Console.WriteLine("当前页面不支持刷新，已导航到仪表盘页面");
                            }
                        }
                        else
                        {
                            // 如果当前没有页面，则导航到仪表盘页面
                            MainFrame.Navigate(new Uri("/Views/DashboardPage.xaml", UriKind.Relative));
                            Console.WriteLine("当前没有页面，已导航到仪表盘页面");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"处理数据库初始化事件时出错: {ex.Message}");
                        Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                        txtStatus.Text = "数据库初始化过程中发生错误";
                    }
                });
            });
        }

        private void SetupMainWindow()
        {
            try
            {
                // 显示状态
                txtStatus.Text = "正在初始化系统...";
                
                // 先导航到仪表盘页面
                MainFrame.Navigate(new Uri("/Views/DashboardPage.xaml", UriKind.Relative));
                
                // 设置初始选中的按钮
                SetSelectedNavButton(NavBtnDashboard);
                
                // 确保UI更新
                Dispatcher.Invoke(() => { });
                
                // 初始化数据库
                bool isInitialized = DatabaseHelper.InitializeDatabase();

                // 设置状态文本
                txtStatus.Text = isInitialized ? "系统已初始化" : "数据库初始化失败";
                
                if (isInitialized)
                {
                    // 只进行一次延迟刷新，避免多次重复刷新
                    RefreshDashboardWithDelay(delaySeconds: 1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化主窗口时出错：{ex.Message}\n{ex.StackTrace}", 
                                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 设置选中的导航按钮，并清除其他按钮的选中状态
        /// </summary>
        /// <param name="selectedButton">要设置为选中的按钮</param>
        private void SetSelectedNavButton(Button selectedButton)
        {
            // 获取所有导航按钮
            var allNavButtons = new Button[] 
            {
                NavBtnDashboard,
                NavBtnCartridgeManage,
                NavBtnStockIn,
                NavBtnStockOut,
                NavBtnRecordQuery,
                NavBtnReports,
                NavBtnSettings
            };
            
            // 清除所有按钮的选中状态
            foreach (var button in allNavButtons)
            {
                button.Tag = null;
            }
            
            // 设置当前按钮为选中状态
            selectedButton.Tag = "Selected";
        }

        private void NavBtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainFrame.Navigate(new Uri("/Views/DashboardPage.xaml", UriKind.Relative));
                txtStatus.Text = "系统概览";
                SetSelectedNavButton(NavBtnDashboard);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到仪表盘页面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavBtnStockIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainFrame.Navigate(new Uri("/Views/StockInPage.xaml", UriKind.Relative));
                txtStatus.Text = "墨盒入库";
                SetSelectedNavButton(NavBtnStockIn);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到入库页面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavBtnStockOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainFrame.Navigate(new Uri("/Views/StockOutPage.xaml", UriKind.Relative));
                txtStatus.Text = "墨盒出库";
                SetSelectedNavButton(NavBtnStockOut);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到出库页面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavBtnCartridgeManage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainFrame.Navigate(new Uri("/Views/CartridgeManagePage.xaml", UriKind.Relative));
                txtStatus.Text = "墨盒管理";
                SetSelectedNavButton(NavBtnCartridgeManage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到墨盒管理页面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavBtnRecordQuery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainFrame.Navigate(new Uri("/Views/RecordQueryPage.xaml", UriKind.Relative));
                txtStatus.Text = "记录查询";
                SetSelectedNavButton(NavBtnRecordQuery);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到记录查询页面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavBtnReports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainFrame.Navigate(new Uri("/Views/ReportsPage.xaml", UriKind.Relative));
                txtStatus.Text = "统计报表";
                SetSelectedNavButton(NavBtnReports);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到统计报表页面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavBtnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainFrame.Navigate(new Uri("/Views/SettingsPage.xaml", UriKind.Relative));
                txtStatus.Text = "系统设置";
                SetSelectedNavButton(NavBtnSettings);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到设置页面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 延迟刷新仪表盘数据
        /// </summary>
        private void RefreshDashboardWithDelay(int delaySeconds = 0, bool immediate = false)
        {
            // 如果是立即刷新
            if (immediate)
            {
                Dispatcher.BeginInvoke(new Action(() => 
                {
                    if (MainFrame.Content is DashboardPage dashboardPage)
                    {
                        Console.WriteLine("立即刷新仪表盘数据");
                        dashboardPage.LoadDashboardData();
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
                return;
            }
            
            // 否则使用定时器延迟刷新
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(delaySeconds);
            timer.Tick += (s, args) => 
            {
                try
                {
                    if (MainFrame.Content is DashboardPage dashboardPage)
                    {
                        Console.WriteLine($"延迟{delaySeconds}秒后刷新仪表盘数据");
                        dashboardPage.LoadDashboardData();
                    }
                    
                    // 只执行一次
                    ((System.Windows.Threading.DispatcherTimer)s).Stop();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"延迟刷新仪表盘出错: {ex.Message}");
                }
            };
            timer.Start();
        }
    }
}
