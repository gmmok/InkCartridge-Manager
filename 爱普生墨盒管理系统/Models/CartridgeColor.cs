using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace 爱普生墨盒管理系统.Models
{
    /// <summary>
    /// 墨盒颜色实体类
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
        public Brush GetBrush()
        {
            try
            {
                if (!string.IsNullOrEmpty(ColorCode))
                {
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom(ColorCode));
                }
            }
            catch { }

            // 默认颜色
            return Brushes.Gray;
        }
    }
} 