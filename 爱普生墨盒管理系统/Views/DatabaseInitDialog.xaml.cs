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
using System.Windows.Shapes;
using 爱普生墨盒管理系统.Utils;

namespace 爱普生墨盒管理系统.Views
{
    /// <summary>
    /// DatabaseInitDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DatabaseInitDialog : Window
    {
        /// <summary>
        /// 对话框标题
        /// </summary>
        public string DialogTitle { get; set; }
        
        /// <summary>
        /// 对话框消息
        /// </summary>
        public string DialogMessage { get; set; }
        
        /// <summary>
        /// 确认按钮文本
        /// </summary>
        public string ConfirmButtonText { get; set; }
        
        /// <summary>
        /// 取消按钮文本
        /// </summary>
        public string CancelButtonText { get; set; }
        
        /// <summary>
        /// 用户是否确认
        /// </summary>
        public bool IsConfirmed { get; private set; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public DatabaseInitDialog()
        {
            InitializeComponent();
            DataContext = this;
            
            // 设置默认文本
            DialogTitle = LocalizationHelper.GetString("DB_INIT_TITLE");
            DialogMessage = LocalizationHelper.GetString("DB_INIT_MESSAGE");
            ConfirmButtonText = LocalizationHelper.GetString("DB_INIT_CONFIRM");
            CancelButtonText = LocalizationHelper.GetString("DB_INIT_CANCEL");
            
            IsConfirmed = false;
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="message">对话框消息</param>
        /// <param name="confirmText">确认按钮文本</param>
        /// <param name="cancelText">取消按钮文本</param>
        public DatabaseInitDialog(string title, string message, string confirmText, string cancelText)
        {
            InitializeComponent();
            DataContext = this;
            
            DialogTitle = title;
            DialogMessage = message;
            ConfirmButtonText = confirmText;
            CancelButtonText = cancelText;
            
            IsConfirmed = false;
        }
        
        /// <summary>
        /// 显示数据库初始化对话框
        /// </summary>
        /// <returns>用户是否确认</returns>
        public static bool ShowInitDialog()
        {
            var dialog = new DatabaseInitDialog();
            dialog.ShowDialog();
            return dialog.IsConfirmed;
        }
        
        /// <summary>
        /// 显示自定义数据库初始化对话框
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="message">对话框消息</param>
        /// <param name="confirmText">确认按钮文本</param>
        /// <param name="cancelText">取消按钮文本</param>
        /// <returns>用户是否确认</returns>
        public static bool ShowCustomDialog(string title, string message, string confirmText, string cancelText)
        {
            var dialog = new DatabaseInitDialog(title, message, confirmText, cancelText);
            dialog.ShowDialog();
            return dialog.IsConfirmed;
        }
        
        /// <summary>
        /// 确认按钮点击事件
        /// </summary>
        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }
        
        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }
    }
} 