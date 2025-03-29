#参考代码文件夹

##这个cankao文件夹只参考以下三个文件的代码来使用，不参与该该项目功能修改。参考软件启动初始化数据库功能，其他代码不要阅读太多。
"\cankao\BaseDbSQLiteClass.cs"
"\cankao\DalSQLite.cs"
"\cankao\MainBCMS.xaml.cs"

##参考代码文件夹
cankao

##数据库初始化流程
MainBCMS.xaml.cs 软件启动初始化分析
主要初始化调用点
###构造函数中的初始化
public MainWindow()
{
    InitializeComponent();
    InitializeTimer();
    // 强制初始化备份服务
    _ = BackupService.Instance; // 此行确保 BackupService 被创建
}

###Window_Loaded 事件中调用 GetConfig 方法
private void Window_Loaded(object sender, RoutedEventArgs e)
{
    GetConfig();
}

3. GetConfig 方法中调用数据库初始化检查
private void GetConfig()
{
    if (DalSQLite.CheckDb() == true)//检测数据库文件是否存在\正常访问
    {
        try
        {
            string ver = "数据库异常";
            if (DalSQLite.GetVersion(ref ver) == true)//读取数据版本号并检测是否与软件要求的数据库版本一致
            {
                labelLoginName.Content = "软件版本：" + DalDataConfig.SoftVerion + "    数据版本：" + ver;
                DalLogin.LoginedUser = new ObjUser();//初始化登录员工
                                                    
            }
            lvHouse.ItemsSource = DalHouse.GetViewList(null);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.ToString());
        }
    }
}

## 初始化流程
1. 应用程序启动时，首先执行 MainWindow 构造函数
2. 构造函数调用 InitializeComponent() 初始化界面组件
3. 然后调用 InitializeTimer() 初始化定时器
4. 接着初始化备份服务 BackupService.Instance
5. 窗体加载完成后触发 Window_Loaded 事件
6. 在 Window_Loaded 事件中调用 GetConfig() 方法
7. GetConfig() 方法中调用 DalSQLite.CheckDb() 检查数据库
8. 如果数据库检查通过，则调用 DalSQLite.GetVersion() 检查数据库版本
9. 最后初始化用户登录信息和加载球桌列表数据
这个初始化流程确保了应用程序启动时能够正确检查和初始化数据库，并加载必要的数据。

###数据库初始化主要调用路径
1. DalSQLite类中的CheckDb方法（DalSQLite.cs）
DalSQLite.cs 文件中的 CheckDb 方法是软件启动时检查数据库的主要入口，它调用了 BaseDbSQLiteClass ：
public static bool CheckDb()
{
    bool result = false;
    if (File.Exists(BaseDbSQLiteClass.DbFilePath) == true)//数据文件存在
    {
        if (GetSoftName() == DalDataConfig.SoftName)
        {
            result = true;
        }
        else
        {
            RepairDB();
        }
    }
    else//数据文件不存在
    {
        if ((System.Windows.MessageBox.Show("新的软件安装、是否创建新的数据文件？", "数据检测", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes))//
        {
            result = BaseDbSQLiteClass.CreateDB("my.sql");
        }
    }
    return result;
}

2. 数据库初始化调用（BaseDbSQLiteClass.cs）
当数据库文件不存在时，会调用 BaseDbSQLiteClass.CreateDB 方法创建数据库：
public static bool CreateDB(string sqlFileName)
{
    bool result = false;
    try
    {
        if (File.Exists(DbFilePath) == false)
        {
            SQLiteConnection.CreateFile(DbFileName);
            string sqlPath = Path.Combine(BaseDirClass.AppPath, sqlFileName);
            string sql = BaseFileClass.FileToString(sqlPath);
            if (ExecuteSql(sql) > 0)
            {
                System.Windows.MessageBox.Show("数据库创建成功！");
                result = true;
            }
            else
            {
                System.Windows.MessageBox.Show("数据库创建失败！");
            }
        }
        else
        {
            System.Windows.MessageBox.Show("数据库文件已存在！");
        }
    }
    catch (Exception ex)
    {
        System.Windows.MessageBox.Show(ex.ToString());
    }
    return result;
}

3. 数据库版本检查（DalSQLite.cs）
软件启动时还会检查数据库版本，通过 DalSQLite.GetVersion 方法：
public static bool GetVersion(ref string version)
{
    bool result = false;
    double ver = 0.0;
    try
    {
        object obj = BaseDbSQLiteClass.GetSingle("SELECT DbVersion FROM sys_config");
        if (obj != null)
        {
            if (double.TryParse(obj.ToString(), out ver) == true)
            {
                if (ver > 0)
                {
                    if (ver == DalDataConfig.DbVerion)
                    {
                        version = obj.ToString();
                        result = true;
                    }
                    else if (ver > DalDataConfig.DbVerion)
                    {
                        // 处理版本高于软件要求的情况
                    }
                    else
                    {
                        // 处理需要升级数据库的情况
                        if (MessageBox.Show("当前数据库版本低于软件要求版本，是否升级数据库结构？", "数据库异常", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            if (BaseDbSQLiteClass.UpdateDB(obj.ToString()) == true)
                            {
                                System.Environment.Exit(0);
                            }
                        }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.ToString());
    }
    return result;
}

4. 数据库修复（DalSQLite.cs）
private static void RepairDB()
{
    try
    {
        if (MessageBox.Show("删除现有数据库并新建数据结构吗？", "数据库异常提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            if (File.Exists(BaseDbSQLiteClass.DbFilePath) == true)
            {
                File.Delete(BaseDbSQLiteClass.DbFilePath);
            }
            BaseDbSQLiteClass.CreateDB("my.sql");
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.ToString());
    }
}

## 初始化流程总结
1. 应用程序启动时，调用 DalSQLite.CheckDb() 检查数据库状态
2. 如果数据库文件不存在，提示用户是否创建新数据库
3. 用户确认后，调用 BaseDbSQLiteClass.CreateDB("my.sql") 创建数据库
4. 创建数据库时，会读取 my.sql 文件中的SQL脚本，执行初始化表结构和数据
5. 如果数据库已存在，会检查软件名和版本号，必要时进行修复或升级
这个初始化流程确保了软件第一次启动时能够正确创建和初始化数据库，为应用程序的正常运行提供数据支持。


