using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using 爱普生墨盒管理系统.Models;
using 爱普生墨盒管理系统.Utils;
using 爱普生墨盒管理系统.Interfaces;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using LvcSeparator = LiveCharts.Wpf.Separator;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using 爱普生墨盒管理系统.Views;

namespace 爱普生墨盒管理系统.Views
{
    /// <summary>
    /// DashboardPage.xaml 的交互逻辑
    /// </summary>
    public partial class DashboardPage : Page, IRefreshable
    {
        // 图表数据类
        public class ChartViewModel
        {
            public SeriesCollection Series { get; set; }
            public ChartValues<double> ChartValues { get; set; }
            public List<string> Labels { get; set; }  // 关键属性
            public Func<double, string> Formatter { get; set; }
            public Brush ColumnFill { get; set; }
            
            // 用于X轴标签颜色格式化
            public Func<double, string> LabelFormatter { get; set; }
            
            // 标签颜色映射
            public Dictionary<string, Brush> LabelColors { get; set; }
            
            public ChartViewModel()
            {
                Series = new SeriesCollection();
                ChartValues = new ChartValues<double>();
                Labels = new List<string>();  // 初始化
                LabelColors = new Dictionary<string, Brush>();
                Formatter = value => value.ToString("N0");
                ColumnFill = Brushes.DodgerBlue;
                
                // 默认标签格式化器 - 确保即使标签为空也显示颜色名称
                LabelFormatter = index => 
                {
                    if (index < 0 || index >= Labels.Count)
                        return string.Empty;
                    
                    string label = Labels[(int)index];
                    // 确保标签显示
                    return string.IsNullOrEmpty(label) ? $"颜色 {index+1}" : label;
                };
            }

            public void ClearData()
            {
                Series.Clear();
                ChartValues.Clear();
                Labels.Clear();
                LabelColors.Clear();
            }
        }


        // 图表数据视图模型
        private readonly ChartViewModel _chartViewModel;
        private readonly object _loadLock = new object();
        private bool _isLoading = false;
        private DateTime _lastLoadTime = DateTime.MinValue;
        private const int MIN_LOAD_INTERVAL_MS = 500; // 最小加载间隔(毫秒)

        // 防止频繁刷新的标志和时间戳
        private bool _isRefreshing = false;
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private readonly TimeSpan _minRefreshInterval = TimeSpan.FromSeconds(1);

        public DashboardPage()
        {
            InitializeComponent();
            
            // 初始化图表数据视图模型
            _chartViewModel = new ChartViewModel();
            
            // 设置图表数据上下文
            chartColorStats.DataContext = _chartViewModel;

            // 注册页面加载完成事件
            this.Loaded += DashboardPage_Loaded;
            
            // 设置初始状态
            Console.WriteLine("DashboardPage 构造函数执行");
            SetInitialState();
        }

        /// <summary>
        /// 设置页面初始状态
        /// </summary>
        private void SetInitialState()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // 设置初始文本
                    txtTotalCartridges.Text = "0";
                    txtLowStockCartridges.Text = "0";
                    txtTotalStock.Text = "0";
                    txtCartridgeCount.Text = "0"; // 初始化墨盒型号数量为0
                    
                    // 显示加载提示
                    tbNoDataHint.Visibility = Visibility.Visible;
                    txtLowStockHint.Visibility = Visibility.Visible;
                    txtRecentOperationsHint.Visibility = Visibility.Visible;
                    
                    // 隐藏图表
                    chartColorStats.Visibility = Visibility.Collapsed;
                    
                    // 清空列表
                    lvLowStock.ItemsSource = null;
                    lvRecentOperations.ItemsSource = null;
                    
                    Console.WriteLine("已设置仪表盘初始状态");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置仪表盘初始状态出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 页面加载完成时执行的操作
        /// </summary>
        private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("仪表盘页面加载完成");
                
                // 设置初始状态
                SetInitialState();
                
