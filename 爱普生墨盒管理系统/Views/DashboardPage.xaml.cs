using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes; // Rectangle, Path等形状类
using System.Windows.Media.Animation;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Configurations;
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
        public class ChartViewModel : INotifyPropertyChanged
        {
            // 修改Series属性以支持通知变更
            private SeriesCollection _series;
            public SeriesCollection Series
            {
                get => _series;
                set
                {
                    _series = value;
                    OnPropertyChanged();
                }
            }
            
            public ChartValues<double> ChartValues { get; set; }
            
            // 修改Labels属性实现INotifyPropertyChanged
            private List<string> _labels;
            public List<string> Labels
            {
                get => _labels;
                set
                {
                    _labels = value;
                    OnPropertyChanged();
                }
            }
            
            // 保留原有属性
            public Func<double, string> Formatter { get; set; }
            public Brush ColumnFill { get; set; }
            public Func<double, string> LabelFormatter { get; set; }
            public Dictionary<string, Brush> LabelColors { get; set; }
            
            // 添加属性变更通知
            public event PropertyChangedEventHandler PropertyChanged;
            
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            
            // 保留原有构造函数
            public ChartViewModel()
            {
                Series = new SeriesCollection();
                ChartValues = new ChartValues<double>();
                Labels = new List<string>();
                LabelColors = new Dictionary<string, Brush>();
                Formatter = value => value.ToString("N0");
                ColumnFill = Brushes.DodgerBlue;
                
                LabelFormatter = index => 
                {
                    if (index < 0 || index >= Labels.Count)
                        return string.Empty;
                    
                    string label = Labels[(int)index];
                    if (string.IsNullOrEmpty(label))
                        return $"颜色 {index+1}";
                    
                    // 由于标签已经旋转90度，不需要再换行处理
                    return label;
                };
            }

            // 修改ClearData方法以包含对Series的更新通知
            public void ClearData()
            {
                // 创建新的SeriesCollection代替清空，以确保触发变更通知
                Series = new SeriesCollection();
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

        // 添加字段保存定时器和参数
        private System.Windows.Threading.DispatcherTimer _colorApplyTimer;
        private List<string> _pendingLabels;
        private Dictionary<string, string> _pendingColorCodes;

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
                
                // 诊断墨盒表状态
                Task.Run(() => {
                    Console.WriteLine("开始诊断墨盒表状态...");
                    string tableStatus = DatabaseHelper.GetCartridgeTableStatus();
                    Console.WriteLine(tableStatus);
                    
                    // 如果表中存在记录但没有库存不足的墨盒，尝试修复表数据
                    if (tableStatus.Contains("墨盒表总记录数:") && tableStatus.Contains("库存不足墨盒数: 0"))
                    {
                        Console.WriteLine("检测到墨盒表中没有库存不足记录，尝试修复...");
                        string fixResult = DatabaseHelper.CheckAndFixCartridgeTable();
                        Console.WriteLine(fixResult);
                    }
                });
                
                // 使用较短延迟加载数据，避免与数据库初始化冲突
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500) // 增加延迟，确保诊断和修复完成
                };
                timer.Tick += (s, args) => 
                {
                    try
                    {
                        LoadDashboardData();
                        ((System.Windows.Threading.DispatcherTimer)s).Stop();
                        
                        // 添加额外的延迟，确保图表在数据加载后能够正确渲染和应用颜色
                        System.Windows.Threading.DispatcherTimer colorTimer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(500)
                        };
                        colorTimer.Tick += (colorSender, colorArgs) => 
                        {
                            try
                            {
                                // 强制刷新柱状图
                                ForceRefreshColumnChart();
                                
                                // 强制刷新库存不足墨盒列表
                                Console.WriteLine("强制刷新库存不足墨盒列表...");
                                LoadLowStockCartridges();
                                
                                ((System.Windows.Threading.DispatcherTimer)colorSender).Stop();
                            }
                            catch (Exception colorEx)
                            {
                                Console.WriteLine($"延迟强制刷新组件出错: {colorEx.Message}");
                            }
                        };
                        colorTimer.Start();
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
        /// 强制刷新柱状图
        /// </summary>
        public void ForceRefreshColumnChart()
        {
            try
            {
                Console.WriteLine("开始强制刷新柱状图...");
                
                Dispatcher.Invoke(() =>
                {
                    if (chartColorStats != null)
                    {
                        // 确保图表可见
                        chartColorStats.Visibility = Visibility.Visible;
                        
                        // 强制更新图表
                        chartColorStats.UpdateLayout();
                        chartColorStats.Update(true);
                        
                        // 如果有待应用的颜色信息，再次尝试应用
                        if (_pendingLabels != null && _pendingLabels.Count > 0 && _pendingColorCodes != null)
                        {
                            Console.WriteLine("强制刷新时应用颜色设置");
                            ApplyChartColors(_pendingLabels, _pendingColorCodes);
                        }
                    }
                    else
                    {
                        Console.WriteLine("找不到图表控件，无法强制刷新");
                    }
                });
                
                Console.WriteLine("强制刷新柱状图完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"强制刷新柱状图出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 加载仪表盘数据
        /// </summary>
        public void LoadDashboardData()
        {
            try
            {
                // 防止重复加载
                lock (_loadLock)
                {
                    if (_isLoading)
                    {
                        Console.WriteLine("正在加载数据，忽略重复请求");
                        return;
                    }

                    // 检查最小加载间隔
                    TimeSpan elapsed = DateTime.Now - _lastLoadTime;
                    if (elapsed.TotalMilliseconds < MIN_LOAD_INTERVAL_MS)
                    {
                        Console.WriteLine($"加载间隔过短 ({elapsed.TotalMilliseconds}ms < {MIN_LOAD_INTERVAL_MS}ms)，忽略请求");
                        return;
                    }

                    _isLoading = true;
                    _lastLoadTime = DateTime.Now;
                }

                Console.WriteLine("开始加载仪表盘数据...");

                // 在后台线程加载数据
                Task.Run(() =>
                {
                    try
                    {
                        // 加载墨盒统计数据
                        LoadCartridgeStatistics();

                        // 加载颜色统计图表
                        LoadColorStatisticsChart();

                        // 加载库存不足墨盒列表
                        LoadLowStockCartridges();

                        // 加载最近操作记录
                        LoadRecentOperations();

                        Console.WriteLine("仪表盘数据加载完成");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"加载仪表盘数据出错: {ex.Message}\n{ex.StackTrace}");
                    }
                    finally
                    {
                        lock (_loadLock)
                        {
                            _isLoading = false;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动仪表盘数据加载出错: {ex.Message}\n{ex.StackTrace}");
                lock (_loadLock)
                {
                    _isLoading = false;
                }
            }
        }

        /// <summary>
        /// 加载墨盒统计数据
        /// </summary>
        private void LoadCartridgeStatistics()
        {
            try
            {
                // 获取所有墨盒数据
                var allCartridges = DatabaseHelper.GetAllCartridges() ?? new List<Cartridge>();
                
                // 直接从数据库CartridgeColors表获取墨盒类型数量
                int totalCartridgeTypes = DatabaseHelper.GetAllCartridgeColors().Count;
                int totalStock = allCartridges.Sum(c => c.CurrentStock);
                int lowStockCount = allCartridges.Count(c => c.CurrentStock < c.MinimumStock);
                
                // 更新UI
                Dispatcher.Invoke(() =>
                {
                    txtTotalCartridges.Text = totalCartridgeTypes.ToString();
                    txtTotalStock.Text = totalStock.ToString();
                    txtLowStockCartridges.Text = lowStockCount.ToString();
                    
                    // 更新提示可见性
                    tbNoDataHint.Visibility = allCartridges.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                });
                
                Console.WriteLine($"墨盒统计数据加载完成: 墨盒类型={totalCartridgeTypes}, 总库存={totalStock}, 库存不足={lowStockCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载墨盒统计数据出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 加载颜色统计图表
        /// </summary>
        private void LoadColorStatisticsChart()
        {
            try
            {
                Console.WriteLine("开始加载颜色统计图表...");
                
                // 确保清空原有的图表数据
                Dispatcher.Invoke(() => 
                {
                    if (chartColorStats != null)
                    {
                        Console.WriteLine("清空原有图表数据");
                        chartColorStats.Series?.Clear();
                        _chartViewModel.ClearData();
                    }
                });
                
                // 获取所有墨盒数据
                var allCartridges = DatabaseHelper.GetAllCartridges() ?? new List<Cartridge>();
                int cartridgeModelCount = allCartridges.Select(c => c.Model).Distinct().Count();
                
                // 更新墨盒型号数量显示
                Dispatcher.Invoke(() => txtCartridgeCount.Text = cartridgeModelCount.ToString());
                
                // 获取所有颜色数据，按显示顺序排序
                var allColors = DatabaseHelper.GetAllCartridgeColors()
                    .OrderBy(c => c.DisplayOrder)
                    .ToList();
                
                Console.WriteLine($"获取到 {allColors.Count} 种颜色");
                
                if (allColors.Count == 0)
                {
                    Console.WriteLine("没有颜色数据，无法显示图表");
                    Dispatcher.Invoke(() => {
                        txtNoChartDataHint.Visibility = Visibility.Visible;
                        chartColorStats.Visibility = Visibility.Collapsed;
                    });
                    return;
                }
                
                // 准备数据 - 仅准备数据，不创建UI元素
                var labels = allColors.Select(c => c.Name).ToList();
                var colorStockCounts = new Dictionary<string, int>();
                
                // 计算每种颜色的墨盒库存总量
                foreach (var color in allColors)
                {
                    // 获取该颜色的所有墨盒
                    var cartridgesOfColor = allCartridges.Where(c => c.Color == color.Name).ToList();
                    // 计算该颜色的总库存
                    int totalStock = cartridgesOfColor.Sum(c => c.CurrentStock);
                    colorStockCounts[color.Name] = totalStock;
                    Console.WriteLine($"颜色: {color.Name}, 库存总量: {totalStock}");
                }
                
                // 准备颜色数据
                var stockData = new List<KeyValuePair<string, int>>();
                var colorCodes = new Dictionary<string, string>(); // 存储颜色代码而不是Brush对象
                
                foreach (var color in allColors)
                {
                    int stockCount = colorStockCounts.ContainsKey(color.Name) ? colorStockCounts[color.Name] : 0;
                    stockData.Add(new KeyValuePair<string, int>(color.Name, stockCount));
                    colorCodes[color.Name] = color.ColorCode; // 存储颜色代码字符串
                }
                
                // 将所有UI操作放在UI线程中执行
                Dispatcher.Invoke(() => 
                {
                    try
                    {
                        // 设置标签和颜色
                        _chartViewModel.Labels = labels;
                        
                        // 创建新的SeriesCollection
                        var newSeries = new SeriesCollection();
                        
                        // 创建单个ColumnSeries
                        var columnSeries = new ColumnSeries
                        {
                            Title = "墨盒库存",
                            Values = new ChartValues<double>(),
                            DataLabels = true,
                            LabelPoint = point => point.Y.ToString("N0"),
                            Stroke = Brushes.DarkGray,
                            StrokeThickness = 1,
                            MaxColumnWidth = 40,
                            ColumnPadding = 12,
                            // 设置悬停指示器样式
                            Configuration = new CartesianMapper<double>()
                                .X((value, index) => index)
                                .Y(value => value)
                                .Fill((value, index) => {
                                    // 获取对应索引的颜色
                                    if (index < labels.Count) {
                                        string colorName = labels[index];
                                        string colorCode = colorCodes.ContainsKey(colorName) ? colorCodes[colorName] : "#CCCCCC";
                                        try {
                                            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorCode));
                                        } catch {
                                            return Brushes.Gray;
                                        }
                                    }
                                    return Brushes.DodgerBlue;
                                })
                        };

                        // 为每个点添加值
                        foreach (var item in stockData)
                        {
                            double value = Convert.ToDouble(item.Value);
                            columnSeries.Values.Add(value);
                        }
                        
                        // 添加到系列集合
                        newSeries.Add(columnSeries);
                        
                        // 设置Series属性以触发绑定更新
                        _chartViewModel.Series = newSeries;
                        
                        // 显示图表
                        txtNoChartDataHint.Visibility = Visibility.Collapsed;
                        chartColorStats.Visibility = Visibility.Visible;
                        
                        // 强制刷新图表
                        chartColorStats.Update(true);
                        
                        Console.WriteLine("图表UI已在UI线程上更新完成");
                        
                        // 应用颜色设置（延迟执行）
                        ApplyChartColorsWithDelay(labels, colorCodes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"在UI线程更新图表时出错: {ex.Message}\n{ex.StackTrace}");
                    }
                });
                
                Console.WriteLine($"颜色统计图表加载完成，共 {allColors.Count} 种颜色");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载颜色统计图表出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 在延迟后应用墨盒颜色到柱状图（确保图表完全渲染）
        /// </summary>
        private void ApplyChartColorsWithDelay(List<string> labels, Dictionary<string, string> colorCodes, int delayMs = 300)
        {
            try
            {
                // 确保在UI线程上执行
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => ApplyChartColorsWithDelay(labels, colorCodes, delayMs));
                    return;
                }

                // 停止之前可能存在的计时器
                _colorApplyTimer?.Stop();
                if (_colorApplyTimer != null)
                {
                    _colorApplyTimer.Tick -= ColorApplyTimer_Tick;
                }

                // 保存当前的参数，供定时器回调使用
                _pendingLabels = new List<string>(labels);
                _pendingColorCodes = new Dictionary<string, string>(colorCodes);

                // 创建新的定时器
                _colorApplyTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(delayMs)
                };

                // 设置定时器回调
                _colorApplyTimer.Tick += ColorApplyTimer_Tick;
                _colorApplyTimer.Start();

                Console.WriteLine($"已安排在{delayMs}毫秒后应用墨盒颜色");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"安排应用墨盒颜色时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 颜色应用定时器回调
        /// </summary>
        private void ColorApplyTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // 停止计时器
                ((System.Windows.Threading.DispatcherTimer)sender).Stop();

                // 应用颜色
                ApplyChartColors(_pendingLabels, _pendingColorCodes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"颜色应用定时器回调出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 应用墨盒颜色到柱状图
        /// </summary>
        private void ApplyChartColors(List<string> labels, Dictionary<string, string> colorCodes)
        {
            try
            {
                // 检查图表是否可用
                var chart = chartColorStats;
                if (chart == null)
                {
                    Console.WriteLine("找不到图表控件");
                    return;
                }

                Console.WriteLine($"开始应用墨盒颜色，标签数量: {labels.Count}, 颜色代码数量: {colorCodes.Count}");
                
                // 调试: 输出所有标签和颜色
                for (int i = 0; i < labels.Count; i++)
                {
                    string colorName = labels[i];
                    string colorCode = colorCodes.ContainsKey(colorName) ? colorCodes[colorName] : "#CCCCCC";
                    Console.WriteLine($"标签[{i}]: '{colorName}', 颜色代码: '{colorCode}'");
                }

                // 使用VisualTreeHelper查找所有Rectangle元素
                var rectangles = new List<Rectangle>();
                FindRectangles(chart, rectangles);

                // 过滤出柱子（一般来说是较大的矩形）
                var columns = rectangles
                    .Where(r => r.Width > 5 && r.Height > 5)
                    .OrderBy(r => Canvas.GetLeft(r))
                    .ToList();

                Console.WriteLine($"找到 {rectangles.Count} 个矩形元素，其中 {columns.Count} 个被认为是柱子");

                // 如果找到的矩形数量与颜色数量不匹配，记录警告
                if (columns.Count != labels.Count)
                {
                    Console.WriteLine($"警告：找到 {columns.Count} 个柱子，但有 {labels.Count} 种颜色，可能导致颜色错位");
                    
                    // 强制更新UI
                    chart.UpdateLayout();
                    
                    // 尝试重新获取柱子
                    rectangles.Clear();
                    FindRectangles(chart, rectangles);
                    columns = rectangles
                        .Where(r => r.Width > 5 && r.Height > 5)
                        .OrderBy(r => Canvas.GetLeft(r))
                        .ToList();
                        
                    Console.WriteLine($"更新后: 找到 {rectangles.Count} 个矩形元素，其中 {columns.Count} 个被认为是柱子");
                }

                // 为每个找到的柱子设置对应的颜色
                for (int i = 0; i < Math.Min(columns.Count, labels.Count); i++)
                {
                    string colorName = labels[i];
                    string colorCode = colorCodes.ContainsKey(colorName) ? colorCodes[colorName] : "#CCCCCC";

                    try
                    {
                        // 在UI线程中创建和应用Brush
                        Brush brush = null;
                        try
                        {
                            brush = (Brush)new BrushConverter().ConvertFromString(colorCode);
                            Console.WriteLine($"柱子[{i}]: 设置颜色 '{colorName}' ({colorCode})");
                        }
                        catch
                        {
                            brush = Brushes.Gray; // 颜色转换失败时的默认值
                            Console.WriteLine($"柱子[{i}]: 颜色转换失败，使用默认灰色");
                        }

                        // 设置柱子的背景色
                        columns[i].Fill = brush;
                    }
                    catch (Exception colEx)
                    {
                        Console.WriteLine($"设置柱子[{i}]颜色时出错: {colEx.Message}");
                    }
                }

                Console.WriteLine($"已为 {Math.Min(columns.Count, labels.Count)} 个柱子设置颜色");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用墨盒颜色到柱状图时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 加载库存不足墨盒列表
        /// </summary>
        private void LoadLowStockCartridges()
        {
            try
            {
                Console.WriteLine("开始加载库存不足墨盒列表...");
                
                // 获取库存不足的墨盒
                var lowStockCartridges = DatabaseHelper.GetLowStockCartridges() ?? new List<Cartridge>();
                
                Console.WriteLine($"从数据库获取到 {lowStockCartridges.Count} 个库存不足墨盒");
                
                // 输出调试信息，查看返回的墨盒详情
                if (lowStockCartridges.Count > 0)
                {
                    Console.WriteLine("库存不足墨盒列表内容:");
                    foreach (var cartridge in lowStockCartridges)
                    {
                        Console.WriteLine($"  ID: {cartridge.Id}, 颜色: {cartridge.Color}, 型号: {cartridge.Model}, 当前库存: {cartridge.CurrentStock}, 最低库存: {cartridge.MinimumStock}");
                    }
                }
                else
                {
                    Console.WriteLine("没有库存不足的墨盒");
                }
                
                // 更新UI
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        Console.WriteLine("在UI线程更新库存不足墨盒列表...");
                        
                        if (lowStockCartridges.Count > 0)
                        {
                            Console.WriteLine("设置列表数据源并显示");
                            lvLowStock.ItemsSource = lowStockCartridges;
                            txtLowStockHint.Visibility = Visibility.Collapsed;
                            
                            // 确保列表可见
                            lvLowStock.Visibility = Visibility.Visible;
                            
                            // 强制更新列表
                            lvLowStock.Items.Refresh();
                            lvLowStock.UpdateLayout();
                        }
                        else
                        {
                            Console.WriteLine("没有数据，显示提示文本");
                            lvLowStock.ItemsSource = null;
                            txtLowStockHint.Visibility = Visibility.Visible;
                        }
                        
                        Console.WriteLine("UI更新完成");
                    }
                    catch (Exception uiEx)
                    {
                        Console.WriteLine($"在UI线程更新库存不足墨盒列表时出错: {uiEx.Message}\n{uiEx.StackTrace}");
                    }
                });
                
                Console.WriteLine($"库存不足墨盒列表加载完成，共 {lowStockCartridges.Count} 条记录");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载库存不足墨盒列表出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 加载最近操作记录
        /// </summary>
        private void LoadRecentOperations()
        {
            try
            {
                // 获取最近的操作记录(最多20条)
                var recentOperations = OperationAdapter.GetRecentOperations(20) ?? new List<Operation>();
                
                // 更新UI
                Dispatcher.Invoke(() =>
                {
                    if (recentOperations.Count > 0)
                    {
                        lvRecentOperations.ItemsSource = recentOperations;
                        txtRecentOperationsHint.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        lvRecentOperations.ItemsSource = null;
                        txtRecentOperationsHint.Visibility = Visibility.Visible;
                    }
                });
                
                Console.WriteLine($"最近操作记录加载完成，共 {recentOperations.Count} 条记录");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载最近操作记录出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 防止频繁刷新
                if (_isRefreshing)
                {
                    Console.WriteLine("正在刷新，忽略重复请求");
                    return;
                }

                TimeSpan elapsed = DateTime.Now - _lastRefreshTime;
                if (elapsed < _minRefreshInterval)
                {
                    Console.WriteLine($"刷新间隔过短 ({elapsed.TotalSeconds}s < {_minRefreshInterval.TotalSeconds}s)，忽略请求");
                    return;
                }

                _isRefreshing = true;
                _lastRefreshTime = DateTime.Now;

                Console.WriteLine("用户点击了全部显示按钮，刷新图表");
                
                // 专门用于刷新柱状图
                if (sender == btnRefreshChart)
                {
                    // 直接调用重新加载图表方法，内部已处理线程安全问题
                    LoadColorStatisticsChart();
                    
                    // 强制图表显示（在UI线程中执行）
                    Dispatcher.Invoke(() => {
                        chartColorStats.Visibility = Visibility.Visible;
                        
                        // 如果图表仍然为空，尝试强制刷新
                        if (_chartViewModel.Series != null && _chartViewModel.Series.Count > 0 && 
                            (chartColorStats.Series == null || chartColorStats.Series.Count == 0))
                        {
                            Console.WriteLine("图表可能未正确绑定，尝试强制刷新");
                            
                            // 强制更新
                            chartColorStats.Update(true);
                        }
                    });
                }
                else
                {
                    // 刷新所有数据
                    LoadDashboardData();
                }

                // 重置刷新标志
                _isRefreshing = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"刷新按钮点击事件处理出错: {ex.Message}\n{ex.StackTrace}");
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// 实现IRefreshable接口的刷新方法
        /// </summary>
        public void Refresh()
        {
            try
            {
                Console.WriteLine("DashboardPage.Refresh() 被调用");
                
                // 防止频繁刷新
                TimeSpan elapsed = DateTime.Now - _lastRefreshTime;
                if (elapsed < _minRefreshInterval)
                {
                    Console.WriteLine($"刷新间隔过短 ({elapsed.TotalSeconds}s < {_minRefreshInterval.TotalSeconds}s)，延迟刷新");
                    return;
                }
                
                _lastRefreshTime = DateTime.Now;
                
                // 刷新数据
                LoadDashboardData();
                
                // 使用短延迟确保在数据加载后强制刷新柱状图
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(400)
                };
                timer.Tick += (s, args) => 
                {
                    try
                    {
                        // 强制刷新柱状图
                        ForceRefreshColumnChart();
                        ((System.Windows.Threading.DispatcherTimer)s).Stop();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"刷新后延迟强制刷新柱状图出错: {ex.Message}");
                    }
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DashboardPage.Refresh() 出错: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 图表加载完成事件处理
        /// </summary>
        private void ChartColorStats_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("图表控件加载完成");
                
                // 此事件已在UI线程中，可以安全地操作UI元素
                if (sender is LiveCharts.Wpf.CartesianChart chart && chart.AxisX != null && chart.AxisX.Count > 0)
                {
                    // 设置X轴的标签旋转角度 如果显示不全，可以调整角度 比如设置为45度
                    chart.AxisX[0].LabelsRotation = 0;
                    
                    // 设置分隔符步长为1，确保每个标签都显示
                    chart.AxisX[0].Separator = new LvcSeparator
                    {
                        Step = 1,
                        IsEnabled = true
                    };
                    
                    // 强制更新图表
                    chart.Update(true);
                    
                    // 如果有待应用的颜色，尝试再次应用
                    if (_pendingLabels != null && _pendingLabels.Count > 0 && _pendingColorCodes != null)
                    {
                        Console.WriteLine("图表加载完成，尝试应用颜色");
                        ApplyChartColorsWithDelay(_pendingLabels, _pendingColorCodes, 100); // 使用较短的延迟
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"图表加载完成事件处理出错: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 图表数据点击事件处理
        /// </summary>
        private void ChartColorStats_DataClick(object sender, ChartPoint chartPoint)
        {
            try
            {
                // 获取点击的柱子对应的颜色名称
                int index = (int)chartPoint.X;
                if (index >= 0 && index < _chartViewModel.Labels.Count)
                {
                    string colorName = _chartViewModel.Labels[index];
                    MessageBox.Show($"{colorName} ，库存数量: {chartPoint.Y}", "墨盒颜色详情", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"图表数据点击事件处理出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查数据库是否已初始化并确保颜色表有数据
        /// </summary>
        private bool CheckDatabaseInitialized()
        {
            try
            {
                // 获取颜色数据
                var colors = DatabaseHelper.GetAllCartridgeColors();
                
                // 如果没有颜色数据，可能需要初始化
                if (colors == null || colors.Count == 0)
                {
                    Console.WriteLine("数据库中没有颜色数据，可能需要初始化");
                    return false;
                }
                
                return true;
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"检查数据库初始化状态出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 递归查找可视化树中的所有Rectangle元素
        /// </summary>
        private void FindRectangles(DependencyObject parent, List<Rectangle> rectangles)
        {
            try
            {
                // 检查当前元素是否为Rectangle
                if (parent is Rectangle rectangle)
                {
                    rectangles.Add(rectangle);
                }
                
                // 获取子元素数量
                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                
                // 递归处理每个子元素
                for (int i = 0; i < childCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                    if (child != null)
                    {
                        FindRectangles(child, rectangles);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查找Rectangle元素时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 递归查找可视化树中的所有Path元素
        /// </summary>
        private void FindPaths(DependencyObject parent, List<Path> paths)
        {
            try
            {
                // 检查当前元素是否为Path
                if (parent is Path path)
                {
                    paths.Add(path);
                }
                
                // 获取子元素数量
                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                
                // 递归处理每个子元素
                for (int i = 0; i < childCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                    if (child != null)
                    {
                        FindPaths(child, paths);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查找Path元素时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找可视化树中的所有子元素
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            try
            {
                // 检查当前元素是否为T类型
                if (parent is T t)
                {
                    return t;
                }
                
                // 获取子元素数量
                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                
                // 递归处理每个子元素
                for (int i = 0; i < childCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                    if (child != null)
                    {
                        T result = FindVisualChild<T>(child);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查找子元素时出错: {ex.Message}");
            }
            return null;
        }
    }
}