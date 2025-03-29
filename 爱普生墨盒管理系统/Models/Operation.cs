using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 爱普生墨盒管理系统.Models
{
    /// <summary>
    /// 操作记录实体类 - 用于仪表盘展示
    /// </summary>
    public class Operation
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 墨盒信息（型号和颜色）
        /// </summary>
        public string CartridgeInfo { get; set; }
        
        /// <summary>
        /// 操作类型文本描述
        /// </summary>
        public string OperationType { get; set; }
        
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
    }
} 