using System;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using System.Windows.Threading;

namespace OpenBCMS
{
    
     
    
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// 2021-06-01 完善更新数据结构相关代码。
    /// </summary>
    public partial class MainWindow : MetroWindow
    {


        private DispatcherTimer timer;
        private DateTime? currentBillStartTime;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
            // 强制初始化备份服务
            _ = BackupService.Instance; // 此行确保 BackupService 被创建
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
            UpdateTimeDisplay(); // 立即更新一次显示
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTimeDisplay();
            UpdateUsedTime(); // 更新已使用时长
        }

        private void UpdateTimeDisplay()
        {
            var now = DateTime.Now;
            string[] weekDays = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
            timeDisplay.Text = $"{now:yyyy年MM月dd日} {weekDays[(int)now.DayOfWeek]} {now:HH:mm:ss}";
        }

        private void UpdateUsedTime()
        {
            if (currentBillStartTime.HasValue)
            {
                TimeSpan usedTime = DateTime.Now - currentBillStartTime.Value;
                string displayText;

                if (usedTime.TotalHours >= 1)
                {
                    int hours = (int)usedTime.TotalHours;
                    int minutes = usedTime.Minutes;
                    if (minutes > 0)
                    {
                        displayText = $"{hours}小时{minutes}分钟";
                    }
                    else
                    {
                        displayText = $"{hours}小时";
                    }
                }
                else
                {
                    displayText = $"{usedTime.Minutes}分钟";
                }

                UsedTimeDisplay.Text = displayText;
            }
            else
            {
                UsedTimeDisplay.Text = string.Empty;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }
            // 释放备份服务资源（如果需要）
            if (BackupService.Instance != null)
            {
                BackupService.Instance.Dispose();
            }
            base.OnClosed(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetConfig();
        }

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

        private void MiManagePwd_Click(object sender, RoutedEventArgs e)
        {
            BaseWindowClass.OpenActionWindow(new WindowChangePassword());
        }

        private void MiDataBackup_Click(object sender, RoutedEventArgs e)
        {
            BaseWindowClass.OpenActionWindow(new WindowDataBackup());
        }

        private void MiDataRestore_Click(object sender, RoutedEventArgs e)
        {
            BaseWindowClass.OpenActionWindow(new WindowDataRestore());
        }

        private void MiExit_Click(object sender, RoutedEventArgs e)
        {
            AppExit();
        }

        private void AppExit()
        {
            if (System.Windows.MessageBox.Show("确定退出程序吗?", "退出", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                System.Environment.Exit(0);
            }
        }

        private void MiBillManage_Click(object sender, RoutedEventArgs e)
        {
            BaseWindowClass.OpenActionWindow(new ContentBillManage()); 
            lvHouse.ItemsSource = DalHouse.GetViewList(null);
        }

        private void MiMemberManage_Click(object sender, RoutedEventArgs e)
        {
            BaseWindowClass.OpenActionWindow(new ContentMemberManage());
        }

        private void MiRechargeManage_Click(object sender, RoutedEventArgs e)
        {
            BaseWindowClass.OpenActionWindow(new ContentRechargeManage());
        }
    

        private void MiHouseManage_Click(object sender, RoutedEventArgs e)
        {
            BaseWindowClass.OpenActionWindow(new ContentHouseManage()); 
            lvHouse.ItemsSource = DalHouse.GetViewList(null);
        }

        private void MiHouseTypeManage_Click(object sender, RoutedEventArgs e)
        {
            BaseWindowClass.OpenActionWindow(new ContentHouseTypeManage());
        }

        private void MiHelpFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("help.doc");
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void MiAboutBox_Click(object sender, RoutedEventArgs e)
        {
            new WindowAbout().ShowDialog();
        }

        private void MiConsult_Click(object sender, RoutedEventArgs e)
        {
            new WindowConsult().ShowDialog();
        }

        private void btnHouse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ObjHouse house)
            {
                if (house.IsUse)
                {
                    var bill = DalBill.GetObject(house.HouseGUID);
                    if (bill != null)
                    {
                        this.DataContext = new { 
                            CurrentBill = bill,
                            CurrentHouse = house
                        };
                        currentBillStartTime = bill.TimeStart;
                        UpdateUsedTime(); // 立即更新一次显示
                    }
                }
                else
                {
                    this.DataContext = null;
                    currentBillStartTime = null;
                    UpdateUsedTime(); // 清除显示
                }
            }
        }

