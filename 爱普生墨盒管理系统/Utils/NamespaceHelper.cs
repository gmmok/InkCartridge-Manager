using System;

// 这个文件创建了一个从宽幅面打印机墨盒管理系统到爱普生墨盒管理系统的命名空间别名
// 用于解决项目文件中RootNamespace与代码中使用的命名空间不一致的问题

namespace 宽幅面打印机墨盒管理系统
{
    /// <summary>
    /// 命名空间别名类，用于解决项目命名空间不一致问题
    /// </summary>
    public static class NamespaceHelper
    {
        /// <summary>
        /// 只是一个占位符方法
        /// </summary>
        public static void Initialize()
        {
            // 什么都不做，只是为了让别名生效
        }
    }
}

namespace 宽幅面打印机墨盒管理系统.Utils
{
    /// <summary>
    /// LocalizationHelper类的别名
    /// </summary>
    public static class LocalizationHelperAlias
    {
        /// <summary>
        /// 获取字符串的别名方法
        /// </summary>
        public static string GetString(string key)
        {
            return 爱普生墨盒管理系统.Utils.LocalizationHelper.GetString(key);
        }

        /// <summary>
        /// 带格式的获取字符串的别名方法
        /// </summary>
        public static string GetString(string key, params object[] args)
        {
            return 爱普生墨盒管理系统.Utils.LocalizationHelper.GetString(key, args);
        }

        /// <summary>
        /// 设置语言的别名方法
        /// </summary>
        public static bool SetLanguage(string languageCode)
        {
            return 爱普生墨盒管理系统.Utils.LocalizationHelper.SetLanguage(languageCode);
        }
    }
}

// 为了解决App.xaml.cs中的引用问题，我们在宽幅面打印机墨盒管理系统命名空间中引用爱普生墨盒管理系统的Views命名空间
namespace 宽幅面打印机墨盒管理系统.Views
{
    /// <summary>
    /// 数据库初始化对话框的别名类
    /// </summary>
    public static class DatabaseInitDialogAlias
    {
        /// <summary>
        /// 显示初始化对话框的别名方法
        /// </summary>
        public static bool ShowInitDialog()
        {
            return 爱普生墨盒管理系统.Views.DatabaseInitDialog.ShowInitDialog();
        }

        /// <summary>
        /// 显示自定义对话框的别名方法
        /// </summary>
        public static bool ShowCustomDialog(string title, string message, string confirmText, string cancelText)
        {
            return 爱普生墨盒管理系统.Views.DatabaseInitDialog.ShowCustomDialog(title, message, confirmText, cancelText);
        }
    }
} 