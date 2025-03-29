using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using Microsoft.Win32;
using 爱普生墨盒管理系统.Models;
using 爱普生墨盒管理系统.Utils;
using OfficeOpenXml; // 添加EPPlus的引用

namespace 爱普生墨盒管理系统.Views
{
    /// <summary>
    /// ReportsPage.xaml 的交互逻辑
    /// </summary>
    public partial class ReportsPage : Page
    {
        private DataTable currentReportData;
        private int currentReportType = 1; // 默认为墨盒使用统计
        
        // 墨盒使用统计分页相关属性
        private int _cartridgeCurrentPage = 1;
        private int _cartridgePageSize = 10;
        private int _cartridgeTotalRecords = 0;
        private int _cartridgeTotalPages = 1;
        private List<dynamic> _cartridgeFullData = new List<dynamic>(); // 存储完整数据
        
        // 项目使用统计分页相关属性
        private int _projectCurrentPage = 1;
        private int _projectPageSize = 10;
        private int _projectTotalRecords = 0;
        private int _projectTotalPages = 1;
        private List<dynamic> _projectFullData = new List<dynamic>(); // 存储完整数据

        public ReportsPage()
        {
            InitializeComponent();
            InitializeDatePickers();
            GenerateReport();
        }

        /// <summary>
        /// 初始化日期选择器
        /// </summary>
        private void InitializeDatePickers()
        {
            // 默认统计当前年份的数据
            var now = DateTime.Now;
            var firstDayOfYear = new DateTime(now.Year, 1, 1);
            var lastDayOfYear = new DateTime(now.Year, 12, 31);
            
            dpStartDate.SelectedDate = firstDayOfYear;
            dpEndDate.SelectedDate = lastDayOfYear;
        }

