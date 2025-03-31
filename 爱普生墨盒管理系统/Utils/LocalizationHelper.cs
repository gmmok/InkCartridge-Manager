using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 爱普生墨盒管理系统.Utils
{
    /// <summary>
    /// 本地化帮助类，用于提供多语言支持
    /// </summary>
    public static class LocalizationHelper
    {
        // 默认语言为中文
        private static string _currentLanguage = "zh-CN";
        
        // 语言资源字典
        private static readonly Dictionary<string, Dictionary<string, string>> _resources = new Dictionary<string, Dictionary<string, string>>();
        
        // 静态构造函数，初始化语言资源
        static LocalizationHelper()
        {
            InitializeResources();
        }
        
        /// <summary>
        /// 初始化语言资源
        /// </summary>
        private static void InitializeResources()
        {
            // 中文资源
            var zhResources = new Dictionary<string, string>
            {
                // 数据库初始化相关
                {"DB_INIT_TITLE", "数据库初始化"},
                {"DB_INIT_MESSAGE", "首次运行墨盒管理系统，需要初始化数据库。\n是否继续？"},
                {"DB_INIT_CONFIRM", "确认"},
                {"DB_INIT_CANCEL", "取消"},
                
                {"DB_ERROR_TITLE", "数据库错误"},
                {"DB_ERROR_MESSAGE", "数据库文件已损坏，是否重新创建数据库？\n注意：所有数据将丢失。"},
                {"DB_WARNING_TITLE", "警告"},
                {"DB_WARNING_EMPTY", "数据库文件存在但为空，将重新创建数据库。"},
                
                {"DB_ERROR_OPERATION", "数据库操作错误: {0}\n错误代码: {1}\n{2}"},
                {"DB_ERROR_INIT", "初始化数据库时发生错误: {0}\n{1}"},
                
                // 其他通用资源
                {"BUTTON_OK", "确定"},
                {"BUTTON_CANCEL", "取消"},
                {"BUTTON_APPLY", "应用"},
                {"BUTTON_CLOSE", "关闭"}
            };
            
            // 英文资源
            var enResources = new Dictionary<string, string>
            {
                // 数据库初始化相关
                {"DB_INIT_TITLE", "Database Initialization"},
                {"DB_INIT_MESSAGE", "First time running Cartridge Management System, database initialization is required.\nDo you want to continue?"},
                {"DB_INIT_CONFIRM", "Confirm"},
                {"DB_INIT_CANCEL", "Cancel"},
                
                {"DB_ERROR_TITLE", "Database Error"},
                {"DB_ERROR_MESSAGE", "The database file is corrupted. Do you want to recreate the database?\nNote: All data will be lost."},
                {"DB_WARNING_TITLE", "Warning"},
                {"DB_WARNING_EMPTY", "The database file exists but is empty. The database will be recreated."},
                
                {"DB_ERROR_OPERATION", "Database operation error: {0}\nError code: {1}\n{2}"},
                {"DB_ERROR_INIT", "Error initializing database: {0}\n{1}"},
                
                // 其他通用资源
                {"BUTTON_OK", "OK"},
                {"BUTTON_CANCEL", "Cancel"},
                {"BUTTON_APPLY", "Apply"},
                {"BUTTON_CLOSE", "Close"}
            };
            
            // 日文资源
            var jaResources = new Dictionary<string, string>
            {
                // 数据库初始化相关
                {"DB_INIT_TITLE", "データベース初期化"},
                {"DB_INIT_MESSAGE", "カートリッジ管理システムの初回実行です。データベースを初期化する必要があります。\n続行しますか？"},
                {"DB_INIT_CONFIRM", "確認"},
                {"DB_INIT_CANCEL", "キャンセル"},
                
                {"DB_ERROR_TITLE", "データベースエラー"},
                {"DB_ERROR_MESSAGE", "データベースファイルが破損しています。データベースを再作成しますか？\n注意：すべてのデータが失われます。"},
                {"DB_WARNING_TITLE", "警告"},
                {"DB_WARNING_EMPTY", "データベースファイルは存在しますが空です。データベースが再作成されます。"},
                
                {"DB_ERROR_OPERATION", "データベース操作エラー: {0}\nエラーコード: {1}\n{2}"},
                {"DB_ERROR_INIT", "データベース初期化中にエラーが発生しました: {0}\n{1}"},
                
                // 其他通用资源
                {"BUTTON_OK", "OK"},
                {"BUTTON_CANCEL", "キャンセル"},
                {"BUTTON_APPLY", "適用"},
                {"BUTTON_CLOSE", "閉じる"}
            };
            
            // 添加到资源字典
            _resources.Add("zh-CN", zhResources);
            _resources.Add("en-US", enResources);
            _resources.Add("ja-JP", jaResources);
            
            // 设置默认语言
            try
            {
                // 尝试使用系统语言
                var culture = CultureInfo.CurrentCulture;
                if (culture != null && _resources.ContainsKey(culture.Name))
                {
                    _currentLanguage = culture.Name;
                }
                else if (culture != null && _resources.Keys.Any(k => k.StartsWith(culture.TwoLetterISOLanguageName)))
                {
                    _currentLanguage = _resources.Keys.First(k => k.StartsWith(culture.TwoLetterISOLanguageName));
                }
            }
            catch (Exception ex)
            {
                // 如果获取系统语言失败，使用默认中文
                Console.WriteLine($"获取系统语言失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                _currentLanguage = "zh-CN";
            }
        }
        
        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="languageCode">语言代码（例如：zh-CN, en-US, ja-JP）</param>
        /// <returns>设置是否成功</returns>
        public static bool SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return false;
                
            if (_resources.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
                return true;
            }
            
            // 尝试匹配语言前缀（例如：zh, en, ja）
            string prefix = languageCode.Split('-')[0].ToLower();
            var matchedKey = _resources.Keys.FirstOrDefault(k => k.ToLower().StartsWith(prefix));
            if (matchedKey != null)
            {
                _currentLanguage = matchedKey;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取当前语言的本地化字符串
        /// </summary>
        /// <param name="key">资源键</param>
        /// <returns>本地化字符串，如果未找到则返回键名</returns>
        public static string GetString(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
                
            if (_resources.TryGetValue(_currentLanguage, out var currentResources))
            {
                if (currentResources.TryGetValue(key, out var value))
                {
                    return value;
                }
            }
            
            // 如果当前语言中没有找到，尝试使用中文
            if (_currentLanguage != "zh-CN" && _resources.TryGetValue("zh-CN", out var defaultResources))
            {
                if (defaultResources.TryGetValue(key, out var value))
                {
                    return value;
                }
            }
            
            // 如果都没有找到，返回键名
            return key;
        }
        
        /// <summary>
        /// 获取带格式化参数的本地化字符串
        /// </summary>
        /// <param name="key">资源键</param>
        /// <param name="args">格式化参数</param>
        /// <returns>格式化后的本地化字符串</returns>
        public static string GetString(string key, params object[] args)
        {
            string value = GetString(key);
            if (!string.IsNullOrEmpty(value) && args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(value, args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"格式化本地化字符串失败，键: {key}, 值: {value}");
                    Console.WriteLine($"错误: {ex.Message}");
                    Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                    return value;
                }
            }
            return value;
        }
        
        /// <summary>
        /// 获取当前语言代码
        /// </summary>
        /// <returns>当前语言代码</returns>
        public static string GetCurrentLanguage()
        {
            return _currentLanguage;
        }
        
        /// <summary>
        /// 获取所有支持的语言代码
        /// </summary>
        /// <returns>支持的语言代码列表</returns>
        public static List<string> GetSupportedLanguages()
        {
            return _resources.Keys.ToList();
        }
    }
} 