        private void MenuItemSettle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu menu && 
                menu.PlacementTarget is Button button && 
                button.Tag is ObjHouse house)
            {
                CompleteBill(house.HouseGUID);
                lvHouse.ItemsSource = DalHouse.GetViewList(null);
                DataContext = null; // 清空右侧信息
            }
        }

        void CompleteBill(Guid houseGUID)
        {
            try
            {
                // 先检查是否能获取到消费单
                ObjBill bill = DalBill.GetObject(houseGUID);
                if (bill == null)
                {
                    MessageBox.Show("未找到相关消费单信息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (MessageBox.Show("已有消费单使用此球桌，是否结束当前球桌消费单？", "消费单结束提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    WindowBill child = new WindowBill();
                    child.Bill = bill;
                    child.Bill.TimeEnd = DateTime.Now;
                    child.IsAdd = false;
                    child.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"结算时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Hyperlink_Click_1(object sender, RoutedEventArgs e)
        {
            new WindowMember().ShowDialog();
        }

        private void Hyperlink_Click_2(object sender, RoutedEventArgs e)
        {
            new WindowRecharge().ShowDialog();
        }

        private void BtnKaidan_Click(object sender, RoutedEventArgs e)
        {
            WindowBill child = new WindowBill();
            child.ShowDialog();
            // 刷新球桌状态
            lvHouse.ItemsSource = DalHouse.GetViewList(null);
        }

        private void BtnHuiyuan_Click(object sender, RoutedEventArgs e)
        {
            MiMemberManage_Click(sender, e);
        }

        private void BtnChongzhi_Click(object sender, RoutedEventArgs e)
        {
            MiRechargeManage_Click(sender, e);
        }

        private void BtnXiaofei_Click(object sender, RoutedEventArgs e)
        {
            MiBillManage_Click(sender, e);
        }

        private void BtnGuanli_Click(object sender, RoutedEventArgs e)
        {
            MiHouseManage_Click(sender, e);
        }

        private void BtnLeibie_Click(object sender, RoutedEventArgs e)
        {
            MiHouseTypeManage_Click(sender, e);
        }

        private void BtnZhuzhuangtu_Click(object sender, RoutedEventArgs e)
        {
            BaseWindowClass.OpenActionWindow(new ContentIncomeView());
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            MiExit_Click(sender, e);
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu menu && 
                menu.PlacementTarget is Button button && 
                button.Tag is ObjHouse house)
            {
                WindowBill child = new WindowBill();
                child.Bill.HouseGUID = house.HouseGUID;
                child.Bill.HousePrice = house.HousePrice;
                child.ShowDialog();
                lvHouse.ItemsSource = DalHouse.GetViewList(null);
            }
        }

        private void MenuItemTimedBilling_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu menu && 
                menu.PlacementTarget is Button button && 
                button.Tag is ObjHouse house)
            {
                var timedBillingWindow = new Bill.Timedbilling();
                timedBillingWindow.ShowDialog();
                
                // 如果需要刷新球桌状态
                lvHouse.ItemsSource = DalHouse.GetViewList(null);
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                // 获取屏幕工作区域大小
                double screenWidth = SystemParameters.WorkArea.Width;
                double screenHeight = SystemParameters.WorkArea.Height;

                // 计算窗口应该在的位置以保持居中
                double left = (screenWidth - this.Width) / 2;
                double top = (screenHeight - this.Height) / 2;

                // 设置窗口位置
                this.Left = left;
                this.Top = top;
            }
        }
    }
}
