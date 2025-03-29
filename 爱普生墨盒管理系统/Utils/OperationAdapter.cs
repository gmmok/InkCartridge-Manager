using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 爱普生墨盒管理系统.Models;

namespace 爱普生墨盒管理系统.Utils
{
    /// <summary>
    /// 操作记录适配器 - 用于将StockRecord转换为Operation
    /// </summary>
    public static class OperationAdapter
    {
        /// <summary>
        /// 获取最近的操作记录
        /// </summary>
        /// <param name="limit">返回的记录数量限制</param>
        /// <returns>操作记录列表</returns>
        public static List<Operation> GetRecentOperations(int limit)
        {
            List<StockRecord> records = DatabaseHelper.GetStockRecords(limit);
            List<Operation> operations = new List<Operation>();
            
            foreach (var record in records)
            {
                operations.Add(new Operation
                {
                    Id = record.Id,
                    CartridgeInfo = $"{record.Cartridge.Color} {record.Cartridge.Model}",
                    OperationType = record.OperationTypeText,
                    Quantity = record.Quantity,
                    OperationTime = record.OperationTime,
                    Operator = record.Operator
                });
            }
            
            return operations;
        }
    }
} 