        /// <summary>
        /// 报表类型选择变更事件
        /// </summary>
        private void cmbReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedItem = cmbReportType.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Tag != null)
            {
                currentReportType = Convert.ToInt32(selectedItem.Tag);
                
                // 切换显示的选项卡
                if (currentReportType == 1)
                {
                    tabReports.SelectedItem = tabCartridgeUsage;
                }
                else
                {
                    tabReports.SelectedItem = tabProjectUsage;
                }
            }
        }

        /// <summary>
        /// 生成报表按钮点击事件
        /// </summary>
        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            GenerateReport();
        }

        /// <summary>
        /// 生成报表
        /// </summary>
        private void GenerateReport()
        {
            try
            {
                // 获取日期范围
                DateTime? startDate = dpStartDate.SelectedDate;
                DateTime? endDate = dpEndDate.SelectedDate;
                
                // 验证日期
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    MessageBox.Show("请选择有效的日期范围", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (startDate > endDate)
                {
                    MessageBox.Show("开始日期不能晚于结束日期", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // 如果结束日期有值，将其设置为当天的23:59:59
                if (endDate.HasValue)
                {
                    endDate = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                }
                
                // 根据报表类型生成相应报表
                if (currentReportType == 1)
                {
                    GenerateCartridgeUsageReport(startDate.Value, endDate.Value);
                }
                else
                {
                    GenerateProjectUsageReport(startDate.Value, endDate.Value);
                }
                
                // 更新时间段显示
                string timeRangeText = $"{startDate.Value:yyyy-MM-dd} 至 {endDate.Value:yyyy-MM-dd}";
                if (currentReportType == 1)
                {
                    txtCartridgeUsageSummary.Text = $"墨盒使用统计（时间段：{timeRangeText}）";
                }
                else
                {
                    txtProjectUsageSummary.Text = $"项目使用统计（时间段：{timeRangeText}）";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成报表时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 生成墨盒使用统计报表
        /// </summary>
        private void GenerateCartridgeUsageReport(DateTime startDate, DateTime endDate)
        {
            try
            {
                Console.WriteLine($"正在生成墨盒使用统计报表，时间范围：{startDate:yyyy-MM-dd} 至 {endDate:yyyy-MM-dd}");
                
                // 获取墨盒使用统计数据
                currentReportData = DatabaseHelper.GetCartridgeUsageStatistics(startDate, endDate);
                
                // 检查数据表结构
                if (currentReportData == null || currentReportData.Rows.Count == 0)
                {
                    Console.WriteLine("没有获取到墨盒使用统计数据");
                    MessageBox.Show("指定时间段内没有墨盒使用记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    dgCartridgeUsage.ItemsSource = null;
                    return;
                }
                
                Console.WriteLine($"获取到{currentReportData.Rows.Count}条墨盒使用统计数据");
                
                if (!currentReportData.Columns.Contains("CurrentStock") || 
                    !currentReportData.Columns.Contains("MinimumStock"))
                {
                    MessageBox.Show("无法生成报表：数据表缺少必要的列。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // 将DataTable转换为集合以支持绑定库存状态
                _cartridgeFullData = new List<dynamic>();
                
                foreach (DataRow row in currentReportData.Rows)
                {
                    // 使用安全的转换方法
                    int currentStock = 0;
                    int minimumStock = 0;
                    
                    if (currentReportData.Columns.Contains("CurrentStock") && row["CurrentStock"] != DBNull.Value)
                    {
                        currentStock = Convert.ToInt32(row["CurrentStock"]);
                    }
                    
                    if (currentReportData.Columns.Contains("MinimumStock") && row["MinimumStock"] != DBNull.Value)
                    {
                        minimumStock = Convert.ToInt32(row["MinimumStock"]);
                    }
                    
                    string stockStatus;
                    if (currentStock <= 0)
                    {
                        stockStatus = "无库存";
                    }
                    else if (currentStock < minimumStock)
                    {
                        stockStatus = "库存不足";
                    }
                    else
                    {
                        stockStatus = "库存正常";
                    }
                    
                    dynamic item = new System.Dynamic.ExpandoObject();
                    
                    // 安全地获取各列值
                    if (currentReportData.Columns.Contains("CartridgeId") && row["CartridgeId"] != DBNull.Value)
                    {
                        item.CartridgeId = Convert.ToInt32(row["CartridgeId"]);
                    }
                    else
                    {
                        item.CartridgeId = 0;
                    }
                    
                    item.Color = row["Color"] != DBNull.Value ? row["Color"].ToString() : "-";
                    item.Model = row["Model"] != DBNull.Value ? row["Model"].ToString() : "-";
                    item.TotalIn = currentReportData.Columns.Contains("TotalIn") && row["TotalIn"] != DBNull.Value ? 
                        Convert.ToInt32(row["TotalIn"]) : 0;
                    item.TotalOut = currentReportData.Columns.Contains("TotalOut") && row["TotalOut"] != DBNull.Value ? 
                        Convert.ToInt32(row["TotalOut"]) : 0;
                    item.CurrentStock = currentStock;
                    item.MinimumStock = minimumStock;
                    item.StockStatus = stockStatus;
                    
                    _cartridgeFullData.Add(item);
                }
                
                // 设置分页信息
                _cartridgeTotalRecords = _cartridgeFullData.Count;
                _cartridgeTotalPages = (_cartridgeTotalRecords + _cartridgePageSize - 1) / _cartridgePageSize;
                _cartridgeCurrentPage = 1;
                
                // 更新页码信息显示
                UpdateCartridgePagingInfo();
                
                // 加载第一页数据
                LoadCartridgePageData();
                
                // 显示标签页
                tabReports.SelectedItem = tabCartridgeUsage;
                
                Console.WriteLine("墨盒使用统计报表生成完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成墨盒使用统计报表时出错: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"生成墨盒使用统计报表时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 加载墨盒使用统计的当前页数据
        /// </summary>
        private void LoadCartridgePageData()
        {
            try
            {
                if (_cartridgeFullData == null || _cartridgeFullData.Count == 0)
                {
                    dgCartridgeUsage.ItemsSource = null;
                    return;
                }
                
                // 计算当前页数据的起始和结束索引
                int startIndex = (_cartridgeCurrentPage - 1) * _cartridgePageSize;
                int endIndex = Math.Min(startIndex + _cartridgePageSize, _cartridgeTotalRecords);
                
                // 获取当前页的数据
                var pageData = _cartridgeFullData.Skip(startIndex).Take(_cartridgePageSize).ToList();
                
                // 设置数据源
                dgCartridgeUsage.ItemsSource = pageData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载墨盒使用统计页面数据时出错: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"加载墨盒使用统计页面数据时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 更新墨盒统计分页信息显示
        /// </summary>
        private void UpdateCartridgePagingInfo()
        {
            txtCartridgeCurrentPage.Text = _cartridgeCurrentPage.ToString();
            txtCartridgeTotalPages.Text = _cartridgeTotalPages.ToString();
            
            // 根据当前页码设置按钮状态
            btnCartridgeFirstPage.IsEnabled = _cartridgeCurrentPage > 1;
            btnCartridgePrevPage.IsEnabled = _cartridgeCurrentPage > 1;
            btnCartridgeNextPage.IsEnabled = _cartridgeCurrentPage < _cartridgeTotalPages;
        }
        
        /// <summary>
        /// 墨盒统计首页按钮点击事件
        /// </summary>
        private void btnCartridgeFirstPage_Click(object sender, RoutedEventArgs e)
        {
            if (_cartridgeCurrentPage > 1)
            {
                _cartridgeCurrentPage = 1;
                LoadCartridgePageData();
                UpdateCartridgePagingInfo();
            }
        }
        
        /// <summary>
        /// 墨盒统计上一页按钮点击事件
        /// </summary>
        private void btnCartridgePrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_cartridgeCurrentPage > 1)
            {
                _cartridgeCurrentPage--;
                LoadCartridgePageData();
                UpdateCartridgePagingInfo();
            }
        }
        
        /// <summary>
        /// 墨盒统计下一页按钮点击事件
        /// </summary>
        private void btnCartridgeNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_cartridgeCurrentPage < _cartridgeTotalPages)
            {
                _cartridgeCurrentPage++;
                LoadCartridgePageData();
                UpdateCartridgePagingInfo();
            }
        }
        
        /// <summary>
        /// 墨盒统计每页显示条数变更事件
        /// </summary>
        private void cmbCartridgePageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCartridgePageSize.SelectedItem != null && IsInitialized)
            {
                ComboBoxItem selectedItem = cmbCartridgePageSize.SelectedItem as ComboBoxItem;
                if (selectedItem != null && int.TryParse(selectedItem.Content.ToString(), out int newPageSize))
                {
                    _cartridgePageSize = newPageSize;
                    _cartridgeCurrentPage = 1; // 切换每页条数时重置为第一页
                    _cartridgeTotalPages = (_cartridgeTotalRecords + _cartridgePageSize - 1) / _cartridgePageSize;
                    LoadCartridgePageData();
                    UpdateCartridgePagingInfo();
                }
            }
        }

        /// <summary>
        /// 生成项目使用统计报表
        /// </summary>
        private void GenerateProjectUsageReport(DateTime startDate, DateTime endDate)
        {
            try
            {
                Console.WriteLine($"正在生成项目使用统计报表，时间范围：{startDate:yyyy-MM-dd} 至 {endDate:yyyy-MM-dd}");
                
                // 获取项目使用统计数据
                currentReportData = DatabaseHelper.GetProjectUsageStatistics(startDate, endDate);
                
                // 检查数据表是否为空
                if (currentReportData == null || currentReportData.Rows.Count == 0)
                {
                    Console.WriteLine("没有获取到项目使用统计数据");
                    MessageBox.Show("指定时间段内没有项目使用记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    dgProjectUsage.ItemsSource = null;
                    return;
                }
                
                Console.WriteLine($"获取到{currentReportData.Rows.Count}条项目使用统计数据");
                
                // 检查数据表中是否含有所需列
                if (!currentReportData.Columns.Contains("Project") || 
                    (!currentReportData.Columns.Contains("TotalUsage") && !currentReportData.Columns.Contains("TotalQuantity")))
                {
                    MessageBox.Show("无法生成项目使用统计报表：数据表结构不符合要求。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // 计算总消耗量以计算百分比
                int totalConsumption = 0;
                foreach (DataRow row in currentReportData.Rows)
                {
                    if (currentReportData.Columns.Contains("TotalUsage") && row["TotalUsage"] != DBNull.Value)
                    {
                        totalConsumption += Convert.ToInt32(row["TotalUsage"]);
                    }
                    else if (currentReportData.Columns.Contains("TotalQuantity") && row["TotalQuantity"] != DBNull.Value)
                    {
                        totalConsumption += Convert.ToInt32(row["TotalQuantity"]);
                    }
                }
                
                // 将DataTable转换为集合以支持绑定百分比和平均值
                _projectFullData = new List<dynamic>();
                
                foreach (DataRow row in currentReportData.Rows)
                {
                    // 使用ExpandoObject便于动态添加属性
                    dynamic item = new System.Dynamic.ExpandoObject();
                    
                    // 安全获取值
                    string project = row["Project"] != DBNull.Value ? row["Project"].ToString() : "未指定项目";
                    item.Project = project;
                    
                    // 获取总使用量，兼容不同的列名
                    int totalQuantity = 0;
                    if (currentReportData.Columns.Contains("TotalUsage") && row["TotalUsage"] != DBNull.Value)
                    {
                        totalQuantity = Convert.ToInt32(row["TotalUsage"]);
                    }
                    else if (currentReportData.Columns.Contains("TotalQuantity") && row["TotalQuantity"] != DBNull.Value)
                    {
                        totalQuantity = Convert.ToInt32(row["TotalQuantity"]);
                    }
                    item.TotalQuantity = totalQuantity;
                    
                    // 获取记录数
                    int recordCount = 1; // 默认至少有一条记录
                    if (currentReportData.Columns.Contains("RecordCount") && row["RecordCount"] != DBNull.Value)
                    {
                        recordCount = Convert.ToInt32(row["RecordCount"]);
                    }
                    item.RecordCount = recordCount;
                    
                    // 计算平均值
                    item.AverageQuantity = recordCount > 0 ? Math.Round((double)totalQuantity / recordCount, 2) : 0;
                    
                    // 计算百分比
                    decimal percentage = totalConsumption > 0 ? (decimal)totalQuantity / totalConsumption : 0;
                    item.Percentage = percentage;
                    
                    // 获取最后使用时间
                    if (currentReportData.Columns.Contains("LastUsageTime") && row["LastUsageTime"] != DBNull.Value)
                    {
                        item.LastUsageTime = Convert.ToDateTime(row["LastUsageTime"]);
                    }
                    else
                    {
                        item.LastUsageTime = endDate; // 使用查询的结束日期作为默认值
                    }
                    
                    // 添加表格显示需要的颜色和型号
                    if (currentReportData.Columns.Contains("Color") && row["Color"] != DBNull.Value)
                    {
                        item.Color = row["Color"].ToString();
                    }
                    else
                    {
                        item.Color = "-";
                    }
                    
                    if (currentReportData.Columns.Contains("Model") && row["Model"] != DBNull.Value)
                    {
                        item.Model = row["Model"].ToString();
                    }
                    else
                    {
                        item.Model = "-";
                    }
                    
                    _projectFullData.Add(item);
                }
                
                // 设置分页信息
                _projectTotalRecords = _projectFullData.Count;
                _projectTotalPages = (_projectTotalRecords + _projectPageSize - 1) / _projectPageSize;
                _projectCurrentPage = 1;
                
                // 更新页码信息显示
                UpdateProjectPagingInfo();
                
                // 加载第一页数据
                LoadProjectPageData();
                
                // 显示标签页
                tabReports.SelectedItem = tabProjectUsage;
                
                Console.WriteLine("项目使用统计报表生成完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成项目使用统计报表时出错: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"生成项目使用统计报表时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 加载项目使用统计的当前页数据
        /// </summary>
        private void LoadProjectPageData()
        {
            try
            {
                if (_projectFullData == null || _projectFullData.Count == 0)
                {
                    dgProjectUsage.ItemsSource = null;
                    return;
                }
                
                // 计算当前页数据的起始和结束索引
                int startIndex = (_projectCurrentPage - 1) * _projectPageSize;
                int endIndex = Math.Min(startIndex + _projectPageSize, _projectTotalRecords);
                
                // 获取当前页的数据
                var pageData = _projectFullData.Skip(startIndex).Take(_projectPageSize).ToList();
                
                // 设置数据源
                dgProjectUsage.ItemsSource = pageData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载项目使用统计页面数据时出错: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"加载项目使用统计页面数据时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 更新项目统计分页信息显示
        /// </summary>
        private void UpdateProjectPagingInfo()
        {
            txtProjectCurrentPage.Text = _projectCurrentPage.ToString();
            txtProjectTotalPages.Text = _projectTotalPages.ToString();
            
            // 根据当前页码设置按钮状态
            btnProjectFirstPage.IsEnabled = _projectCurrentPage > 1;
            btnProjectPrevPage.IsEnabled = _projectCurrentPage > 1;
            btnProjectNextPage.IsEnabled = _projectCurrentPage < _projectTotalPages;
        }
        
        /// <summary>
        /// 项目统计首页按钮点击事件
        /// </summary>
        private void btnProjectFirstPage_Click(object sender, RoutedEventArgs e)
        {
            if (_projectCurrentPage > 1)
            {
                _projectCurrentPage = 1;
                LoadProjectPageData();
                UpdateProjectPagingInfo();
            }
        }
        
        /// <summary>
        /// 项目统计上一页按钮点击事件
        /// </summary>
        private void btnProjectPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_projectCurrentPage > 1)
            {
                _projectCurrentPage--;
                LoadProjectPageData();
                UpdateProjectPagingInfo();
            }
        }
        
        /// <summary>
        /// 项目统计下一页按钮点击事件
        /// </summary>
        private void btnProjectNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_projectCurrentPage < _projectTotalPages)
            {
                _projectCurrentPage++;
                LoadProjectPageData();
                UpdateProjectPagingInfo();
            }
        }
        
        /// <summary>
        /// 项目统计每页显示条数变更事件
        /// </summary>
        private void cmbProjectPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProjectPageSize.SelectedItem != null && IsInitialized)
            {
                ComboBoxItem selectedItem = cmbProjectPageSize.SelectedItem as ComboBoxItem;
                if (selectedItem != null && int.TryParse(selectedItem.Content.ToString(), out int newPageSize))
                {
                    _projectPageSize = newPageSize;
                    _projectCurrentPage = 1; // 切换每页条数时重置为第一页
                    _projectTotalPages = (_projectTotalRecords + _projectPageSize - 1) / _projectPageSize;
                    LoadProjectPageData();
                    UpdateProjectPagingInfo();
                }
            }
        }

        /// <summary>
        /// 导出Excel按钮点击事件
        /// </summary>
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否有数据可以导出
                if (currentReportData == null || currentReportData.Rows.Count == 0)
                {
                    // 首先检查当前显示的DataGrid是否有数据
                    var currentTab = tabReports.SelectedItem;
                    var hasData = false;
                    
                    if (currentTab == tabCartridgeUsage && dgCartridgeUsage.Items.Count > 0)
                    {
                        hasData = true;
                    }
                    else if (currentTab == tabProjectUsage && dgProjectUsage.Items.Count > 0)
                    {
                        hasData = true;
                    }
                    
                    if (hasData)
                    {
                        // 如果界面上有数据但currentReportData为空，尝试重新生成报表
                        MessageBox.Show("报表数据尚未准备好导出，正在尝试重新生成报表，请稍候再试。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        GenerateReport(); // 重新生成报表以更新currentReportData
                        
                        // 再次检查currentReportData
                        if (currentReportData == null || currentReportData.Rows.Count == 0)
                        {
                            MessageBox.Show("无法准备导出数据，请先生成报表。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("没有可导出的数据，请先生成报表。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                
                Console.WriteLine($"准备导出报表数据，共{currentReportData.Rows.Count}行");
                
                // 创建保存文件对话框
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                // 提供Excel格式选项
                saveFileDialog.Filter = "Excel文件 (*.xlsx)|*.xlsx";
                
                string reportTypeName = currentReportType == 1 ? "墨盒使用统计" : "项目使用统计";
                saveFileDialog.Title = $"保存{reportTypeName}报表";
                saveFileDialog.FileName = $"{reportTypeName}_{DateTime.Now:yyyyMMdd}";
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    // 获取选择的文件路径
                    string filePath = saveFileDialog.FileName;
                    
                    // 确保扩展名是.xlsx
                    if (!filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        filePath = System.IO.Path.ChangeExtension(filePath, ".xlsx");
                    }
                    
                    // 导出到Excel
                    bool success = ExportToExcel(currentReportData, filePath);
                    
                    if (success)
                    {
                        MessageBox.Show($"报表导出成功！\n文件已保存至：{filePath}", 
                                        "导出结果", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // 打开文件所在目录
                        try
                        {
                            string directoryPath = System.IO.Path.GetDirectoryName(filePath);
                            System.Diagnostics.Process.Start("explorer.exe", directoryPath);
                        }
                        catch (Exception)
                        {
                            // 忽略打开目录的错误
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出报表时出错: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"导出报表时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出数据表到Excel文件(使用EPPlus)
        /// </summary>
        private bool ExportToExcel(DataTable dataTable, string filePath)
        {
            try
            {
                // 确保有数据可导出
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    MessageBox.Show("没有数据可导出", "导出失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Console.WriteLine("导出失败：数据表为空");
                    return false;
                }
                
                Console.WriteLine($"准备导出Excel，共{dataTable.Rows.Count}行数据...");
                
                // 列名映射（英文到中文）
                Dictionary<string, string> columnMapping = new Dictionary<string, string>
                {
                    // 墨盒使用统计报表列名映射
                    { "CartridgeId", "墨盒ID" },
                    { "Color", "颜色" },
                    { "Model", "型号" },
                    { "CurrentStock", "当前库存" },
                    { "MinimumStock", "最低库存" },
                    { "TotalIn", "入库总量" },
                    { "TotalOut", "出库总量" },
                    
                    // 项目使用统计报表列名映射
                    { "Project", "项目名称" },
                    { "TotalUsage", "使用总量" },
                    { "TotalQuantity", "使用总量" },
                    { "RecordCount", "记录次数" },
                    { "AverageQuantity", "平均数量" },
                    { "Percentage", "占比" },
                    { "LastUsageTime", "最后使用时间" }
                };
                
                // 设置EPPlus许可证
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                FileInfo excelFile = new FileInfo(filePath);
                
                // 确保目录存在
                if (!excelFile.Directory.Exists) excelFile.Directory.Create();
                
                using (ExcelPackage package = new ExcelPackage(excelFile))
                {
                    // 创建工作表
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("报表");
                    
                    // 写入列标题（用中文名称）
                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        string columnName = dataTable.Columns[col].ColumnName;
                        string displayName = columnMapping.ContainsKey(columnName) ? 
                            columnMapping[columnName] : columnName;
                        
                        worksheet.Cells[1, col + 1].Value = displayName;
                        worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                    }
                    
                    // 写入数据行
                    for (int row = 0; row < dataTable.Rows.Count; row++)
                    {
                        for (int col = 0; col < dataTable.Columns.Count; col++)
                        {
                            object value = dataTable.Rows[row][col];
                            
                            // 特殊处理不同类型的数据
                            if (value is DateTime dtValue)
                            {
                                worksheet.Cells[row + 2, col + 1].Value = dtValue;
                                worksheet.Cells[row + 2, col + 1].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                            }
                            else if (value is decimal decimalValue && 
                                     dataTable.Columns[col].ColumnName == "Percentage")
                            {
                                worksheet.Cells[row + 2, col + 1].Value = decimalValue;
                                worksheet.Cells[row + 2, col + 1].Style.Numberformat.Format = "0.00%";
                            }
                            else
                            {
                                worksheet.Cells[row + 2, col + 1].Value = value;
                            }
                        }
                    }
                    
                    // 自动调整列宽
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    
                    // 添加表格样式
                    var tableRange = worksheet.Cells[1, 1, dataTable.Rows.Count + 1, dataTable.Columns.Count];
                    var table = worksheet.Tables.Add(tableRange, "ReportTable");
                    table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;
                    
                    // 冻结顶部标题行
                    worksheet.View.FreezePanes(2, 1);
                    
                    // 保存文件
                    package.Save();
                }
                
                Console.WriteLine($"Excel文件导出成功: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出Excel文件时出错: {ex.Message}\n文件路径: {filePath}\n{ex.StackTrace}");
                MessageBox.Show($"导出文件时出错: {ex.Message}\n文件路径: {filePath}", 
                                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
} 