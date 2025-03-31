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
    /// 操作记录实体类 - 用于仪表盘展示
    /// </summary>
    public class Operation : INotifyPropertyChanged
    {
        private int _id;
        private string _cartridgeInfo;
        private string _operationType;
        private int _quantity;
        private DateTime _operationTime;
        private string _operator;
        
        /// <summary>
        /// 记录ID
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
        /// 墨盒信息（型号和颜色）
        /// </summary>
        public string CartridgeInfo 
        { 
            get => _cartridgeInfo;
            set
            {
                if (_cartridgeInfo != value)
                {
                    _cartridgeInfo = value;
                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// 操作类型文本描述
        /// </summary>
        public string OperationType 
        { 
            get => _operationType;
            set
            {
                if (_operationType != value)
                {
                    _operationType = value;
                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity 
        { 
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperationTime 
        { 
            get => _operationTime;
            set
            {
                if (_operationTime != value)
                {
                    _operationTime = value;
                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// 操作人
        /// </summary>
        public string Operator 
        { 
            get => _operator;
            set
            {
                if (_operator != value)
                {
                    _operator = value;
                    OnPropertyChanged();
                }
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