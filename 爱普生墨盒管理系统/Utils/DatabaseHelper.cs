using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 爱普生墨盒管理系统.Models;
using 爱普生墨盒管理系统.Views;
using 爱普生墨盒管理系统.Utils;
using System.Reflection;

namespace 爱普生墨盒管理系统.Utils
{
    /// <summary>
    /// SQLite数据库帮助类
    /// </summary>
    public class DatabaseHelper
    {
        /// <summary>
        /// 墨盒颜色实体类 - 内部定义以解决引用问题
        /// </summary>
        public class CartridgeColor
        {
            /// <summary>
            /// 颜色ID
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// 颜色名称
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 颜色代码（十六进制）
            /// </summary>
            public string ColorCode { get; set; }

            /// <summary>
            /// 显示顺序
            /// </summary>
            public int DisplayOrder { get; set; }

            /// <summary>
            /// 获取颜色的Brush对象，用于图表显示
            /// </summary>
            public System.Windows.Media.Brush GetBrush()
            {
                try
                {
                    if (!string.IsNullOrEmpty(ColorCode))
                    {
                        return (System.Windows.Media.SolidColorBrush)(new System.Windows.Media.BrushConverter().ConvertFrom(ColorCode));
                    }
                }
                catch (Exception ex)
                {
                    // 记录颜色转换错误但继续使用默认值
                    Console.WriteLine($"转换颜色代码'{ColorCode}'时出错: {ex.Message}");
                    Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                }

                // 默认颜色
                return System.Windows.Media.Brushes.Gray;
            }
        }

        // 数据库文件路径
        private static readonly string DbFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CartridgeDB.db");
        
        // 数据库SQL脚本文件路径
        private static readonly string DbScriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "SQLiteDB.sql");
        
        // 连接字符串
        private static readonly string ConnectionString = $"Data Source={DbFilePath};Version=3;";
        private static bool _isDbInitialized = false;
        private static bool _isSQLiteAvailable = true;
        
        // 添加控制默认数据初始化的标志
        // 设置为false则在应用启动时不会自动创建默认墨盒数据
        public static bool ShouldInitializeDefaultData { get; set; } = false;
        
        // 数据库版本号，用于后续版本迁移
        public static int CurrentDatabaseVersion { get; } = 1;
        
        // 初始化模态框的回调事件
        public delegate void InitializationModalCallback(bool confirmed);
        
        // 添加数据库初始化完成事件
        public static event EventHandler DatabaseInitialized;
        
