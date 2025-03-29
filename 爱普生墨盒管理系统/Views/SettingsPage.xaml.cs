using System;
using System.Collections.Generic;
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
using 爱普生墨盒管理系统.Utils;
using System.Data.SQLite;

namespace 爱普生墨盒管理系统.Views
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        private readonly string dbFilePath;
        private readonly string settingsFilePath;

        public SettingsPage()
        {
            InitializeComponent();
            
            // 获取应用程序根目录作为设置文件的位置
            string appRootPath = AppDomain.CurrentDomain.BaseDirectory;
            dbFilePath = System.IO.Path.Combine(appRootPath, "CartridgeDB.db");
            settingsFilePath = System.IO.Path.Combine(appRootPath, "settings.ini");
            
            LoadSettings();
            LoadDatabaseInfo();
        }

        /// <summary>
        /// 加载系统设置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // 设置版本号
                txtVersion.Text = $"版本: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
                
                // 如果设置文件存在，则加载设置
                if (File.Exists(settingsFilePath))
                {
                    string[] lines = File.ReadAllLines(settingsFilePath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();
                            
                            switch (key)
                            {
                                case "SystemName":
                                    txtSystemName.Text = value;
                                    break;
                                case "DefaultProject":
                                    txtDefaultProject.Text = value;
                                    break;
                                case "BackupPath":
                                    txtBackupPath.Text = value;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载设置时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存系统设置
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                // 创建设置字符串
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"SystemName={txtSystemName.Text.Trim()}");
                sb.AppendLine($"DefaultProject={txtDefaultProject.Text.Trim()}");
                sb.AppendLine($"BackupPath={txtBackupPath.Text.Trim()}");
                
                // 保存设置文件到应用程序根目录
                File.WriteAllText(settingsFilePath, sb.ToString());
                
                MessageBox.Show("设置已保存到程序根目录", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载数据库信息
        /// </summary>
        private void LoadDatabaseInfo()
        {
            try
            {
                // 显示数据库路径
                txtDbPath.Text = dbFilePath;
                
                // 获取墨盒数量
                int cartridgeCount = DatabaseHelper.GetAllCartridges().Count;
                txtCartridgeCount.Text = cartridgeCount.ToString();
                
                // 获取操作记录数量
                int recordCount = DatabaseHelper.GetAllStockRecords().Count;
                txtRecordCount.Text = recordCount.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据库信息时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存设置按钮点击事件
        /// </summary>
        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(txtSystemName.Text))
                {
                    MessageBox.Show("系统名称不能为空", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtSystemName.Focus();
                    return;
                }
                
                
                // 保存设置
                SaveSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 浏览按钮点击事件
        /// </summary>
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建文件夹浏览对话框
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "选择数据库备份保存位置",
                    ShowNewFolderButton = true
                };
                
                // 如果已经有路径，设置为初始路径
                if (!string.IsNullOrEmpty(txtBackupPath.Text))
                {
                    string fullPath = System.IO.Path.IsPathRooted(txtBackupPath.Text) 
                        ? txtBackupPath.Text 
                        : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, txtBackupPath.Text);
                    
                    if (Directory.Exists(fullPath))
                    {
                        dialog.SelectedPath = fullPath;
                    }
                }
                
                // 显示对话框并处理结果
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtBackupPath.Text = dialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开文件夹浏览器时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 备份数据库按钮点击事件
        /// </summary>
        private void BtnBackupDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取备份路径
                string backupPath = txtBackupPath.Text.Trim();
                if (string.IsNullOrEmpty(backupPath))
                {
                    MessageBox.Show("请先设置备份路径", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // 确保路径存在
                if (!System.IO.Path.IsPathRooted(backupPath))
                {
                    backupPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, backupPath);
                }
                
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }
                
                // 创建备份文件名
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFile = System.IO.Path.Combine(backupPath, $"CartridgeDB_Backup_{timestamp}.db");
                
                // 复制数据库文件
                File.Copy(dbFilePath, backupFile, true);
                
                MessageBox.Show($"数据库已成功备份到:\n{backupFile}", "备份成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"备份数据库时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 恢复数据库按钮点击事件
        /// </summary>
        private void BtnRestoreDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建打开文件对话框
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "数据库文件 (*.db)|*.db";
                openFileDialog.Title = "选择要恢复的数据库备份文件";
                
                // 设置初始目录
                string backupPath = string.IsNullOrEmpty(txtBackupPath.Text) ? "Backups" : txtBackupPath.Text;
                if (!System.IO.Path.IsPathRooted(backupPath))
                {
                    backupPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, backupPath);
                }
                
                if (Directory.Exists(backupPath))
                {
                    openFileDialog.InitialDirectory = backupPath;
                }
                
                if (openFileDialog.ShowDialog() == true)
                {
                    // 获取选择的文件路径
                    string selectedFilePath = openFileDialog.FileName;
                    
                    // 确认恢复操作
                    MessageBoxResult result = MessageBox.Show(
                        "恢复数据库将覆盖当前的所有数据，确定要继续吗？",
                        "确认恢复",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // 关闭数据库连接
                        try
                        {
                            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
                            {
                                connection.Close();
                            }
                        }
                        catch { }
                        
                        // 备份当前数据库
                        string currentBackupPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                        if (!Directory.Exists(currentBackupPath))
                        {
                            Directory.CreateDirectory(currentBackupPath);
                        }
                        
                        string autoBackupFileName = $"CartridgeDB_AutoBackup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                        string autoBackupFilePath = System.IO.Path.Combine(currentBackupPath, autoBackupFileName);
                        
                        File.Copy(dbFilePath, autoBackupFilePath);
                        
                        // 恢复选定的备份
                        File.Copy(selectedFilePath, dbFilePath, true);
                        
                        MessageBox.Show(
                            $"数据库已成功恢复！\n\n当前数据库已自动备份至：\n{autoBackupFilePath}",
                            "恢复成功",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                        
                        // 刷新数据库信息
                        LoadDatabaseInfo();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"恢复数据库时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 压缩数据库按钮点击事件
        /// </summary>
        private void BtnCompactDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 确认操作
                MessageBoxResult result = MessageBox.Show(
                    "压缩数据库将优化数据库结构，减小文件大小，但可能需要一些时间。是否继续？",
                    "确认压缩",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                
                if (result == MessageBoxResult.Yes)
                {
                    // 执行压缩操作
                    using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbFilePath};Version=3;"))
                    {
                        connection.Open();
                        
                        // 执行VACUUM命令
                        using (SQLiteCommand command = new SQLiteCommand("VACUUM;", connection))
                        {
                            command.ExecuteNonQuery();
                        }
                        
                        connection.Close();
                    }
                    
                    MessageBox.Show("数据库压缩完成！", "压缩成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"压缩数据库时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 