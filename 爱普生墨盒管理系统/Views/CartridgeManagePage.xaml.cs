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

namespace 爱普生墨盒管理系统.Views
{
    /// <summary>
    /// CartridgeManagePage.xaml 的交互逻辑
    /// </summary>
    public partial class CartridgeManagePage : Page
    {
        private List<Cartridge> allCartridges = new List<Cartridge>();
        private List<DatabaseHelper.CartridgeColor> allCartridgeColors = new List<DatabaseHelper.CartridgeColor>();
        private int currentEditId = 0;
        private bool isAddMode = false;

        public CartridgeManagePage()
        {
            InitializeComponent();
            
            // 注册页面加载完成事件，确保所有控件都已初始化
            this.Loaded += CartridgeManagePage_Loaded;
            
            Console.WriteLine("墨盒管理页面构造函数执行完成");
        }
        
        /// <summary>
        /// 页面加载完成事件处理
        /// </summary>
        private void CartridgeManagePage_Loaded(object sender, RoutedEventArgs e)
        {
            try 
            {
                Console.WriteLine("墨盒管理页面Loaded事件触发，开始加载数据");
                
                // 先加载颜色数据，再加载墨盒数据，确保颜色信息可用
                LoadCartridgeColors();
                LoadCartridges();
                LoadColors();
                ApplyFilters();
                
                // 输出加载完成信息
                Console.WriteLine("墨盒管理页面数据加载完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化墨盒管理页面时出错: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"初始化墨盒管理页面时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载所有墨盒数据
        /// </summary>
        private void LoadCartridges()
        {
            try
            {
                Console.WriteLine("正在加载墨盒数据...");
                
                // 清空现有数据
                if (allCartridges == null)
                {
                    allCartridges = new List<Cartridge>();
                }
                else
                {
                    allCartridges.Clear();
                }
                
                // 从数据库加载最新数据
                var cartridges = DatabaseHelper.GetAllCartridges();
                
                if (cartridges != null && cartridges.Count > 0)
                {
                    // 添加新加载的数据
                    allCartridges.AddRange(cartridges);
                    Console.WriteLine($"成功加载 {cartridges.Count} 个墨盒数据");
                }
                else
                {
                    Console.WriteLine("数据库中没有找到墨盒数据");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载墨盒数据时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"加载墨盒数据异常: {ex.Message}\n{ex.StackTrace}");
                
                // 确保allCartridges不为null
                if (allCartridges == null)
                {
                    allCartridges = new List<Cartridge>();
                }
            }
        }

        /// <summary>
        /// 加载颜色下拉列表
        /// </summary>
        private void LoadColors()
        {
            try
            {
                // 获取不重复的颜色列表
                var colors = allCartridges.Select(c => c.Color).Distinct().OrderBy(c => c).ToList();
                
                // 清空并重新添加颜色选项
                cmbColor.Items.Clear();
                cmbColor.Items.Add(new ComboBoxItem { Content = "全部", IsSelected = true });
                
                foreach (var color in colors)
                {
                    cmbColor.Items.Add(new ComboBoxItem { Content = color });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载颜色列表时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载墨盒颜色数据
        /// </summary>
        private void LoadCartridgeColors()
        {
            try
            {
                Console.WriteLine("正在加载墨盒颜色数据...");
                
                // 确保控件已初始化
                if (txtColor == null)
                {
                    Console.WriteLine("警告: txtColor控件为null，无法加载颜色数据");
                    return;
                }
                
                // 清空现有数据
                if (allCartridgeColors == null)
                {
                    allCartridgeColors = new List<DatabaseHelper.CartridgeColor>();
                }
                else
                {
                    allCartridgeColors.Clear();
                }
                
                // 从数据库加载所有墨盒颜色
                var colors = DatabaseHelper.GetAllCartridgeColors();
                
                if (colors != null && colors.Count > 0)
                {
                    // 过滤掉null项，防止OrderBy时出现NullReferenceException
                    colors = colors.Where(c => c != null).ToList();
                    
                    if (colors.Count > 0)
                    {
                        try
                        {
                            // 按显示顺序排序，处理可能的null属性
                            colors = colors.OrderBy(c => c.DisplayOrder).ToList();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"排序颜色时出错: {ex.Message}，将使用原始顺序");
                        }
                        
                        // 添加到集合
                        allCartridgeColors.AddRange(colors);
                        
                        // 更新下拉框
                        txtColor.ItemsSource = allCartridgeColors;
                        
                        Console.WriteLine($"成功加载 {colors.Count} 种墨盒颜色");
                    }
                    else
                    {
                        Console.WriteLine("过滤后没有有效的颜色数据");
                        txtColor.ItemsSource = null;
                    }
                }
                else
                {
                    Console.WriteLine("没有找到墨盒颜色数据");
                    txtColor.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                // 不要在这里使用MessageBox，可能会触发UI事件导致循环错误
                Console.WriteLine($"加载墨盒颜色数据异常: {ex.Message}\n{ex.StackTrace}");
                
                // 确保allCartridgeColors不为null
                if (allCartridgeColors == null)
                {
                    allCartridgeColors = new List<DatabaseHelper.CartridgeColor>();
                }
                
                // 安全地设置ItemsSource为null
                if (txtColor != null)
                {
                    try
                    {
                        txtColor.ItemsSource = null;
                    }
                    catch (Exception setEx)
                    {
                        Console.WriteLine($"设置txtColor.ItemsSource时出错: {setEx.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilters()
        {
            try
            {
                Console.WriteLine($"开始应用筛选，当前墨盒总数: {allCartridges?.Count ?? 0}");
                
                // 确保控件已初始化
                if (dgCartridges == null)
                {
                    Console.WriteLine("警告: DataGrid控件为null，无法应用筛选");
                    return;
                }
                
                // 确保allCartridges已初始化
                if (allCartridges == null)
                {
                    allCartridges = new List<Cartridge>();
                    try
                    {
                        LoadCartridges();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"加载墨盒数据时出错: {ex.Message}");
                    }
                }

                // 创建要显示的墨盒列表，过滤null值
                var filteredList = allCartridges.Where(c => c != null).ToList();
                Console.WriteLine($"过滤null项后的墨盒数量: {filteredList.Count}");
                
                // 应用颜色筛选
                if (cmbColor != null && cmbColor.SelectedIndex > 0 && cmbColor.SelectedItem is ComboBoxItem selectedColorItem)
                {
                    string selectedColor = selectedColorItem.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedColor))
                    {
                        filteredList = filteredList.Where(c => string.Equals(c.Color, selectedColor, StringComparison.OrdinalIgnoreCase)).ToList();
                        Console.WriteLine($"应用颜色筛选 '{selectedColor}'，筛选后数量: {filteredList.Count}");
                    }
                }
                
                // 应用搜索筛选
                if (txtSearch != null && !string.IsNullOrEmpty(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    filteredList = filteredList.Where(c => 
                        (c.Color?.ToLower()?.Contains(searchText) == true) ||
                        (c.Model?.ToLower()?.Contains(searchText) == true) ||
                        (c.Notes?.ToLower()?.Contains(searchText) == true)
                    ).ToList();
                    Console.WriteLine($"应用搜索筛选 '{searchText}'，筛选后数量: {filteredList.Count}");
                }
                
                // 获取所有颜色数据用于显示
                if (allCartridgeColors == null || allCartridgeColors.Count == 0)
                {
                    try
                    {
                        LoadCartridgeColors();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"加载颜色数据时出错: {ex.Message}");
                    }
                    
                    // 确保颜色集合不为null
                    if (allCartridgeColors == null)
                    {
                        allCartridgeColors = new List<DatabaseHelper.CartridgeColor>();
                    }
                }
                
                // 创建视图模型列表，添加颜色代码等信息
                var cartridgeViewModels = new List<dynamic>();
                
                foreach (var cartridge in filteredList)
                {
                    try
                    {
                        // 查找对应的颜色对象
                        var colorObj = allCartridgeColors?.FirstOrDefault(color => 
                            color != null && !string.IsNullOrEmpty(color.Name) && 
                            string.Equals(color.Name, cartridge.Color, StringComparison.OrdinalIgnoreCase));
                        
                        // 获取颜色代码，默认为白色
                        string hexColor = "#FFFFFF";
                        if (colorObj != null && !string.IsNullOrEmpty(colorObj.ColorCode))
                        {
                            hexColor = colorObj.ColorCode;
                            if (!hexColor.StartsWith("#"))
                            {
                                hexColor = "#" + hexColor;
                            }
                        }
                        
                        // 创建视图模型
                        cartridgeViewModels.Add(new 
                        {
                            cartridge.Id,
                            Color = cartridge.Color ?? "未知",
                            HexColor = hexColor,
                            Model = cartridge.Model ?? "未知",
                            cartridge.Capacity,
                            cartridge.CurrentStock,
                            cartridge.MinimumStock,
                            Notes = cartridge.Notes ?? "",
                            cartridge.UpdateTime
                        });
                    }
                    catch (Exception itemEx)
                    {
                        Console.WriteLine($"处理墨盒项时出错: {itemEx.Message}，跳过此项");
                    }
                }
                
                // 记住当前选中墨盒的ID
                int? selectedId = null;
                try
                {
                    if (dgCartridges.SelectedItem != null)
                    {
                        var selectedItem = dgCartridges.SelectedItem;
                        selectedId = (int?)selectedItem.GetType().GetProperty("Id")?.GetValue(selectedItem);
                    }
                }
                catch (Exception selEx)
                {
                    Console.WriteLine($"获取选中项ID时出错: {selEx.Message}");
                }
                
                // 更新DataGrid
                dgCartridges.ItemsSource = cartridgeViewModels;
                
                // 尝试恢复选择
                if (selectedId.HasValue)
                {
                    try
                    {
                        foreach (var item in dgCartridges.Items)
                        {
                            var id = (int?)item.GetType().GetProperty("Id")?.GetValue(item);
                            if (id.HasValue && id.Value == selectedId.Value)
                            {
                                dgCartridges.SelectedItem = item;
                                dgCartridges.ScrollIntoView(item);
                                break;
                            }
                        }
                    }
                    catch (Exception restoreEx)
                    {
                        Console.WriteLine($"恢复选中项时出错: {restoreEx.Message}");
                    }
                }
                
                Console.WriteLine($"筛选应用完成，显示墨盒数量: {cartridgeViewModels.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用筛选异常: {ex.Message}\n{ex.StackTrace}");
                
                // 在异常情况下不显示消息框，避免UI循环触发
                // 而是尝试清空DataGrid并显示空列表
                try
                {
                    if (dgCartridges != null)
                    {
                        dgCartridges.ItemsSource = new List<dynamic>();
                    }
                }
                catch
                {
                    // 忽略进一步的异常
                }
            }
        }

        /// <summary>
        /// 搜索框键入事件
        /// </summary>
        private void TxtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 颜色下拉框选择变更事件
        /// </summary>
        private void CmbColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 添加新墨盒按钮点击事件
        /// </summary>
        private void BtnAddNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 每次点击添加按钮时清空表单，便于用户从头开始添加新墨盒
                ClearDetailForm();
                
                // 显示编辑区域
                gridDetails.Visibility = Visibility.Visible;
                
                // 设置为添加模式
                isAddMode = true;
                currentEditId = 0;
                
                // 设置焦点
                txtColor.Focus();
                
                // 显示提示信息
                MessageBox.Show("您可以从列表中点击任意一行墨盒，将其信息加载为模板快速添加新墨盒。", 
                    "操作提示", MessageBoxButton.OK, MessageBoxImage.Information);
                
                Console.WriteLine("进入添加新墨盒模式，已清空表单");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"准备添加墨盒时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 清空详情表单
        /// </summary>
        private void ClearDetailForm()
        {
            txtColor.SelectedIndex = -1;
            txtModel.Text = string.Empty;
            txtCapacity.Text = "0";
            txtCurrentStock.Text = "0";
            txtMinimumStock.Text = "0";
            txtNotes.Text = string.Empty;
            txtHexColor.Text = string.Empty;
            colorPreview.Background = Brushes.Transparent;
        }

        /// <summary>
        /// 颜色下拉框选择变更事件
        /// </summary>
        private void TxtColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (txtColor.SelectedItem is DatabaseHelper.CartridgeColor selectedColor)
                {
                    // 更新Hex颜色文本框
                    txtHexColor.Text = selectedColor.ColorCode;
                    
                    // 更新颜色预览
                    try
                    {
                        if (!string.IsNullOrEmpty(selectedColor.ColorCode))
                        {
                            var brush = new BrushConverter().ConvertFromString(selectedColor.ColorCode) as SolidColorBrush;
                            colorPreview.Background = brush ?? Brushes.Transparent;
                        }
                        else
                        {
                            colorPreview.Background = Brushes.Transparent;
                        }
                    }
                    catch
                    {
                        // 如果颜色转换失败，使用透明色
                        colorPreview.Background = Brushes.Transparent;
                    }
                }
                else
                {
                    // 清除Hex颜色文本框和预览
                    txtHexColor.Text = string.Empty;
                    colorPreview.Background = Brushes.Transparent;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新Hex颜色时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 墨盒选择变更事件
        /// </summary>
        private void DgCartridges_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // 如果已经显示了添加墨盒表单，并且选中了行，则将行数据加载到表单中
                if (gridDetails.Visibility == Visibility.Visible && isAddMode && dgCartridges.SelectedItem != null)
                {
                    // 使用dynamic类型接收选中项
                    dynamic selectedCartridge = dgCartridges.SelectedItem;
                    
                    // 填充表单，但保持为添加模式
                    var selectedColor = allCartridgeColors.FirstOrDefault(c => c.Name == selectedCartridge.Color);
                    txtColor.SelectedItem = selectedColor;
                    txtModel.Text = selectedCartridge.Model;
                    txtCapacity.Text = selectedCartridge.Capacity.ToString();
                    
                    // 可选是否要复制当前库存，一般新墨盒库存从0开始
                    txtCurrentStock.Text = "0"; // 默认设为0，而不是复制原有库存
                    txtMinimumStock.Text = selectedCartridge.MinimumStock.ToString();
                    txtNotes.Text = selectedCartridge.Notes;
                    
                    Console.WriteLine($"从选中墨盒加载信息到添加表单: {selectedCartridge.Color} {selectedCartridge.Model}");
                }
            }
            catch (Exception ex)
            {
                // 不显示消息框，避免干扰用户体验，仅记录日志
                Console.WriteLine($"从选中墨盒加载信息到表单时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 编辑按钮点击事件
        /// </summary>
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取选中的墨盒
                if (sender is Button button && button.DataContext != null)
                {
                    // 使用dynamic类型变量接收DataContext
                    dynamic cartridge = button.DataContext;
                    
                    // 填充表单
                    var selectedColor = allCartridgeColors.FirstOrDefault(c => c.Name == cartridge.Color);
                    txtColor.SelectedItem = selectedColor;
                    txtModel.Text = cartridge.Model;
                    txtCapacity.Text = cartridge.Capacity.ToString();
                    txtCurrentStock.Text = cartridge.CurrentStock.ToString();
                    txtMinimumStock.Text = cartridge.MinimumStock.ToString();
                    txtNotes.Text = cartridge.Notes;
                    
                    // 更新Hex颜色（SelectionChanged事件会自动处理）
                    
                    // 显示编辑区域
                    gridDetails.Visibility = Visibility.Visible;
                    
                    // 设置为编辑模式
                    isAddMode = false;
                    currentEditId = cartridge.Id;
                    
                    // 设置焦点
                    txtColor.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"准备编辑墨盒时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除按钮点击事件
        /// </summary>
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取选中的墨盒
                if (sender is Button button && button.DataContext != null)
                {
                    // 使用dynamic类型变量接收DataContext
                    dynamic cartridge = button.DataContext;
                    
                    // 询问用户是否确认删除
                    var result = MessageBox.Show($"确认要删除墨盒 [{cartridge.Color} {cartridge.Model}] 吗？\n这将同时删除相关的进出库记录。", 
                        "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // 执行删除
                        if (DatabaseHelper.DeleteCartridge(cartridge.Id))
                        {
                            MessageBox.Show("墨盒删除成功", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // 重新加载数据并确保颜色正确显示
                            ReloadData();
                        }
                        else
                        {
                            MessageBox.Show("墨盒删除失败", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除墨盒时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证控件初始化
                if (txtColor == null || txtModel == null || txtCapacity == null || 
                    txtCurrentStock == null || txtMinimumStock == null || txtNotes == null)
                {
                    MessageBox.Show("表单控件未正确初始化，请重试", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    Console.WriteLine("保存墨盒失败：表单控件未初始化");
                    return;
                }
                
                // 获取表单数据
                if (txtColor.SelectedItem is DatabaseHelper.CartridgeColor selectedColor)
                {
                    // 验证选中的颜色不为空
                    if (selectedColor == null || string.IsNullOrEmpty(selectedColor.Name))
                    {
                        MessageBox.Show("请选择有效的墨盒颜色", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    string colorName = selectedColor.Name;
                    string modelName = txtModel.Text?.Trim() ?? "";

                    // 验证基本数据
                    if (string.IsNullOrEmpty(colorName))
                    {
                        MessageBox.Show("请选择墨盒颜色", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrEmpty(modelName))
                    {
                        MessageBox.Show("请输入墨盒型号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 获取墨盒容量
                    int capacity = 0;
                    if (!int.TryParse(txtCapacity.Text, out capacity) || capacity <= 0)
                    {
                        MessageBox.Show("墨盒容量必须是大于0的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 获取当前库存
                    int currentStock = 0;
                    if (!int.TryParse(txtCurrentStock.Text, out currentStock) || currentStock < 0)
                    {
                        MessageBox.Show("当前库存必须是大于或等于0的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 获取最低库存警戒线
                    int minimumStock = 0;
                    if (!int.TryParse(txtMinimumStock.Text, out minimumStock) || minimumStock < 0)
                    {
                        MessageBox.Show("最低库存警戒线必须是大于或等于0的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 创建墨盒对象
                    Cartridge cartridge = new Cartridge
                    {
                        Id = isAddMode ? 0 : currentEditId,
                        Color = colorName,
                        Model = modelName,
                        Capacity = capacity,
                        CurrentStock = currentStock,
                        MinimumStock = minimumStock,
                        Notes = txtNotes.Text?.Trim() ?? "",
                        UpdateTime = DateTime.Now
                    };

                    bool success = false;

                    // 添加或更新墨盒
                    if (isAddMode)
                    {
                        // 确保allCartridges已初始化
                        if (allCartridges == null)
                        {
                            allCartridges = new List<Cartridge>();
                        }
                        
                        // 尝试添加墨盒
                        Console.WriteLine($"尝试添加墨盒: 颜色={colorName}, 型号={modelName}");
                        success = DatabaseHelper.AddCartridge(cartridge);
                        
                        if (success)
                        {
                            // 查询新添加的墨盒（获取数据库分配的ID）
                            try
                            {
                                Console.WriteLine("查询新添加的墨盒...");
                                var newCartridges = DatabaseHelper.GetAllCartridges()
                                    ?.Where(c => c != null && 
                                             string.Equals(c.Color, colorName, StringComparison.OrdinalIgnoreCase) && 
                                             string.Equals(c.Model, modelName, StringComparison.OrdinalIgnoreCase))
                                    ?.OrderByDescending(c => c.Id)
                                    ?.ToList() ?? new List<Cartridge>();

                                if (newCartridges.Count > 0)
                                {
                                    // 获取新添加的墨盒
                                    var newCartridge = newCartridges[0];
                                    Console.WriteLine($"找到新添加的墨盒, ID: {newCartridge.Id}");
                                    
                                    // 直接添加到列表
                                    allCartridges.Add(newCartridge);
                                    
                                    // 切换到编辑模式
                                    isAddMode = false;
                                    currentEditId = newCartridge.Id;
                                    
                                    // 更新UI
                                    ApplyFilters();
                                    
                                    // 显示成功消息
                                    MessageBox.Show("墨盒添加成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                                    Console.WriteLine($"墨盒添加成功，ID: {newCartridge.Id}");
                                }
                                else
                                {
                                    // 如果找不到新添加的墨盒，完全重新加载数据
                                    Console.WriteLine("未找到新添加的墨盒，将重新加载所有数据");
                                    MessageBox.Show("墨盒添加成功，正在刷新数据...", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadCartridges();
                                    ApplyFilters();
                                }
                            }
                            catch (Exception queryEx)
                            {
                                Console.WriteLine($"查询新添加墨盒时出错: {queryEx.Message}");
                                MessageBox.Show("墨盒已添加，但查询新墨盒时出错，将重新加载数据", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                                LoadCartridges();
                                ApplyFilters();
                            }
                        }
                        else
                        {
                            MessageBox.Show("添加墨盒失败，请检查数据后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else // 编辑模式
                    {
                        // 确保allCartridges已初始化
                        if (allCartridges == null)
                        {
                            allCartridges = new List<Cartridge>();
                            LoadCartridges();
                        }
                        
                        // 尝试更新墨盒
                        Console.WriteLine($"尝试更新墨盒ID: {currentEditId}");
                        success = DatabaseHelper.UpdateCartridge(cartridge);
                        
                        if (success)
                        {
                            try
                            {
                                // 更新内存中的对象
                                int index = allCartridges.FindIndex(c => c != null && c.Id == currentEditId);
                                if (index >= 0)
                                {
                                    Console.WriteLine($"更新内存中墨盒，索引: {index}");
                                    allCartridges[index] = cartridge;
                                    ApplyFilters();
                                }
                                else
                                {
                                    // 找不到墨盒，重新加载
                                    Console.WriteLine("在内存中找不到要更新的墨盒，将重新加载所有数据");
                                    LoadCartridges();
                                    ApplyFilters();
                                }
                            }
                            catch (Exception updateEx)
                            {
                                Console.WriteLine($"更新内存墨盒数据时出错: {updateEx.Message}");
                                LoadCartridges();
                                ApplyFilters();
                            }
                            
                            MessageBox.Show("墨盒更新成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("更新墨盒失败，请检查数据后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    // 保存成功后不自动关闭窗口，让用户自己决定何时关闭
                    if (success)
                    {
                        // 如果是添加模式，可以考虑清空表单以便添加下一个墨盒
                        if (isAddMode)
                        {
                            // 询问用户是否继续添加新墨盒
                            var result = MessageBox.Show("墨盒添加成功！是否继续添加新墨盒？", 
                                "添加成功", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            
                            if (result == MessageBoxResult.Yes)
                            {
                                // 保持添加模式，清空表单以便继续添加
                                ClearDetailForm();
                                txtColor.Focus();
                            }
                            else
                            {
                                // 不再添加新墨盒，但窗口保持打开状态
                                // 用户可以继续编辑或点击"关闭"按钮关闭窗口
                            }
                        }
                        
                        // 刷新仪表盘数据
                        var mainWindow = Window.GetWindow(this) as MainWindow;
                        if (mainWindow != null)
                        {
                            mainWindow.RefreshDashboardIfVisible();
                            Console.WriteLine("已请求刷新仪表盘数据");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("请选择墨盒颜色", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存墨盒时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"保存墨盒异常: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                // 取消编辑，恢复上次保存的内容
                if (!isAddMode && currentEditId > 0)
                {
                    // 如果是编辑模式，尝试重新加载当前编辑的墨盒数据
                    var cartridge = DatabaseHelper.GetAllCartridges().FirstOrDefault(c => c.Id == currentEditId);
                    if (cartridge != null)
                    {
                        // 填充表单
                        var selectedColor = allCartridgeColors.FirstOrDefault(c => c.Name == cartridge.Color);
                        txtColor.SelectedItem = selectedColor;
                        txtModel.Text = cartridge.Model;
                        txtCapacity.Text = cartridge.Capacity.ToString();
                        txtCurrentStock.Text = cartridge.CurrentStock.ToString();
                        txtMinimumStock.Text = cartridge.MinimumStock.ToString();
                        txtNotes.Text = cartridge.Notes;
                        
                        MessageBox.Show("已恢复上次保存的内容", "操作取消", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // 如果是添加模式，询问用户是否要清空表单
                    var result = MessageBox.Show("您是否要清空当前填写的内容？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        ClearDetailForm();
                    }
                }
                
                // 不关闭编辑区域，让用户可以继续编辑
                // gridDetails.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"取消编辑时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // 隐藏编辑区域
            gridDetails.Visibility = Visibility.Collapsed;
            Console.WriteLine("用户手动关闭了编辑窗口");
        }

        /// <summary>
        /// 重新加载所有数据
        /// </summary>
        private void ReloadData()
        {
            try
            {
                // 保存当前表单状态
                string currentColor = null;
                string currentModel = null;
                bool detailsVisible = gridDetails.Visibility == Visibility.Visible;
                
                // 如果正在编辑中，记录选中的颜色和型号
                if (detailsVisible && txtColor.SelectedItem != null)
                {
                    var selectedColor = txtColor.SelectedItem as DatabaseHelper.CartridgeColor;
                    if (selectedColor != null)
                    {
                        currentColor = selectedColor.Name;
                    }
                    
                    if (!string.IsNullOrEmpty(txtModel.Text))
                    {
                        currentModel = txtModel.Text.Trim();
                    }
                }
                
                // 清空数据
                if (allCartridges != null)
                {
                    allCartridges.Clear();
                }
                else
                {
                    allCartridges = new List<Cartridge>();
                }
                
                // 加载所有数据
                LoadCartridgeColors();  // 首先加载颜色数据
                LoadColors();           // 然后加载颜色筛选器
                LoadCartridges();       // 最后加载墨盒数据
                
                // 更新UI显示
                dgCartridges.ItemsSource = null;  // 先清空数据源
                ApplyFilters();                  // 应用筛选器重新加载数据源
                
                // 如果之前在编辑状态，恢复当前表单内容
                if (detailsVisible && 
                    !string.IsNullOrEmpty(currentColor) && 
                    !string.IsNullOrEmpty(currentModel))
                {
                    // 1. 恢复选中的颜色
                    foreach (var item in txtColor.Items)
                    {
                        var color = item as DatabaseHelper.CartridgeColor;
                        if (color != null && string.Equals(color.Name, currentColor, StringComparison.OrdinalIgnoreCase))
                        {
                            txtColor.SelectedItem = color;
                            break;
                        }
                    }
                    
                    // 2. 恢复填写的型号
                    txtModel.Text = currentModel;
                }
                
                Console.WriteLine($"数据已重新加载，共 {allCartridges.Count} 个墨盒。表单状态已保留。");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重新加载数据时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 清空所有墨盒按钮点击事件
        /// </summary>
        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 询问用户是否确认删除所有墨盒
                var result = MessageBox.Show(
                    "确认要删除所有墨盒数据吗？\n这将删除所有墨盒及其相关的进出库记录，且此操作无法撤销。",
                    "确认清空数据",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // 再次确认，防止误操作
                    var confirmResult = MessageBox.Show(
                        "再次确认：您确定要删除所有墨盒数据吗？\n所有数据将被永久删除！",
                        "最终确认",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Exclamation);
                    
                    if (confirmResult == MessageBoxResult.Yes)
                    {
                        // 执行清空操作
                        if (DatabaseHelper.ClearAllCartridges())
                        {
                            MessageBox.Show("所有墨盒数据已成功删除", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // 禁用初始化默认数据的标志，确保删除后不会自动重新创建默认墨盒
                            DatabaseHelper.ShouldInitializeDefaultData = false;
                            
                            // 重新加载数据
                            ReloadData();
                        }
                        else
                        {
                            MessageBox.Show("删除墨盒数据失败", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清空墨盒数据时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 