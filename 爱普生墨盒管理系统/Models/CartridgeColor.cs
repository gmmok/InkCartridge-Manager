using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace 爱普生墨盒管理系统.Models
{
    /// <summary>
    /// 墨盒颜色实体类
    /// </summary>
    public class CartridgeColor : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _colorCode;
        private int _displayOrder;
        
        /// <summary>
        /// 颜色ID
        /// </summary>
        public int Id 
        { 
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 颜色名称
        /// </summary>
        public string Name 
        { 
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 颜色代码（十六进制）
        /// </summary>
        public string ColorCode 
        { 
            get => _colorCode;
            set
            {
                if (_colorCode != value)
                {
                    _colorCode = value;
                    OnPropertyChanged();
                    // GetBrush依赖于ColorCode，所以也需要通知
                    OnPropertyChanged(nameof(GetBrush));
                }
            }
        }

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int DisplayOrder 
        { 
            get => _displayOrder;
            set
            {
                if (_displayOrder != value)
                {
                    _displayOrder = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 获取颜色的Brush对象，用于图表显示
        /// </summary>
        public Brush GetBrush()
        {
            try
            {
                if (!string.IsNullOrEmpty(ColorCode))
                {
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom(ColorCode));
                }
            }
            catch (Exception ex) 
            {
                // 记录异常信息便于调试
                Console.WriteLine($"转换颜色代码'{ColorCode}'时发生异常: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }

            // 默认颜色
            return Brushes.Gray;
        }
        
        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 