                // 使用较短延迟加载数据，避免与数据库初始化冲突
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(200)
                };
                timer.Tick += (s, args) => 
                {
                    try
                    {
                        LoadDashboardData();
                        ((System.Windows.Threading.DispatcherTimer)s).Stop();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"延迟加载仪表盘数据出错: {ex.Message}");
                    }
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"仪表盘页面加载完成事件处理出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 检查数据库是否已初始化并确保颜色表有数据
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051:未使用的私有成员", Justification = "保留供将来使用")]
        private bool CheckDatabaseInitialized()
        {
            try
            {
                // 获取颜色数据
                var colors = DatabaseHelper.GetAllCartridgeColors();
                
                // 如果没有颜色数据，尝试直接查询
                if (colors == null || colors.Count == 0)
                {
                    int colorCount = GetDirectColorCount();
                    if (colorCount == 0)
                    {
                        Console.WriteLine("检测到数据库已初始化但颜色表为空，尝试强制初始化颜色表");
                        ForceInitializeColorTable();
                        // 即使颜色表为空，也认为数据库已初始化
                        return true;
                    }
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 检查SQLite是否可用的属性
        [SuppressMessage("CodeQuality", "IDE0051:未使用的私有成员", Justification = "保留供将来使用")]
        private bool IsSQLiteAvailable
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

        // 显示错误消息
        private void ShowError(string message, Exception exception = null)
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
        
        // 显示警告消息
        private void ShowWarning(string message)
        {
            MessageBox.Show(message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        
        /// <summary>
        /// 显示信息消息（当前未使用）
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051:未使用的私有成员", Justification = "保留供将来使用")]
        private void ShowInfo(string message)
        {
            MessageBox.Show(message, "信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        /// <summary>
        /// 显示确认对话框（当前未使用）
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051:未使用的私有成员", Justification = "保留供将来使用")]
        private bool Confirm(string message)
        {
            return MessageBox.Show(message, "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
        
        // 显示受限模式警告
        private void ShowLimitedModeWarning(UIElement controlToDisable = null)
        {
            ShowWarning("SQLite库不可用，系统处于受限模式。部分功能不可用。\n请安装SQLite以获得完整功能。");
            
            // 如果提供了控件，则禁用它
            if (controlToDisable != null)
            {
                controlToDisable.IsEnabled = false;
            }
        }

        /// <summary>
        /// 同步墨盒颜色数据，确保所有使用的颜色在颜色表中存在
        /// </summary>
        private void SyncCartridgeColors()
        {
            try
            {
                Console.WriteLine("开始同步墨盒颜色数据...");
                
                // 首先检查颜色表是否为空，如果为空则尝试强制初始化
                int colorCount = GetDirectColorCount();
                if (colorCount == 0)
                {
                    Console.WriteLine("颜色表为空，尝试从SQL文件初始化...");
                    ForceInitializeColorTable();
                    
                    // 再次检查颜色表
                    colorCount = GetDirectColorCount();
                    Console.WriteLine($"初始化后颜色表中的颜色数量：{colorCount}");
                }
                else
                {
                    Console.WriteLine($"颜色表已存在数据，颜色数量：{colorCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"同步墨盒颜色数据出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 强制从SQLiteDB.sql文件初始化颜色表
        /// </summary>
        private void ForceInitializeColorTable()
        {
            try
            {
                Console.WriteLine("开始强制初始化颜色表...");
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CartridgeDB.db");
                string sqlFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQLiteDB.sql");
                string connectionString = $"Data Source={dbPath};Version=3;";
                
                // 检查SQL文件是否存在
                if (!System.IO.File.Exists(sqlFilePath))
                {
                    Console.WriteLine("初始化SQL文件不存在: " + sqlFilePath);
                    return;
                }
                
                // 读取SQL文件内容
                string sqlContent = System.IO.File.ReadAllText(sqlFilePath);
                
                // 提取插入CartridgeColors表的SQL语句
                string pattern = @"INSERT\s+INTO\s+CartridgeColors\s*\([^)]*\)\s*VALUES\s*\([^)]*\)\s*;";
                var matches = System.Text.RegularExpressions.Regex.Matches(sqlContent, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (matches.Count == 0)
                {
                    Console.WriteLine("在SQL文件中未找到CartridgeColors的INSERT语句");
                    return;
                }
                
                // 连接数据库并执行INSERT语句
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (SQLiteTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (System.Text.RegularExpressions.Match match in matches)
                            {
                                string insertSql = match.Value;
                                using (SQLiteCommand command = new SQLiteCommand(insertSql, connection))
                                {
                                    command.ExecuteNonQuery();
                                }
                            }
                            
                            transaction.Commit();
                            Console.WriteLine($"成功执行了{matches.Count}条颜色数据插入语句");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"执行颜色数据插入语句出错: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"强制初始化颜色表出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 加载仪表盘数据，现在改为公共方法以便可以从外部调用
        /// </summary>
        public void LoadDashboardData()
        {
            // 检查是否可以加载
            if (_isLoading || (DateTime.Now - _lastLoadTime).TotalMilliseconds < MIN_LOAD_INTERVAL_MS)
            {
                Console.WriteLine("跳过重复加载");
                return;
            }

            // 尝试获取锁
            if (!Monitor.TryEnter(_loadLock))
            {
                Console.WriteLine("另一个加载操作正在进行中");
                return;
            }

            try
            {
                _isLoading = true;
                _lastLoadTime = DateTime.Now;
                
                Console.WriteLine("开始加载仪表盘数据...");
                
                // 强制同步墨盒颜色数据
                SyncCartridgeColors();
                
                // 获取所有颜色数据
                List<DatabaseHelper.CartridgeColor> allColorData = DatabaseHelper.GetAllCartridgeColors();
                int colorCount = allColorData?.Count ?? 0;
                
                Console.WriteLine($"已获取颜色数据，颜色数量：{colorCount}");
                
                // 如果没有颜色数据，尝试从数据库直接查询颜色表
                if (colorCount == 0)
                {
                    colorCount = GetDirectColorCount();
                    Console.WriteLine($"从数据库直接查询到颜色数量：{colorCount}");
                }
                
                // 获取所有墨盒数据
                List<Cartridge> allCartridges = DatabaseHelper.GetAllCartridges();
                Console.WriteLine($"已获取墨盒数据，墨盒数量：{allCartridges?.Count ?? 0}");
                
                // 确保在UI线程更新
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // 如果没有颜色数据，显示空状态并返回
                        if (colorCount == 0)
                        {
                            Console.WriteLine("颜色数据为空，显示空状态");
                            ShowEmptyState();
                            return;
                        }

                        // 如果没有墨盒数据，显示空状态但保留颜色数量
                        if (allCartridges == null || allCartridges.Count == 0)
                        {
                            Console.WriteLine($"墨盒数据为空，但有{colorCount}种颜色数据，显示空状态");
                            ShowEmptyState();
                            return;
                        }
                        
                        // 计算库存信息
                        int totalStock = allCartridges.Sum(c => c.CurrentStock);
                        
                        // 获取低库存墨盒(库存小于或等于最低库存的)
                        var lowStockCartridges = allCartridges.Where(c => c.CurrentStock <= c.MinimumStock).ToList();
                        
                        // 获取最近操作记录
                        var recentOperations = GetRecentOperations(5);
                        
                        // 更新UI显示
                        txtTotalCartridges.Text = $"{colorCount}";
                        txtLowStockCartridges.Text = lowStockCartridges.Count.ToString();
                        txtTotalStock.Text = totalStock.ToString();
                        
                        // 填充低库存列表
                        lvLowStock.ItemsSource = lowStockCartridges;
                        
                        // 填充最近操作列表
                        var recentOperationViewModels = recentOperations.Select(record => new
                        {
                            record.Id,
                            CartridgeInfo = $"{record.Cartridge.Color} {record.Cartridge.Model}",
                            OperationType = record.OperationType == 1 ? "入库" : "出库",
                            record.Quantity,
                            record.OperationTime,
                            record.Operator
                        }).ToList();
                        
                        lvRecentOperations.ItemsSource = recentOperationViewModels;
                        
                        // 根据是否有操作记录显示或隐藏提示
                        txtRecentOperationsHint.Visibility = recentOperations.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                        
                        // 更新图表 - 始终显示图表，即使没有墨盒数据
                        LoadColorStatisticsChart();
                        
                        // 根据数据显示或隐藏空状态提示
                        tbNoDataHint.Visibility = allCartridges.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                        txtLowStockHint.Visibility = lowStockCartridges.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                        
                        // 图表始终显示，即使没有墨盒数据
                        chartColorStats.Visibility = Visibility.Visible;
                        
                        // 确保Y轴刻度适合空状态显示
                        AdjustYAxisScale(0);
                        
                        // 在UI渲染后应用标签颜色
                        Dispatcher.InvokeAsync(() => ApplyAxisLabelColors(), 
                            System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                        
                        Console.WriteLine("仪表盘数据加载完成");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"更新UI时出错: {ex.Message}\n{ex.StackTrace}");
                        ShowEmptyState();
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载仪表盘数据时出错: {ex.Message}\n{ex.StackTrace}");
                Dispatcher.Invoke(() => ShowEmptyState());
            }
            finally
            {
                _isLoading = false;
                Monitor.Exit(_loadLock);
            }
        }

        /// <summary>
        /// 获取最近的操作记录
        /// </summary>
        private List<StockRecord> GetRecentOperations(int count)
        {
            try
            {
                return DatabaseHelper.GetStockRecords(count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取最近操作记录出错: {ex.Message}");
                return new List<StockRecord>();
            }
        }

        /// <summary>
        /// 直接从数据库获取颜色表中的颜色数量
        /// </summary>
        private int GetDirectColorCount()
        {
            try
            {
                Console.WriteLine("正在直接从数据库获取颜色数量");
                string connectionString = $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CartridgeDB.db")};Version=3;";
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string sql = "SELECT COUNT(*) FROM CartridgeColors";
                    using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                    {
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        Console.WriteLine($"数据库中的颜色数量: {count}");
                        return count;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取颜色数量出错: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 显示空状态
        /// </summary>
        private void ShowEmptyState()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // 更新UI元素
                    int colorCount = GetDirectColorCount();
                    if (colorCount > 0)
                    {
                        txtTotalCartridges.Text = $"{colorCount}";
                    }
                    else
                    {
                        txtTotalCartridges.Text = "0";
                    }
                    
                    txtLowStockCartridges.Text = "0";
                    txtTotalStock.Text = "0";
                    txtCartridgeCount.Text = "0"; // 更新墨盒型号数量为0
                    
                    // 显示无数据状态
                    tbNoDataHint.Visibility = Visibility.Visible;
                    txtLowStockHint.Visibility = Visibility.Visible;
                    txtRecentOperationsHint.Visibility = Visibility.Visible;
                    
                    // 清空低库存列表
                    lvLowStock.ItemsSource = null;
                    lvRecentOperations.ItemsSource = null;
                    
                    // 即使在空状态下也加载图表
                    LoadColorStatisticsChart();
                    
                    // 确保图表可见
                    chartColorStats.Visibility = Visibility.Visible;
                    
                    // 确保Y轴刻度适合空状态显示
                    AdjustYAxisScale(0);
                    
                    Console.WriteLine("已显示空状态，同时保留颜色图表显示");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ShowEmptyState 出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 根据库存最大值调整Y轴刻度 - 确保图表始终正确显示
        /// </summary>
        /// <param name="maxStockValue">库存最大值</param>
        private void AdjustYAxisScale(double maxStockValue)
        {
            try
            {
                if (chartColorStats.AxisY == null || chartColorStats.AxisY.Count == 0) return;
                var yAxis = chartColorStats.AxisY[0];

                // 缩小Y轴最大值，使小值数据能够更明显地显示
                yAxis.MinValue = 0;

                // 获取墨盒管理中最大的墨盒数量
                int maxCartridgeCount = GetMaxCartridgeCount();

                // 根据最大值调整Y轴范围，确保小值也能显示
                if (maxStockValue <= 3)
                {
                    // 最大值低于或等于3时，固定Y轴最大值为4以增强小值显示
                    yAxis.MaxValue = 4;
                }
                else
                {
                    // 其他情况保持原来的6
                    //yAxis.MaxValue = 6;
                    yAxis.MaxValue = Math.Max(maxCartridgeCount + 2, (int)maxStockValue + 2);
                }
                
                // 设置固定步长为1，使刻度线和数值清晰可见
                yAxis.Separator = new LvcSeparator
                {
                    Step = 1,
                    IsEnabled = true,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"))
                };

                Console.WriteLine($"已设置Y轴: 最小值=0, 最大值={yAxis.MaxValue}, 步长=1");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调整Y轴刻度出错: {ex.Message}");
            }
        }

        // 获取墨盒管理中最大的墨盒数量方法
        private int GetMaxCartridgeCount()
        {
            try
            {
                // 获取所有墨盒数据
                var allCartridges = DatabaseHelper.GetAllCartridges();
                if (allCartridges == null || allCartridges.Count == 0)
                    return 0;

                // 按颜色分组并找出每种颜色的最大库存量
                var colorGroups = allCartridges
                    .GroupBy(c => c.Color)
                    .Select(g => new {
                        Color = g.Key,
                        TotalStock = g.Sum(c => c.CurrentStock)
                    });

                // 找出最大库存量
                int maxStock = colorGroups.Any() ? (int)colorGroups.Max(g => g.TotalStock) : 0;

                return maxStock;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取最大墨盒数量出错: {ex.Message}");
                return 10; // 出错时返回默认值10
            }
        }
        /// <summary>
        /// 使用新方法加载颜色统计图表，解决单墨盒不显示和比例不准确问题
        /// </summary>
        private void LoadColorStatisticsChart()
        {
            try
            {
                Console.WriteLine("开始加载颜色统计图表...");
                
                // 首先获取所有墨盒数据
                var allCartridges = DatabaseHelper.GetAllCartridges() ?? new List<Cartridge>();
                int cartridgeModelCount = allCartridges.Select(c => c.Model).Distinct().Count();
                
                // 更新墨盒型号数量显示
                Dispatcher.Invoke(() => txtCartridgeCount.Text = cartridgeModelCount.ToString());
                
                // 获取所有颜色数据，按显示顺序排序
                var allColors = DatabaseHelper.GetAllCartridgeColors()
                    .OrderBy(c => c.DisplayOrder)
                    .ToList();
                
                if (allColors.Count == 0)
                {
                    Console.WriteLine("没有颜色数据，无法显示图表");
                    Dispatcher.Invoke(() => {
                    txtNoChartDataHint.Visibility = Visibility.Visible;
                    chartColorStats.Visibility = Visibility.Collapsed;
                    });
                    return;
                }
                
                // 确保图表可见，隐藏无数据提示
                Dispatcher.Invoke(() => {
                    chartColorStats.Visibility = Visibility.Visible;
                    txtNoChartDataHint.Visibility = Visibility.Collapsed;
                });
                
                // 创建颜色库存统计字典
                Dictionary<string, int> colorStockCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                
                // 直接从墨盒数据计算每种颜色的库存总量
                var colorGroups = allCartridges
                    .GroupBy(c => c.Color)
                    .ToDictionary(
                        g => g.Key,
                        g => new {
                            TotalStock = g.Sum(c => c.CurrentStock),
                            Models = g.Select(c => c.Model).Distinct().ToList()
                        }
                    );
                
                // 为所有颜色准备数据
                foreach (var color in allColors)
                {
                    if (colorGroups.ContainsKey(color.Name))
                    {
                        colorStockCounts[color.Name] = colorGroups[color.Name].TotalStock;
                        }
                        else
                        {
                        colorStockCounts[color.Name] = 0;
                    }
                }
                
                // 创建图表数据结构
                var seriesCollection = new SeriesCollection();
                var labels = new List<string>();
                
                // 记录最大库存值，用于调整Y轴
                double maxStock = 0;

                // 计算每个柱子的最大宽度和间距，基于颜色数量动态调整
                int colorCount = allColors.Count;

                // 根据颜色数量动态计算柱宽度
                double maxColumnWidth = Math.Max(50, Math.Min(120, 600 / Math.Max(1, colorCount))); // 在20-120之间动态调整

                // 根据颜色数量动态计算间距
                double defaultPadding = 5; // 默认间距
                double maxPadding = 30;     // 最大间距
                double columnPadding = Math.Max(defaultPadding, Math.Min(maxPadding, 200 / Math.Max(1, colorCount))); // 在5-30之间动态调整
                
                Console.WriteLine($"动态计算柱宽: {maxColumnWidth}, 间距: {columnPadding}, 基于 {colorCount} 种颜色");
                
                // 为每种颜色创建数据系列
                foreach (var color in allColors)
                {
                    // 获取该颜色的库存数量
                    int stockCount = 0;
                    if (colorStockCounts.ContainsKey(color.Name))
                    {
                        stockCount = colorStockCounts[color.Name];
                    }
                    
                    // 更新最大库存值
                    if (stockCount > maxStock) maxStock = stockCount;
                    
                    // 准备更丰富的标签文本
                    string labelText = color.Name;
                    
                    // 尝试获取此颜色墨盒的型号信息
                    if (colorGroups.ContainsKey(color.Name) && colorGroups[color.Name].Models.Count > 0)
                    {
                        var models = colorGroups[color.Name].Models;
                        // 如果型号太多，只显示前两个并加省略号
                        if (models.Count > 2)
                        {
                            labelText = $"{color.Name}";
                        }
                    else
                    {
                            labelText = $"{color.Name}";
                        }
                    }
                    
                    // 解析颜色代码获取画刷
                    SolidColorBrush brush = Brushes.Gray; // 默认灰色
                    try
                    {
                        if (!string.IsNullOrEmpty(color.ColorCode))
                        {
                            string colorCode = color.ColorCode;
                            if (!colorCode.StartsWith("#")) colorCode = "#" + colorCode;
                            brush = (SolidColorBrush)new BrushConverter().ConvertFrom(colorCode);
                        }
                    }
                    catch { /* 使用默认灰色 */ }
                  
                    // 创建数据列
                    var columnSeries = new ColumnSeries
                    {
                        Title = color.Name,
                        Values = new ChartValues<double> { stockCount }, // 这里只有一个值
                        Fill = brush,
                        DataLabels = true, // 显示数据标签
                        FontSize = 16,     // 数据标签字体大小
                        FontWeight = FontWeights.SemiBold, // 加粗数据标签
                        MaxColumnWidth = maxColumnWidth, // 使用动态计算的宽度
                        MinWidth = Math.Min(20, maxColumnWidth * 0.5), // 确保小列也能看见，但不超过最大宽度的一半
                        ColumnPadding = columnPadding,  // 使用动态计算的间距
                        ScalesYAt = 0,        // 使用第一个Y轴
                        Foreground = brush    // 使用相同的颜色作为标签颜色
                        //MaxColumnWidth = 60,  // 控制柱子的最大宽度
                        //MinWidth = 20,        // 控制柱子的最小宽度
                        //ColumnPadding = 20,   // 这里控制柱子之间的间距
                    };
                    
                    // 针对小值(1和2)的柱子进行特殊处理，确保它们有足够的可见高度
                    if (stockCount <= 2 && stockCount > 0)
                    {
                        // 增加边框使小值柱体更明显
                        columnSeries.Stroke = new SolidColorBrush(Colors.Black);
                        columnSeries.StrokeThickness = 1;
                        // 确保数据标签可见
                        columnSeries.DataLabels = true;
                        // 增加小值的字体可见性
                        columnSeries.FontSize = 12;
                        columnSeries.FontWeight = FontWeights.Bold;
                    }
                    
                    // 添加到集合
                    seriesCollection.Add(columnSeries);
                    labels.Add(labelText);
                    
                    Console.WriteLine($"添加数据系列: {labelText}, 库存={stockCount}");
                }
                
                // 特殊处理：如果只有一个数据系列，添加一个辅助系列确保图表显示
                if (seriesCollection.Count == 1)
                {
                    Console.WriteLine("检测到仅有一个数据系列，添加辅助系列以确保图表正常显示");
                    
                    // 获取现有系列的值
                    double existingValue = Convert.ToDouble(((ColumnSeries)seriesCollection[0]).Values[0]);
                    
                    // 创建一个辅助系列，值设为0.01，位置远离主柱体
                    var helperSeries = new ColumnSeries
                    {
                        Title = "辅助",
                        Values = new ChartValues<double> { 0.01 }, // 使用非零微小值
                        Fill = new SolidColorBrush(Colors.Transparent), // 完全透明
                        DataLabels = false, // 不显示标签
                        MaxColumnWidth = 1, // 极小的宽度
                        ScalesYAt = 0
                    };
                    
                    // 添加到集合，但使用空标签
                    seriesCollection.Add(helperSeries);
                    labels.Add(""); // 添加空标签对应辅助系列
                    
                    // 为单一数据系列增加额外的视觉强调
                    var mainSeries = (ColumnSeries)seriesCollection[0];
                    mainSeries.MaxColumnWidth = Math.Min(100, maxColumnWidth * 1.5); // 单一柱子可以更宽，但有上限
                    mainSeries.MinWidth = Math.Min(40, maxColumnWidth); // 确保最小宽度
                    mainSeries.FontSize = 14;      // 更大的字体
                    mainSeries.FontWeight = FontWeights.Bold; // 加粗文本
                    
                    // 确保Y轴最大值足够
                    maxStock = Math.Max(6, existingValue * 1.5);
                }
                
                // 更新图表数据
                Dispatcher.Invoke(() => {
                    try
                    {
                        // 清除原有图表数据
                        _chartViewModel.ClearData();
                        
                        // 保存标签和对应的颜色信息
                        int index = 0;
                        foreach (var series in seriesCollection)
                        {
                            if (series is ColumnSeries columnSeries && index < labels.Count)
                            {
                                // 保存每个标签对应的颜色
                                string label = labels[index];
                                _chartViewModel.LabelColors[label] = columnSeries.Fill;
                                index++;
                            }
                        }
                        
                        // 使用自定义标签格式化方法，确保每个标签都完整显示
                        _chartViewModel.LabelFormatter = idx => {
                            if (idx < 0 || idx >= labels.Count) return "";
                            
                            // 确保每个标签都清晰可见
                            string label = labels[(int)idx];
                            return string.IsNullOrEmpty(label) ? $"颜色 {idx+1}" : label;
                        };
                        
                        // 添加标签和系列数据
                        labels.ForEach(label => _chartViewModel.Labels.Add(label));
                        seriesCollection.ToList().ForEach(series => _chartViewModel.Series.Add(series));

                        // 强制设置固定Y轴刻度
                        AdjustYAxisScale(maxStock);
                        
                        // 确保柱状图可见
                        chartColorStats.Visibility = Visibility.Visible;
                        
                        // 在UI渲染后应用标签颜色
                        Dispatcher.InvokeAsync(() => ApplyAxisLabelColors(), 
                            System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                        
                        Console.WriteLine($"图表数据已更新，显示 {labels.Count} 种颜色的数据");
            }
            catch (Exception ex)
            {
                        Console.WriteLine($"更新图表数据时出错: {ex.Message}");
                    }
                });
                
                Console.WriteLine($"图表加载完成，显示 {labels.Count} 种颜色数据，最大库存值: {maxStock}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载颜色统计图表失败: {ex.Message}\n{ex.StackTrace}");
                
                // 出错时确保图表区域可见，显示空图表
                try 
                {
                    Dispatcher.Invoke(() => 
                    {
                        chartColorStats.Visibility = Visibility.Visible;
                        txtNoChartDataHint.Visibility = Visibility.Collapsed;
                        
                        // 清空数据
                        _chartViewModel.ClearData();
                        
                        // 应用默认Y轴设置
                        AdjustYAxisScale(0);
                    });
                }
                catch { /* 忽略UI操作异常 */ }
            }
        }

        /// <summary>
        /// 显示受限模式状态（SQLite不可用时）
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051:未使用的私有成员", Justification = "保留供将来使用")]
        private void ShowLimitedModeState()
        {
            // 显示限制模式警告
            ShowLimitedModeWarning();
            
            // 设置基本数据为不可用
            txtTotalCartridges.Text = "--";
            txtLowStockCartridges.Text = "--";
            txtTotalStock.Text = "--";
            
            // 显示对应提示
            TextBlock tbLimitedMode = new TextBlock
            {
                Text = "SQLite数据库不可用，无法显示数据。请安装SQLite以获得完整功能。",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20)
            };
            
            // 清空并添加提示到低库存列表区域
            if (lvLowStock.Parent is Grid lowStockGrid)
            {
                lowStockGrid.Children.Clear();
                lowStockGrid.Children.Add(tbLimitedMode);
            }
            
            // 清空并添加提示到最近操作记录区域
            if (lvRecentOperations.Parent is Grid recentOpsGrid)
            {
                recentOpsGrid.Children.Clear();
                recentOpsGrid.Children.Add(new TextBlock
                {
                    Text = "SQLite数据库不可用，无法显示数据。请安装SQLite以获得完整功能。",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(20)
                });
            }
            
            // 清空并添加提示到图表区域
            if (chartColorStats.Parent is Grid chartGrid)
            {
                chartGrid.Children.Clear();
                chartGrid.Children.Add(new TextBlock
                {
                    Text = "SQLite数据库不可用，无法显示数据。请安装SQLite以获得完整功能。",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(20)
                });
            }
        }

        /// <summary>
        /// 加载低库存墨盒列表
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051:未使用的私有成员", Justification = "保留供将来使用")]
        private void LoadLowStockList(List<Cartridge> lowStockCartridges)
        {
            try
            {
                // 清空当前列表
                lvLowStock.Items.Clear();
                
                // 如果没有低库存墨盒，显示空状态
                if (lowStockCartridges.Count == 0)
                {
                    txtLowStockHint.Visibility = Visibility.Visible;
                    lvLowStock.Visibility = Visibility.Collapsed;
                    return;
                }
                
                // 显示低库存墨盒列表
                txtLowStockHint.Visibility = Visibility.Collapsed;
                lvLowStock.Visibility = Visibility.Visible;
                
                // 添加低库存墨盒到ListView
                foreach (var cartridge in lowStockCartridges)
                {
                    lvLowStock.Items.Add(new
                    {
                        cartridge.Id,
                        cartridge.Color,
                        cartridge.Model,
                        cartridge.CurrentStock,
                        cartridge.MinimumStock,
                        StockStatus = cartridge.CurrentStock == 0 ? "无库存" : "库存不足"
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError("加载低库存墨盒列表失败", ex);
            }
        }

        /// <summary>
        /// 加载近期操作记录
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051:未使用的私有成员", Justification = "保留供将来使用")]
        private void LoadRecentOperations()
        {
            try
            {
                // 获取最近10条操作记录
                var recentRecords = DatabaseHelper.GetStockRecords(10);
                
                // 如果没有记录，显示空状态
                if (recentRecords.Count == 0)
                {
                    txtRecentOperationsHint.Visibility = Visibility.Visible;
                    lvRecentOperations.Visibility = Visibility.Collapsed;
                    return;
                }
                
                // 显示操作记录列表
                txtRecentOperationsHint.Visibility = Visibility.Collapsed;
                lvRecentOperations.Visibility = Visibility.Visible;
                
                // 清空当前列表
                lvRecentOperations.Items.Clear();
                
                // 添加记录到ListView
                foreach (var record in recentRecords)
                {
                    lvRecentOperations.Items.Add(new
                    {
                        record.Id,
                        CartridgeInfo = $"{record.Cartridge.Color} {record.Cartridge.Model}",
                        OperationType = record.OperationType == 1 ? "入库" : "出库",
                        record.Quantity,
                        record.OperationTime,
                        record.Operator
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError("加载近期操作记录失败", ex);
            }
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 防止频繁刷新和重复调用
                if (_isRefreshing)
                {
                    Console.WriteLine("刷新正在进行中，忽略重复请求");
                    return;
                }
                
                // 检查刷新时间间隔
                var now = DateTime.Now;
                if ((now - _lastRefreshTime) < _minRefreshInterval)
                {
                    Console.WriteLine("刷新间隔过短，忽略请求");
                    return;
                }
                
                // 设置刷新状态和时间戳
                _isRefreshing = true;
                _lastRefreshTime = now;
                
                Console.WriteLine("手动刷新仪表盘数据...");
                
                try
                {
                    // 禁用刷新按钮，防止重复点击
                    if (btnRefreshChart != null)
                        btnRefreshChart.IsEnabled = false;
                    
                    // 1. 确保图表显示区域可见
                chartColorStats.Visibility = Visibility.Visible;
                
                    // 2. 重新加载数据 - 只调用最必要的方法避免重复加载
                    
                    // 同步墨盒颜色数据 - 这是基础数据
                    SyncCartridgeColors();
                    
                    // 直接加载图表数据 - 这会设置柱状图和数据绑定
                    LoadColorStatisticsChart();
                    
                    // 显示成功消息
                //MessageBox.Show("数据刷新成功！", "刷新", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                finally
                {
                    // 恢复刷新按钮状态
                    if (btnRefreshChart != null)
                        btnRefreshChart.IsEnabled = true;
                    
                    // 重置刷新状态标志
                    _isRefreshing = false;
                }
            }
            catch (Exception ex)
            {
                // 重置刷新状态，确保意外情况下不会永久锁定刷新功能
                _isRefreshing = false;
                if (btnRefreshChart != null)
                    btnRefreshChart.IsEnabled = true;
                
                ShowError("刷新数据失败", ex);
                Console.WriteLine($"刷新数据时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 显示低库存墨盒信息，但目前未被使用
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051:未使用的私有成员", Justification = "保留供将来使用")]
        private void RefreshLowStockDisplay()
        {
            try
            {
                Console.WriteLine("刷新低库存墨盒显示...");
                var allCartridges = DatabaseHelper.GetAllCartridges();
                if (allCartridges == null || allCartridges.Count == 0)
                {
                    Console.WriteLine("没有墨盒数据，无法显示低库存墨盒");
                    txtLowStockHint.Visibility = Visibility.Visible;
                    lvLowStock.Visibility = Visibility.Collapsed;
                    
                    // 即使没有墨盒数据，仍然保持Y轴刻度为固定值以确保图表显示正确
                    AdjustYAxisScale(0);
                    return;
                }
                
                // 获取所有库存低于或等于警戒线的墨盒
                var lowStockCartridges = allCartridges.Where(c => c.CurrentStock <= c.MinimumStock).ToList();
                
                Console.WriteLine($"刷新后发现 {lowStockCartridges.Count} 个低库存墨盒");
                foreach (var cart in lowStockCartridges)
                {
                    Console.WriteLine($"  - {cart.Color} {cart.Model}: 当前={cart.CurrentStock}, 警戒线={cart.MinimumStock}");
                }
                
                // 更新UI
                txtLowStockCartridges.Text = lowStockCartridges.Count.ToString();
                
                if (lowStockCartridges.Count > 0)
                {
                    // 有低库存墨盒，填充列表
                    lvLowStock.ItemsSource = lowStockCartridges;
                    txtLowStockHint.Visibility = Visibility.Collapsed;
                    lvLowStock.Visibility = Visibility.Visible;
                }
                else
                {
                    // 无低库存墨盒，显示提示
                    lvLowStock.ItemsSource = null;
                    txtLowStockHint.Visibility = Visibility.Visible;
                    lvLowStock.Visibility = Visibility.Collapsed;
                }
                
                // 刷新后，尝试根据当前库存最大值调整Y轴刻度
                double maxStock = allCartridges.Count > 0 ? allCartridges.Max(c => c.CurrentStock) : 0;
                AdjustYAxisScale(maxStock);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"刷新低库存墨盒显示出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 在图表加载完成后动态设置X轴标签颜色
        /// </summary>
        private void ApplyAxisLabelColors()
        {
            try
            {
                // 等待UI渲染完成后执行
                Dispatcher.InvokeAsync(() =>
                {
                    // 查找图表中的X轴标签
                    var chartContainer = chartColorStats;
                    var children = LogicalTreeHelper.GetChildren(chartContainer).OfType<UIElement>().ToList();
                    
                    Console.WriteLine($"找到图表子元素: {children.Count}个");
                    
                    // 遍历所有TextBlock，找到轴标签
                    var allTextBlocks = new List<TextBlock>();
                    FindAllTextBlocks(chartContainer, allTextBlocks);
                    
                    Console.WriteLine($"找到图表中的文本块: {allTextBlocks.Count}个");
                    
                    // 处理标签颜色
                    foreach (var textBlock in allTextBlocks)
                    {
                        // 如果文本块内容匹配Labels中的项，就应用对应颜色
                        string text = textBlock.Text;
                        if (string.IsNullOrEmpty(text)) continue;
                        
                        // 检查文本是否匹配任何墨盒颜色标签
                        if (_chartViewModel.Labels.Contains(text))
                        {
                            // 应用颜色
                            if (_chartViewModel.LabelColors.TryGetValue(text, out Brush brush))
                            {
                                textBlock.Foreground = brush;
                                textBlock.FontWeight = FontWeights.Bold;
                                textBlock.FontSize = 30; /*X轴标签的文字大小*/

                                // 给文本添加白色外发光效果，增强在彩色背景上的可见性
                                textBlock.Effect = new System.Windows.Media.Effects.DropShadowEffect
                                {
                                    ShadowDepth = 1,
                                    Direction = 320,
                                    Color = Colors.White,
                                    Opacity = 1.0,
                                    BlurRadius = 3
                                };
                                
                                // 确保文本完全可见
                                textBlock.TextTrimming = TextTrimming.None;
                                textBlock.TextWrapping = TextWrapping.NoWrap;
                                
                                Console.WriteLine($"已为标签'{text}'设置颜色和样式");
                            }
                        }
                    }
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置轴标签颜色时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 递归查找所有TextBlock
        /// </summary>
        private void FindAllTextBlocks(DependencyObject parent, List<TextBlock> textBlocks)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBlock textBlock)
                {
                    textBlocks.Add(textBlock);
                }
                else
                {
                    FindAllTextBlocks(child, textBlocks);
                }
            }
        }

        /// <summary>
        /// 图表加载完成事件
        /// </summary>
        private void ChartColorStats_Loaded(object sender, RoutedEventArgs e)
        {
            // 图表加载完成后应用轴标签颜色
            Dispatcher.InvokeAsync(() => ApplyAxisLabelColors(), 
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
        
        /// <summary>
        /// 图表数据点击事件
        /// </summary>
        private void ChartColorStats_DataClick(object sender, LiveCharts.ChartPoint chartPoint)
        {
            try
            {
                // 数据点被点击时执行
                var series = chartPoint.SeriesView;
                if (series is ColumnSeries columnSeries)
                {
                    string colorName = columnSeries.Title;
                    int value = (int)chartPoint.Y;
                    
                    // 查找该颜色对应的所有墨盒型号
                    var allCartridges = DatabaseHelper.GetAllCartridges() ?? new List<Cartridge>();
                    var matchingCartridges = allCartridges.Where(c => c.Color == colorName).ToList();
                    
                    StringBuilder detailMessage = new StringBuilder();
                    detailMessage.AppendLine($"墨盒颜色: {colorName}");
                    detailMessage.AppendLine($"当前库存总量: {value}");
                    detailMessage.AppendLine();
                    
                    if (matchingCartridges.Count > 0)
                    {
                        detailMessage.AppendLine("包含以下型号:");
                        foreach (var cart in matchingCartridges)
                        {
                            detailMessage.AppendLine($"- {cart.Model}: {cart.CurrentStock} 个 (警戒线: {cart.MinimumStock})");
                        }
                    }
                    else
                    {
                        detailMessage.AppendLine("没有找到此颜色的墨盒型号信息");
                    }
                    
                    // 显示详细信息窗口
                    MessageBox.Show(detailMessage.ToString(), $"{colorName}墨盒详情", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    Console.WriteLine($"用户点击了{colorName}墨盒，库存数量：{value}，显示了详情对话框");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理图表点击事件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 实现IRefreshable接口的Refresh方法
        /// </summary>
        public void Refresh()
        {
            try
            {
                LoadDashboardData();
            }
            catch (Exception ex)
            {
                // 记录详细错误信息
                Console.WriteLine($"刷新仪表盘时出错: {ex.Message}");
                Console.WriteLine($"异常详情: {ex.StackTrace}");
                
                MessageBox.Show($"刷新仪表盘数据时出错: {ex.Message}", 
                    "刷新错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
