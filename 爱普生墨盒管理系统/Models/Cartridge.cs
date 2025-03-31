using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace 爱普生墨盒管理系统.Models
{
    /// <summary>
    /// 墨盒实体类
    /// </summary>
    public class Cartridge : INotifyPropertyChanged
    {
        private int _id;
        private string _color;
        private string _model;
        private decimal _capacity;
        private int _currentStock;
        private int _minimumStock;
        private string _notes;
        private DateTime _updateTime;

        /// <summary>
        /// 墨盒ID
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
        /// 墨盒颜色
        /// </summary>
        public string Color 
        { 
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// 墨盒型号
        /// </summary>
        public string Model 
        { 
            get => _model;
            set
            {
                if (_model != value)
                {
                    _model = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// 墨盒容量（单位：ml）
        /// </summary>
        public decimal Capacity 
        { 
            get => _capacity;
            set
            {
                if (_capacity != value)
                {
                    _capacity = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 当前库存
        /// </summary>
        public int CurrentStock 
        { 
            get => _currentStock;
            set
            {
                if (_currentStock != value)
                {
                    _currentStock = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StockStatus));
                }
            }
        }

        /// <summary>
        /// 最低库存警戒线
        /// </summary>
        public int MinimumStock 
        { 
            get => _minimumStock;
            set
            {
                if (_minimumStock != value)
                {
                    _minimumStock = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StockStatus));
                }
            }
        }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Notes 
        { 
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime 
        { 
            get => _updateTime;
            set
            {
                if (_updateTime != value)
                {
                    _updateTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName => $"{Color} {Model}";

        /// <summary>
        /// 库存状态
        /// </summary>
        public string StockStatus
        {
            get
            {
                if (CurrentStock <= 0)
                    return "无库存";
                else if (MinimumStock > 0 && CurrentStock <= MinimumStock)
                    return "库存不足";
                else
                    return "库存正常";
            }
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