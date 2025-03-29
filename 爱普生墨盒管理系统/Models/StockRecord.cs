using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 爱普生墨盒管理系统.Models
{
    /// <summary>
    /// 墨盒出入库记录
    /// </summary>
    public class StockRecord
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 墨盒ID
        /// </summary>
        public int CartridgeId { get; set; }
        
        /// <summary>
        /// 墨盒信息
        /// </summary>
        public Cartridge Cartridge { get; set; }
        
        /// <summary>
        /// 操作类型 (1=入库, 2=出库)
        /// </summary>
        public int OperationType { get; set; }
        
        /// <summary>
        /// 操作类型文本
        /// </summary>
        public string OperationTypeText => OperationType == 1 ? "入库" : "出库";
        
        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }
        
        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperationTime { get; set; }
        
        /// <summary>
        /// 操作人
        /// </summary>
        public string Operator { get; set; }
        
        /// <summary>
        /// 相关项目
        /// </summary>
        public string Project { get; set; }
        
        /// <summary>
        /// 备注
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// 操作类型描述
        /// </summary>
        public string Type => OperationType == 1 ? "入库" : "出库";
    }
} 