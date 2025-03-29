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
using 爱普生墨盒管理系统.Models;
using 爱普生墨盒管理系统.Utils;
using System.Windows.Threading;
using 爱普生墨盒管理系统.Views;
using 爱普生墨盒管理系统.Interfaces;

namespace 爱普生墨盒管理系统.Views
{
    /// <summary>
    /// StockInPage.xaml 的交互逻辑
    /// </summary>
    public partial class StockInPage : Page, IRefreshable
    {
        private DispatcherTimer _timer;
        
        // 分页相关属性
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalRecords = 0;
        private int _totalPages = 1;
        
        public StockInPage()
        {
            InitializeComponent();
            
            // 初始化定时器
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            // 设置当前日期和时间
            dpOperationDate.SelectedDate = DateTime.Today;
            UpdateCurrentTime();
            
            LoadCartridges();
            LoadOperators();
            LoadRecentRecords();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateCurrentTime();
        }

        private void UpdateCurrentTime()
        {
            txtOperationTime.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        /// <summary>
        /// 加载墨盒下拉列表
        /// </summary>
        private void LoadCartridges()
        {
            try
            {
                var cartridges = DatabaseHelper.GetAllCartridges();
                
                // 创建带有显示名称的墨盒列表
                var cartridgeItems = cartridges.Select(c => new
                {
                    Id = c.Id,
                    Color = c.Color,
                    Model = c.Model,
                    CurrentStock = c.CurrentStock,
                    DisplayName = $"{c.Color} {c.Model} (库存: {c.CurrentStock})"
                }).ToList();
                
                cmbCartridge.ItemsSource = cartridgeItems;
                
                if (cartridgeItems.Count > 0)
                {
                    cmbCartridge.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载墨盒数据时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载操作人员下拉列表
        /// </summary>
        private void LoadOperators()
        {
            try
            {
                // 获取操作人员列表
                var operators = DatabaseHelper.GetOperators();
                
                // 设置下拉列表数据源
                cmbOperator.ItemsSource = operators;
                
                // 选择最后使用的操作人员
                string lastUsedOperator = DatabaseHelper.GetLastUsedOperator();
                if (!string.IsNullOrEmpty(lastUsedOperator))
                {
                    cmbOperator.Text = lastUsedOperator;
                }
                
                // 为ComboBox添加KeyDown事件处理程序
                cmbOperator.KeyDown += CmbOperator_KeyDown;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载操作人员列表时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 操作人员ComboBox的键盘事件处理
        /// </summary>
        private void CmbOperator_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string operatorName = cmbOperator.Text.Trim();
                
                // 验证操作人员名称非空
                if (!string.IsNullOrWhiteSpace(operatorName))
                {
                    // 更新操作人员数据
                    DatabaseHelper.UpdateOperator(operatorName);
                    
                    // 重新加载操作人员列表
                    LoadOperators();
                    
                    // 将焦点移动到下一个控件
                    txtProject.Focus();
                }
                
                // 标记事件已处理
                e.Handled = true;
            }
        }

        /// <summary>
        /// 加载最近的入库记录（分页）
        /// </summary>
        private void LoadRecentRecords()
        {
            try
            {
                // 计算要跳过的记录数，用于分页
                int skip = (_currentPage - 1) * _pageSize;
                
                // 首先获取总记录数
                _totalRecords = DatabaseHelper.GetStockRecordCount(1); // 1表示入库
                
                // 计算总页数
                _totalPages = (_totalRecords + _pageSize - 1) / _pageSize;
                
                // 更新页码信息显示
                UpdatePagingInfo();
                
                // 获取当前页的入库记录（类型1表示入库）
                var recentRecords = DatabaseHelper.QueryStockRecords(skip, 1, null, null)
                    .Take(_pageSize).ToList();
                
                // 为最近记录添加墨盒信息
                var recordsWithInfo = recentRecords.Select(r => new
                {
                    r.Id,
                    r.CartridgeId,
                    CartridgeInfo = $"{r.Cartridge.Color} {r.Cartridge.Model}",
                    r.Quantity,
                    r.Operator,
                    r.Project,
                    r.OperationTime,
                    r.Notes
                }).ToList();
                
                dgRecentRecords.ItemsSource = recordsWithInfo;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载入库记录时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新分页信息显示
        /// </summary>
        private void UpdatePagingInfo()
        {
            txtCurrentPage.Text = _currentPage.ToString();
            txtTotalPages.Text = _totalPages.ToString();
            
            // 根据当前页码设置按钮状态
            btnFirstPage.IsEnabled = _currentPage > 1;
            btnPrevPage.IsEnabled = _currentPage > 1;
            btnNextPage.IsEnabled = _currentPage < _totalPages;
        }
        
        /// <summary>
        /// 首页按钮点击事件
        /// </summary>
        private void btnFirstPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage = 1;
                LoadRecentRecords();
            }
        }
        
        /// <summary>
        /// 上一页按钮点击事件
        /// </summary>
        private void btnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadRecentRecords();
            }
        }
        
        /// <summary>
        /// 下一页按钮点击事件
        /// </summary>
        private void btnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadRecentRecords();
            }
        }
        
