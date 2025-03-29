using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 爱普生墨盒管理系统.Models
{
    /// <summary>
    /// 墨盒实体类
    /// </summary>
    public class Cartridge
    {
        /// <summary>
        /// 墨盒ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 墨盒颜色
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// 墨盒型号
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 墨盒容量（单位：ml）
        /// </summary>
        public decimal Capacity { get; set; }

        /// <summary>
        /// 当前库存
        /// </summary>
        public int CurrentStock { get; set; }

        /// <summary>
        /// 最低库存警戒线
        /// </summary>
        public int MinimumStock { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

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
                else if (CurrentStock < MinimumStock)
                    return "库存不足";
                else
                    return "库存正常";
            }
        }
    }
} 