        static DatabaseHelper()
        {
            try
            {
                // 尝试加载SQLite库，检查是否可用
                Type sqliteType = typeof(SQLiteConnection);
                _isSQLiteAvailable = true;
            }
            catch (Exception ex)
            {
                _isSQLiteAvailable = false;
                Console.WriteLine($"加载SQLite库时出错: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                MessageBox.Show("未能加载SQLite库，系统将以受限模式运行。\n请确保已正确安装SQLite。", 
                    "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 初始化数据库，创建表结构
        /// </summary>
        /// <returns>初始化是否成功</returns>
        public static bool InitializeDatabase()
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法初始化数据库。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            try
            {
                if (_isDbInitialized)
                    return true;
                
                // 规范化数据库文件路径
                string normalizedDbPath = Path.GetFullPath(DbFilePath);
                string dbDir = Path.GetDirectoryName(normalizedDbPath);
                
                // 创建数据库目录
                if (!Directory.Exists(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                }
                
                // 检查数据库文件是否存在
                bool isDbExists = File.Exists(normalizedDbPath);
                
                // 如果数据库文件不存在，询问用户是否初始化
                if (!isDbExists)
                {
                    // 显示模态对话框询问用户是否要初始化数据库
                    var result = MessageBox.Show(
                        "新的软件安装、是否创建新的数据文件？", 
                        "数据检测", 
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                        
                    if (result != MessageBoxResult.Yes)
                    {
                        // 用户取消，退出应用
                        Application.Current.Shutdown();
                        return false;
                    }
                    
                    // 用户确认，标记为新数据库（现在所有数据初始化都由SQLiteDB.sql文件提供）
                    ShouldInitializeDefaultData = true;
                }
                else
                {
                    // 检查数据库文件是否为空
                    FileInfo fileInfo = new FileInfo(normalizedDbPath);
                    if (fileInfo.Length == 0)
                    {
                        MessageBox.Show("数据库文件存在但为空，将重新创建数据库。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                        File.Delete(normalizedDbPath);
                        ShouldInitializeDefaultData = true;
                        isDbExists = false;
                    }
                }
                
                // 创建数据库连接并确保它可以打开
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    try
                    {
                        connection.Open();
                        Console.WriteLine("成功打开数据库连接");
                        
                        // 如果数据库文件存在，检查其完整性
                        if (isDbExists)
                        {
                            bool isIntegrityValid = CheckDatabaseIntegrity(connection);
                            if (!isIntegrityValid)
                            {
                                // 数据库完整性检查失败，询问用户是否重新创建
                                var result = MessageBox.Show(
                                    "数据库文件已损坏，是否重新创建数据库？\n注意：所有数据将丢失。",
                                    "数据库错误", 
                                    MessageBoxButton.YesNo, 
                                    MessageBoxImage.Warning);
                                    
                                if (result != MessageBoxResult.Yes)
                                {
                                    // 用户取消，退出应用
                                    Application.Current.Shutdown();
                                    return false;
                                }
                                
                                // 用户确认，删除现有数据库并重新创建
                                connection.Close();
                                File.Delete(normalizedDbPath);
                                connection.Open();
                                isDbExists = false;
                                ShouldInitializeDefaultData = true;
                                Console.WriteLine("数据库文件已重新创建");
                            }
                        }
                        
                        // 如果是新数据库或数据库已重置，使用SQL脚本创建表结构
                        if (!isDbExists)
                        {
                            try
                            {
                                // 创建新的数据库文件
                                connection.Close();
                                SQLiteConnection.CreateFile(normalizedDbPath);
                                connection.Open();
                                
                                // 使用SQL脚本创建表结构
                                CreateDatabaseSchema(connection);
                                
                                MessageBox.Show("数据库创建成功！", "初始化完成", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.Contains("请联系管理员"))
                                {
                                    // 如果是找不到SQL脚本的错误，清理并退出
                                    if (File.Exists(normalizedDbPath))
                                    {
                                        File.Delete(normalizedDbPath);
                                    }
                                    Application.Current.Shutdown();
                                    return false;
                                }
                                throw; // 重新抛出其他类型的异常
                            }
                        }
                        
                        // 检查并初始化默认数据
                        CheckAndInitializeDefaultData(connection);
                        
                        // 检查数据库版本并执行必要的升级
                        CheckAndUpgradeDatabase(connection);
                        
                        _isDbInitialized = true;
                        Console.WriteLine("数据库初始化完成");
                        
                        // 触发数据库初始化完成事件
                        DatabaseInitialized?.Invoke(null, EventArgs.Empty);
                        
                        return true;
                    }
                    catch (SQLiteException ex)
                    {
                        // SQLite特定错误处理
                        MessageBox.Show($"数据库操作错误: {ex.Message}\n错误代码: {ex.ErrorCode}\n{ex.StackTrace}", 
                            "数据库错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化数据库时发生错误: {ex.Message}\n{ex.StackTrace}", 
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 显示初始化确认对话框
        /// </summary>
        /// <returns>用户的选择结果</returns>
        private static MessageBoxResult ShowInitializationConfirmation()
        {
            try
            {
                // 使用自定义对话框
                bool confirmed = 宽幅面打印机墨盒管理系统.Views.DatabaseInitDialogAlias.ShowInitDialog();
                return confirmed ? MessageBoxResult.OK : MessageBoxResult.Cancel;
            }
            catch (Exception ex)
            {
                // 如果自定义对话框加载失败，使用标准消息框作为备选
                Console.WriteLine($"加载自定义初始化对话框失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                return MessageBox.Show(
                    宽幅面打印机墨盒管理系统.Utils.LocalizationHelperAlias.GetString("DB_INIT_MESSAGE"),
                    宽幅面打印机墨盒管理系统.Utils.LocalizationHelperAlias.GetString("DB_INIT_TITLE"),
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 检查数据库完整性
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <returns>数据库是否完整有效</returns>
        private static bool CheckDatabaseIntegrity(SQLiteConnection connection)
        {
            try
            {
                using (SQLiteCommand command = new SQLiteCommand("PRAGMA integrity_check", connection))
                {
                    string result = command.ExecuteScalar()?.ToString();
                    return result == "ok";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库完整性检查失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建数据库表结构
        /// </summary>
        /// <param name="connection">数据库连接</param>
        private static void CreateDatabaseSchema(SQLiteConnection connection)
        {
            try
            {
                Console.WriteLine("开始创建数据库表结构...");
                
                // 获取SQL脚本
                string sqlScript = GetSqlScript();
                
                if (string.IsNullOrEmpty(sqlScript))
                {
                    // 显示自定义错误消息
                    MessageBox.Show("软件没有数据库，请联系管理员！", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new Exception("找不到数据库初始化脚本");
                }
                
                Console.WriteLine($"获取的SQL脚本长度: {sqlScript.Length} 字符");
                
                // 执行整个脚本，先尝试一次性执行
                try
                {
                    using (SQLiteCommand command = new SQLiteCommand(sqlScript, connection))
                        {
                            command.ExecuteNonQuery();
                        Console.WriteLine("一次性执行SQL脚本成功");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"一次性执行SQL脚本失败，将分割执行: {ex.Message}");
                    // 失败时继续尝试分割执行
                }
                
                // 分割SQL语句
                string[] sqlCommands = sqlScript.Split(
                    new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                
                Console.WriteLine($"SQL脚本分割为 {sqlCommands.Length} 条语句");
                
                // 执行每个SQL语句
                int successCount = 0;
                foreach (string sql in sqlCommands)
                {
                    string trimmedSql = sql.Trim();
                    if (!string.IsNullOrEmpty(trimmedSql) && 
                        !trimmedSql.StartsWith("--") && 
                        !trimmedSql.StartsWith("PRAGMA") &&
                        !trimmedSql.StartsWith("BEGIN") &&
                        !trimmedSql.StartsWith("COMMIT"))
                    {
                        using (SQLiteCommand command = new SQLiteCommand(trimmedSql, connection))
                        {
                            try
                        {
                            command.ExecuteNonQuery();
                                successCount++;
                                
                                // 记录创建表的语句执行情况
                                if (trimmedSql.Contains("CREATE TABLE") && trimmedSql.Contains("Cartridges"))
                                {
                                    Console.WriteLine("成功创建Cartridges表");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"执行SQL语句出错: {trimmedSql.Substring(0, Math.Min(50, trimmedSql.Length))}...\n错误: {ex.Message}");
                                // 对于非关键错误，继续尝试执行其他语句
                                if (ex.Message.Contains("already exists"))
                                {
                                    Console.WriteLine("表已存在，继续执行");
                                    continue;
                                }
                                throw;
                            }
                        }
                    }
                }
                
                Console.WriteLine($"成功执行 {successCount}/{sqlCommands.Length} 条SQL语句");
                
                // 检查Cartridges表是否成功创建
                bool tableExists = CheckIfCartridgesTableExists(connection);
                if (!tableExists)
                {
                    throw new Exception("Cartridges表创建失败，请检查SQL脚本");
                }
                
                Console.WriteLine("数据库表结构创建完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建数据库表结构时发生错误: {ex.Message}\n{ex.StackTrace}");
                throw new Exception($"创建数据库表结构时发生错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查数据库版本并升级数据库结构
        /// </summary>
        /// <param name="connection">数据库连接</param>
        private static void CheckAndUpgradeDatabase(SQLiteConnection connection)
        {
            try
            {
                // 检查是否存在数据库版本表
                bool hasVersionTable = false;
                        using (SQLiteCommand command = new SQLiteCommand(
                    "SELECT name FROM sqlite_master WHERE type='table' AND name='DatabaseVersion'", connection))
                        {
                            var result = command.ExecuteScalar();
                    hasVersionTable = result != null;
                        }
                        
                // 如果不存在版本表，创建并设置为当前版本
                if (!hasVersionTable)
                        {
                            using (SQLiteCommand command = new SQLiteCommand(
                        "CREATE TABLE DatabaseVersion (" +
                        "Id INTEGER PRIMARY KEY, " +
                        "Version INTEGER NOT NULL, " +
                        "UpdateTime DATETIME DEFAULT CURRENT_TIMESTAMP" +
                                ")", connection))
                            {
                                command.ExecuteNonQuery();
                            }
                    
                    using (SQLiteCommand command = new SQLiteCommand(
                        "INSERT INTO DatabaseVersion (Id, Version) VALUES (1, @Version)", connection))
                    {
                        command.Parameters.AddWithValue("@Version", CurrentDatabaseVersion);
                        command.ExecuteNonQuery();
                    }
                    return;
                }
                
                // 获取当前数据库版本
                int dbVersion = 0;
                    using (SQLiteCommand command = new SQLiteCommand(
                    "SELECT Version FROM DatabaseVersion WHERE Id=1", connection))
                {
                    var result = command.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int version))
                    {
                        dbVersion = version;
                    }
                }
                
                // 如果数据库版本低于当前版本，执行升级
                if (dbVersion < CurrentDatabaseVersion)
                {
                    // 执行版本升级逻辑
                    UpgradeDatabaseToVersion(connection, dbVersion, CurrentDatabaseVersion);
                    
                    // 更新数据库版本
                    using (SQLiteCommand command = new SQLiteCommand(
                        "UPDATE DatabaseVersion SET Version=@Version, UpdateTime=CURRENT_TIMESTAMP WHERE Id=1", connection))
                    {
                        command.Parameters.AddWithValue("@Version", CurrentDatabaseVersion);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查和升级数据库版本时出错: {ex.Message}");
                // 不抛出异常，继续使用现有数据库
            }
        }

        /// <summary>
        /// 升级数据库到指定版本
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <param name="fromVersion">起始版本</param>
        /// <param name="toVersion">目标版本</param>
        private static void UpgradeDatabaseToVersion(SQLiteConnection connection, int fromVersion, int toVersion)
        {
            // 示例：版本1到版本2的升级
            if (fromVersion < 2 && toVersion >= 2)
            {
                // 检查索引是否存在
                bool hasColorIndex = false;
                        using (SQLiteCommand command = new SQLiteCommand(
                    "SELECT name FROM sqlite_master WHERE type='index' AND name='idx_cartridges_color'", connection))
                {
                    var result = command.ExecuteScalar();
                    hasColorIndex = result != null;
                }
                
                // 为Cartridges表的Color字段创建索引（如果不存在）
                if (!hasColorIndex)
                {
                    using (SQLiteCommand command = new SQLiteCommand(
                        "CREATE INDEX idx_cartridges_color ON Cartridges(Color)", connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                    
                // 其他版本2需要的升级操作...
            }
            
            // 可以添加更多版本升级逻辑
            // if (fromVersion < 3 && toVersion >= 3) { ... }
        }

        /// <summary>
        /// 检查并初始化默认数据
        /// </summary>
        /// <param name="connection">数据库连接</param>
        private static void CheckAndInitializeDefaultData(SQLiteConnection connection)
        {
            try
            {
                Console.WriteLine("开始检查数据库表结构...");
                
                // 检查表是否存在
                bool tableExists = CheckIfCartridgesTableExists(connection);
                if (!tableExists)
                {
                    Console.WriteLine("Cartridges表不存在，请检查SQLiteDB.sql文件");
                    throw new Exception("无法找到Cartridges表，请确保数据库表结构已正确创建");
                }
                
                // 检查颜色表是否存在且有数据
                bool hasColors = false;
                using (SQLiteCommand command = new SQLiteCommand(
                    "SELECT COUNT(*) FROM CartridgeColors", connection))
                {
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    hasColors = count > 0;
                }
                
                if (!hasColors)
                {
                    Console.WriteLine("墨盒颜色表为空，请检查SQLiteDB.sql文件是否正确初始化");
                }
                else
                {
                    Console.WriteLine($"墨盒颜色表已初始化，包含 {hasColors} 个颜色记录");
                }
                
                Console.WriteLine("数据库结构检查完成");
                
                // 不再执行任何数据初始化，完全依赖SQLiteDB.sql文件
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查数据库结构时出错: {ex.Message}\n{ex.StackTrace}");
                throw; // 重新抛出异常以便上层处理
            }
        }
        
        /// <summary>
        /// 检查Cartridges表是否存在
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <returns>表是否存在</returns>
        private static bool CheckIfCartridgesTableExists(SQLiteConnection connection)
        {
            try
            {
                using (SQLiteCommand command = new SQLiteCommand(
                "SELECT name FROM sqlite_master WHERE type='table' AND name='Cartridges'", connection))
                {
                    var result = command.ExecuteScalar();
                    bool exists = result != null;
                    Console.WriteLine($"Cartridges表{(exists ? "存在" : "不存在")}");
                    return exists;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查Cartridges表是否存在时出错: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 修改ShouldInitializeDefaultData的意义，现在仅表示是否为新建数据库
        /// </summary>
        public static bool IsFreshDatabase
        {
            get { return ShouldInitializeDefaultData; }
            set { ShouldInitializeDefaultData = value; }
        }

        #region 墨盒相关操作

        /// <summary>
        /// 获取所有墨盒信息
        /// </summary>
        /// <returns>墨盒列表</returns>
        public static List<Cartridge> GetAllCartridges()
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法获取墨盒数据。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<Cartridge>();
            }

            var cartridges = new List<Cartridge>();
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string sql = "SELECT * FROM Cartridges ORDER BY Color, Model;";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cartridges.Add(new Cartridge
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Color = reader["Color"].ToString(),
                                    Model = reader["Model"].ToString(),
                                    Capacity = reader["Capacity"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Capacity"]),
                                    CurrentStock = Convert.ToInt32(reader["CurrentStock"]),
                                    MinimumStock = Convert.ToInt32(reader["MinimumStock"]),
                                    Notes = reader["Notes"].ToString(),
                                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取墨盒信息时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return cartridges;
        }

        /// <summary>
        /// 确保颜色在颜色表中存在
        /// </summary>
        /// <param name="colorName">颜色名称</param>
        /// <returns>操作是否成功</returns>
        public static bool EnsureColorExists(string colorName)
        {
            if (!_isSQLiteAvailable || string.IsNullOrEmpty(colorName))
            {
                return false;
            }
            
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    // 检查颜色是否已存在
                    bool colorExists = false;
                    using (var command = new SQLiteCommand("SELECT COUNT(*) FROM CartridgeColors WHERE Name = @Name", connection))
                    {
                        command.Parameters.AddWithValue("@Name", colorName);
                        var result = command.ExecuteScalar();
                        colorExists = Convert.ToInt32(result) > 0;
                    }
                    
                    // 如果颜色不存在，添加到颜色表
                    if (!colorExists)
                    {
                        // 获取当前最大的DisplayOrder
                        int maxDisplayOrder = 0;
                        using (var command = new SQLiteCommand("SELECT MAX(DisplayOrder) FROM CartridgeColors", connection))
                        {
                            var result = command.ExecuteScalar();
                            if (result != DBNull.Value)
                            {
                                maxDisplayOrder = Convert.ToInt32(result);
                            }
                        }
                        
                        // 添加新颜色，使用默认灰色
                        using (var command = new SQLiteCommand(
                            "INSERT INTO CartridgeColors (Name, ColorCode, DisplayOrder) VALUES (@Name, @ColorCode, @DisplayOrder)", 
                            connection))
                        {
                            command.Parameters.AddWithValue("@Name", colorName);
                            command.Parameters.AddWithValue("@ColorCode", "#808080"); // 默认使用灰色
                            command.Parameters.AddWithValue("@DisplayOrder", maxDisplayOrder + 1);
                            command.ExecuteNonQuery();
                        }
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"确保颜色存在时出错: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 添加新墨盒
        /// </summary>
        /// <param name="cartridge">墨盒对象</param>
        /// <returns>添加成功返回true，否则返回false</returns>
        public static bool AddCartridge(Cartridge cartridge)
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法添加墨盒。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            try
            {
                // 确保颜色在颜色表中存在
                EnsureColorExists(cartridge.Color);
                
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string sql = @"
                    INSERT INTO Cartridges (Color, Model, Capacity, CurrentStock, MinimumStock, Notes, UpdateTime)
                    VALUES (@Color, @Model, @Capacity, @CurrentStock, @MinimumStock, @Notes, @UpdateTime);";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Color", cartridge.Color);
                        command.Parameters.AddWithValue("@Model", cartridge.Model);
                        command.Parameters.AddWithValue("@Capacity", cartridge.Capacity);
                        command.Parameters.AddWithValue("@CurrentStock", cartridge.CurrentStock);
                        command.Parameters.AddWithValue("@MinimumStock", cartridge.MinimumStock);
                        command.Parameters.AddWithValue("@Notes", cartridge.Notes ?? string.Empty);
                        command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                        
                        return command.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加墨盒时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 更新墨盒信息
        /// </summary>
        /// <param name="cartridge">墨盒对象</param>
        /// <returns>更新成功返回true，否则返回false</returns>
        public static bool UpdateCartridge(Cartridge cartridge)
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法更新墨盒。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            try
            {
                // 确保颜色在颜色表中存在
                EnsureColorExists(cartridge.Color);
                
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string sql = @"
                    UPDATE Cartridges 
                    SET Color = @Color, 
                        Model = @Model,
                        Capacity = @Capacity,
                        CurrentStock = @CurrentStock, 
                        MinimumStock = @MinimumStock, 
                        Notes = @Notes, 
                        UpdateTime = @UpdateTime
                    WHERE Id = @Id;";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", cartridge.Id);
                        command.Parameters.AddWithValue("@Color", cartridge.Color);
                        command.Parameters.AddWithValue("@Model", cartridge.Model);
                        command.Parameters.AddWithValue("@Capacity", cartridge.Capacity);
                        command.Parameters.AddWithValue("@CurrentStock", cartridge.CurrentStock);
                        command.Parameters.AddWithValue("@MinimumStock", cartridge.MinimumStock);
                        command.Parameters.AddWithValue("@Notes", cartridge.Notes ?? string.Empty);
                        command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                        
                        return command.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新墨盒时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 删除墨盒
        /// </summary>
        /// <param name="id">墨盒ID</param>
        /// <returns>删除成功返回true，否则返回false</returns>
        public static bool DeleteCartridge(int id)
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法删除墨盒。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 先删除关联的进出库记录
                            string deleteRecordsSql = "DELETE FROM StockRecords WHERE CartridgeId = @Id;";
                            using (var command = new SQLiteCommand(deleteRecordsSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Id", id);
                                command.ExecuteNonQuery();
                            }

                            // 再删除墨盒本身
                            string deleteCartridgeSql = "DELETE FROM Cartridges WHERE Id = @Id;";
                            using (var command = new SQLiteCommand(deleteCartridgeSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Id", id);
                                command.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除墨盒时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 获取库存不足的墨盒列表
        /// </summary>
        /// <returns>库存不足的墨盒列表</returns>
        public static List<Cartridge> GetLowStockCartridges()
        {
            Console.WriteLine("开始获取库存不足墨盒列表...");
            
            if (!_isSQLiteAvailable)
            {
                Console.WriteLine("SQLite库不可用，无法获取库存不足墨盒数据");
                MessageBox.Show("SQLite库不可用，无法获取库存不足墨盒数据。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<Cartridge>();
            }
            
            List<Cartridge> cartridges = new List<Cartridge>();
            
            try
            {
                Console.WriteLine("尝试连接数据库获取库存不足墨盒数据...");
                
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    // 修改SQL查询，按照新的警戒线判定规则查询库存不足的墨盒
                    string sql = "SELECT * FROM Cartridges WHERE CurrentStock <= 0 OR (MinimumStock > 0 AND CurrentStock <= MinimumStock) ORDER BY CurrentStock ASC, Color, Model;";
                    Console.WriteLine($"执行SQL查询: {sql}");
                    
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            int count = 0;
                            while (reader.Read())
                            {
                                count++;
                                var cartridge = new Cartridge
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Color = reader["Color"].ToString(),
                                    Model = reader["Model"].ToString(),
                                    Capacity = reader["Capacity"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Capacity"]),
                                    CurrentStock = Convert.ToInt32(reader["CurrentStock"]),
                                    MinimumStock = Convert.ToInt32(reader["MinimumStock"]),
                                    Notes = reader["Notes"].ToString(),
                                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"])
                                };
                                
                                Console.WriteLine($"读取到库存不足/无库存墨盒: ID={cartridge.Id}, 颜色={cartridge.Color}, 型号={cartridge.Model}, 当前库存={cartridge.CurrentStock}, 最低库存={cartridge.MinimumStock}, 状态={(cartridge.CurrentStock <= 0 ? "无库存" : "库存不足")}");
                                cartridges.Add(cartridge);
                            }
                            Console.WriteLine($"总共读取到 {count} 条库存不足/无库存墨盒记录");
                        }
                    }
                }
                
                Console.WriteLine($"成功获取 {cartridges.Count} 条库存不足/无库存墨盒数据");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取库存不足墨盒数据时出错: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"获取库存不足墨盒数据时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return cartridges;
        }

        /// <summary>
        /// 获取墨盒表的状态信息，用于调试
        /// </summary>
        /// <returns>墨盒表的状态描述</returns>
        public static string GetCartridgeTableStatus()
        {
            if (!_isSQLiteAvailable)
            {
                return "SQLite库不可用，无法获取墨盒表状态";
            }
            
            StringBuilder sb = new StringBuilder();
            
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    // 获取表结构
                    sb.AppendLine("墨盒表结构:");
                    string pragmaSql = "PRAGMA table_info(Cartridges);";
                    using (var command = new SQLiteCommand(pragmaSql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sb.AppendLine($"  列名: {reader["name"]}, 类型: {reader["type"]}, 非空: {reader["notnull"]}, 默认值: {reader["dflt_value"]}");
                            }
                        }
                    }
                    
                    // 获取总记录数
                    string countSql = "SELECT COUNT(*) FROM Cartridges;";
                    using (var command = new SQLiteCommand(countSql, connection))
                    {
                        int totalCount = Convert.ToInt32(command.ExecuteScalar());
                        sb.AppendLine($"墨盒表总记录数: {totalCount}");
                    }
                    
                    // 获取库存不足的记录数
                    string lowStockSql = "SELECT COUNT(*) FROM Cartridges WHERE CurrentStock <= 0 OR CurrentStock < MinimumStock;";
                    using (var command = new SQLiteCommand(lowStockSql, connection))
                    {
                        int lowStockCount = Convert.ToInt32(command.ExecuteScalar());
                        sb.AppendLine($"库存不足或无库存墨盒数: {lowStockCount}");
                    }
                    
                    // 分别获取无库存和库存不足的记录数
                    string zeroStockSql = "SELECT COUNT(*) FROM Cartridges WHERE CurrentStock <= 0;";
                    using (var command = new SQLiteCommand(zeroStockSql, connection))
                    {
                        int zeroStockCount = Convert.ToInt32(command.ExecuteScalar());
                        sb.AppendLine($"无库存墨盒数: {zeroStockCount}");
                    }
                    
                    string belowMinSql = "SELECT COUNT(*) FROM Cartridges WHERE CurrentStock > 0 AND CurrentStock < MinimumStock;";
                    using (var command = new SQLiteCommand(belowMinSql, connection))
                    {
                        int belowMinCount = Convert.ToInt32(command.ExecuteScalar());
                        sb.AppendLine($"低于警戒线墨盒数: {belowMinCount}");
                    }
                    
                    // 检查各种特殊情况
                    sb.AppendLine("特殊情况检查:");
                    
                    // 检查CurrentStock为负数的记录
                    string negativeStockSql = "SELECT COUNT(*) FROM Cartridges WHERE CurrentStock < 0;";
                    using (var command = new SQLiteCommand(negativeStockSql, connection))
                    {
                        int negativeStockCount = Convert.ToInt32(command.ExecuteScalar());
                        sb.AppendLine($"  负库存记录数: {negativeStockCount}");
                    }
                    
                    // 检查MinimumStock为负数的记录
                    string negativeMinSql = "SELECT COUNT(*) FROM Cartridges WHERE MinimumStock < 0;";
                    using (var command = new SQLiteCommand(negativeMinSql, connection))
                    {
                        int negativeMinCount = Convert.ToInt32(command.ExecuteScalar());
                        sb.AppendLine($"  负最低库存记录数: {negativeMinCount}");
                    }
                    
                    // 检查MinimumStock为0的记录
                    string zeroMinSql = "SELECT COUNT(*) FROM Cartridges WHERE MinimumStock = 0;";
                    using (var command = new SQLiteCommand(zeroMinSql, connection))
                    {
                        int zeroMinCount = Convert.ToInt32(command.ExecuteScalar());
                        sb.AppendLine($"  最低库存为0的记录数: {zeroMinCount}");
                    }
                    
                    // 获取前5条记录作为样本
                    sb.AppendLine("墨盒表样本数据(前5条):");
                    string sampleSql = "SELECT * FROM Cartridges LIMIT 5;";
                    using (var command = new SQLiteCommand(sampleSql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sb.AppendLine($"  ID: {reader["Id"]}, 颜色: {reader["Color"]}, 型号: {reader["Model"]}, " +
                                            $"当前库存: {reader["CurrentStock"]}, 最低库存: {reader["MinimumStock"]}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"获取墨盒表状态时出错: {ex.Message}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 检查并修复墨盒表数据，确保库存不足警告正常工作
        /// </summary>
        /// <returns>修复结果</returns>
        public static string CheckAndFixCartridgeTable()
        {
            if (!_isSQLiteAvailable)
            {
                return "SQLite库不可用，无法执行修复";
            }
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("开始检查并修复墨盒表数据...");
            
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. 修复负库存值
                            string fixNegativeStockSql = "UPDATE Cartridges SET CurrentStock = 0 WHERE CurrentStock < 0;";
                            using (var command = new SQLiteCommand(fixNegativeStockSql, connection, transaction))
                            {
                                int affected = command.ExecuteNonQuery();
                                sb.AppendLine($"修复负库存值: {affected}条记录");
                            }
                            
                            // 2. 修复负最低库存值
                            string fixNegativeMinSql = "UPDATE Cartridges SET MinimumStock = 1 WHERE MinimumStock < 0;";
                            using (var command = new SQLiteCommand(fixNegativeMinSql, connection, transaction))
                            {
                                int affected = command.ExecuteNonQuery();
                                sb.AppendLine($"修复负最低库存值: {affected}条记录");
                            }
                            
                            // 3. 将最低库存为0的记录设置为1，确保库存不足警告能够正常工作
                            string fixZeroMinSql = "UPDATE Cartridges SET MinimumStock = 1 WHERE MinimumStock = 0;";
                            using (var command = new SQLiteCommand(fixZeroMinSql, connection, transaction))
                            {
                                int affected = command.ExecuteNonQuery();
                                sb.AppendLine($"修复最低库存为0的记录: {affected}条记录");
                            }
                            
                            // 提交事务
                            transaction.Commit();
                            sb.AppendLine("修复完成，事务已提交");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            sb.AppendLine($"修复过程中出错，事务已回滚: {ex.Message}");
                        }
                    }
                    
                    // 验证修复结果
                    sb.AppendLine("验证修复结果:");
                    string verifyLowStockSql = "SELECT COUNT(*) FROM Cartridges WHERE CurrentStock < MinimumStock;";
                    using (var command = new SQLiteCommand(verifyLowStockSql, connection))
                    {
                        int lowStockCount = Convert.ToInt32(command.ExecuteScalar());
                        sb.AppendLine($"修复后库存不足墨盒数: {lowStockCount}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"执行修复时出错: {ex.Message}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// 删除所有墨盒数据
        /// </summary>
        /// <returns>删除成功返回true，否则返回false</returns>
        public static bool ClearAllCartridges()
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法删除墨盒数据。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 先删除所有进出库记录
                            string deleteRecordsSql = "DELETE FROM StockRecords;";
                            using (var command = new SQLiteCommand(deleteRecordsSql, connection, transaction))
                            {
                                command.ExecuteNonQuery();
                            }

                            // 再删除所有墨盒
                            string deleteCartridgesSql = "DELETE FROM Cartridges;";
                            using (var command = new SQLiteCommand(deleteCartridgesSql, connection, transaction))
                            {
                                command.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除所有墨盒数据时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        #endregion

        #region 进出库记录相关操作

        /// <summary>
        /// 添加墨盒出入库记录
        /// </summary>
        /// <param name="record">记录对象</param>
        /// <returns>操作是否成功</returns>
        public static bool AddStockRecord(StockRecord record)
        {
            if (!_isSQLiteAvailable || record == null)
                return false;

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    // 使用事务包装操作
                    using (SQLiteTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 使用参数化查询添加记录
                            string insertSql = @"INSERT INTO StockRecords 
                                            (CartridgeId, OperationType, Quantity, OperationTime, Operator, Project, Notes) 
                                            VALUES 
                                            (@CartridgeId, @OperationType, @Quantity, @OperationTime, @Operator, @Project, @Notes);
                                            SELECT last_insert_rowid();";
                            
                            using (SQLiteCommand cmd = new SQLiteCommand(insertSql, connection))
                            {
                                // 添加参数
                                cmd.Parameters.AddWithValue("@CartridgeId", record.CartridgeId);
                                cmd.Parameters.AddWithValue("@OperationType", record.OperationType);
                                cmd.Parameters.AddWithValue("@Quantity", record.Quantity);
                                cmd.Parameters.AddWithValue("@OperationTime", record.OperationTime);
                                cmd.Parameters.AddWithValue("@Operator", record.Operator);
                                cmd.Parameters.AddWithValue("@Project", record.Project);
                                cmd.Parameters.AddWithValue("@Notes", record.Notes);
                                
                                // 执行命令并获取新记录的ID
                                record.Id = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                            
                            // 更新墨盒库存
                            string updateCartridgeSql = "UPDATE Cartridges SET CurrentStock = CurrentStock + @StockChange, UpdateTime = @UpdateTime WHERE Id = @CartridgeId";
                            
                            // 直接使用记录中的数量，因为出库时Quantity已经是负数
                            int stockChange = record.Quantity;
                            
                            using (SQLiteCommand cmd = new SQLiteCommand(updateCartridgeSql, connection))
                            {
                                cmd.Parameters.AddWithValue("@StockChange", stockChange);
                                cmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                                cmd.Parameters.AddWithValue("@CartridgeId", record.CartridgeId);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // 提交事务
                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // 回滚事务
                            transaction.Rollback();
                            
                            // 记录详细的异常信息
                            Console.WriteLine($"添加墨盒记录时出错: {ex.Message}");
                            Console.WriteLine($"异常详情: {ex.StackTrace}");
                            Console.WriteLine($"操作参数: CartridgeId={record.CartridgeId}, Type={record.OperationType}, Quantity={record.Quantity}");
                            
                            MessageBox.Show($"添加墨盒记录时出错: {ex.Message}\n请检查输入数据后重试。", 
                                "数据库错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接数据库时出错: {ex.Message}");
                Console.WriteLine($"异常详情: {ex.StackTrace}");
                MessageBox.Show($"数据库连接错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 获取所有进出库记录
        /// </summary>
        /// <returns>进出库记录列表</returns>
        public static List<StockRecord> GetAllStockRecords()
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法获取库存记录。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<StockRecord>();
            }
            
            List<StockRecord> records = new List<StockRecord>();
            
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string sql = @"
                    SELECT r.*, c.Color, c.Model 
                    FROM StockRecords r
                    JOIN Cartridges c ON r.CartridgeId = c.Id
                    ORDER BY r.OperationTime DESC;";
                    
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var record = new StockRecord
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    CartridgeId = Convert.ToInt32(reader["CartridgeId"]),
                                    OperationType = Convert.ToInt32(reader["OperationType"]),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
                                    OperationTime = Convert.ToDateTime(reader["OperationTime"]),
                                    Operator = reader["Operator"].ToString(),
                                    Project = reader["Project"].ToString(),
                                    Notes = reader["Notes"].ToString(),
                                    Cartridge = new Cartridge
                                    {
                                        Color = reader["Color"].ToString(),
                                        Model = reader["Model"].ToString()
                                    }
                                };
                                records.Add(record);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取库存记录时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return records;
        }

        /// <summary>
        /// 查询库存记录
        /// </summary>
        /// <param name="skip">跳过的记录数量，用于分页</param>
        /// <param name="operationType">操作类型，1=入库，2=出库，0=所有</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>进出库记录列表</returns>
        public static List<StockRecord> QueryStockRecords(int skip, int operationType, DateTime? startDate, DateTime? endDate)
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法查询库存记录。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<StockRecord>();
            }
            
            List<StockRecord> records = new List<StockRecord>();
            
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    // 构建SQL查询条件
                    var conditions = new List<string>();
                    var parameters = new Dictionary<string, object>();
                    
                    if (operationType > 0)
                    {
                        conditions.Add("r.OperationType = @OperationType");
                        parameters.Add("@OperationType", operationType);
                    }
                    
                    if (startDate.HasValue)
                    {
                        conditions.Add("r.OperationTime >= @StartDate");
                        parameters.Add("@StartDate", startDate.Value.Date);
                    }
                    
                    if (endDate.HasValue)
                    {
                        conditions.Add("r.OperationTime <= @EndDate");
                        parameters.Add("@EndDate", endDate.Value.Date.AddDays(1).AddSeconds(-1));
                    }
                    
                    string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";
                    
                    string sql = $@"
                    SELECT r.*, c.Color, c.Model 
                    FROM StockRecords r
                    JOIN Cartridges c ON r.CartridgeId = c.Id
                    {whereClause}
                    ORDER BY r.OperationTime DESC
                    LIMIT @PageSize OFFSET @Skip;";
                    
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        // 添加参数
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                        
                        // 添加分页参数
                        command.Parameters.AddWithValue("@Skip", skip);
                        command.Parameters.AddWithValue("@PageSize", 1000); // 设置一个比较大的值，实际限制由Take()方法控制
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var record = new StockRecord
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    CartridgeId = Convert.ToInt32(reader["CartridgeId"]),
                                    OperationType = Convert.ToInt32(reader["OperationType"]),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
                                    OperationTime = Convert.ToDateTime(reader["OperationTime"]),
                                    Operator = reader["Operator"].ToString(),
                                    Project = reader["Project"].ToString(),
                                    Notes = reader["Notes"].ToString(),
                                    Cartridge = new Cartridge
                                    {
                                        Color = reader["Color"].ToString(),
                                        Model = reader["Model"].ToString()
                                    }
                                };
                                records.Add(record);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询库存记录时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return records;
        }

        /// <summary>
        /// 获取指定操作类型的库存记录数量
        /// </summary>
        /// <param name="operationType">操作类型，1=入库，2=出库，0=所有</param>
        /// <returns>记录数量</returns>
        public static int GetStockRecordCount(int operationType)
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法获取库存记录数量。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }
            
            int count = 0;
            
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    string sql = "SELECT COUNT(*) FROM StockRecords";
                    if (operationType > 0)
                    {
                        sql += " WHERE OperationType = @OperationType";
                    }
                    
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        if (operationType > 0)
                        {
                            command.Parameters.AddWithValue("@OperationType", operationType);
                        }
                        
                        count = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取库存记录数量时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return count;
        }

        /// <summary>
        /// 获取指定数量的最近操作记录
        /// </summary>
        /// <param name="count">要获取的记录数量，0表示获取所有</param>
        /// <returns>操作记录列表</returns>
        public static List<StockRecord> GetStockRecords(int count = 0)
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法获取操作记录。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<StockRecord>();
            }

            List<StockRecord> records = new List<StockRecord>();
            
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    string sql = @"
                        SELECT 
                            r.Id, r.CartridgeId, r.OperationType, r.Quantity, 
                            r.OperationTime, r.Operator, r.Project, r.Notes,
                            c.Color, c.Model
                        FROM StockRecords r
                        JOIN Cartridges c ON r.CartridgeId = c.Id
                        ORDER BY r.OperationTime DESC";
                        
                    if (count > 0)
                    {
                        sql += $" LIMIT {count}";
                    }
                    
                    using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Cartridge cartridge = new Cartridge
                                {
                                    Id = reader.GetInt32(1),  // CartridgeId
                                    Color = reader.GetString(8),  // Color
                                    Model = reader.GetString(9)   // Model
                                };
                                
                                StockRecord record = new StockRecord
                                {
                                    Id = reader.GetInt32(0),
                                    CartridgeId = reader.GetInt32(1),
                                    Cartridge = cartridge,
                                    OperationType = reader.GetInt32(2),
                                    Quantity = reader.GetInt32(3),
                                    OperationTime = reader.GetDateTime(4),
                                    Operator = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    Project = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    Notes = reader.IsDBNull(7) ? null : reader.GetString(7)
                                };
                                
                                records.Add(record);
                            }
                        }
                    }
                    
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取操作记录时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return records;
        }

        /// <summary>
        /// 获取最近的操作记录
        /// </summary>
        /// <param name="limit">返回的记录数量限制</param>
        /// <returns>操作记录列表</returns>
        public static List<Operation> GetRecentOperations(int limit)
        {
            List<Operation> operations = new List<Operation>();
            
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string sql = @"
                        SELECT 
                            r.Id, 
                            c.Color || ' ' || c.Model AS CartridgeInfo,
                            CASE r.OperationType 
                                WHEN 1 THEN '入库'
                                WHEN 2 THEN '出库'
                                ELSE '未知操作'
                            END AS OperationType,
                            r.Quantity,
                            r.OperationTime,
                            r.Operator
                        FROM StockRecords r
                        JOIN Cartridges c ON r.CartridgeId = c.Id
                        ORDER BY r.OperationTime DESC
                        LIMIT @Limit";
                        
                        using (var command = new SQLiteCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@Limit", limit);
                            
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    operations.Add(new Operation
                                    {
                                        Id = Convert.ToInt32(reader["Id"]),
                                        CartridgeInfo = reader["CartridgeInfo"].ToString(),
                                        OperationType = reader["OperationType"].ToString(),
                                        Quantity = Convert.ToInt32(reader["Quantity"]),
                                        OperationTime = Convert.ToDateTime(reader["OperationTime"]),
                                        Operator = reader["Operator"].ToString()
                                    });
                                }
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取最近操作记录出错: {ex.Message}\n{ex.StackTrace}");
            }
            
            return operations;
        }

        #endregion

        #region 统计报表相关

        /// <summary>
        /// 获取墨盒消耗统计数据
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>墨盒消耗统计数据</returns>
        public static DataTable GetCartridgeUsageStatistics(DateTime startDate, DateTime endDate)
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法获取墨盒使用统计。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new DataTable();
            }
            
            DataTable result = new DataTable();
            
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    // 修改SQL查询，确保包含CartridgeId、CurrentStock和MinimumStock列
                    string sql = @"
                    SELECT 
                        c.Id AS CartridgeId,
                        c.Color, 
                        c.Model,
                        c.CurrentStock,
                        c.MinimumStock,
                        SUM(CASE WHEN r.OperationType = 1 THEN r.Quantity ELSE 0 END) AS TotalIn,
                        SUM(CASE WHEN r.OperationType = 2 THEN r.Quantity ELSE 0 END) AS TotalOut
                    FROM Cartridges c
                    LEFT JOIN StockRecords r ON c.Id = r.CartridgeId AND r.OperationTime BETWEEN @StartDate AND @EndDate
                    GROUP BY c.Id, c.Color, c.Model, c.CurrentStock, c.MinimumStock
                    ORDER BY c.Color, c.Model;";
                    
                    using (var adapter = new SQLiteDataAdapter(sql, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@StartDate", startDate.Date);
                        adapter.SelectCommand.Parameters.AddWithValue("@EndDate", endDate.Date.AddDays(1).AddSeconds(-1));
                        adapter.Fill(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取墨盒使用统计时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return result;
        }

        /// <summary>
        /// 获取项目使用墨盒统计数据
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>项目使用墨盒统计数据</returns>
        public static DataTable GetProjectUsageStatistics(DateTime startDate, DateTime endDate)
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法获取项目使用统计。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new DataTable();
            }
            
            DataTable result = new DataTable();
            
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    string sql = @"
                    SELECT 
                        r.Project,
                        c.Color, 
                        c.Model,
                        SUM(r.Quantity) AS TotalUsage,
                        COUNT(r.Id) AS RecordCount,
                        MAX(r.OperationTime) AS LastUsageTime
                    FROM StockRecords r
                    JOIN Cartridges c ON r.CartridgeId = c.Id
                    WHERE r.OperationType = 2 
                      AND r.OperationTime BETWEEN @StartDate AND @EndDate
                      AND r.Project IS NOT NULL AND r.Project <> ''
                    GROUP BY r.Project, c.Color, c.Model
                    ORDER BY r.Project, c.Color, c.Model;";
                    
                    using (var adapter = new SQLiteDataAdapter(sql, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@StartDate", startDate.Date);
                        adapter.SelectCommand.Parameters.AddWithValue("@EndDate", endDate.Date.AddDays(1).AddSeconds(-1));
                        adapter.Fill(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取项目使用统计时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return result;
        }

        #endregion

        #region 操作人员相关方法

        /// <summary>
        /// 获取操作人员列表
        /// </summary>
        /// <returns>操作人员列表</returns>
        public static List<string> GetOperators()
        {
            if (!_isSQLiteAvailable)
            {
                return new List<string> { "系统管理员" };
            }

            var operators = new List<string>();
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    // 按上次使用时间倒序排列，使最近使用的排在前面
                    string sql = "SELECT Name FROM Operators ORDER BY LastUsed DESC, UseCount DESC;";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                operators.Add(reader["Name"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取操作人员列表时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                // 出错时返回默认操作人员
                return new List<string> { "系统管理员" };
            }

            // 确保至少有一个默认操作人员
            if (operators.Count == 0)
            {
                operators.Add("系统管理员");
            }
            
            // 确保系统管理员始终在列表中
            if (!operators.Contains("系统管理员"))
            {
                operators.Add("系统管理员");
            }
            
            return operators;
        }

        /// <summary>
        /// 获取最后使用的操作人员
        /// </summary>
        /// <returns>最后使用的操作人员名称</returns>
        public static string GetLastUsedOperator()
        {
            if (!_isSQLiteAvailable)
            {
                return "系统管理员";
            }

            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string sql = "SELECT Name FROM Operators ORDER BY LastUsed DESC, UseCount DESC LIMIT 1;";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            return result.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取最后使用的操作人员时出错: {ex.Message}");
                // 不显示错误消息，静默失败
            }

            return "系统管理员";
        }

        /// <summary>
        /// 更新或添加操作人员
        /// </summary>
        /// <param name="operatorName">操作人员名称</param>
        public static void UpdateOperator(string operatorName)
        {
            if (string.IsNullOrWhiteSpace(operatorName) || !_isSQLiteAvailable)
            {
                return;
            }

            try
            {
                // 系统管理员为固定操作人员，不需要更新使用次数
                if (operatorName == "系统管理员")
                {
                    return;
                }

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    // 检查操作人员是否已存在
                    string checkSql = "SELECT Id, UseCount FROM Operators WHERE Name = @Name;";
                    int? id = null;
                    int useCount = 0;
                    
                    using (var command = new SQLiteCommand(checkSql, connection))
                    {
                        command.Parameters.AddWithValue("@Name", operatorName);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                id = reader.GetInt32(0);
                                useCount = reader.GetInt32(1);
                            }
                        }
                    }
                    
                    if (id.HasValue)
                    {
                        // 更新现有操作人员
                        string updateSql = "UPDATE Operators SET LastUsed = @LastUsed, UseCount = @UseCount WHERE Id = @Id;";
                        using (var command = new SQLiteCommand(updateSql, connection))
                        {
                            command.Parameters.AddWithValue("@Id", id.Value);
                            command.Parameters.AddWithValue("@LastUsed", DateTime.Now);
                            command.Parameters.AddWithValue("@UseCount", useCount + 1);
                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // 添加新操作人员
                        string insertSql = "INSERT INTO Operators (Name, LastUsed, UseCount) VALUES (@Name, @LastUsed, @UseCount);";
                        using (var command = new SQLiteCommand(insertSql, connection))
                        {
                            command.Parameters.AddWithValue("@Name", operatorName);
                            command.Parameters.AddWithValue("@LastUsed", DateTime.Now);
                            command.Parameters.AddWithValue("@UseCount", 1);
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新操作人员信息时出错: {ex.Message}");
                // 不显示错误消息，静默失败
            }
        }

        /// <summary>
        /// 删除操作人员
        /// </summary>
        /// <param name="operatorName">操作人员名称</param>
        /// <returns>是否删除成功</returns>
        public static bool DeleteOperator(string operatorName)
        {
            if (string.IsNullOrWhiteSpace(operatorName) || !_isSQLiteAvailable)
            {
                return false;
            }

            // 系统管理员不允许删除
            if (operatorName == "系统管理员")
            {
                MessageBox.Show("系统管理员不能删除！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string sql = "DELETE FROM Operators WHERE Name = @Name;";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Name", operatorName);
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除操作人员时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        #endregion

        #region 墨盒颜色相关方法

        /// <summary>
        /// 获取所有墨盒颜色
        /// </summary>
        /// <returns>墨盒颜色列表</returns>
        public static List<CartridgeColor> GetAllCartridgeColors()
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法获取墨盒颜色数据。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<CartridgeColor>();
            }

            var colors = new List<CartridgeColor>();
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string sql = "SELECT * FROM CartridgeColors ORDER BY DisplayOrder;";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                colors.Add(new CartridgeColor
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = reader["Name"].ToString(),
                                    ColorCode = reader["ColorCode"].ToString(),
                                    DisplayOrder = Convert.ToInt32(reader["DisplayOrder"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取墨盒颜色信息时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return colors;
        }

        /// <summary>
        /// 获取墨盒颜色统计数据
        /// </summary>
        /// <returns>按颜色统计的墨盒数量</returns>
        public static Dictionary<string, int> GetCartridgeColorStatistics()
        {
            if (!_isSQLiteAvailable)
            {
                MessageBox.Show("SQLite库不可用，无法获取墨盒颜色统计数据。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new Dictionary<string, int>();
            }

            var colorStats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 使用不区分大小写的比较器
            try
            {
                // 1. 首先获取所有颜色记录，确保每种颜色都有统计数据
                var allColors = GetAllCartridgeColors();
                foreach (var color in allColors)
                {
                    // 初始化所有颜色的统计数量为0
                    colorStats[color.Name] = 0;
                }
                
                // 2. 然后获取实际的库存统计数据
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    // 使用COLLATE NOCASE确保SQL查询不区分大小写
                    string sql = @"
                    SELECT 
                        c.Color, 
                        SUM(c.CurrentStock) as TotalStock
                    FROM Cartridges c
                    GROUP BY c.Color COLLATE NOCASE
                    ORDER BY c.Color;";
                    
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string cartridgeColor = reader["Color"].ToString();
                                int totalStock = Convert.ToInt32(reader["TotalStock"]);
                                
                                // 尝试找到匹配的颜色（不区分大小写）
                                bool matched = false;
                                foreach (var knownColor in allColors)
                                {
                                    if (string.Equals(knownColor.Name, cartridgeColor, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // 如果找到匹配，使用标准颜色名称作为键
                                        colorStats[knownColor.Name] = totalStock;
                                        matched = true;
                                        Console.WriteLine($"颜色统计: 将墨盒颜色 '{cartridgeColor}' 匹配到 '{knownColor.Name}', 库存: {totalStock}");
                                        break;
                                    }
                                }
                                
                                // 如果没有匹配的标准颜色，添加为新条目
                                if (!matched)
                                {
                                    colorStats[cartridgeColor] = totalStock;
                                    Console.WriteLine($"颜色统计: 添加非标准颜色 '{cartridgeColor}', 库存: {totalStock}");
                                }
                            }
                        }
                    }
                    
                    // 3. 获取所有墨盒的颜色分布情况
                    Console.WriteLine("颜色统计结果:");
                    foreach (var pair in colorStats)
                    {
                        Console.WriteLine($"  颜色: {pair.Key}, 库存: {pair.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取墨盒颜色统计数据时出错: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return colorStats;
        }

        #endregion

        // 获取SQL脚本的方法 - 只从根目录读取
        private static string GetSqlScript()
        {
            try
            {
                // 只从应用程序根目录获取SQL脚本
                string rootDbScriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQLiteDB.sql");
                Console.WriteLine($"尝试从根目录读取SQL脚本: {rootDbScriptPath}");
                
                if (File.Exists(rootDbScriptPath))
                {
                    string content = File.ReadAllText(rootDbScriptPath);
                    Console.WriteLine($"成功从根目录读取SQL脚本，内容长度: {content.Length}字符");
                    return content;
                }
                
                // 如果文件不存在，返回空字符串
                Console.WriteLine($"根目录下未找到SQLiteDB.sql文件");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取SQL脚本时出错: {ex.Message}\n{ex.StackTrace}");
                return string.Empty;
            }
        }
    }
} 