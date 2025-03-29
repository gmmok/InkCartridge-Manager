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
using OfficeOpenXml;
using 爱普生墨盒管理系统.Models;
using 爱普生墨盒管理系统.Utils;

namespace 爱普生墨盒管理系统.Views
{
    /// <summary>
    /// RecordQueryPage.xaml 的交互逻辑
    /// </summary>
    public partial class RecordQueryPage : Page
    {
        private List<StockRecord> queryResults = new List<StockRecord>();

        public RecordQueryPage()
        {
            InitializeComponent();
            LoadCartridges();
            InitializeDatePickers();
        }

        /// <summary>
        /// 加载墨盒下拉列表
        /// </summary>
        private void LoadCartridges()
        {
            try
            {
                var cartridges = DatabaseHelper.GetAllCartridges();
                
                // 清空现有项并添加"全部"选项
                cmbCartridge.Items.Clear();
                var allItem = new ComboBoxItem { Content = "全部", Tag = 0 };
                cmbCartridge.Items.Add(allItem);
                cmbCartridge.SelectedItem = allItem;
                
                // 添加墨盒项
                foreach (var cartridge in cartridges)
                {
                    var cbi = new ComboBoxItem 
                    { 
                        Content = $"{cartridge.Color} {cartridge.Model}",
                        Tag = cartridge.Id
                    };
                    cmbCartridge.Items.Add(cbi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载墨盒数据时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化日期选择器
        /// </summary>
        private void InitializeDatePickers()
        {
            // 默认查询当前月份的数据
            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            
            dpStartDate.SelectedDate = firstDayOfMonth;
            dpEndDate.SelectedDate = lastDayOfMonth;
        }

        /// <summary>
        /// 执行查询
        /// </summary>
        private void ExecuteQuery()
        {
            try
            {
                // 获取查询条件
                int cartridgeId = 0;
                if (cmbCartridge.SelectedItem != null && cmbCartridge.SelectedItem is ComboBoxItem cartridgeItem)
                {
                    if (cartridgeItem.Tag != null)
                    {
                        cartridgeId = Convert.ToInt32(cartridgeItem.Tag);
                    }
                }

                int operationType = 0;
                if (cmbOperationType.SelectedItem != null)
                {
                    var operationItem = cmbOperationType.SelectedItem as ComboBoxItem;
                    if (operationItem != null)
                    {
                        operationType = Convert.ToInt32(operationItem.Tag);
                    }
                }

                DateTime? startDate = dpStartDate.SelectedDate;
                DateTime? endDate = dpEndDate.SelectedDate;

                // 执行查询
                var records = DatabaseHelper.QueryStockRecords(cartridgeId, operationType, startDate, endDate);
                
                // 更新queryResults变量，用于导出Excel
                queryResults = records;

                // 处理查询结果
                var displayRecords = records.Select(r => new
                {
                    r.Id,
                    CartridgeInfo = $"{r.Cartridge.Color} {r.Cartridge.Model}",
                    r.OperationTypeText,
                    Quantity = r.OperationType == 2 ? Math.Abs(r.Quantity) : r.Quantity, // 出库显示正数
                    r.Operator,
                    r.Project,
                    r.OperationTime,
                    r.Notes
                }).ToList();

                // 更新UI
                dgRecords.ItemsSource = displayRecords;
                txtResultCount.Text = $"查询结果 ({displayRecords.Count}条记录)";
                
                // 记录日志
                Console.WriteLine($"查询到{displayRecords.Count}条记录，queryResults包含{queryResults.Count}条数据");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行查询时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 查询按钮点击事件
        /// </summary>
        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            ExecuteQuery();
        }

        /// <summary>
        /// 重置按钮点击事件
        /// </summary>
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 重置筛选条件
                cmbCartridge.SelectedIndex = 0;
                cmbOperationType.SelectedIndex = 0;
                
                // 重置日期范围为当前月
                InitializeDatePickers();
                
                // 清空结果
                dgRecords.ItemsSource = null;
                txtResultCount.Text = "查询结果 (0条记录)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置查询条件时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (queryResults == null || queryResults.Count == 0)
                {
                    // 如果查询结果为空，但DataGrid有数据，可能是queryResults没有更新
                    var dataSource = dgRecords.ItemsSource;
                    if (dataSource != null && dataSource is IEnumerable<object> items && items.Count() > 0)
                    {
                        MessageBox.Show("查询结果尚未准备好导出，请先点击查询按钮。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("没有可导出的数据，请先执行查询。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }

                // 创建保存文件对话框
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx",
                    Title = "保存墨盒记录",
                    FileName = $"墨盒记录导出_{DateTime.Now:yyyyMMdd}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    // 创建数据表
                    DataTable dt = new DataTable("墨盒记录");
                    dt.Columns.Add("ID", typeof(int));
                    dt.Columns.Add("墨盒型号", typeof(string));
                    dt.Columns.Add("操作类型", typeof(string));
                    dt.Columns.Add("数量", typeof(int));
                    dt.Columns.Add("操作人员", typeof(string));
                    dt.Columns.Add("相关项目", typeof(string));
                    dt.Columns.Add("操作时间", typeof(DateTime));
                    dt.Columns.Add("备注", typeof(string));

                    // 使用存储的查询结果填充数据表，而不是直接从DataGrid提取
                    foreach (var record in queryResults)
                    {
                        dt.Rows.Add(
                            record.Id,
                            $"{record.Cartridge.Color} {record.Cartridge.Model}",
                            record.OperationTypeText,
                            record.OperationType == 2 ? Math.Abs(record.Quantity) : record.Quantity, // 出库显示正数
                            record.Operator,
                            record.Project,
                            record.OperationTime,
                            record.Notes
                        );
                    }

                    // 导出到Excel
                    bool success = ExportToExcel(dt, filePath);

                    if (success)
                    {
                        MessageBox.Show($"数据已成功导出到：\n{filePath}", "导出成功", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出数据表到Excel文件
        /// </summary>
        private bool ExportToExcel(DataTable dataTable, string filePath)
        {
            try
            {
                // 设置 EPPlus 许可证（社区版无需许可证）
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                FileInfo excelFile = new FileInfo(filePath);

                // 确保目录存在
                if (!excelFile.Directory.Exists) excelFile.Directory.Create();

                using (ExcelPackage package = new ExcelPackage(excelFile))
                {
                    // 创建工作表
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("墨盒记录");

                    // 写入列头（加粗样式）
                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        worksheet.Cells[1, col + 1].Value = dataTable.Columns[col].ColumnName;
                        worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                    }

                    // 写入数据
                    for (int row = 0; row < dataTable.Rows.Count; row++)
                    {
                        for (int col = 0; col < dataTable.Columns.Count; col++)
                        {
                            object value = dataTable.Rows[row][col];

                            // 处理日期格式
                            if (value is DateTime dtValue)
                            {
                                worksheet.Cells[row + 2, col + 1].Value = dtValue;
                                worksheet.Cells[row + 2, col + 1].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                            }
                            else
                            {
                                worksheet.Cells[row + 2, col + 1].Value = value;
                            }
                        }
                    }

                    // 自动调整列宽
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    
                    // 添加表格样式和筛选功能
                    if (dataTable.Rows.Count > 0)
                    {
                        var tableRange = worksheet.Cells[1, 1, dataTable.Rows.Count + 1, dataTable.Columns.Count];
                        var table = worksheet.Tables.Add(tableRange, "RecordsTable");
                        table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;
                        
                        // 启用筛选功能
                        table.ShowFilter = true;
                        
                        // 冻结顶部标题行，便于滚动时保持标题可见
                        worksheet.View.FreezePanes(2, 1);
                    }

                    // 保存文件
                    package.Save();
                }
                
                // 记录导出成功信息
                Console.WriteLine($"成功导出{dataTable.Rows.Count}条墨盒记录到Excel: {filePath}");
                
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel导出错误：{ex.Message}");
                Console.WriteLine($"导出Excel失败: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
    }
} 