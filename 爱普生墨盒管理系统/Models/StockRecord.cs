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
    /// 墨盒出入库记录
    /// </summary>
    public class StockRecord : INotifyPropertyChanged
    {
        private int _id;
        private int _cartridgeId;
        private Cartridge _cartridge;
        private int _operationType;
        private int _quantity;
        private DateTime _operationTime;
        private string _operator;
        private string _project;
        private string _notes;
        
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
        /// 墨盒ID
        /// </summary>
        public int CartridgeId 
        { 
            get => _cartridgeId;
            set
            {
                if (_cartridgeId != value)
                {
                    _cartridgeId = value;
                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// 墨盒信息
        /// </summary>
        public Cartridge Cartridge 
        { 
            get => _cartridge;
            set
            {
                if (_cartridge != value)
                {
                    _cartridge = value;
                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// 操作类型 (1=入库, 2=出库)
        /// </summary>
        public int OperationType 
        { 
            get => _operationType;
            set
            {
                if (_operationType != value)
                {
                    _operationType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(OperationTypeText));
                    OnPropertyChanged(nameof(Type));
                }
            }
        }
        
        /// <summary>
        /// 操作类型文本
        /// </summary>
        public string OperationTypeText => OperationType == 1 ? "入库" : "出库";
        
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
        /// 相关项目
        /// </summary>
        public string Project 
        { 
            get => _project;
            set
            {
                if (_project != value)
                {
                    _project = value;
                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// 备注
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
        /// 操作类型描述
        /// </summary>
        public string Type => OperationType == 1 ? "入库" : "出库";
        
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