        /// <summary>
        /// 每页显示条数变更事件
        /// </summary>
        private void cmbPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPageSize.SelectedItem != null && IsInitialized)
            {
                ComboBoxItem selectedItem = cmbPageSize.SelectedItem as ComboBoxItem;
                if (selectedItem != null && int.TryParse(selectedItem.Content.ToString(), out int newPageSize))
                {
                    _pageSize = newPageSize;
                    _currentPage = 1; // 切换每页条数时重置为第一页
                    LoadRecentRecords();
                }
            }
        }

        /// <summary>
        /// 墨盒选择变更事件
        /// </summary>
        private void cmbCartridge_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCartridge.SelectedItem != null)
            {
                dynamic selectedItem = cmbCartridge.SelectedItem;
                txtCurrentStock.Text = selectedItem.CurrentStock.ToString();
            }
        }

        /// <summary>
        /// 提交入库按钮点击事件
        /// </summary>
        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (cmbCartridge.SelectedItem == null)
                {
                    MessageBox.Show("请选择墨盒", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbCartridge.Focus();
                    return;
                }
                
                // 验证数量
                if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
                {
                    MessageBox.Show("入库数量必须是大于0的整数", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtQuantity.Focus();
                    return;
                }
                
                // 验证日期
                if (!dpOperationDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("请选择入库日期", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    dpOperationDate.Focus();
                    return;
                }
                
                // 验证操作人员
                if (string.IsNullOrWhiteSpace(cmbOperator.Text))
                {
                    MessageBox.Show("请输入操作人员", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbOperator.Focus();
                    return;
                }
                
                // 获取选中的墨盒
                dynamic selectedItem = cmbCartridge.SelectedItem;
                int cartridgeId = selectedItem.Id;
                
                // 获取操作人员并更新使用情况
                string operatorName = cmbOperator.Text.Trim();
                DatabaseHelper.UpdateOperator(operatorName);
                
                // 获取完整的日期时间
                var selectedDate = dpOperationDate.SelectedDate ?? DateTime.Now;
                var currentTime = DateTime.Now.TimeOfDay;
                var operationDateTime = selectedDate.Date.Add(currentTime);

                // 创建入库记录
                var record = new StockRecord
                {
                    CartridgeId = cartridgeId,
                    OperationType = 1, // 1表示入库
                    Quantity = quantity,
                    OperationTime = operationDateTime,
                    Operator = operatorName,
                    Project = txtProject.Text.Trim(),
                    Notes = txtNotes.Text.Trim()
                };
                
                // 保存入库记录
                if (DatabaseHelper.AddStockRecord(record))
                {
                    MessageBox.Show("墨盒入库成功", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // 重置表单
                    txtQuantity.Text = "1";
                    // 不清空操作人员字段，保留当前选择
                    txtProject.Text = string.Empty;
                    txtNotes.Text = string.Empty;
                    dpOperationDate.SelectedDate = DateTime.Now;
                    
                    // 重新加载数据
                    LoadCartridges();
                    _currentPage = 1; // 重置到第一页
                    LoadRecentRecords();
                    // 重新加载操作人员列表，确保新添加的操作人员在列表中
                    LoadOperators();
                }
                else
                {
                    MessageBox.Show("墨盒入库失败", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存入库记录时发生错误：{ex.Message}\n\n详细信息：{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 实现IRefreshable接口的Refresh方法
        /// </summary>
        public void Refresh()
        {
            try
            {
                // 重新加载页面所需的所有数据
                LoadInitialData();
            }
            catch (Exception ex)
            {
                // 记录异常信息
                Console.WriteLine($"刷新入库页面时出错: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                
                MessageBox.Show($"刷新页面数据时出错: {ex.Message}", 
                    "刷新错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载页面初始数据
        /// </summary>
        public void LoadInitialData()
        {
            // 设置当前日期和时间
            dpOperationDate.SelectedDate = DateTime.Today;
            UpdateCurrentTime();
            
            // 加载相关数据
            LoadCartridges();
            LoadOperators();
            LoadRecentRecords();
        }
    }
} 